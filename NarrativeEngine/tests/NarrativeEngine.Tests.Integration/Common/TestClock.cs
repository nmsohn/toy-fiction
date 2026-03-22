using NarrativeEngine.Common;

namespace NarrativeEngine.Tests.Integration.Common;

public sealed class TestClock : IClock
{
    private DateTimeOffset _current;

    public TestClock(DateTimeOffset? initialTime = null)
    {
        _current = initialTime ?? DateTimeOffset.UtcNow;
    }

    public DateTime UtcNow => _current.UtcDateTime;
    public DateTimeOffset UtcNowOffset => _current;

    public void Advance(TimeSpan duration) => _current += duration;
    public void SetTime(DateTimeOffset time) => _current = time;

}