using SUP.Models.Enums;

namespace SUP.Models;

public interface IPlayer
{
    long Id { get; }
    string Nickname { get; set; }
    CellState Symbol { get; }
}