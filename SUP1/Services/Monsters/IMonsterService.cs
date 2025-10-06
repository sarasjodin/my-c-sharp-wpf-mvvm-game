using SUP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SUP.Services.Monsters;

    public interface IMonsterService
    {
    bool ShouldMonsterAppear();

    int RandomCell(IList<CellViewModel> cells);
    bool AddMonster(IList<CellViewModel> cells);
    }

