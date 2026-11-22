namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.XPath.Functions.Regex;

/// <summary>
/// The match-segment iterator alone: the order of the segments, their kinds, and the ranges of the capture groups. A segment is
/// <c>N</c> (non-matching) or <c>M</c> (matching) with its index and length; a group is <c>offset,length</c> or <c>-</c>.
/// </summary>
public class MatchSegmentEnumeratorTests
{
    [Test]
    [Arguments("b+", "aaa", "N0,3")]
    [Arguments("b+", "a", "N0,1")]
    public async Task NoMatch_IsOneNonMatchingSegmentOfTheWholeInput(string pattern, string input, string expected) =>
        await Assert.That(RegexHarness.Segments(pattern, input)).IsEqualTo(expected);

    [Test]
    public async Task EmptyInput_WithoutAMatch_HasNoSegment() =>
        await Assert.That(RegexHarness.Segments("b+", string.Empty)).IsEqualTo(string.Empty);

    [Test]
    [Arguments("a", "aa", "M0,1;M1,1")]
    [Arguments("a", "aab", "M0,1;M1,1;N2,1")]
    [Arguments("a", "baa", "N0,1;M1,1;M2,1")]
    [Arguments("ab", "ababab", "M0,2;M2,2;M4,2")]
    [Arguments("ab", "abxab", "M0,2;N2,1;M3,2")]
    public async Task AdjacentMatches_HaveNoNonMatchingSegmentBetweenThem(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexHarness.Segments(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("a", "abc", "M0,1;N1,2")]
    [Arguments("a", "bca", "N0,2;M2,1")]
    [Arguments("a", "abca", "M0,1;N1,2;M3,1")]
    [Arguments("a", "a", "M0,1")]
    public async Task MatchAtTheStartOrTheEnd_HasNoEmptyNonMatchingSegment(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexHarness.Segments(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("((a)(b))c", "xabcx", "N0,1;M1,3|1,2|1,1|2,1;N4,1")]
    [Arguments("(a(b(c)))", "abc", "M0,3|0,3|1,2|2,1")]
    [Arguments("(?:a)(b)", "ab", "M0,2|1,1")]
    public async Task NestedGroups_AreNumberedByTheirOpeningParenthesis(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexHarness.Segments(pattern, input)).IsEqualTo(expected);

    // Limitation of the regex engine (#198): a group keeps the bounds of the last iteration of its loop in which it took part.
    [Test]
    [Arguments("((a)|(b))+", "ab", "M0,2|1,1|0,1|1,1")]
    [Arguments("(a|(b))+", "ba", "M0,2|1,1|0,1")]
    public async Task GroupInALoop_KeepsTheLastIterationInWhichItTookPart(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexHarness.Segments(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("(a)?b", "xb", "N0,1;M1,1|-")]
    [Arguments("(a)|(b)", "ab", "M0,1|0,1|-;M1,1|-|1,1")]
    [Arguments("(a*)b", "b", "M0,1|0,0")]
    public async Task UnmatchedOptionalGroup_IsReportedAsUnmatched_AndAnEmptyGroupAsMatched(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexHarness.Segments(pattern, input)).IsEqualTo(expected);

    [Test]
    public async Task UnmatchedGroup_ReadsAsEmptyText_AndAMatchedOneAsItsText()
    {
        var (kind, text, whole, unmatched, matched, tookPart, more) = ReadFirstMatch("(a)|(b)", "b");

        await Assert.That(kind).IsEqualTo(MatchSegmentKind.Match);
        await Assert.That(text).IsEqualTo("b");
        await Assert.That(whole).IsEqualTo("b");
        await Assert.That(unmatched).IsEqualTo(string.Empty);
        await Assert.That(tookPart).IsFalse();
        await Assert.That(matched).IsEqualTo("b");
        await Assert.That(more).IsFalse();
    }

    [Test]
    public async Task NonMatchingSegment_HasItsTextAndNoGroups()
    {
        var (kind, text, groups, hasGroup) = ReadFirstNonMatch("(b)", "abc");

        await Assert.That(kind).IsEqualTo(MatchSegmentKind.NonMatch);
        await Assert.That(text).IsEqualTo("a");
        await Assert.That(groups).IsEqualTo(0);
        await Assert.That(hasGroup).IsFalse();
    }

    [Test]
    [Arguments("x*", "ab", "M0,0;N0,1;M1,0;N1,1;M2,0")]
    [Arguments("x*", "\U0001F600", "M0,0;N0,2;M2,0")]
    [Arguments("a*", "baab", "M0,0;N0,1;M1,2;M3,0;N3,1;M4,0")]
    public async Task EmptyMatches_AreSegmentsOfLengthZero_AndTheIterationEnds(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexHarness.Segments(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("A", "a", RegexOptions.IgnoreCase, "M0,1")]
    [Arguments("a b", "ab", RegexOptions.FreeSpacing, "M0,2")]
    [Arguments("a.b", "a\nb", RegexOptions.DotAll, "M0,3")]
    public async Task Flags_ReachTheMatcher(string pattern, string input, RegexOptions options, string expected) =>
        await Assert.That(RegexHarness.Segments(pattern, input, options)).IsEqualTo(expected);

    [Test]
    public async Task AStepCeiling_EndsTheIteration_AndIsReported()
    {
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(1);

        await Assert.That(RegexHarness.Segments("a+b", new string('a', 100), RegexOptions.None, limits)).IsEqualTo("!");
    }

    [Test]
    public async Task Dispose_CalledTwice_HasNoEffect() => await Assert.That(DisposeTwiceThenMove()).IsFalse();

    private static (
        MatchSegmentKind Kind,
        string Text,
        string Whole,
        string Unmatched,
        string Matched,
        bool TookPart,
        bool More
    ) ReadFirstMatch(string pattern, string input)
    {
        var segments = new MatchSegmentEnumerator(RegexHarness.Compile(pattern), input.AsSpan());
        try
        {
            _ = segments.MoveNext();
            var segment = segments.Current;
            return (
                segment.Kind,
                segment.Text.ToString(),
                segment.GetGroupText(0).ToString(),
                segment.GetGroupText(1).ToString(),
                segment.GetGroupText(2).ToString(),
                segment.TryGetGroup(1, out _),
                segments.MoveNext()
            );
        }
        finally
        {
            segments.Dispose();
        }
    }

    private static (MatchSegmentKind Kind, string Text, int Groups, bool HasGroup) ReadFirstNonMatch(
        string pattern,
        string input
    )
    {
        var segments = new MatchSegmentEnumerator(RegexHarness.Compile(pattern), input.AsSpan());
        try
        {
            _ = segments.MoveNext();
            var segment = segments.Current;
            return (segment.Kind, segment.Text.ToString(), segment.GroupCount, segment.TryGetGroup(0, out _));
        }
        finally
        {
            segments.Dispose();
        }
    }

    private static bool DisposeTwiceThenMove()
    {
        var segments = new MatchSegmentEnumerator(RegexHarness.Compile("a"), "aa".AsSpan());
        _ = segments.MoveNext();

        segments.Dispose();
        segments.Dispose();

        return segments.MoveNext();
    }
}
