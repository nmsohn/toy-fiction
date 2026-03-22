namespace NarrativeEngine.Api.Settings;

public class JwtSettings
{
    public string PrivateKeyBase64 { get; set; } = string.Empty;
    public string PublicKeyBase64 { get; set; } = string.Empty;
    public string Issuer { get; set; } = "NarrativeEngine";
    public string Audience { get; set; } = "NarrativeEngine";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
