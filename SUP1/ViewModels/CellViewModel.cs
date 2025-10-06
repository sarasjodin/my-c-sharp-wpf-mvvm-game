using PropertyChanged;
using SUP.Commands;
using SUP.Models.Enums;
using System.Windows.Input;

namespace SUP.ViewModels;

[AddINotifyPropertyChangedInterface]

public class CellViewModel
{
    public int Id { get; private set; }

    public CellState CellState { get; set; } = CellState.Empty;
    public bool IsWinningRow { get; set; }
    public bool IsSuperCell { get; set; }
    public bool HasPancakeMonster { get; set; }
    public string SfxKey { get; private set; }

    public ICommand ClickCommand { get; private set; }


    public CellViewModel(int id, Func<int, Task> onClickAsync, string sfxKey)
    {
        Id = id;
        SfxKey = sfxKey;
        ClickCommand = new RelayCommand(_ => onClickAsync(Id));
    }
}

