using SUP.Services;
using SUP.ViewModels;
using System.Windows.Input;

public class GameViewModel
{
    private readonly IDbService _db;
    private readonly INavigationService _nav;
    private readonly GameState _state;

    public ICommand ShowStartCommand { get; }
    public ICommand ShowScoreboardCommand { get; }
    public ICommand ShowRulesCommand { get; }
    public ICommand StartNewGameCommand { get; }
    public ICommand RestartCommand { get; }

    public GameViewModel(IDbService db, INavigationService nav, GameState state)
    {
        _db = db;
        _nav = nav;
        _state = state;

        ShowStartCommand = new NavigationService.NavigateCommand<StartViewModel>(_nav);
        ShowRulesCommand = new NavigationService.NavigateCommand<RulesViewModel>(_nav);
        ShowScoreboardCommand = new NavigationService.NavigateCommand<ScoreboardViewModel>(_nav, vm => _ = vm.RefreshAsync());

        // Start New återställer namn vid start
        StartNewGameCommand = new NavigationService.NavigateCommand<BoardViewModel>(_nav, vm =>
        {
            _state.ResetNamesToDefaults();
            InitNames(vm, _state.PlayerXName, _state.PlayerOName);
            AttachOneShotGameEnd(vm);
            _ = vm.StartNewGameAsync();
        });

        // Restart behåller namn i state sedan startar
        RestartCommand = new NavigationService.NavigateCommand<BoardViewModel>(_nav, vm =>
        {
            InitNames(vm, _state.PlayerXName, _state.PlayerOName);
            AttachOneShotGameEnd(vm);
            _ = vm.StartNewGameAsync();
        });
    }

    private static void InitNames(BoardViewModel vm, string xName, string oName)
    {
        vm.PlayerXNickname = xName;
        vm.PlayerONickname = oName;
    }

    /// <summary>
    /// När serien är slut sparar vi och navigerar till EndView.
    /// </summary>
    private void AttachOneShotGameEnd(BoardViewModel vm)
    {
        Action<string>? handler = null;
        handler = async status =>
        {
            // koppla bort gamla
            vm.GameEnd -= handler;

            var series = vm.GetCompletedSeries();
            _ = await _db.SaveBestOfThreeAsync(series).ConfigureAwait(false);

            var px = vm.PlayerX.Nickname;
            var po = vm.PlayerO.Nickname;

            _nav.NavigateTo<EndViewModel>(end => end.Init(px, po, vm.WinsX, vm.WinsO, status));
        };

        vm.GameEnd += handler;
    }
}