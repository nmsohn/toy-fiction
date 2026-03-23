using NarrativeEngine.Domain.Common;
using Pgvector;

namespace NarrativeEngine.Domain.Entities;

public class CharacterEmbedding : Entity
{
    public Vector Embedding { get; set; } = null!;
    public long CharacterId { get; set; }
    
    public Character Character { get; set; } = null!;
}