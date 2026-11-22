namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;

/// <summary>
/// <c>fn:matches</c> (Functions and Operators 1.0, 7.6.2) through the public API: the examples of the specification, the four flags
/// alone and together, the empty sequence, and code points above the Basic Multilingual Plane.
/// </summary>
public class MatchesTests
{
    private const string Poem =
        "<poem author=\"Wilhelm Busch\">Kaum hat dies der Hahn gesehen,\nFängt er auch schon an zu krähen:\nKikeriki! Kikikerikih!!\nTak, tak, tak! - da kommen sie.</poem>";

    [Test]
    [Arguments("matches('abracadabra', 'bra')", "true")]
    [Arguments("matches('abracadabra', '^a.*a$')", "true")]
    [Arguments("matches('abracadabra', '^bra')", "false")]
    [Arguments("matches('abracadabra', '')", "true")]
    [Arguments("matches('', '')", "true")]
    [Arguments("matches('', 'a')", "false")]
    [Arguments("matches('abab', '(ab)\\1')", "true")]
    [Arguments("matches('abc', '(?:a)(b)')", "true")]
    [Arguments("matches('a', 'a|b')", "true")]
    [Arguments("matches('ab', 'a{2}')", "false")]
    public async Task Examples_AndEdges(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("matches(., 'Kaum.*krähen')", "false")]
    [Arguments("matches(., 'Kaum.*krähen', 's')", "true")]
    [Arguments("matches(., '^Kaum.*gesehen,$')", "false")]
    [Arguments("matches(., '^Kaum.*gesehen,$', 'm')", "true")]
    [Arguments("matches(., '^Kaum.*gesehen,$', 'ms')", "true")]
    [Arguments("matches(., '^Kaum.*gesehen,')", "true")]
    [Arguments("matches(., '^Fängt er auch')", "false")]
    [Arguments("matches(., '^Fängt er auch', 'm')", "true")]
    [Arguments("matches(., 'kiki', 'i')", "true")]
    [Arguments("matches(., 'kiki')", "false")]
    [Arguments("matches(., 'Kaum . hat', 'x')", "true")]
    [Arguments("matches(., 'Kaum hat', 'x')", "false")]
    [Arguments("matches(., 'Kaum\\shat', 'x')", "true")]
    public async Task ThePoemOfTheSpecification_WithFlags(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression, Poem)).IsEqualTo(expected);

    [Test]
    [Arguments("matches('ABC', 'abc', 'i')", "true")]
    [Arguments("matches('ABC', 'abc')", "false")]
    [Arguments("matches('ab', 'a b', 'x')", "true")]
    [Arguments("matches('ab', 'a b')", "false")]
    [Arguments("matches('a b', 'a[ ]b', 'x')", "true")]
    [Arguments("matches('a\nb', 'a.b')", "false")]
    [Arguments("matches('a\nb', 'a.b', 's')", "true")]
    [Arguments("matches('a\nb', '^b$')", "false")]
    [Arguments("matches('a\nb', '^b$', 'm')", "true")]
    [Arguments("matches('A\nB', '^b$', 'smix')", "true")]
    [Arguments("matches('ABC', 'abc', 'ii')", "true")]
    [Arguments("matches('ABC', 'abc', '')", "false")]
    public async Task Flags_AloneAndTogether(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("matches((), 'a')", "false")]
    [Arguments("matches((), '^$')", "true")]
    [Arguments("matches((), '')", "true")]
    public async Task TheEmptySequenceAsTheInput_IsTheZeroLengthString(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("matches('a\U0001F600b', 'a.b')", "true")]
    [Arguments("matches('a\U0001F600b', '^.{3}$')", "true")]
    [Arguments("matches('a\U0001F600b', '\U0001F600')", "true")]
    [Arguments("matches('a\U0001F600b', '[\U0001F600]b')", "true")]
    public async Task CodePointsAboveTheBasicMultilingualPlane_AreOneCharacter(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task TheInputOfAnotherAtomicType_IsXPTY0004() =>
        await Assert.That(ApiFixture.Run("matches(1, 'a')")).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task ThePatternAsTheEmptySequence_IsXPTY0004() =>
        await Assert.That(ApiFixture.Run("matches('a', ())")).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("matches('a')")]
    [Arguments("matches('a', 'a', 'i', 'x')")]
    public async Task AnotherNumberOfArguments_IsXPST0017(string expression) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo("error:XPST0017");
}
