namespace NarrativeEngine.Domain.Entities;

public class RefreshToken
{
    public long Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevokeReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long UserId { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
