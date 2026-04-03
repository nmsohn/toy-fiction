using NarrativeEngine.Domain.Common;

namespace NarrativeEngine.Domain.Entities;

public class Chapter : Entity, ISoftDeletable
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int OrderIndex { get; set; }
    public int TokenCount { get; set; }
    public string? Summary { get; set; }
    public long ProjectId { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<EmotionScore> EmotionScores { get; set; } = [];
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}