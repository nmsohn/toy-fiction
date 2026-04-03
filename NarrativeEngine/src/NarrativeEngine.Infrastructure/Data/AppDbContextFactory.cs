using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NarrativeEngine.Domain.Common;

namespace NarrativeEngine.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                               ?? "Host=localhost;Port=5432;Database=narrative_engine;Username=narrative_user;Password=narrative_password";

        optionsBuilder.UseNpgsql(connectionString, o => o.UseVector())
            .UseCamelCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options, new SystemCurrentUserAccessor());
    }

    private sealed class SystemCurrentUserAccessor : ICurrentUserAccessor
    {
        public long? UserId => null;
    }
}
