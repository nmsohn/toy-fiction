using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NarrativeEngine.Api.Requests;
using NarrativeEngine.Api.Services.Interfaces;
using NarrativeEngine.Api.Settings;
using NarrativeEngine.Common;
using NarrativeEngine.Domain.Entities;
using NarrativeEngine.Infrastructure.Data;

namespace NarrativeEngine.Api.Services.Auth;

public class AuthService(
    AppDbContext context,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<JwtSettings> jwtOptions,
    IClock clock) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var emailNormalized = request.Email.ToLowerInvariant();

        if (await context.Users.AnyAsync(u => u.Email == emailNormalized, ct))
            throw new InvalidOperationException("Email already in use.");
        
        var now = clock.UtcNow;

        var user = new User
        {
            Email = emailNormalized,
            CreatedAt = now,
            ModifiedAt = now,
            Settings = new UserSettings { ModifiedAt = now }
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        context.Users.Add(user);
        await context.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email.Equals(request.Email, StringComparison.InvariantCultureIgnoreCase), ct);

        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> RefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var tokenHash = ComputeHash(rawRefreshToken);

        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        //pg_advisory_lock
        var lockKey = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(tokenHash)), 0);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockKey})", ct); //safe from sql injection

        var existing = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (existing is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (existing.ExpiresAt < clock.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");

        if (existing.IsRevoked)
        {
            // Reuse Detection 
            await RevokeAllUserTokensAsync(existing.UserId, ct);
            await tx.CommitAsync(ct);
            throw new UnauthorizedAccessException("Refresh token reuse detected. All sessions invalidated.");
        }

        var (newRaw, newHash) = jwtTokenGenerator.GenerateRefreshToken();
        var expiry = clock.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays);

        existing.IsRevoked = true;
        existing.RevokedAt = clock.UtcNow;
        existing.ReplacedByTokenHash = newHash;
        existing.RevokeReason = "Rotation";

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newHash,
            ExpiresAt = expiry,
            CreatedAt = clock.UtcNow
        });

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(existing.UserId, existing.User.Email, existing.User.Role);
        return new AuthResult(accessToken, newRaw, expiry);
    }

    public async Task RevokeAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var tokenHash = ComputeHash(rawRefreshToken);

        var token = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsRevoked, ct);

        if (token is null) return;

        token.IsRevoked = true;
        token.RevokedAt = clock.UtcNow;
        token.RevokeReason = "Logout";

        await context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllUserTokensAsync(long userId, CancellationToken ct = default)
    {
        await context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, clock.UtcNow)
                .SetProperty(t => t.RevokeReason, "ReuseDetected"), ct);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (rawToken, tokenHash) = jwtTokenGenerator.GenerateRefreshToken();
        var expiry = clock.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays);

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiry,
            CreatedAt = clock.UtcNow
        });
        await context.SaveChangesAsync(ct);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role);
        return new AuthResult(accessToken, rawToken, expiry);
    }

    private static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
