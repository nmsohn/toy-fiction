using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Testcontainers.PostgreSql;
using NarrativeEngine.Api.Requests;
using NarrativeEngine.Api.Services.Auth;
using NarrativeEngine.Api.Services.Interfaces;
using NarrativeEngine.Api.Settings;
using NarrativeEngine.Common;
using NarrativeEngine.Domain.Common;
using NarrativeEngine.Domain.Entities;
using NarrativeEngine.Domain.Enums;
using NarrativeEngine.Infrastructure.Data;
using NarrativeEngine.Tests.Integration.Common;

namespace NarrativeEngine.Tests.Integration.Auth;

[Collection("SharedDatabase")]
public sealed class RefreshTokenIntegrationTests : IAsyncLifetime
{
    private readonly SharedDatabaseFixture _fixture;
    private string _connectionString => _fixture.ConnectionString;

    public RefreshTokenIntegrationTests(SharedDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
   
    // ── Req 1.7: Refresh Token Rotation ──────────────────────────────────────
    /// <summary>Req 1.7: A valid Refresh request issues a new token and invalidates the existing one.</summary>
    [Fact]
    public async Task RefreshToken_ValidToken_RotatesSuccessfully()
    {
        var (_, rawToken) = await RegisterAndGetTokenAsync();

        await using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var result = await svc.RefreshTokenAsync(rawToken);

        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEqual(rawToken, result.RefreshToken);
    }

    /// <summary>Req 1.7: After rotation, the existing token has IsRevoked=true and the reason ‘Rotation’ is recorded.</summary>
    [Fact]
    public async Task RefreshToken_AfterRotation_OldTokenIsRevokedWithReason()
    {
        var (_, rawToken) = await RegisterAndGetTokenAsync();
        var oldHash = JwtTokenGenerator.ComputeHash(rawToken);

        await using var ctx = CreateContext();
        var svc = CreateService(ctx);
        await svc.RefreshTokenAsync(rawToken);

        var oldToken = await ctx.RefreshTokens.SingleAsync(t => t.TokenHash == oldHash);
        Assert.True(oldToken.IsRevoked);
        Assert.Equal("Rotation", oldToken.RevokeReason);
        Assert.NotNull(oldToken.ReplacedByTokenHash);
    }

    /// <summary>Req 1.7: After rotation, the new token is stored in the database as active.</summary>
    [Fact]
    public async Task RefreshToken_AfterRotation_NewTokenIsActiveInDb()
    {
        var (userId, rawToken) = await RegisterAndGetTokenAsync();

        await using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var result = await svc.RefreshTokenAsync(rawToken);

        var newHash = JwtTokenGenerator.ComputeHash(result.RefreshToken);
        var newToken = await ctx.RefreshTokens.SingleOrDefaultAsync(
            t => t.UserId == userId && t.TokenHash == newHash && !t.IsRevoked);
        Assert.NotNull(newToken);
    }

    // ── Req 1.9: Expired/invalid token ─────────────────────────────────────
    /// <summary>Req 1.9: If a request is made using an expired refresh token, an UnauthorisedAccessException is thrown.</summary>
    [Fact]
    public async Task RefreshToken_ExpiredToken_ThrowsUnauthorized()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        await using var setupCtx = CreateContext();
        var setupSvc = CreateService(setupCtx, clock);
        var registerResult = await setupSvc.RegisterAsync(new RegisterRequest("exp@test.com", "StrongPass1!"));
        var rawToken = registerResult.RefreshToken;

        //Expired
        clock.Advance(TimeSpan.FromDays(7).Add(TimeSpan.FromSeconds(1)));

        await using var ctx = CreateContext();
        var svc = CreateService(ctx, clock);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.RefreshTokenAsync(rawToken));
    }

    /// <summary>Req 1.9: If a request is made using a non-existent refresh token, an UnauthorisedAccessException is thrown.</summary>
    [Fact]
    public async Task RefreshToken_InvalidToken_ThrowsUnauthorized()
    {
        await using var ctx = CreateContext();
        var svc = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.RefreshTokenAsync("invalid-token-that-does-not-exist"));
    }

    // ── Req 1.10: Reuse Detection ─────────────────────────────────────────────
    /// <summary>Req 1.10: If a previously used (expired) refresh token is reused, all tokens will be revoked.</summary>
    [Fact]
    public async Task RefreshToken_ReuseDetected_RevokesAllUserTokens()
    {
        var (userId, rawToken) = await RegisterAndGetTokenAsync();

        // First Rotation
        await using var ctx1 = CreateContext();
        await CreateService(ctx1).RefreshTokenAsync(rawToken);

        // Retry (Reuse)
        await using var ctx2 = CreateContext();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateService(ctx2).RefreshTokenAsync(rawToken));

        // All tokens must be destroyed
        await using var verifyCtx = CreateContext();
        var activeCount = await verifyCtx.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .CountAsync();
        Assert.Equal(0, activeCount);
    }

    /// <summary>Req 1.10: The ‘reuse’ keyword is included in the Reuse Detection exception message.</summary>
    [Fact]
    public async Task RefreshToken_ReuseDetected_ExceptionMessageIndicatesReuse()
    {
        var (_, rawToken) = await RegisterAndGetTokenAsync("reuse-msg@test.com");

        await using var ctx1 = CreateContext();
        await CreateService(ctx1).RefreshTokenAsync(rawToken);

        await using var ctx2 = CreateContext();
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateService(ctx2).RefreshTokenAsync(rawToken));

        //Check error message that detected reuse (the message should include the keyword 'reuse'
        Assert.Contains("reuse", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── 6.2: Preventing duplicate issuance during simultaneous refreshes ────────────────────────────────────

    /// <summary>
    /// 6.2: When two requests arrive simultaneously using the same refresh token
    /// pg_advisory_xact_lock serialises them, triggering Reuse Detection and preventing duplicate issuance.
    /// </summary>
    [Fact]
    public async Task ConcurrentRefresh_SameToken_PreventsDoubleIssuance()
    {
        var (userId, rawToken) = await RegisterAndGetTokenAsync("concurrent@test.com");

        // Simulating concurrent requests using two independent DbContexts (= independent database connections + independent transactions)
        var task1 = Task.Run(async () =>
        {
            await using var ctx = CreateContext();
            return await CreateService(ctx).RefreshTokenAsync(rawToken);
        });

        var task2 = Task.Run(async () =>
        {
            await using var ctx = CreateContext();
            return await CreateService(ctx).RefreshTokenAsync(rawToken);
        });

        Exception? ex1 = null, ex2 = null;
        AuthResult? result1 = null, result2 = null;

        try { result1 = await task1; } catch (Exception e) { ex1 = e; }
        try { result2 = await task2; } catch (Exception e) { ex2 = e; }

        // At least one must fail (to prevent duplicate issuance)
        var failureCount = new[] { ex1, ex2 }.Count(e => e is UnauthorizedAccessException);
        Assert.True(failureCount >= 1, "At least one must fail (to prevent duplicate issuance)");

        // If Reuse Detection is triggered, all tokens are discarded
        if (failureCount == 2 ||
            ex1?.Message.Contains("reuse", StringComparison.OrdinalIgnoreCase) == true ||
            ex2?.Message.Contains("reuse", StringComparison.OrdinalIgnoreCase) == true)
        {
            await using var verifyCtx = CreateContext();
            var activeCount = await verifyCtx.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .CountAsync();
            Assert.Equal(0, activeCount);
        }
    }

    // ── Immutable Refresh Token Rotation ────────────────────────────

    /// <summary>
    /// After performing N consecutive rotations, only the last active token remains.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task RefreshToken_NConsecutiveRotations_OnlyOneActiveTokenExists(int rotationCount)
    {
        var (userId, currentRaw) = await RegisterAndGetTokenAsync($"rotation{rotationCount}@test.com");

        for (var i = 0; i < rotationCount; i++)
        {
            await using var ctx = CreateContext();
            var result = await CreateService(ctx).RefreshTokenAsync(currentRaw);
            currentRaw = result.RefreshToken;
        }

        await using var verifyCtx = CreateContext();
        var activeCount = await verifyCtx.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .CountAsync();

        Assert.Equal(1, activeCount); // Always keep only the last token active
    }

    /// <summary>
    /// Subsequent requests will only succeed if they use the RefreshToken from the response following the rotation
    /// (The previous token is no longer valid)
    /// </summary>
    [Fact]
    public async Task RefreshToken_AfterRotation_OldTokenCannotBeUsedAgain()
    {
        var (_, rawToken) = await RegisterAndGetTokenAsync("oldtoken@test.com");

        await using var ctx1 = CreateContext();
        await CreateService(ctx1).RefreshTokenAsync(rawToken);

        //Old token -> should fail
        await using var ctx2 = CreateContext();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateService(ctx2).RefreshTokenAsync(rawToken));
    }

    private AppDbContext CreateContext()
    {
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.UserId.Returns((long?)null);
        
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_connectionString)
                .UseCamelCaseNamingConvention()
                .Options,
            currentUser);

    }

    private static AuthService CreateService(
        AppDbContext ctx,
        IClock? clock = null,
        IJwtTokenGenerator? gen = null)
    {
        gen ??= CreateDefaultGenerator();
        clock ??= new TestClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        return new AuthService(
            ctx,
            new PasswordHasher<User>(),
            gen,
            Options.Create(new JwtSettings { RefreshTokenExpiryDays = 7 }),
            clock);
    }

    private static IJwtTokenGenerator CreateDefaultGenerator()
    {
        var gen = Substitute.For<IJwtTokenGenerator>();
        gen.GenerateRefreshToken().Returns(ci =>
        {
            var raw = Guid.NewGuid().ToString("N");
            return (raw, JwtTokenGenerator.ComputeHash(raw));
        });
        gen.GenerateAccessToken(Arg.Any<long>(), Arg.Any<string>(), UserRole.User)
            .Returns(ci => $"access-{ci.ArgAt<long>(0)}-{Guid.NewGuid():N}");
        return gen;
    }

    private async Task<(long UserId, string RawToken)> RegisterAndGetTokenAsync(string email = "user@test.com")
    {
        await using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var result = await svc.RegisterAsync(new RegisterRequest(email, "StrongPass1!"));
        var user = await ctx.Users.SingleAsync(u => u.Email == email);
        return (user.Id, result.RefreshToken);
    }
}
