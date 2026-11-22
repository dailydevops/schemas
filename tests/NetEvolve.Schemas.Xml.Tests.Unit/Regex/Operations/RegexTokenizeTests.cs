namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Operations;

using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;

public class RegexTokenizeTests
{
    [Test]
    [Arguments(",", "a,b,c", "[a][b][c]")]
    [Arguments(",", "a", "[a]")]
    [Arguments(",", "abc", "[abc]")]
    [Arguments("\\s+", "a  b \t c", "[a][b][c]")]
    [Arguments("abc", "xabcyabcz", "[x][y][z]")]
    [Arguments("b+", "abbbcbd", "[a][c][d]")]
    public async Task Tokenize_ReturnsTheTextBetweenTheSeparators(string pattern, string input, string expected) =>
        await Assert.That(RegexOps.Tokenize(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments(",", ",a", "[][a]")]
    [Arguments(",", "a,", "[a][]")]
    [Arguments(",", ",a,", "[][a][]")]
    [Arguments(",", ",", "[][]")]
    [Arguments(",", ",,", "[][][]")]
    [Arguments(",", "a,,b", "[a][][b]")]
    [Arguments(",", "a,,,b", "[a][][][b]")]
    [Arguments("ab", "abab", "[][][]")]
    public async Task Tokenize_KeepsEmptyTokensAtTheEndsAndBetweenAdjacentSeparators(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(RegexOps.Tokenize(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments(",")]
    [Arguments("a")]
    [Arguments("\\s*x")]
    public async Task Tokenize_OfTheEmptyInput_IsTheEmptySequence(string pattern) =>
        await Assert.That(RegexOps.Tokenize(pattern, "")).IsEqualTo("");

    [Test]
    [Arguments("a*")]
    [Arguments("(a)?")]
    [Arguments("")]
    [Arguments("^")]
    [Arguments("$")]
    [Arguments("a|")]
    public async Task Tokenize_WithAPatternThatMatchesTheEmptyString_IsTheCodedError(string pattern)
    {
        await Assert.That(RegexOps.Tokenize(pattern, "aaa")).IsEqualTo("ZeroLengthMatch");
        await Assert.That(RegexOps.Tokenize(pattern, "")).IsEqualTo("ZeroLengthMatch");
    }

    [Test]
    public async Task Tokenize_WithTheFlags_UsesThem()
    {
        await Assert.That(RegexOps.Tokenize("B", "abAbc", RegexOptions.IgnoreCase)).IsEqualTo("[a][A][c]");
        await Assert.That(RegexOps.Tokenize("^a", "a\nab", RegexOptions.MultiLine)).IsEqualTo("[][\n][b]");
    }

    [Test]
    public async Task Tokenize_WithGroups_IgnoresThem() =>
        await Assert.That(RegexOps.Tokenize("(,)(;)?", "a,b;c")).IsEqualTo("[a][b;c]");

    [Test]
    public async Task Tokenize_WithABackReference_Works() =>
        await Assert.That(RegexOps.Tokenize("(,)\\1", "a,,b,c")).IsEqualTo("[a][b,c]");

    [Test]
    public async Task Tokenize_OfALongInput_ReturnsEveryToken()
    {
        var input = RegexOps.Repeat("ab,", 3_000);
        var operations = RegexOps.Create(",");
        var status = operations.TryTokenize(input, null, out var tokens);
        var count = 0;
        var empty = 0;
        while (tokens.MoveNext())
        {
            count++;
            empty += tokens.Current.Length == 0 ? 1 : 0;
        }

        tokens.Dispose();

        await Assert.That(status).IsEqualTo(RegexOperationStatus.Success);
        await Assert.That(count).IsEqualTo(3_001);
        await Assert.That(empty).IsEqualTo(1);
    }

    [Test]
    public async Task Tokenize_ThatReachesTheCeiling_IsAborted()
    {
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(50);

        await Assert.That(RegexOps.Tokenize("a*b", RegexOps.Repeat("a", 100), limits: limits)).IsEqualTo("!");
    }

    [Test]
    public async Task Tokens_AfterTheEnd_StayAtTheEnd()
    {
        var operations = RegexOps.Create(",");
        _ = operations.TryTokenize("a", null, out var tokens);
        var first = tokens.MoveNext();
        var second = tokens.MoveNext();
        tokens.Dispose();

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
    }

    [Test]
    public async Task Tokens_CanBeUsedByForEach() => await Assert.That(CountWithForEach(",", "a,b,,c")).IsEqualTo(4);

    private static int CountWithForEach(string pattern, string input)
    {
        _ = RegexOps.Create(pattern).TryTokenize(input, null, out var tokens);
        var count = 0;
        using (tokens)
        {
            foreach (var token in tokens)
            {
                count += token.Length >= 0 ? 1 : 0;
            }
        }

        return count;
    }
}
