using NarrativeEngine.Domain.Common;

namespace NarrativeEngine.Domain.Entities;

public class User : ISoftDeletable
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public UserSettings? Settings { get; set; }
    
    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
