namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Functions.Regex;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;

/// <summary>
/// The errors of the regular expression functions with a minimal expression each: <c>FORX0001</c> (flags), <c>FORX0002</c> (pattern),
/// <c>FORX0003</c> (the pattern matches the zero-length string), and <c>FORX0004</c> (replacement string); and the engine code of a
/// pattern or a search that exceeds a limit.
/// </summary>
public class RegexErrorTests
{
    [Test]
    [Arguments("matches('a', 'a', 'z')")]
    [Arguments("matches('a', 'a', 'I')")]
    [Arguments("matches('a', 'a', 'sq')")]
    [Arguments("matches('a', 'a', ' ')")]
    [Arguments("replace('a', 'a', 'b', 'z')")]
    [Arguments("tokenize('a', 'a', 'z')")]
    [Arguments("matches('a', '(', 'z')")]
    public async Task AnInvalidFlag_IsFORX0001(string expression) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo("error:FORX0001");

    [Test]
    public async Task TheFlagQ_OfTheLaterEditions_IsAnInvalidFlag() =>
        await Assert.That(ApiFixture.Run("matches('a.b', 'a.b', 'q')")).IsEqualTo("error:FORX0001");

    [Test]
    [Arguments("matches('a', '(')")]
    [Arguments("matches('a', '[')")]
    [Arguments("matches('a', 'a{2,1}')")]
    [Arguments("matches('a', '(?=a)')")]
    [Arguments("matches('a', '\\1')")]
    [Arguments("matches('a', '\\p{Foo}')")]
    [Arguments("matches('a', '*')")]
    [Arguments("replace('a', '(', 'b')")]
    [Arguments("tokenize('a', '(')")]
    public async Task AnInvalidPattern_IsFORX0002(string expression) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo("error:FORX0002");

    [Test]
    [Arguments("replace('abc', 'x*', 'y')")]
    [Arguments("replace('abc', '^', 'y')")]
    [Arguments("replace('', 'x*', 'y')")]
    [Arguments("replace('abc', '(a)?', 'y')")]
    [Arguments("tokenize('abc', 'x*')")]
    [Arguments("tokenize('', 'x*')")]
    [Arguments("tokenize('abc', '$')")]
    [Arguments("replace('abc', 'x*', '$')")]
    public async Task APatternThatMatchesTheZeroLengthString_IsFORX0003InReplaceAndTokenize(string expression) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo("error:FORX0003");

    [Test]
    [Arguments("matches('abc', 'x*')", "true")]
    [Arguments("matches('', '^$')", "true")]
    public async Task APatternThatMatchesTheZeroLengthString_IsNoErrorInMatches(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("replace('abc', 'b', '$')")]
    [Arguments("replace('abc', 'b', '$x')")]
    [Arguments("replace('abc', 'b', 'a$')")]
    [Arguments("replace('abc', 'b', '\\')")]
    [Arguments("replace('abc', 'b', '\\x')")]
    [Arguments("replace('abc', 'b', 'a\\')")]
    [Arguments("replace('abc', 'x', '$')")]
    [Arguments("replace('', 'x', '\\')")]
    public async Task AnInvalidReplacementString_IsFORX0004_AlsoWhenNothingMatches(string expression) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo("error:FORX0004");

    [Test]
    public async Task ASearchThatReachesItsStepCeiling_IsTheEngineCodeOfTheLimit()
    {
        var input = new string('a', 60);

        await Assert
            .That(ApiFixture.Run($"matches('{input}', '(a+)+\\1b')"))
            .IsEqualTo("error:" + RegexErrors.LimitExceeded);
        await Assert
            .That(ApiFixture.Run($"replace('{input}', '(a+)+\\1b', 'x')"))
            .IsEqualTo("error:" + RegexErrors.LimitExceeded);
        await Assert
            .That(ApiFixture.Run($"tokenize('{input}', '(a+)+\\1b')"))
            .IsEqualTo("error:" + RegexErrors.LimitExceeded);
    }

    [Test]
    public async Task APatternThatExceedsTheProgramLimit_IsTheEngineCodeOfTheLimit()
    {
        var pattern = "(a{1000}){1000}";

        await Assert.That(ApiFixture.Run($"matches('a', '{pattern}')")).IsEqualTo("error:" + RegexErrors.LimitExceeded);
    }
}
