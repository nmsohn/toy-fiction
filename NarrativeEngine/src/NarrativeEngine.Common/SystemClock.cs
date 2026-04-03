namespace NarrativeEngine.Common;

public class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
    public DateTimeOffset UtcNowOffset => timeProvider.GetUtcNow();
}