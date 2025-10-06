namespace SUP.Models;
public record LeaderboardRow(int Rank, string Nickname, decimal TotalPoints)
{
    public bool IsCurrentPlayer { get; set; }
}

public record SeriesResult(
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string PlayerX,
    string PlayerO,
    int WinsX,
    int WinsO,
    int RoundsPlayed,
    int TotalRounds
);