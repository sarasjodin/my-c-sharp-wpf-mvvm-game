using PropertyChanged;
using SUP.Commands;
using SUP.Services;
using System.Windows.Input;

namespace SUP.ViewModels;
[AddINotifyPropertyChangedInterface]
public class RulesViewModel
{
    private readonly INavigationService _nav;

    public ICommand ShowStartCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand StartNewGameCommand { get; }
    public string Title => "Spelregler";

    public RulesViewModel(INavigationService nav)
    {
        _nav = nav;
        ShowStartCommand = new RelayCommand(_ => _nav.NavigateTo<StartViewModel>());
        StartNewGameCommand = new RelayCommand(_ =>
            _nav.NavigateTo<BoardViewModel>(vm => _ = vm.StartNewGameAsync()));
        RestartCommand = StartNewGameCommand;
    }
}
