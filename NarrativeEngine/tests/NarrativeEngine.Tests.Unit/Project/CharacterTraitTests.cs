using NarrativeEngine.Domain.Entities;
using NarrativeEngine.Domain.Enums;

namespace NarrativeEngine.Tests.Unit.Project;

public class CharacterTraitTests
{
    [Fact]
    public void IsValueValid_ShortText_AlwaysValid()
    {
        var trait = new CharacterTrait { Type = TraitType.ShortText, Value = "any value" };
        Assert.True(trait.IsValueValid());
    }

    [Fact]
    public void IsValueValid_LongText_AlwaysValid()
    {
        var trait = new CharacterTrait { Type = TraitType.LongText, Value = "long text content..." };
        Assert.True(trait.IsValueValid());
    }

    [Fact]
    public void IsValueValid_Dropdown_ValueInOptions_ReturnsTrue()
    {
        var trait = new CharacterTrait
        {
            Type = TraitType.Dropdown,
            Options = ["남", "여", "기타"],
            Value = "여"
        };
        Assert.True(trait.IsValueValid());
    }

    [Fact]
    public void IsValueValid_Dropdown_ValueNotInOptions_ReturnsFalse()
    {
        var trait = new CharacterTrait
        {
            Type = TraitType.Dropdown,
            Options = ["남", "여", "기타"],
            Value = "unknown"
        };
        Assert.False(trait.IsValueValid());
    }

    [Fact]
    public void IsValueValid_Dropdown_NullValue_ReturnsFalse()
    {
        var trait = new CharacterTrait
        {
            Type = TraitType.Dropdown,
            Options = ["남", "여"],
            Value = null
        };
        Assert.False(trait.IsValueValid());
    }

    [Fact]
    public void IsValueValid_Dropdown_NullOptions_ReturnsFalse()
    {
        var trait = new CharacterTrait
        {
            Type = TraitType.Dropdown,
            Options = null,
            Value = "남"
        };
        Assert.False(trait.IsValueValid());
    }
}
