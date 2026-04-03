namespace NarrativeEngine.Api.Requests;

public record AuthResult(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiry);