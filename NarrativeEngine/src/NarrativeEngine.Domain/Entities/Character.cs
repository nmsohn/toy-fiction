using NarrativeEngine.Domain.Common;

namespace NarrativeEngine.Domain.Entities;

public class Character : Entity, ISoftDeletable
{
    public long ProjectId { get; set; }
    public string Name { get; set; } = null!;
    public string Profile { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public Project Project { get; set; } = null!;
    public CharacterEmbedding? Embedding { get; set; }
}