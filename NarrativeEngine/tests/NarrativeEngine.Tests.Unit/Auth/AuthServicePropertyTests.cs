using FsCheck;
using FsCheck.Xunit;
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

public class AuthServicePropertyTests
{
    private static (AppDbContext ctx, AuthService svc, IJwtTokenGenerator gen) BuildSut(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.UserId.Returns((long?)null);
        var ctx = new AppDbContext(options, currentUser);

        var gen = Substitute.For<IJwtTokenGenerator>();
        gen.GenerateRefreshToken().Returns(ci =>
        {
            var raw = Guid.NewGuid().ToString("N");
            return (raw, raw + "-hash");
        });
        gen.GenerateAccessToken(Arg.Any<long>(), Arg.Any<string>(), UserRole.User)
            .Returns(ci => $"access-{ci.ArgAt<long>(0)}");

        var jwtOptions = Options.Create(new JwtSettings { RefreshTokenExpiryDays = 7 });
        var clock = new TestClock();
        var svc = new AuthService(ctx, new PasswordHasher<User>(), gen, jwtOptions, clock);

        return (ctx, svc, gen);
    }

    // ── Valid registration ─────────────────────────────────────────────

    /// <summary>
    /// Provided the email address is valid and the password is of sufficient length, `Register` will always return a non-empty token pair.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_ValidCredentials_AlwaysReturnsNonEmptyTokenPair(
        NonEmptyString localPart,
        PositiveInt passwordSuffix)
    {
        // Valid email/password generation (normalisation required as FsCheck-generated values may contain special characters)
        var safeLocal = new string(localPart.Get.Where(char.IsLetterOrDigit).ToArray());
        if (safeLocal.Length == 0) safeLocal = "user";
        var email = $"{safeLocal}@test.com";
        var password = $"Pass{passwordSuffix.Get:D4}!";   // At least 10 characters, always valid

        var (ctx, svc, _) = BuildSut();
        try
        {
            var result = svc.RegisterAsync(new RegisterRequest(email, password))
                .GetAwaiter().GetResult();

            return !string.IsNullOrEmpty(result.AccessToken)
                   && !string.IsNullOrEmpty(result.RefreshToken);
        }
        finally
        {
            ctx.Dispose();
        }
    }

    /// <summary>
    /// The Register function always creates exactly one user and one RefreshToken in the database.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool Register_ValidCredentials_CreatesSingleUserAndToken(PositiveInt seed)
    {
        var email = $"u{seed.Get}@test.com";
        var password = $"Pass{seed.Get:D4}!";

        var (ctx, svc, _) = BuildSut();
        try
        {
            svc.RegisterAsync(new RegisterRequest(email, password)).GetAwaiter().GetResult();

            var userCount = ctx.Users.Count();
            var tokenCount = ctx.RefreshTokens.Count();
            return userCount == 1 && tokenCount == 1;
        }
        finally
        {
            ctx.Dispose();
        }
    }

    /// <summary>
    /// The PasswordHasher never returns any password in plain text.
    /// Immutable property of the PBKDF2-based ASP.NET Core PasswordHasher.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool PasswordHasher_NeverReturnsPlaintext(NonEmptyString password)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User();
        var hash = hasher.HashPassword(user, password.Get);
        return hash != password.Get && hash.Length > 0;
    }

    /// <summary>
    /// The PasswordHash stored in the database following AuthService.Register is different from the original password.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool Register_StoredPasswordHash_NeverEqualsPlaintext(PositiveInt seed)
    {
        var plainPassword = $"Pass{seed.Get:D4}!";

        var (ctx, svc, _) = BuildSut();
        try
        {
            svc.RegisterAsync(new RegisterRequest($"u{seed.Get}@test.com", plainPassword))
                .GetAwaiter().GetResult();

            var user = ctx.Users.Single();
            return user.PasswordHash != plainPassword
                   && !string.IsNullOrEmpty(user.PasswordHash);
        }
        finally
        {
            ctx.Dispose();
        }
    }

    /// <summary>
    /// Even if the same password is hashed twice, different hash values are produced (salt verification).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool PasswordHasher_SamePasswordProducesDifferentHashes(NonEmptyString password)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User();
        var hash1 = hasher.HashPassword(user, password.Get);
        var hash2 = hasher.HashPassword(user, password.Get);
        return hash1 != hash2; // A different hash every time due to the salt
    }

    /// <summary>
    /// The original RefreshToken from the sign-up response and the TokenHash in the database are always different.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool Register_RefreshTokenRaw_NeverEqualsStoredHash(PositiveInt seed)
    {
        var (ctx, svc, _) = BuildSut();
        try
        {
            var result = svc.RegisterAsync(new RegisterRequest($"u{seed.Get}@test.com", $"Pass{seed.Get:D4}!"))
                .GetAwaiter().GetResult();

            var token = ctx.RefreshTokens.Single();
            return result.RefreshToken != token.TokenHash;
        }
        finally
        {
            ctx.Dispose();
        }
    }
}
