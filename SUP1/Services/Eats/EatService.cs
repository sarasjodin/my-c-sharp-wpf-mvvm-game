using SUP.Models.Enums;
using SUP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SUP.Services.Eats;

public class EatService : IEatService                   //implementerat interface
{


    public bool HasPower { get; set; } = false;          //property, true/false                           (propert binder till vy)            




    public void GrantPower()                            //metod: kallas när spelaren vinner en runda          (1/3) och då kan Eat-knappen användas av spelaren
    {
        HasPower = true;                                      //Eatknappen kan användas av spelaren                 
    }







    public void DoEat(IList<CellViewModel> cells, CellState winner)                                         //tar in winner så att vet vem som ska ätas
    {
        if (!HasPower) 
            return;                                     //###sh gör inget om ej har kraft

        CellState opponentSymbol;                         //### variabel sparar opponent

        if (winner == CellState.X)
        {  
            opponentSymbol = CellState.O; 
        }
        else
        {
            opponentSymbol= CellState.X; 
        }



        List<CellViewModel> opponentCells = new List<CellViewModel>(); //lista för att spara opponent rutor

        for (int i = 0; i < cells.Count; i++)
        { 
            if (cells[i].CellState == opponentSymbol)           //om rutan har opposing symbol
            {
                opponentCells.Add(cells[i]);                    //add till listan
            }
        }
        if (opponentCells.Count == 0)                        //om inga opponentsymboler så gör inget
        {
            return;
        }

        Random random = new Random();   
        int randomIndex = random.Next(opponentCells.Count);       //ta en random opponent symbol

        opponentCells[randomIndex].CellState = CellState.Empty;    //ät/radera symbolen

        HasPower = false;                                         //kan endast användas en gång

    }



}
