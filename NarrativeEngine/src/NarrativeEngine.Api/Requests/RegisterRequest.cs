namespace NarrativeEngine.Api.Requests;

public record RegisterRequest(string Email, string Password)
{
    public override string ToString() => $"LoginRequest {{ Email = {Email}, Password = [REDACTED] }}";
}