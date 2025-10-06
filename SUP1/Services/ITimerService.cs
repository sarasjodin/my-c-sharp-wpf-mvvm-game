namespace SUP.Services;

public interface ITimerService
{
    // kvar för att kunna anv sekunder
    event Action<int> SecondsLeft;

    // Nytt högupplös tid
    event Action<TimeSpan> TimeLeft;

    event Action Timeout;

    void Start(int seconds);           // kvar
    void Start(TimeSpan duration);
    void Stop();
}