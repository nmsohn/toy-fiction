using NarrativeEngine.Domain.Common;

namespace NarrativeEngine.Domain.Entities;

public class EmotionScore : Entity
{
    public int ParagraphIndex { get; set; }
    public float? Score { get; set; }
    public string? EmotionTag { get; set; }
    public long ChapterId { get; set; }

    public Chapter Chapter { get; set; } = null!;
}