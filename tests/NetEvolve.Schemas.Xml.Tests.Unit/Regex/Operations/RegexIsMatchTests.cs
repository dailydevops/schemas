namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Operations;

using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

public class RegexIsMatchTests
{
    public const RegexOptions Flags = RegexOptions.None;

    private static RegexMatchResult Enumerated(RegexOperations operations, string input, RegexStepLimits? limits)
    {
        var matches = operations.EnumerateMatches(input, limits);
        try
        {
            return matches.MoveNext() ? RegexMatchResult.Match
                : matches.IsAborted ? RegexMatchResult.Aborted
                : RegexMatchResult.NoMatch;
        }
        finally
        {
            matches.Dispose();
        }
    }

    [Test]
    [Arguments("abc", "xxabcxx", Flags)]
    [Arguments("abc", "xxabxx", Flags)]
    [Arguments("^(A|B|C)$", "B", Flags)]
    [Arguments("^(A|B|C)$", "BB", Flags)]
    [Arguments("^(A|B|C)$", "", Flags)]
    [Arguments("(a)(b)?", "xa", Flags)]
    [Arguments("((a)|(b))+", "zzabab", Flags)]
    [Arguments("a*", "", Flags)]
    [Arguments("a*", "bbb", Flags)]
    [Arguments("()", "", Flags)]
    [Arguments("^$", "", Flags)]
    [Arguments("^$", "a", Flags)]
    [Arguments("a.b", "a\nb", Flags)]
    [Arguments("a.b", "a\nb", RegexOptions.DotAll)]
    [Arguments("^b$", "a\nb\nc", Flags)]
    [Arguments("^b$", "a\nb\nc", RegexOptions.MultiLine)]
    [Arguments("(ABC)", "xabcx", Flags)]
    [Arguments("(ABC)", "xabcx", RegexOptions.IgnoreCase)]
    [Arguments("( a b ) c", "abc", RegexOptions.FreeSpacing)]
    [Arguments("( a b ) c", "abc", Flags)]
    [Arguments("(a+?)b", "aaab", Flags)]
    [Arguments("(a)\\1", "xaa", Flags)]
    [Arguments("(a)\\1", "xab", Flags)]
    [Arguments("(a|b)\\1", "xbb", RegexOptions.IgnoreCase)]
    [Arguments("[a-z]+(\\d{2,3})", "ab1", Flags)]
    [Arguments("[a-z]+(\\d{2,3})", "ab123", Flags)]
    [Arguments("(\\p{L}+)@", "me\ud83d\ude00@", Flags)]
    public async Task IsMatch_EqualsTheFirstEnumeratedMatch(string pattern, string input, RegexOptions flags)
    {
        var operations = RegexOps.Create(pattern, flags);

        var result = operations.IsMatch(input);

        await Assert.That(result).IsEqualTo(Enumerated(operations, input, null));
    }

    [Test]
    [Arguments("(a|b)*c", "abababababababababab")]
    [Arguments("(a|b)*c", "abababababababababc")]
    [Arguments("x*(y|z)*q", "yzyzyzyzyzyzyzyz")]
    public async Task IsMatch_WithATightStepLimit_AnswersLikeTheEnumeration(string pattern, string input)
    {
        var operations = RegexOps.Create(pattern);

        for (var steps = 1; steps < 2_000; steps *= 3)
        {
            var limits = RegexStepLimits.Default.WithMaxNfaSteps(steps);
            var aborted = operations.IsMatch(input, limits) == RegexMatchResult.Aborted;

            // The pattern without capture instructions takes fewer steps, so it may finish where the enumeration aborts.
            await Assert.That(aborted && Enumerated(operations, input, limits) != RegexMatchResult.Aborted).IsFalse();
        }
    }

    [Test]
    public async Task IsMatch_OfAPatternWithAGroup_AbortsAtTheStepCeiling()
    {
        var operations = RegexOps.Create("(a|b)*c");

        var result = operations.IsMatch(RegexOps.Repeat("ab", 500), RegexStepLimits.Default.WithMaxNfaSteps(10));

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
    }

    [Test]
    public async Task IsMatch_OfABackReferencePattern_AbortsAtTheBacktrackCeiling()
    {
        var operations = RegexOps.Create("(a*)*\\1b");

        var result = operations.IsMatch(RegexOps.Repeat("a", 40), RegexStepLimits.Default.WithMaxBacktrackSteps(1_000));

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
    }

    [Test]
    public async Task IsMatch_CalledTwice_ReusesTheProgram()
    {
        var operations = RegexOps.Create("(a)(b)");

        await Assert.That(operations.IsMatch("ab")).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(operations.IsMatch("ba")).IsEqualTo(RegexMatchResult.NoMatch);
    }
}
