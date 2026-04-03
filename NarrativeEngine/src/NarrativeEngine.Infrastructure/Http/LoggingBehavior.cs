using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NarrativeEngine.Infrastructure.Http;

public class LoggingBehavior(ILogger<LoggingBehavior> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestUri = request.RequestUri?.ToString() ?? "(unknown)";
        var sw = Stopwatch.StartNew();

        var response = await base.SendAsync(request, cancellationToken);

        sw.Stop();

        if (sw.Elapsed > TimeSpan.FromSeconds(5))
        {
            logger.LogWarning(
                "Slow external API call to {Url} took {ElapsedMs}ms",
                requestUri,
                sw.ElapsedMilliseconds);
        }

        return response;
    }
}
