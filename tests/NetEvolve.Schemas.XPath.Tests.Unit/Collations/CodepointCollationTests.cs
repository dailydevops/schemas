namespace NetEvolve.Schemas.XPath.Tests.Unit.Collations;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Collations;

/// <summary>
/// The Unicode codepoint collation: strings compare by code point, not by UTF-16 code unit, and the substring operations never match
/// half of a surrogate pair. Nothing here allocates.
/// </summary>
public class CodepointCollationTests
{
    private const int Warmups = 50;

    private static readonly CodepointCollation Collation = CodepointCollation.Instance;

    [Test]
    [Arguments("a", "b", -1)]
    [Arguments("b", "a", 1)]
    [Arguments("b", "b", 0)]
    [Arguments("", "", 0)]
    [Arguments("", "a", -1)]
    [Arguments("a", "", 1)]
    [Arguments("abc", "abcd", -1)]
    [Arguments("abcd", "abc", 1)]
    [Arguments("A", "a", -1)]
    [Arguments("z", "é", -1)]
    public async Task Compare_OrderedPairs_HaveTheSignOfTheOrder(string first, string second, int sign)
    {
        await Assert.That(Math.Sign(Collation.Compare(first, second))).IsEqualTo(sign);
        await Assert.That(Math.Sign(Collation.Compare(second, first))).IsEqualTo(-sign);
    }

    [Test]
    [Arguments("\U00010000", "�")]
    [Arguments("\U00010001", "￰")]
    [Arguments("\U0010FFFF", "")]
    [Arguments("a\U00010000", "a￿")]
    public async Task Compare_SupplementaryCharacterAgainstTheTopOfTheBasicPlane_FollowsTheCodePoint(
        string supplementary,
        string basic
    )
    {
        // The code unit order disagrees: the high surrogate D800 to DBFF precedes U+E000 to U+FFFF.
        await Assert.That(string.CompareOrdinal(supplementary, basic)).IsLessThan(0);
        await Assert.That(Collation.Compare(supplementary, basic)).IsGreaterThan(0);
        await Assert.That(Collation.Compare(basic, supplementary)).IsLessThan(0);
    }

    [Test]
    public async Task Compare_TwoSupplementaryCharacters_FollowsTheCodePoint()
    {
        await Assert.That(Collation.Compare("\U00010001", "\U00010002")).IsLessThan(0);
        await Assert.That(Collation.Compare("\U00010002", "\U00010001")).IsGreaterThan(0);
        await Assert.That(Collation.Compare("\U00010001", "\U00010001")).IsEqualTo(0);
    }

    [Test]
    [Arguments("a", "a", true)]
    [Arguments("", "", true)]
    [Arguments("a", "A", false)]
    [Arguments("\U00010000", "\U00010000", true)]
    [Arguments("garçon", "garçon", false)]
    public async Task AreEqual_IsEqualityOfTheCodePoints(string first, string second, bool expected) =>
        await Assert.That(Collation.AreEqual(first, second)).IsEqualTo(expected);

    [Test]
    [Arguments("tattoo", "tat", 0, 3)]
    [Arguments("tattoo", "too", 3, 3)]
    [Arguments("tattoo", "t", 0, 1)]
    [Arguments("tattoo", "", 0, 0)]
    [Arguments("", "", 0, 0)]
    [Arguments("a\U00010000b", "\U00010000", 1, 2)]
    [Arguments("a\U00010000b", "b", 3, 1)]
    [Arguments("\U00010000\U00010001", "\U00010001", 2, 2)]
    public async Task TryFind_FindsTheFirstMatch(string text, string value, int index, int length)
    {
        var found = Collation.TryFind(text, value, out var at, out var size);

        await Assert.That(found).IsTrue();
        await Assert.That(at).IsEqualTo(index);
        await Assert.That(size).IsEqualTo(length);
    }

    [Test]
    [Arguments("tattoo", "z")]
    [Arguments("Tattoo", "tat")]
    [Arguments("", "a")]
    [Arguments("ab", "\U00010000")]
    [Arguments("\U00010000", "\uDC00")]
    [Arguments("\U00010000", "\uD800")]
    [Arguments("a\U00010000", "\uDC00")]
    public async Task TryFind_NoMatch_ReportsNone(string text, string value)
    {
        var found = Collation.TryFind(text, value, out var at, out _);

        await Assert.That(found).IsFalse();
        await Assert.That(at).IsEqualTo(-1);
    }

    [Test]
    public async Task TryFind_ASurrogateHalfBeforeAWholeCharacter_SkipsTheHalfAndFindsTheNextMatch()
    {
        // The first D800 is the first half of the pair U+10000: not a match for the lone unit. A text with a lone unit does match it.
        var found = Collation.TryFind("\U00010000\uD800", "\uD800", out var at, out var length);

        await Assert.That(found).IsTrue();
        await Assert.That(at).IsEqualTo(2);
        await Assert.That(length).IsEqualTo(1);
    }

    [Test]
    [Arguments("tattoo", "tat", true)]
    [Arguments("tattoo", "att", false)]
    [Arguments("tattoo", "", true)]
    [Arguments("", "", true)]
    [Arguments("", "a", false)]
    [Arguments("\U00010000b", "\U00010000", true)]
    [Arguments("\U00010000", "\uD800", false)]
    [Arguments("\U00010000", "\uDC00", false)]
    public async Task StartsWith_ComparesCodePoints(string text, string value, bool expected) =>
        await Assert.That(Collation.StartsWith(text, value)).IsEqualTo(expected);

    [Test]
    [Arguments("tattoo", "too", true)]
    [Arguments("tattoo", "tat", false)]
    [Arguments("tattoo", "", true)]
    [Arguments("", "", true)]
    [Arguments("", "a", false)]
    [Arguments("b\U00010000", "\U00010000", true)]
    [Arguments("\U00010000", "\uDC00", false)]
    [Arguments("\U00010000", "\uD800", false)]
    public async Task EndsWith_ComparesCodePoints(string text, string value, bool expected) =>
        await Assert.That(Collation.EndsWith(text, value)).IsEqualTo(expected);

    [Test]
    public async Task Comparison_AllocatesNothing()
    {
        const string first = "ab\U00010001cd�";
        const string second = "ab\U00010001cd\U00010000";

        AllocationAssert.NoAllocation(() => _ = Collation.Compare(first, second), warmups: Warmups);
        AllocationAssert.NoAllocation(() => _ = Collation.AreEqual(first, second), warmups: Warmups);
        AllocationAssert.NoAllocation(() => _ = Collation.StartsWith(first, "ab"), warmups: Warmups);
        AllocationAssert.NoAllocation(() => _ = Collation.EndsWith(first, "�"), warmups: Warmups);
        AllocationAssert.NoAllocation(() => _ = Collation.TryFind(first, "cd", out _, out _), warmups: Warmups);

        await Assert.That(Collation.Compare(first, second)).IsGreaterThan(0);
    }
}
