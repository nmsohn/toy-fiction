using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NarrativeEngine.Api.Services.Interfaces;
using NarrativeEngine.Api.Settings;
using NarrativeEngine.Common;
using NarrativeEngine.Domain.Enums;

namespace NarrativeEngine.Api.Services.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtSettings> options, IClock clock) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = options.Value;

    public string GenerateAccessToken(long userId, string email, UserRole role)
    {
        var privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(_settings.PrivateKeyBase64));
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        var signingKey = new ECDsaSecurityKey(ecdsa) { KeyId = "nar-es256" };
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256);

        var now = clock.UtcNow;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: now.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string RawToken, string TokenHash) GenerateRefreshToken()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = ComputeHash(rawToken);
        return (rawToken, tokenHash);
    }

    public static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
