using ShortcutMaster.Core.Parsing;

namespace ShortcutMaster.Core.Tests;

public class ChordParserTests
{
    [Theory]
    [InlineData("Ctrl+C", 1, 0x43)]
    [InlineData("Ctrl+Shift+P", 2, 0x50)]
    [InlineData("Win+V", 1, 0x56)]
    [InlineData("Alt+Shift+Equals", 2, 0xBB)]
    [InlineData("F11", 0, 0x7A)]
    public void ParseStep_ReturnsExpectedModifiersAndKey(string token, int modifierCount, int virtualKey)
    {
        var step = ChordParser.ParseStep(token);

        Assert.Equal(modifierCount, step.Modifiers.Count);
        Assert.Equal((ushort)virtualKey, step.Key.VirtualKey);
        Assert.False(step.IsModifierTap);
    }

    [Fact]
    public void ParseStep_AltAlone_IsModifierTap()
    {
        var step = ChordParser.ParseStep("Alt");

        Assert.True(step.IsModifierTap);
        Assert.Empty(step.Modifiers);
        Assert.Equal((ushort)0xA4, step.Key.VirtualKey);
    }

    [Fact]
    public void ParseStep_ExtendedKey_HasExtendedFlag()
    {
        var step = ChordParser.ParseStep("Shift+Delete");

        Assert.True(step.Key.IsExtended);
        Assert.Single(step.Modifiers);
    }

    [Fact]
    public void ParseSequence_ParsesAllSteps()
    {
        var steps = ChordParser.ParseSequence(new[] { "Alt", "H", "B" });

        Assert.Equal(3, steps.Count);
        Assert.True(steps[0].IsModifierTap);
        Assert.False(steps[1].IsModifierTap);
    }

    [Fact]
    public void ParseSequence_TwoStepChord_Parses()
    {
        var steps = ChordParser.ParseSequence(new[] { "Ctrl+K", "Ctrl+S" });

        Assert.Equal(2, steps.Count);
        Assert.All(steps, s => Assert.Single(s.Modifiers));
    }

    [Theory]
    [InlineData("Ctrl+Foo")]
    [InlineData("A+B")]
    [InlineData("")]
    [InlineData("+")]
    public void ParseStep_InvalidToken_Throws(string token)
    {
        Assert.Throws<FormatException>(() => ChordParser.ParseStep(token));
    }

    [Fact]
    public void VirtualKeyMap_WinKey_IsExtended()
    {
        Assert.True(VirtualKeyMap.TryGet("Win", out var def));
        Assert.True(def.IsExtended);
    }
}
