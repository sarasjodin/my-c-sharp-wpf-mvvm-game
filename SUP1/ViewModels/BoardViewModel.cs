using PropertyChanged;
using SUP.Commands;
using SUP.Models;
using SUP.Models.Enums;
using SUP.Services;
using SUP.Services.Eats;
using SUP.Services.Monsters;
using SUP.Services.SuperCell;
using SUP.ViewModels.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SUP.ViewModels;
[AddINotifyPropertyChangedInterface]

public class BoardViewModel : ISupportPadInput, IDisposable
{
    private readonly IDbService _db;
    private readonly INavigationService _nav;
    private readonly GameState _state;

    private readonly IMonsterService _monsterService;
    private readonly ISuperCellService _superCellService;
    private readonly IEatService _eatService;
    private readonly ITimerService _timer;

    private CellState _currentPlayer = CellState.X;
    private const int DefaultTimeSeconds = 10;
    public bool IsTurnTimeCritical { get; private set; }
    public string TurnTime { get; private set; } = "10:000";

    private DateTimeOffset _seriesStartedAt;
    private CellState _lastRoundWinner;

    public IPlayer PlayerX { get; }
    public IPlayer PlayerO { get; }

    public string PlayerXNickname
    {
        get => PlayerX.Nickname;
        set => PlayerX.Nickname = value;
    }

    public string PlayerONickname
    {
        get => PlayerO.Nickname;
        set => PlayerO.Nickname = value;
    }

    public ObservableCollection<CellViewModel> Cells { get; private set; } = new();

    // GLOBAL COMMANDS
    public ICommand StartNewGameCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand ShowStartCommand { get; private set; }

    // LOCAL COMMANDS
    public ICommand AddPlayersCommand { get; private set; }
    public ICommand PressPadIndexCommand { get; }
    public ICommand EatCommand { get; private set; }

    public bool CanUseEat { get; set; } = false;
    public bool ShowEatButton => RoundsPlayed >= 1;
    public bool BtnIsEnabled { get; set; }
    public string StatusMessage { get; set; } = "";

    public int WinsX { get; private set; }
    public int WinsO { get; private set; }
    public int RoundsPlayed { get; private set; }
    public int CurrentRound => RoundsPlayed + 1;
    public int CurrentRoundDisplay => Math.Min(RoundsPlayed + 1, TotalRounds);
    public int TotalRounds { get; } = 3;

    public event Action<string>? GameEnd;
    public event Action<string>? RequestCellClickedSound;
    public event Action? RequestWinSound;
    public event Action? RequestSuperCellSound;
    public event Action? RequestMonsterSound;

    public BoardViewModel(INavigationService nav, IDbService db, IMonsterService monsterService, ISuperCellService superCellService, IEatService eatService, ITimerService timer, GameState state)
    {
        _nav = nav;
        _db = db;
        _state = state; // starta med namnen från state
        _timer = timer;

        // Starta nytt spel återställer namn till default
        StartNewGameCommand = new NavigationService.NavigateCommand<BoardViewModel>(_nav, vm =>
        {
            _state.ResetNamesToDefaults();
            vm.PlayerXNickname = _state.PlayerXName; // "Spelare X"
            vm.PlayerONickname = _state.PlayerOName; // "Spelare O"
            _ = vm.StartNewGameAsync();
        });

        // Restart behåller nuvarande namn i state
        RestartCommand = new NavigationService.NavigateCommand<BoardViewModel>(_nav, vm =>
        {
            vm.PlayerXNickname = _state.PlayerXName;
            vm.PlayerONickname = _state.PlayerOName;
            _ = vm.StartNewGameAsync();
        });

        PlayerX = new Player(CellState.X, _state.PlayerXName);
        PlayerO = new Player(CellState.O, _state.PlayerOName);

        _timer.TimeLeft += OnTimeLeft;
        _timer.Timeout += OnTimeout;

        // Visa värde direkt vid start
        TurnTime = TimeSpan.FromSeconds(DefaultTimeSeconds).ToString(@"ss\:fff");
        IsTurnTimeCritical = false;

        _monsterService = monsterService;
        _superCellService = superCellService;
        _eatService = eatService;

        ConfigureCells();

        AddPlayersCommand = new RelayCommand(_ => AddPlayers());

        EatCommand = new RelayCommand(_ =>
        {
            _eatService.DoEat(Cells, _lastRoundWinner);
            CanUseEat = false;
        });
        CanUseEat = false;

        PressPadIndexCommand = new RelayCommand(async p =>
        {
            // Vår guard
            if (p is null) return;
            await OnCellClicked(Convert.ToInt32(p));
        });

        BtnIsEnabled = true;

        // spara dependenser, koppla timer-events mm
        ShowStartCommand = new RelayCommand(_ => _nav.NavigateTo<StartViewModel>());
    }

    private void ConfigureCells()
    {
        for (int i = 0; i < 9; i++)
        {
            string sfx = $"cell{i}";
            Cells.Add(new CellViewModel(i, OnCellClicked, sfx));
        }
    }

    private void AddPlayers()
    {
        // Redan normaliserade namn
        var x = PlayerX.Nickname;
        var o = PlayerO.Nickname;

        // X o O får inte vara samma namn
        if (!string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(o) &&
            string.Equals(x, o, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Namnen får inte vara samma.";
            return;
        }

        // Trigga UI - ersätt objekten
        PlayerX.Nickname = x;
        PlayerO.Nickname = o;

        _state.PlayerXName = x;
        _state.PlayerOName = o;

        UpdateMessageText();
    }

    private string GetCurrentPlayerName() =>
    _currentPlayer == CellState.X ? PlayerX.Nickname : PlayerO.Nickname;

    private void UpdateMessageText()
    {
        StatusMessage = $"{GetCurrentPlayerName()} börjar!";
    }

    public async Task StartNewGameAsync()
    {
        ResetGame();
        StartSeries(PlayerX.Nickname, PlayerO.Nickname);
    }

    private void SwitchPlayer()
    {
        _currentPlayer = _currentPlayer == CellState.X ? CellState.O : CellState.X;
    }

    private bool HasWinnerMarked() => Cells.Any(c => c.IsWinningRow);

    private async Task OnCellClicked(int index)
    {

        var cell = Cells[index];

        if (cell.HasPancakeMonster || cell.CellState != CellState.Empty || HasWinnerMarked())
            return;

        RequestCellClickedSound?.Invoke(Cells[index].SfxKey);

        cell.CellState = _currentPlayer;

        if (HasWin())
        {
            await HandleWinAsync();
            return;
        }

        if (IsBoardFull())
        {
            await HandleDrawAsync();
            return;
        }

        StopTimer();
        StartTimer();
        SwitchPlayer();
    }

    // SeriesResult skapas när serien är klar
    public SeriesResult GetCompletedSeries()
    {
        bool isSeriesOver = RoundsPlayed >= TotalRounds;

        var endedAt = isSeriesOver ? DateTimeOffset.Now : (DateTimeOffset?)null;

        return new SeriesResult(
            StartedAt: _seriesStartedAt,
            EndedAt: endedAt,
            PlayerX: PlayerX.Nickname,
            PlayerO: PlayerO.Nickname,
            WinsX: WinsX,
            WinsO: WinsO,
            RoundsPlayed: RoundsPlayed,
            TotalRounds: TotalRounds
        );
    }

    private async Task HandleWinAsync()
    {
        StopTimer();

        UpdatePoints();

        var winner = _currentPlayer;
        var roundWinnerName = GetCurrentPlayerName();

        StatusMessage = $"Vinst för {roundWinnerName}";
        RequestWinSound?.Invoke();
        BtnIsEnabled = false;

        // Ge power till vinnaren för NÄSTA rond
        _lastRoundWinner = winner;
        _eatService.GrantPower();
        CanUseEat = true;

        await Task.Delay(1500);
        OnRoundEnded(wasWin: true, winner: winner);
    }

    private async Task HandleDrawAsync()
    {
        StopTimer();
        StatusMessage = "Oavgjort";
        BtnIsEnabled = false;
        await Task.Delay(1500);
        OnRoundEnded(wasWin: false, winner: null);
    }


    private void UpdatePoints()
    {

        bool hasSuperCell = false;

        for (int i = 0; i < Cells.Count; i++) //kontroll om vinnande rad innehåller supercell.
        {
            if (Cells[i].IsWinningRow && Cells[i].IsSuperCell)
            {
                hasSuperCell = true;
                break;
            }
        }
        var points = hasSuperCell ? 2 : 1;

        if (_currentPlayer == CellState.X) WinsX += points;
        else WinsO += points;

    }

    // enum =
    // Empty, [=0]
    // X, [=1]
    // O [=2]
    private bool Same3Symbols(int a, int b, int c)
    {
        var s = Cells[a].CellState;
        return s != CellState.Empty &&
            Cells[b].CellState == s &&
            Cells[c].CellState == s;
    }

    private bool TryMarkWinningRow(int a, int b, int c)
    {
        if (!Same3Symbols(a, b, c)) return false;
        Cells[a].IsWinningRow = Cells[b].IsWinningRow = Cells[c].IsWinningRow = true;
        return true;
    }

    private bool HasWin()

    {// 3 raderna
        if (TryMarkWinningRow(0, 1, 2)) return true;
        if (TryMarkWinningRow(3, 4, 5)) return true;
        if (TryMarkWinningRow(6, 7, 8)) return true;
        // 3 klumner
        if (TryMarkWinningRow(0, 3, 6)) return true;
        if (TryMarkWinningRow(1, 4, 7)) return true;
        if (TryMarkWinningRow(2, 5, 8)) return true;
        // 2 diagonaler
        if (TryMarkWinningRow(0, 4, 8)) return true;
        if (TryMarkWinningRow(2, 4, 6)) return true;
        return false;
    }

    private bool IsBoardFull()
    {
        for (int i = 0; i < Cells.Count; i++)
        {
            if (Cells[i].CellState == CellState.Empty && !Cells[i].HasPancakeMonster)
            {
                return false;
            }
        }
        return true;
    }

    private void ResetBoard()
    {
        for (int i = 0; i < Cells.Count; i++)
        {
            Cells[i].CellState = CellState.Empty;
            Cells[i].IsWinningRow = false;
            Cells[i].HasPancakeMonster = false;
            Cells[i].IsSuperCell = false;
        }

        bool monsterAdded = _monsterService.AddMonster(Cells);
        if (monsterAdded) RequestMonsterSound?.Invoke();

        bool superCellAdded = _superCellService.TryAddSuperCell(Cells);
        if (superCellAdded) RequestSuperCellSound?.Invoke();

        _currentPlayer = CellState.X;
        BtnIsEnabled = true;
        UpdateMessageText();
    }

    private void ResetGame()
    {
        StopTimer();
        ResetBoard();
        WinsX = 0;
        WinsO = 0;
        RoundsPlayed = 0;
        StatusMessage = "";
        BtnIsEnabled = true;
    }

    private async Task TimeOutLoss()
    {
        // Poäng till motståndaren vid timeout
        var winner = _currentPlayer == CellState.X ? CellState.O : CellState.X;

        // Ge poängen till vinnaren
        if (winner == CellState.X) WinsX++;
        else WinsO++;

        // Nuvarande spelare är förloraren
        var loserName = GetCurrentPlayerName();
        StatusMessage = $"Tiden rann ut! Förlust för {loserName}!";

        BtnIsEnabled = false;

        await Task.Delay(1500);
        OnRoundEnded(wasWin: true, winner: winner);
    }

    public void StartSeries(string playerX, string playerO)
    {

        PlayerX.Nickname = playerX;
        PlayerO.Nickname = playerO;

        _seriesStartedAt = DateTimeOffset.Now;
        WinsX = WinsO = 0;
        RoundsPlayed = 0;
        StatusMessage = "";
        BtnIsEnabled = true;
    }

    private void OnRoundEnded(bool wasWin, CellState? winner)
    {
        RoundsPlayed++;

        bool seriesOver = RoundsPlayed >= TotalRounds;

        if (seriesOver)
        {
            var seriesWinner =
                WinsX == WinsO ? "Oavgjort"
              : (WinsX > WinsO ? PlayerX.Nickname : PlayerO.Nickname);

            StatusMessage = WinsX == WinsO
                ? $"Serien slut: Oavgjort ({WinsX}-{WinsO})"
                : $"Serien vinns av {seriesWinner} ({WinsX}-{WinsO})";

            GameEnd?.Invoke(StatusMessage);

            _ = FinalizeSeriesAndGoToEndAsync(StatusMessage);
            return;
        }

        ResetBoard();
        _currentPlayer = CellState.X;
        BtnIsEnabled = true;
        UpdateMessageText();
    }


    // TODO: try / catch
    private async Task FinalizeSeriesAndGoToEndAsync(string status)
    {
        var series = GetCompletedSeries();
        _ = await _db.SaveBestOfThreeAsync(series).ConfigureAwait(false);

        var px = PlayerX.Nickname;
        var po = PlayerO.Nickname;

        _nav.NavigateTo<EndViewModel>(end => end.Init(px, po, WinsX, WinsO, status));
    }

    public void Dispose()
    {
        _timer.TimeLeft -= OnTimeLeft;
        _timer.Timeout -= OnTimeout;
        _timer.Stop();
    }

    // Visar tid som ss:fff och blir "röd" sista 3 sek
    private void OnTimeLeft(TimeSpan ts)
    {
        TurnTime = ts.ToString(@"ss\:fff");
        IsTurnTimeCritical = ts <= TimeSpan.FromSeconds(3);
    }

    // Körs när timern slår noll
    private async void OnTimeout()
    {
        TurnTime = "00:000";
        IsTurnTimeCritical = true;
        BtnIsEnabled = false;

        await TimeOutLoss();
    }


    private void StartTimer() => _timer.Start(TimeSpan.FromSeconds(DefaultTimeSeconds));
    private void StopTimer() => _timer.Stop();

}
