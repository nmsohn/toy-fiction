using NarrativeEngine.Domain.Common;
using NarrativeEngine.Domain.Enums;

namespace NarrativeEngine.Domain.Entities;

public class User : Entity, ISoftDeletable
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public UserSettings? Settings { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
