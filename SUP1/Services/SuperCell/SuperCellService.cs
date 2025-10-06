using SUP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using SUP.Models.Enums;

namespace SUP.Services.SuperCell;

public class SuperCellService : ISuperCellService
{
    private readonly Random _random = new Random();
    private int _superCellChancePercent = 15;

    public bool ShouldTriggerSuperCell()
    {
        return _random.Next(0, 100) < _superCellChancePercent;
    }
    public int GetRandomAvailableCell(IList<CellViewModel> cells)
    {
        var availableCells = new List<int>();

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].CellState == CellState.Empty && !cells[i].HasPancakeMonster)
            {
                availableCells.Add(i);
            } 
        }

        int randomCellIndex = _random.Next(availableCells.Count);
        return availableCells[randomCellIndex];
    }

    public bool TryAddSuperCell (IList<CellViewModel> cells)
    {
        if(!ShouldTriggerSuperCell()) 
            return false;

        int randomCellNumber = GetRandomAvailableCell(cells);
        cells[randomCellNumber].IsSuperCell = true;
        return true;
    }

  
}
