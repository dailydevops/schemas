namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;

/// <summary>The Unicode codepoint collation, the only collation of the comparison operators (#123 AP19).</summary>
public class CodepointComparisonTests
{
    private static string Supplementary(int codePoint) => char.ConvertFromUtf32(codePoint);

    private static string Basic(int unit) => new((char)unit, 1);

    [Test]
    public async Task EqualStrings_CompareAsZero()
    {
        await Assert.That(CodepointComparison.Compare("abc", "abc")).IsEqualTo(0);
        await Assert.That(CodepointComparison.Compare(string.Empty, string.Empty)).IsEqualTo(0);
        await Assert.That(CodepointComparison.Compare(Supplementary(0x10000), Supplementary(0x10000))).IsEqualTo(0);
    }

    [Test]
    public async Task TheFirstDifferentCodepoint_Decides()
    {
        await Assert.That(CodepointComparison.Compare("abc", "abd")).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare("abd", "abc")).IsGreaterThan(0);
        await Assert.That(CodepointComparison.Compare("b", "abc")).IsGreaterThan(0);
    }

    [Test]
    public async Task APrefix_PrecedesTheLongerString()
    {
        await Assert.That(CodepointComparison.Compare("ab", "abc")).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare("abc", "ab")).IsGreaterThan(0);
        await Assert.That(CodepointComparison.Compare(string.Empty, "a")).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare("a", string.Empty)).IsGreaterThan(0);
    }

    [Test]
    public async Task TheOrder_IsNotCultureSensitive()
    {
        await Assert.That(CodepointComparison.Compare("B", "a")).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare("a", "A")).IsGreaterThan(0);
        await Assert.That(CodepointComparison.Compare("ä", "z")).IsGreaterThan(0);
    }

    [Test]
    public async Task ASupplementaryCodepoint_FollowsEveryBasicCodepoint()
    {
        // In UTF-16 order U+10000 (D800 DC00) precedes U+E000 and U+FFFF; in codepoint order it follows them.
        await Assert.That(CodepointComparison.Compare(Basic(0xFFFF), Supplementary(0x10000))).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare(Basic(0xE000), Supplementary(0x10000))).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare(Supplementary(0x10000), Basic(0xFFFF))).IsGreaterThan(0);
        await Assert.That(CodepointComparison.Compare(Basic(0xD7FF), Supplementary(0x10000))).IsLessThan(0);
    }

    [Test]
    public async Task SupplementaryCodepoints_AreOrderedAmongThemselves()
    {
        await Assert.That(CodepointComparison.Compare(Supplementary(0x10000), Supplementary(0x10001))).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare(Supplementary(0x10000), Supplementary(0x20000))).IsLessThan(0);
        await Assert
            .That(CodepointComparison.Compare(Supplementary(0x10FFFF), Supplementary(0x10000)))
            .IsGreaterThan(0);
        await Assert.That(CodepointComparison.Compare(Supplementary(0x1F600), Supplementary(0x1F601))).IsLessThan(0);
    }

    [Test]
    public async Task ThePositionOfTheDifference_IsAfterACommonPrefixOfSurrogates()
    {
        var left = "a" + Supplementary(0x10000) + Basic(0xFFFF);
        var right = "a" + Supplementary(0x10000) + Supplementary(0x10000);

        await Assert.That(CodepointComparison.Compare(left, right)).IsLessThan(0);
        await Assert.That(CodepointComparison.Compare(right, left)).IsGreaterThan(0);
    }

    [Test]
    public async Task ANullArgument_IsRejected()
    {
        await Assert.That(() => CodepointComparison.Compare(null!, "a")).Throws<ArgumentNullException>();
        await Assert.That(() => CodepointComparison.Compare("a", null!)).Throws<ArgumentNullException>();
    }
}
