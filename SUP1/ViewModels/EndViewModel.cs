using PropertyChanged;
using SUP.Services;
using System.Windows.Input;

namespace SUP.ViewModels;
[AddINotifyPropertyChangedInterface]
public class EndViewModel
{
    private readonly INavigationService _nav;
    private string? _statusOverride; // sätts vid init från BVM o skriver över standardtexten
    private readonly GameState _state;

    public string Title { get; } = "Bäst-av-3 serieresultat";

    public int WinsX { get; private set; }
    public int WinsO { get; private set; }
    public string PlayerXName { get; private set; } = "Spelare X";
    public string PlayerOName { get; private set; } = "Spelare O";

    public bool ShowDraw => WinsX == WinsO;
    public bool ShowWin => !ShowDraw;

    public string? WinnerName => ShowDraw ? null : (WinsX > WinsO ? PlayerXName : PlayerOName);


    public string StatusMessage // returnerar _statusOverride eller standardtext
    {
        get => _statusOverride ?? (
            ShowDraw
                ? $"Oavgjort ({WinsX}-{WinsO})"
                : $"Vinst för {WinnerName} ({WinsX}-{WinsO})"
        );
        set => _statusOverride = value?.Trim();
    }

    public string DrawMessage => "Bra kämpat båda två!";
    public string WinMessage => ShowDraw ? "" : $"Grattis, {WinnerName}!";

    public ICommand StartNewGameCommand { get; }
    public ICommand ShowStartCommand { get; }
    public ICommand ShowScoreboardCommand { get; }
    public ICommand RestartCommand { get; }


    public EndViewModel(INavigationService nav, GameState state)
    {
        _nav = nav;
        _state = state; // startar med default namn från state

        ShowStartCommand = new NavigationService.NavigateCommand<StartViewModel>(_nav);
        StartNewGameCommand = new NavigationService.NavigateCommand<BoardViewModel>(_nav, vm =>
        {
            _state.ResetNamesToDefaults();
            vm.PlayerXNickname = _state.PlayerXName;
            vm.PlayerONickname = _state.PlayerOName;
            _ = vm.StartNewGameAsync();
        });

        // Restart behåller namn i state
        RestartCommand = new NavigationService.NavigateCommand<BoardViewModel>(_nav, vm =>
        {
            vm.PlayerXNickname = PlayerXName;
            vm.PlayerONickname = PlayerOName;
            _ = vm.StartNewGameAsync();
        });
        ShowScoreboardCommand = new NavigationService.NavigateCommand<ScoreboardViewModel>(_nav);
    }

    public void Init(string playerXName, string playerOName, int winsX, int winsO, string? statusOverride = null)
    {
        // sättrt namn från parameter
        PlayerXName = playerXName;
        PlayerOName = playerOName;

        _state.PlayerXName = PlayerXName;
        _state.PlayerOName = PlayerOName;

        WinsX = winsX;
        WinsO = winsO;

        _statusOverride = statusOverride;
    }
}