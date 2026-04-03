using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using NarrativeEngine.Api.Requests;
using NarrativeEngine.Api.Services.Auth;
using NarrativeEngine.Api.Services.Interfaces;
using NarrativeEngine.Api.Settings;
using NarrativeEngine.Domain.Common;
using NarrativeEngine.Domain.Entities;
using NarrativeEngine.Domain.Enums;
using NarrativeEngine.Infrastructure.Data;
using NarrativeEngine.Tests.Unit.Common;

namespace NarrativeEngine.Tests.Unit.Auth;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly TestClock _clock;
    private readonly AuthService _sut;

    private static readonly IOptions<JwtSettings> DefaultJwtOptions = Options.Create(new JwtSettings
    {
        RefreshTokenExpiryDays = 7,
        AccessTokenExpiryMinutes = 60
    });

    public AuthServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.UserId.Returns((long?)null);

        _context = new AppDbContext(dbOptions, currentUser);
        _jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        _clock = new TestClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        _jwtGenerator.GenerateRefreshToken().Returns(("raw-refresh-token", "hashed-refresh-token"));
        _jwtGenerator.GenerateAccessToken(Arg.Any<long>(), Arg.Any<string>(), UserRole.User).Returns("access-token");

        _sut = new AuthService(_context, new PasswordHasher<User>(), _jwtGenerator, DefaultJwtOptions, _clock);
    }

    public void Dispose() => _context.Dispose();

    // ── Req 1.1 ──────────────────────────────────────────────────────────────
    
    /// <summary>Req 1.1: A successful registration returns an AccessToken and a RefreshToken.</summary>
    [Fact]
    public async Task Register_ValidRequest_ReturnsAccessAndRefreshTokens()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("raw-refresh-token", result.RefreshToken);
    }

    /// <summary>Req 1.1: Save the email address in lowercase.</summary>
    [Fact]
    public async Task Register_MixedCaseEmail_StoresNormalized()
    {
        await _sut.RegisterAsync(new RegisterRequest("User@Example.COM", "StrongPass1!"));

        var user = await _context.Users.SingleAsync();
        Assert.Equal("user@example.com", user.Email);
    }

    /// <summary>Req 1.1: UserSettings are automatically reset upon registration.</summary>
    [Fact]
    public async Task Register_ValidRequest_InitializesUserSettings()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));

        var settings = await _context.UserSettings.SingleOrDefaultAsync();
        Assert.NotNull(settings);
    }

    // ── Req 1.2 ──────────────────────────────────────────────────────────────
    
    /// <summary>Req 1.2: An InvalidOperationException is thrown when attempting to register using an email address that is already registered (HTTP 409).</summary>
    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(new RegisterRequest("USER@EXAMPLE.COM", "AnotherPass2!")));
    }

    // ── Req 1.4 ──────────────────────────────────────────────────────────────

    /// <summary>Req 1.4: Returns a token upon successful login with valid credentials.</summary>
    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));
        _jwtGenerator.GenerateRefreshToken().Returns(("login-refresh", "login-hash"));
        _jwtGenerator.GenerateAccessToken(Arg.Any<long>(), Arg.Any<string>(), UserRole.User).Returns("login-access");

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "StrongPass1!"));

        Assert.Equal("login-access", result.AccessToken);
        Assert.Equal("login-refresh", result.RefreshToken);
    }

    /// <summary>Req 1.4: The login system is not case-sensitive.</summary>
    [Fact]
    public async Task Login_EmailCaseInsensitive_ReturnsTokens()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));
        _jwtGenerator.GenerateRefreshToken().Returns(("r", "h"));
        _jwtGenerator.GenerateAccessToken(Arg.Any<long>(), Arg.Any<string>(), UserRole.User).Returns("a");

        var result = await _sut.LoginAsync(new LoginRequest("USER@EXAMPLE.COM", "StrongPass1!"));

        Assert.NotNull(result.AccessToken);
    }

    /// <summary>Req 1.4: The RefreshToken expiry time is calculated based on the JwtSettings.RefreshTokenExpiryDays setting.</summary>
    [Fact]
    public async Task Login_ValidCredentials_RefreshTokenExpiryIsCorrect()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));
        _jwtGenerator.GenerateRefreshToken().Returns(("r2", "h2"));
        _jwtGenerator.GenerateAccessToken(Arg.Any<long>(), Arg.Any<string>(), UserRole.User).Returns("a2");

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "StrongPass1!"));

        Assert.Equal(_clock.UtcNow.AddDays(7), result.RefreshTokenExpiry);
    }

    // ── Req 1.5 ──────────────────────────────────────────────────────────────

    /// <summary>Req 1.5: The RefreshToken is stored in the database as a hash value (not the original text).</summary>
    [Fact]
    public async Task Register_ValidRequest_StoresRefreshTokenAsHash()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));

        var token = await _context.RefreshTokens.SingleAsync();
        Assert.Equal("hashed-refresh-token", token.TokenHash);
        Assert.NotEqual("raw-refresh-token", token.TokenHash);
    }

    // ── Req 1.11 ─────────────────────────────────────────────────────────────

    /// <summary>Req 1.11: Revoke marks the token as IsRevoked=true and records the reason for logout.</summary>
    [Fact]
    public async Task Revoke_ValidToken_MarksRevokedWithLogoutReason()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));
        await _sut.RevokeAsync("raw-refresh-token");

        var token = await _context.RefreshTokens.SingleAsync();
        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
        Assert.Equal("Logout", token.RevokeReason);
    }

    /// <summary>Req 1.11: Revoking a token that has already been revoked does not cause an exception (idempotence).</summary>
    [Fact]
    public async Task Revoke_AlreadyRevokedToken_IsIdempotent()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));
        await _sut.RevokeAsync("raw-refresh-token");

        var ex = await Record.ExceptionAsync(() => _sut.RevokeAsync("raw-refresh-token"));

        Assert.Null(ex);
    }

    // ── Req 1.12 ─────────────────────────────────────────────────────────────

    /// <summary>Req 1.12: Throws an UnauthorizedAccessException when logging in with an incorrect password (HTTP 401).</summary>
    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest("user@example.com", "WrongPassword!")));
    }

    /// <summary>Req 1.12: Throws an UnauthorizedAccessException when attempting to log in with a non-existent email address.</summary>
    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest("nobody@example.com", "AnyPass1!")));
    }

    // ── Req 1.14 ─────────────────────────────────────────────────────────────

    /// <summary>Req 1.14: Passwords are not stored in plain text.</summary>
    [Fact]
    public async Task Register_ValidRequest_DoesNotStorePasswordAsPlaintext()
    {
        const string plainPassword = "StrongPass1!";
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", plainPassword));

        var user = await _context.Users.SingleAsync();
        Assert.NotEqual(plainPassword, user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    // ── Req 1.15 ─────────────────────────────────────────────────────────────

    /// <summary>Req 1.15: The sign-up response returns the original RefreshToken, whilst only the hash is stored in the database.</summary>
    [Fact]
    public async Task Register_ValidRequest_RefreshTokenNotExposedInDbPlaintext()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));

        var token = await _context.RefreshTokens.SingleAsync();

        // The original text exists only in the response; only the hash exists in the database
        Assert.NotEqual(result.RefreshToken, token.TokenHash);
    }

    // ── RevokeAll ─────────────────────────────────────────────────────────────

    /// <summary>RevokeAllUserTokensAsync: Revokes all active tokens for the specified user.</summary>
    [Fact]
    public async Task RevokeAllUserTokens_RevokesOnlyActiveTokens()
    {
        await _sut.RegisterAsync(new RegisterRequest("user@example.com", "StrongPass1!"));

        // Issue an additional token upon the second login
        _jwtGenerator.GenerateRefreshToken().Returns(("raw2", "hash2"));
        _jwtGenerator.GenerateAccessToken(Arg.Any<long>(), Arg.Any<string>(), UserRole.User).Returns("access2");
        await _sut.LoginAsync(new LoginRequest("user@example.com", "StrongPass1!"));

        var user = await _context.Users.SingleAsync();
        await _sut.RevokeAllUserTokensAsync(user.Id);

        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .CountAsync();
        Assert.Equal(0, activeTokens);
    }
}
