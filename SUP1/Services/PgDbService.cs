using Npgsql;
using NpgsqlTypes;
using SUP.Models;

namespace SUP.Services;

public sealed class PgDbService(NpgsqlDataSource dataSource) : IDbService
{
    // https://www.npgsql.org/doc/api/Npgsql.NpgsqlDataSource.html
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private const string GameName = "TicTacPancakes";

    /// <summary>
    /// Det är privat asynkron funktion som öppnar en connection, anropar work (varpå resultatet kommer att returneras).
    /// Inte ärvbar i det här skedet. Refaktoreras till basklass om fler databastjänser läggs till.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// En generisk typ-parameter som kan vara vilken typ som helst (int, string, lista...).
    /// <param name="work"></param>
    /// Innehåller själva "pekaren"/delegaten till koden/affärslogiken som ska köras mot databasen.
    /// <param name="cToken"></param>
    /// Möjliggör "avbryt"
    /// <returns>Resultatet från databasen baserat på/beroende av typparametern T och innehåll i "work"
    /// avvaktar work(conn)
    /// </returns>
    private async Task<T> WithConnectionAsync<T>(Func<NpgsqlConnection, Task<T>> work, CancellationToken cToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cToken);
        return await work(conn);
    }

    /// <summary>
    /// Allt sker i EN databastransaktion. Dvs commit vid lyckat sparande, annars rollback.
    /// Metoden använder "Common Table Expression (CTE) WITH-frågor" och "ON CONFLICT" och sparar resultatet av en "bäst av tre"-serie i databasen
    /// endast om serien avslutats. "ON CONFLICT" ser till att den skapar alterantivt uppdaterar nödvändiga rader (spel, spelare, session, poäng) och returnerar sessionens id.
    /// </summary>
    /// <param name="seriesResult">
    /// Resultatet från serien (spelarnas namn, start-/sluttid, antal vinster, mm).
    /// Måste vara avslutad: EndedAt satt och WinsX + WinsO == 3.
    /// </param>
    /// <param name="cToken">
    /// En Cancellation Token används för att kunna avbryta de asynkrona databasanropen.
    /// Den skickas vidare till väntande asyncrona databasanrop:
    /// OpenConnectionAsync(inne i WithConnectionAsync) -> BeginTransactionAsync -> ExecuteScalarAsync -> CommitAsync
    /// Om ct triggas, kringgås hela metoden och en rollback utförs = inget sparas.
    /// </param>
    /// <returns>
    /// string? Sessionens id som text om sparandet genomfördes annars null
    /// </returns>
    /// <remarks>
    /// Serien poängsätts med både rondvinster (1p) och (om det finns en vinnare) matchvinst (5p)
    /// </remarks>

    public Task<string?> SaveBestOfThreeAsync(SeriesResult seriesResult, CancellationToken cToken = default)
    {
        // Series måste vara Ended
        if (!seriesResult.EndedAt.HasValue)
            return Task.FromResult<string?>(null);

        // Verifiera mot reglerna
        if (!(seriesResult.EndedAt.HasValue && seriesResult.RoundsPlayed >= seriesResult.TotalRounds))
            return Task.FromResult<string?>(null);

        // Rensa strängarna
        var px = TextHelper.NormalizeText(seriesResult.PlayerX);
        var po = TextHelper.NormalizeText(seriesResult.PlayerO);

        // Namn måste finnas och inte samma efter normalisering ovan
        if (string.IsNullOrEmpty(px) || string.IsNullOrEmpty(po) ||
        string.Equals(px, po, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<string?>(null);

        var winner = GetWinner(seriesResult);

        // WITH queries https://www.postgresql.org/docs/current/queries-with.html#QUERIES-WITH-SELECT
        // ON CONFLICT https://codedamn.com/news/sql/on-conflict-upsert-in-postgresql

        const string sql = @"
                            WITH ensured_game AS (
                                INSERT INTO game (name)
                                VALUES (@game_name)
                                ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
                                RETURNING game_id
                            ),
                            px AS (
                                INSERT INTO player (nickname)
                                VALUES (@player_x)
                                ON CONFLICT (nickname) DO UPDATE SET nickname = EXCLUDED.nickname
                                RETURNING player_id
                            ),
                            po AS (
                                INSERT INTO player (nickname)
                                VALUES (@player_o)
                                ON CONFLICT (nickname) DO UPDATE SET nickname = EXCLUDED.nickname
                                RETURNING player_id
                            ),
                            s AS (
                                INSERT INTO session (game_id, started_at, ended_at)
                                SELECT eg.game_id, @started_at, @ended_at FROM ensured_game eg
                                RETURNING session_id
                            ),
                            sp AS (
                                INSERT INTO session_participant (session_id, player_id)
                                SELECT s.session_id, px.player_id FROM s, px
                                UNION ALL
                                SELECT s.session_id, po.player_id FROM s, po
                                ON CONFLICT DO NOTHING
                            ),
                            st_round AS (
                                INSERT INTO score_type (code, name, unit)
                                VALUES ('ROUND_WIN','Rondvinst','pt')
                                ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name
                                RETURNING score_type_id
                            ),
                            gst_round AS (
                                INSERT INTO game_score_type (game_id, score_type_id, points_per_unit, is_primary)
                                SELECT eg.game_id, st_round.score_type_id, 1.00, false FROM ensured_game eg, st_round
                                ON CONFLICT (game_id, score_type_id) DO NOTHING
                            ),
                            st_series AS (
                                INSERT INTO score_type (code, name, unit)
                                VALUES ('SERIES_WIN','Matchvinst','pt')
                                ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name
                                RETURNING score_type_id
                            ),
                            gst_series AS (
                                INSERT INTO game_score_type (game_id, score_type_id, points_per_unit, is_primary)
                                SELECT eg.game_id, st_series.score_type_id, 5.00, true FROM ensured_game eg, st_series
                                ON CONFLICT (game_id, score_type_id) DO NOTHING
                            ),
                            round_src AS (
                                SELECT s.session_id, px.player_id AS player_id, st_round.score_type_id AS score_type_id, @wins_x::numeric AS value
                                FROM s, px, st_round
                                UNION ALL
                                SELECT s.session_id, po.player_id, st_round.score_type_id, @wins_o::numeric
                                FROM s, po, st_round
                            ),
                            round_dedup AS (
                                SELECT session_id, player_id, score_type_id, SUM(value) AS value
                                FROM round_src
                                GROUP BY session_id, player_id, score_type_id
                            ),
                            upsert_round AS (
                                INSERT INTO session_score (session_id, player_id, score_type_id, value)
                                SELECT session_id, player_id, score_type_id, value
                                FROM round_dedup
                                ON CONFLICT (session_id, player_id, score_type_id)
                                DO UPDATE SET value = EXCLUDED.value
                            )
                            -- Skriv series-vinst endast vid vinnare
                            , upsert_series AS (
                                INSERT INTO session_score (session_id, player_id, score_type_id, value)
                                SELECT s.session_id,
                                        CASE WHEN @winner = 'X' THEN px.player_id
                                            WHEN @winner = 'O' THEN po.player_id
                                        END,
                                        st_series.score_type_id,
                                        1
                                FROM s, px, po, st_series
                                WHERE @winner IN ('X','O')
                                ON CONFLICT (session_id, player_id, score_type_id)
                                DO UPDATE SET value = EXCLUDED.value
                            )
                            SELECT session_id FROM s;
                            ";
        return WithConnectionAsync<string?>(async conn =>
        {
            // BeginTransactionAsync https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection.begintransactionasync?view=net-9.0
            await using var tx = await conn.BeginTransactionAsync(cToken);
            try
            {
                await using var command = new NpgsqlCommand(sql, conn, tx);
                AddParam(command, "@game_name", GameName);
                AddParam(command, "@player_x", px); // Normaliserade ovan
                AddParam(command, "@player_o", po); // Normaliserade ovan
                AddParam(command, "@started_at", seriesResult.StartedAt.ToUniversalTime(), NpgsqlDbType.TimestampTz);
                AddParam(command, "@ended_at", seriesResult.EndedAt?.ToUniversalTime(), NpgsqlDbType.TimestampTz);
                AddParam(command, "@wins_x", seriesResult.WinsX);
                AddParam(command, "@wins_o", seriesResult.WinsO);
                AddParam(command, "@winner", winner);
                var idObj = await command.ExecuteScalarAsync(cToken);
                if (idObj is null or DBNull)
                {
                    await tx.RollbackAsync(cToken);
                    return null;
                }
                var id = Convert.ToInt64(idObj); // SQL bigint -> C# long -> omvandlat till string (returvärde).

                await tx.CommitAsync(cToken);
                return id.ToString();
            }
            catch
            {
                await tx.RollbackAsync(cToken);  // rollback vid fel/avbryt
                throw;
            }
        }, cToken);
    }

    /// <summary>
    /// Hämtar de 50 bästa spelarna från databasen baserat på deras totala poäng
    /// (rondvinster och matchvinster). Returnerar en lista med rank, namn och poäng
    /// sorterad i fallande ordning.
    /// </summary>
    /// <param name="cToken">
    /// Kan användas för att avbryta anropet innan eller under databasfrågan.
    /// </param>
    /// <returns>
    /// En lista med de högst rankade spelarna.
    /// </returns>
    public async Task<IReadOnlyList<LeaderboardRow>> GetLeaderboardTopAsync(CancellationToken cToken = default)
    {
        const string sql = @"
                            WITH totals AS (
                              SELECT
                                  p.player_id,
                                  p.nickname,
                                  SUM(ss.value * gst.points_per_unit)::numeric(10,2) AS total_points
                              FROM session_score ss
                              JOIN score_type st       ON st.score_type_id = ss.score_type_id
                              JOIN session s           ON s.session_id = ss.session_id
                              JOIN game g              ON g.game_id = s.game_id
                              JOIN game_score_type gst ON gst.game_id = g.game_id AND gst.score_type_id = st.score_type_id
                              JOIN player p            ON p.player_id = ss.player_id
                              WHERE st.code IN ('ROUND_WIN','SERIES_WIN')
                                AND s.ended_at IS NOT NULL
                                AND g.name = @game_name
                              GROUP BY p.player_id, p.nickname
                            )
                            SELECT
                              CAST(ROW_NUMBER() OVER (ORDER BY total_points DESC, nickname ASC) AS int) AS rank,
                              nickname,
                              total_points
                            FROM totals
                            ORDER BY rank
                            LIMIT 50;";

        return await WithConnectionAsync(async conn =>
        {
            var list = new List<LeaderboardRow>(); // tillfällig "db lista"
            await using var command = new NpgsqlCommand(sql, conn);
            AddParam(command, "@game_name", GameName);

            await using var r = await command.ExecuteReaderAsync(cToken);
            while (await r.ReadAsync(cToken))
            {
                var rank = r.GetInt32(0); // kolumn 0 (rank)
                var nick = r.GetString(1); // kolumn 1 (nickname)
                var points = r.GetDecimal(2); // kolumn 2 (total_points)
                list.Add(new Models.LeaderboardRow((int)rank, nick, points)); // Läggs in ett nytt LeaderboardRow-objekt ger ny lista
            }
            return (IReadOnlyList<LeaderboardRow>)list;
        }, cToken);
    }

    /// <summary>
    /// En hjälpmetod för att lägga till en parameter till ett NpgsqlCommand -som hanterar både null och DBNull
    /// </summary>
    private static void AddParam(NpgsqlCommand cmd, string name, object? value, NpgsqlTypes.NpgsqlDbType? type = null)
    {
        if (type is null) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        else cmd.Parameters.AddWithValue(name, type.Value, value ?? DBNull.Value);
    }

    /// <summary>
    /// /// Bestämmer vinnaren i en serie beroende på vinster
    /// </summary>
    /// <param name="result">Resultaten WinsX o WinsY</param>
    /// <returns>"X" om X har fler vinster "O" om O har fler vinster annars null vid oavgjort</returns>
    private static string? GetWinner(Models.SeriesResult result) =>
    result.WinsX == result.WinsO
        ? null
        : (result.WinsX > result.WinsO ? "X" : "O");
}