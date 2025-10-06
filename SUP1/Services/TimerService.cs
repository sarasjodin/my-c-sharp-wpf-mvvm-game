using System.Windows.Threading;

namespace SUP.Services;

internal class TimerService : ITimerService, IDisposable
// https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
{
    // en WPF dispatcher timer: (https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatchertimer?view=windowsdesktop-9.0)
    // Eriks kodstuga: https://www.youtube.com/watch?v=nr9gWmKv7d0&list=PLVIpiAdAmhgTmMz7YvyqMBlQEaExb-Hgd&index=5
    // (Kodstuga 4 minut 48:00) 
    //+ tutorial "Dispatcher Timer in C# WPF: A Step-by-Step Tutorial!" https://www.youtube.com/watch?v=DwYgXNDWNn8

    private readonly DispatcherTimer _timer; // https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher?view=windowsdesktop-9.0
    private DateTime _endUtc;
    private TimeSpan _duration;

    public event Action<int>? SecondsLeft;
    public event Action<TimeSpan>? TimeLeft;
    public event Action? Timeout;

    public TimerService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            // 10 ms för "ss:fff", byter till 50 ms för "ss:ff"
            Interval = TimeSpan.FromMilliseconds(10)
        };
        _timer.Tick += OnTick;
    }

    public void Start(int seconds) => Start(TimeSpan.FromSeconds(seconds));
    public void Start(TimeSpan duration)
    {
        _duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        // Startvärde direkt, UI visar "10:00"/"10:000"
        TimeLeft?.Invoke(_duration);
        SecondsLeft?.Invoke((int)Math.Ceiling(_duration.TotalSeconds));

        if (_duration == TimeSpan.Zero)
        {
            Timeout?.Invoke();
            return;
        }

        _endUtc = DateTime.UtcNow + _duration;
        _timer.Start();
    }

    private void OnTick(object? s, EventArgs e)
    {
        var remaining = _endUtc - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            _timer.Stop();
            TimeLeft?.Invoke(TimeSpan.Zero);
            SecondsLeft?.Invoke(0);
            Timeout?.Invoke();
            return;
        }

        TimeLeft?.Invoke(remaining);
        SecondsLeft?.Invoke((int)Math.Ceiling(remaining.TotalSeconds));
    }

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Stop();
}



