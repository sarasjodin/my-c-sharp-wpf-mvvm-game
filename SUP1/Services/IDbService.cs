using SUP.Models;

namespace SUP.Services
{
    public interface IDbService
    {
        // Spara matchresultat hela listan, returnerar session_id
        Task<string?> SaveBestOfThreeAsync(SeriesResult seriesResult, CancellationToken cToken = default);

        // Läser hela listan
        Task<IReadOnlyList<LeaderboardRow>> GetLeaderboardTopAsync(CancellationToken ct = default);
    }
}