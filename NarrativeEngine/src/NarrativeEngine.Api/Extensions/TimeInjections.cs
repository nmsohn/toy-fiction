using NarrativeEngine.Common;
using IClock = NarrativeEngine.Common.IClock;

namespace NarrativeEngine.Api.Extensions;

public static class TimeInjections
{
    public static void AddTimeInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
    }
}