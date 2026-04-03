using NarrativeEngine.Domain.Common;
using NarrativeEngine.Domain.Enums;

namespace NarrativeEngine.Domain.Entities;

public class Project : Entity, ISoftDeletable
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public long UserId { get; set; }
    
    public User User { get; set; } = null!;
    public ICollection<Chapter> Chapters { get; set; } = [];
    public ICollection<Character> Characters { get; set; } = [];
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}