namespace NarrativeEngine.Api.Requests;

public record LoginRequest(string Email, string Password)
{
    public override string ToString() => $"LoginRequest {{ Email = {Email}, Password = [REDACTED] }}";
}