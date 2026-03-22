using Microsoft.EntityFrameworkCore;
using NarrativeEngine.Infrastructure.Data;
using NSubstitute;
using Testcontainers.PostgreSql;
using NarrativeEngine.Domain.Common;

namespace NarrativeEngine.Tests.Integration.Common;

public sealed class SharedDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();

        // Run migrations or EnsureCreated once
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseCamelCaseNamingConvention()
            .Options;
        
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.UserId.Returns((long?)null);

        await using var context = new AppDbContext(options, currentUser);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseCamelCaseNamingConvention()
            .Options;

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.UserId.Returns((long?)null);

        await using var context = new AppDbContext(options, currentUser);
        
        // Truncate tables. In PostgreSQL, TRUNCATE is faster than DELETE.
        // We use RESTART IDENTITY to reset IDs.
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE users, refresh_tokens, \"userSettings\" RESTART IDENTITY CASCADE;");
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition("SharedDatabase")]
public class SharedDatabaseCollection : ICollectionFixture<SharedDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
