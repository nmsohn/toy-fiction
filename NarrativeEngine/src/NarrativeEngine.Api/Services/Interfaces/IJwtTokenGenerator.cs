using NarrativeEngine.Domain.Enums;

namespace NarrativeEngine.Api.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(long userId, string email, UserRole role);
    (string RawToken, string TokenHash) GenerateRefreshToken();
}
