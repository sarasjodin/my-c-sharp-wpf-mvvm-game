using SUP.Models.Enums;
using SUP.ViewModels;

namespace SUP.Services.Monsters;

internal class PancakeMonsterService : IMonsterService
{
    private readonly Random _random = new Random();

    public bool ShouldMonsterAppear()
    {
        return _random.Next(0, 100) < 10;
    }

    public int RandomCell(IList<CellViewModel> cells)
    {
        int randomCell = _random.Next(0, cells.Count);

        if (cells[randomCell].CellState != CellState.Empty || cells[randomCell].HasPancakeMonster)
        {
            return RandomCell(cells);
        }

        return randomCell;
    }

    public bool AddMonster(IList<CellViewModel> cells)
    {
        bool willAppear = ShouldMonsterAppear();
        if (!willAppear)
            return false;

        int randomCellNumber = RandomCell(cells);
        cells[randomCellNumber].HasPancakeMonster = true;
        return true;
    }

}

