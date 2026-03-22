using NarrativeEngine.Common;

namespace NarrativeEngine.Tests.Unit.Common;

public sealed class TestClock(DateTimeOffset? initialTime = null) : IClock
{
    private DateTimeOffset _current = initialTime ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public DateTime UtcNow => _current.UtcDateTime;
    public DateTimeOffset UtcNowOffset => _current;

    public void Advance(TimeSpan duration) => _current += duration;
    public void SetTime(DateTimeOffset time) => _current = time;
}
