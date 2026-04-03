using NarrativeEngine.Domain.Common;
using NarrativeEngine.Domain.Enums;

namespace NarrativeEngine.Domain.Entities;

public class CharacterTrait : Entity
{
    public long CharacterId { get; set; }
    public string Key { get; set; } = null!; 
    public string Label { get; set; } = null!;
    public TraitType Type { get; set; }
    public string[]? Options { get; set; }
    public string? Value { get; set; }
    public int OrderIndex { get; set; }

    public Character Character { get; set; } = null!;

    /// <summary>
    /// If it's dropdown type, check if value is one of options
    /// ShortText/LongText is always valid
    /// </summary>
    public bool IsValueValid() => Type switch
    {
        TraitType.Dropdown => Options is { Length: > 0 } && Value is not null && Options.Contains(Value),
        _ => true
    };
}
