using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace NarrativeEngine.Api.Infrastructure;

public static class ResiliencePolicy
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (retryAttempt, response, _) =>
                {
                    // If 429 with Retry-After header, respect that delay
                    if (response?.Result?.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = response.Result.Headers.RetryAfter;
                        if (retryAfter?.Delta.HasValue == true)
                            return retryAfter.Delta.Value;
                        if (retryAfter?.Date.HasValue == true)
                        {
                            var delay = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                            if (delay > TimeSpan.Zero)
                                return delay;
                        }
                    }
                    // Exponential backoff: 1s, 2s, 4s
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                },
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(GetRetryPolicy(), GetCircuitBreakerPolicy());
    }
}
