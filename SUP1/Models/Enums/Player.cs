namespace SUP.Models.Enums;

public class Player : IPlayer
{
    private string _nickname = string.Empty;

    public long Id { get; private set; } // private från db
    public string Nickname
    {
        get => _nickname;
        set
        {
            var fallback = Symbol == CellState.X ? "Spelare X" : "Spelare O";
            _nickname = TextHelper.NormalizeText(value, fallback);
        }
    }

    public CellState Symbol { get; }

    public Player(CellState symbol, string nickname, long id = 0)
    {
        Symbol = symbol;
        Nickname = nickname; // via set
        Id = id;
    }
}