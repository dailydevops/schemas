namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Operations;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

public class RegexCaptureTests
{
    [Test]
    [Arguments("((a)(b))(c)", "abc", "0,3|0,2|0,1|1,1|2,1")]
    [Arguments("(a(b(c)))", "abc", "0,3|0,3|1,2|2,1")]
    [Arguments("(?:a)(b)", "ab", "0,2|1,1")]
    [Arguments("(a)(?:(b))", "ab", "0,2|0,1|1,1")]
    [Arguments("a+", "baab", "1,2")]
    public async Task Groups_AreNumberedByTheirOpeningParenthesis(string pattern, string input, string expected) =>
        await Assert.That(RegexOps.Matches(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("(a)|(b)", "b", "0,1|-|0,1")]
    [Arguments("(a)|(b)", "a", "0,1|0,1|-")]
    [Arguments("(a)?b", "b", "0,1|-")]
    [Arguments("(a)(b)?", "a", "0,1|0,1|-")]
    [Arguments("(a)|(b)|(c)", "c", "0,1|-|-|0,1")]
    public async Task Group_ThatTookNoPartInTheMatch_IsReportedAsUnmatched(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexOps.Matches(pattern, input)).IsEqualTo(expected);

    [Test]
    public async Task Group_ThatMatchedTheEmptyString_IsMatchedWithLengthZero()
    {
        await Assert.That(RegexOps.Matches("(a*)b", "b")).IsEqualTo("0,1|0,0");
        await Assert.That(RegexOps.IsGroupMatched("(a*)b", "b", 1)).IsTrue();
        await Assert.That(RegexOps.IsGroupMatched("(a)?b", "b", 1)).IsFalse();
    }

    [Test]
    public async Task UnmatchedGroup_ReadsAsEmptyText()
    {
        await Assert.That(RegexOps.GroupText("(a)|(b)", "b", 1)).IsEqualTo("");
        await Assert.That(RegexOps.GroupText("(a)|(b)", "b", 2)).IsEqualTo("b");
        await Assert.That(RegexOps.GroupText("(a)|(b)", "b", 0)).IsEqualTo("b");
    }

    [Test]
    public async Task GroupNumbers_OutOfRange_AreRejected()
    {
        await Assert.That(() => RegexOps.GroupText("(a)", "a", -1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexOps.GroupText("(a)", "a", 2)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexOps.IsGroupMatched("(a)", "a", 2)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments("(a|ab)(c|bcd)(d*)", "abcd", "0,4|0,1|1,3|4,0")]
    [Arguments("(a+?)(a*)", "aaa", "0,3|0,1|1,2")]
    [Arguments("(a+)(a*)", "aaa", "0,3|0,3|3,0")]
    [Arguments("(a|b)+", "ab", "0,2|1,1")]
    [Arguments("(a)*", "aaa", "0,3|2,1;3,0|-")]
    [Arguments("(?:(a)|b)+", "ab", "0,2|0,1")]
    [Arguments("(a){2}", "aaaa", "0,2|1,1;2,2|3,1")]
    [Arguments("(a?)+", "aa", "0,2|1,1;2,0|2,0")]
    public async Task Groups_FollowThePreferenceOrderOfThePattern(string pattern, string input, string expected) =>
        await Assert.That(RegexOps.Matches(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("(a)\\1", "xaa", "1,2|1,1")]
    [Arguments("(a)?b\\1", "b", "0,1|-")]
    [Arguments("(\\w)\\1", "abccd", "2,2|2,1")]
    [Arguments("(a)(b)\\2\\1", "abba", "0,4|0,1|1,1")]
    [Arguments("(a)\\1", "ab", "")]
    [Arguments("(a)|b\\1", "b", "0,1|-")]
    [Arguments("(a)\\1", "aaaa", "0,2|0,1;2,2|2,1")]
    public async Task Groups_OfAPatternWithABackReference_AreReported(string pattern, string input, string expected) =>
        await Assert.That(RegexOps.Matches(pattern, input)).IsEqualTo(expected);

    [Test]
    public async Task BackReference_WithIgnoreCase_ComparesByFolding() =>
        await Assert.That(RegexOps.Matches("(a)\\1", "aA", RegexOptions.IgnoreCase)).IsEqualTo("0,2|0,1");

    [Test]
    [Arguments("^(a)", "aa", "0,1|0,1")]
    [Arguments("(a)$", "aa", "1,1|1,1")]
    [Arguments("^(a)$", "a", "0,1|0,1")]
    [Arguments("^(a)", "ba", "")]
    [Arguments("(a)$", "ab", "")]
    public async Task Groups_RespectTheAnchors(string pattern, string input, string expected) =>
        await Assert.That(RegexOps.Matches(pattern, input)).IsEqualTo(expected);

    [Test]
    public async Task Groups_RespectTheFlags()
    {
        await Assert.That(RegexOps.Matches("^(a)", "a\na", RegexOptions.MultiLine)).IsEqualTo("0,1|0,1;2,1|2,1");
        await Assert.That(RegexOps.Matches("(a)$", "a\na", RegexOptions.MultiLine)).IsEqualTo("0,1|0,1;2,1|2,1");
        await Assert.That(RegexOps.Matches("(A)", "a", RegexOptions.IgnoreCase)).IsEqualTo("0,1|0,1");
        await Assert.That(RegexOps.Matches("(a.b)", "a\nb", RegexOptions.DotAll)).IsEqualTo("0,3|0,3");
        await Assert.That(RegexOps.Matches("(a.b)", "a\nb")).IsEqualTo("");
        await Assert.That(RegexOps.Matches("( a )( b )", "ab", RegexOptions.FreeSpacing)).IsEqualTo("0,2|0,1|1,1");
    }

    [Test]
    [Arguments("a", "", "")]
    [Arguments("a", "xyz", "")]
    [Arguments("a", "a", "0,1")]
    [Arguments("a", "xa", "1,1")]
    [Arguments("a", "ax", "0,1")]
    [Arguments("a", "aXa", "0,1;2,1")]
    [Arguments("a", "aaa", "0,1;1,1;2,1")]
    [Arguments("(a)", "aaa", "0,1|0,1;1,1|1,1;2,1|2,1")]
    [Arguments("a+", "aabaaa", "0,2;3,3")]
    [Arguments("^a", "aaa", "0,1")]
    [Arguments("a*", "baab", "0,0;1,2;3,0;4,0")]
    [Arguments("(a*)", "baab", "0,0|0,0;1,2|1,2;3,0|3,0;4,0|4,0")]
    [Arguments("x*", "", "0,0")]
    [Arguments("x*", "\U0001F600", "0,0;2,0")]
    [Arguments(".", "\U0001F600a", "0,2;2,1")]
    public async Task Matches_AreEnumeratedLeftToRightWithoutOverlap(string pattern, string input, string expected) =>
        await Assert.That(RegexOps.Matches(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("a", "bab")]
    [Arguments("(a)", "bab")]
    [Arguments("(a)\\1", "baab")]
    [Arguments("(a|ab)(c|bcd)(d*)", "xabcdx")]
    [Arguments("((?:b?|c)*)", "bcc")]
    [Arguments("(?:b?|c)*", "bcc")]
    [Arguments("(a+?)(a*)", "aaa")]
    [Arguments("(^|x)a", "xa")]
    [Arguments("(a|b)*c", "abbc")]
    [Arguments("(a)(b)?", "xyz")]
    public async Task FirstMatch_HasTheBoundsOfTheSearchOfTheMatcher(string pattern, string input)
    {
        var (result, index, length) = XPathRegex.Search(pattern, input);
        var first = FirstMatch(pattern, input);

        await Assert.That(first.Found).IsEqualTo(result == RegexMatchResult.Match);
        await Assert.That(first.Index).IsEqualTo(index);
        await Assert.That(first.Length).IsEqualTo(length);
    }

    [Test]
    public async Task EmptyBodyLoop_KeepsTheMatchOfTheAutomaton() =>
        await Assert.That(RegexOps.Matches("((?:b?|c)*)", "bcc")).IsEqualTo("0,3|0,3;3,0|3,0");

    [Test]
    [Arguments("a*b")]
    [Arguments("(a+)+b")]
    [Arguments("(a+)+b\\1")]
    public async Task Matches_ThatReachTheAutomatonCeiling_EndAborted(string pattern)
    {
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(50).WithMaxBacktrackSteps(50);

        await Assert.That(RegexOps.Matches(pattern, RegexOps.Repeat("a", 100), limits: limits)).IsEqualTo("!");
    }

    [Test]
    public async Task Matches_AfterAMatch_CanEndAborted()
    {
        var operations = RegexOps.Create("(a)|b");
        var text = "a" + RegexOps.Repeat("x", 100);
        var aborted = false;
        for (var ceiling = 1; ceiling < 400 && !aborted; ceiling++)
        {
            var matches = RegexOps.Matches(operations, text, RegexStepLimits.Default.WithMaxNfaSteps(ceiling));
            aborted = matches.StartsWith("0,1|0,1", StringComparison.Ordinal) && matches.AsSpan().IndexOf('!') >= 0;
        }

        await Assert.That(aborted).IsTrue();
    }

    [Test]
    public async Task Enumerator_AfterItsEnd_StaysAtTheEnd()
    {
        var results = EnumerateTwiceBeyondTheEnd();

        await Assert.That(results).IsEqualTo("TFFFF");
    }

    [Test]
    public async Task Enumerator_CanBeUsedByForEach() =>
        await Assert.That(CountWithForEach("(a)(b)?", "ab a")).IsEqualTo((2, 4));

    [Test]
    public async Task Operations_ReportTheNumberOfCapturingGroups()
    {
        await Assert.That(RegexOps.Create("a").GroupCount).IsEqualTo(0);
        await Assert.That(RegexOps.Create("(?:a)").GroupCount).IsEqualTo(0);
        await Assert.That(RegexOps.Create("(a)((b))").GroupCount).IsEqualTo(3);
        await Assert.That(RegexOps.Create("(a)\\1").GroupCount).IsEqualTo(1);
    }

    [Test]
    [Arguments("a(b)c", "xabcx")]
    [Arguments("a+", "xxaaxx")]
    [Arguments("(a)\\1", "xaax")]
    [Arguments("(a)|(b)", "xabx")]
    public async Task Iterating_OverASpan_AllocatesZeroBytes(string pattern, string unit)
    {
        var operations = RegexOps.Create(pattern);
        ArgumentNullException.ThrowIfNull(unit);
        var input = RegexOps.Repeat(unit, 2_500);
        var count = 0;
        var characters = 0;

        var allocated = AllocationAssert.Measure(() =>
        {
            count = 0;
            characters = 0;
            foreach (var match in operations.EnumerateMatches(input))
            {
                count++;
                characters += match.GetGroupText(0).Length + match.GroupCount;
            }
        });

        await Assert.That(allocated).IsEqualTo(0L);
        await Assert.That(count).IsGreaterThan(1_000);
        await Assert.That(characters).IsGreaterThan(0);
    }

    private static (bool Found, int Index, int Length) FirstMatch(string pattern, string input)
    {
        var matches = RegexOps.Create(pattern).EnumerateMatches(input);
        try
        {
            return matches.MoveNext() ? (true, matches.Current.Index, matches.Current.Length) : (false, -1, 0);
        }
        finally
        {
            matches.Dispose();
        }
    }

    private static string EnumerateTwiceBeyondTheEnd()
    {
        var matches = RegexOps.Create("(a)").EnumerateMatches("a");
        var text =
            (matches.MoveNext() ? "T" : "F") + (matches.MoveNext() ? "T" : "F") + (matches.MoveNext() ? "T" : "F");
        matches.Dispose();
        matches.Dispose();
        var disposed = matches.MoveNext() ? "T" : "F";
        var none = default(RegexMatchEnumerator);
        return text + disposed + (none.MoveNext() ? "T" : "F");
    }

    private static (int Matches, int Groups) CountWithForEach(string pattern, string input)
    {
        var count = 0;
        var groups = 0;
        foreach (var match in RegexOps.Create(pattern).EnumerateMatches(input))
        {
            count++;
            groups += match.GroupCount;
        }

        return (count, groups);
    }
}
