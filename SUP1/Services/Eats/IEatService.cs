using SUP.Models.Enums;
using SUP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SUP.Services.Eats
{
    public interface IEatService                                             //###Sh Interface
    {
        void GrantPower();                                    //kallas när en spelare vinner en rond

        bool HasPower { get; set; }                  //prop som bestämmer om man har kraften eller ej               (styrs via property*

        void DoEat(IList<CellViewModel> cells, CellState currentPlayer);  //metod som äter
                                                                        //(skickar in hela brädet + vinnaren)      

    }
}
