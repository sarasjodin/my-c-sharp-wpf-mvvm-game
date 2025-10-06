using PropertyChanged;
using SUP.Services;
using System.Windows.Input;

namespace SUP.ViewModels;

[AddINotifyPropertyChangedInterface]

public class StartViewModel
{
    private readonly INavigationService _nav;
    private readonly GameState _gameState;
    public ICommand StartCommand { get; private set; }
    public ICommand ShowScoreboardCommand { get; private set; }
    public ICommand ShowRulesCommand { get; private set; }

    public StartViewModel(INavigationService nav, GameState gameState)
    {
        _nav = nav;
        _gameState = gameState;
        StartCommand = new NavigationService.NavigateCommand<BoardViewModel>(_nav);
        ShowScoreboardCommand = new NavigationService.NavigateCommand<ScoreboardViewModel>(_nav);
        ShowRulesCommand = new NavigationService.NavigateCommand<RulesViewModel>(_nav);
    }
}
