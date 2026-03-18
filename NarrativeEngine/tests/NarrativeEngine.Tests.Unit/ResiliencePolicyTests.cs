using Xunit;

namespace NarrativeEngine.Tests.Unit;

public class ResiliencePolicyTests
{
    private static TimeSpan ExponentialBackoff(int retryAttempt)
        => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    public void ExponentialBackoff_ReturnsCorrectDelay(int attempt, int expectedSeconds)
    {
        var delay = ExponentialBackoff(attempt);
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }
}
