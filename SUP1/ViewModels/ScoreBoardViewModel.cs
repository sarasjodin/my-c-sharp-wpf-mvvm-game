using PropertyChanged;
using SUP.Commands;
using SUP.Models;
using SUP.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static SUP.Services.NavigationService;

namespace SUP.ViewModels;
[AddINotifyPropertyChangedInterface]
public class ScoreboardViewModel
{
    private readonly IDbService _dbService;
    private readonly INavigationService _nav;
    private readonly GameState _state;

    private bool _isBusy;
    private CancellationTokenSource? _refreshCts;

    public string Title { get; } = "Scoreboard";
    public string StatusMessage { get; private set; } = "";
    public void UpdateStatus(string message) => StatusMessage = message;

    public ObservableCollection<LeaderboardRow> Leaderboard { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand CancelRefreshCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StartNewGameCommand { get; }
    public ICommand ShowStartCommand { get; }
    public ICommand RestartCommand { get; }

    public ScoreboardViewModel(INavigationService nav, IDbService dbService, GameState state)
    {
        _dbService = dbService;
        _nav = nav;
        _state = state;

        // Navigation
        ShowStartCommand = new NavigateCommand<StartViewModel>(_nav);
        StartNewGameCommand = new NavigateCommand<BoardViewModel>(_nav, vm =>
        {
            vm.PlayerXNickname = GameState.DefaultX;
            vm.PlayerONickname = GameState.DefaultO;
            _ = vm.StartNewGameAsync();
        });
        RestartCommand = new NavigateCommand<BoardViewModel>(_nav, vm => _ = vm.StartNewGameAsync());

        // TODO Förmodligen räcker en knapp som visar "Uppdatera" när inaktiv och "Avbryt" när pågår...
        // TODO Säkerställ att endast EN refresh kan köras (guard med _isBusy)
        // TODO Verifiera CancellationTokenSource för RefreshAsync och passera token till DB-anrop
        // TODO Skilj på avbrutet (OperationCanceledException) och "riktiga fel" Visa olika meddelanden???
        // Ladda/avbryt databaslista
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !_isBusy);
        CancelRefreshCommand = new RelayCommand(_ => CancelRefresh(), _ => _isBusy);
    }

    public async Task RefreshAsync()
    {
        // avbryt ev. pågående tidigare lörning
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();

        _refreshCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // valfri timeout
        var token = _refreshCts.Token;

        _isBusy = true;
        CommandManager.InvalidateRequerySuggested();
        StatusMessage = "Laddar…";

        try
        {
            var rows = (await _dbService.GetLeaderboardTopAsync(token)
                   ?? Array.Empty<LeaderboardRow>())
                  .ToList();

            MarkCurrentPlayersIfPresent(rows);

            // Tillbaka till UI-tråden och uppdatera listan
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Leaderboard.Clear(); //UI listan töms
                foreach (var r in rows) Leaderboard.Add(r); // Uppdaterade rader läggs till inkl MarkCurrentPlayersIfPresent

                StatusMessage = Leaderboard.Count == 0
                    ? "Inga resultat ännu"
                    : $"Uppdaterad {DateTime.Now:t}";
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Uppdatering avbruten.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kunde ej uppd. listan. ({ex.Message})";
        }
        finally
        {
            _isBusy = false;
            CommandManager.InvalidateRequerySuggested();

            _refreshCts?.Dispose();
            _refreshCts = null;
        }
    }

    // tillfällig lista LeaderboardRow från DB m lokal variabel rows
    private void MarkCurrentPlayersIfPresent(IList<LeaderboardRow> rows)
    {
        var x = _state.PlayerXName;
        var o = _state.PlayerOName;

        foreach (var r in rows)
        {
            var n = r.Nickname;
            r.IsCurrentPlayer =
                !string.IsNullOrWhiteSpace(n) &&
                (string.Equals(n, x, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(n, o, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void CancelRefresh() => _refreshCts?.Cancel(); // Aktiveras vid navigering, restart, fönster stängs
}