namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// <c>fn:tokenize</c> (Functions and Operators 1.0, 7.6.4) through the public API: the examples of the specification, the empty first,
/// last, and adjacent tokens, the empty input, and the sequence size limit. The tokens are joined by a bar.
/// </summary>
public class TokenizeTests
{
    [Test]
    [Arguments("tokenize('abracadabra', '(ab)|(a)')", "|r|c|d|r|")]
    [Arguments("tokenize('The quick brown fox', '\\s+')", "The|quick|brown|fox")]
    [Arguments("tokenize('1, 15, 24, 50', ',\\s*')", "1|15|24|50")]
    [Arguments("tokenize('1,15,,24,50,', ',')", "1|15||24|50|")]
    [Arguments("tokenize('Some unparsed <br> HTML <BR> text', '\\s*<br>\\s*', 'i')", "Some unparsed|HTML|text")]
    public async Task Examples(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("tokenize(',a', ',')", "|a")]
    [Arguments("tokenize('a,', ',')", "a|")]
    [Arguments("tokenize(',', ',')", "|")]
    [Arguments("tokenize('a,,b', ',')", "a||b")]
    [Arguments("tokenize(',,', ',')", "||")]
    [Arguments("tokenize('abc', 'x')", "abc")]
    [Arguments("tokenize('abc', 'c')", "ab|")]
    [Arguments("tokenize('abc', 'a')", "|bc")]
    public async Task LeadingTrailingAndAdjacentSeparators_GiveEmptyTokens(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("count(tokenize('', ','))", "0")]
    [Arguments("count(tokenize((), ','))", "0")]
    [Arguments("count(tokenize('a', ','))", "1")]
    [Arguments("count(tokenize('a,b', ','))", "2")]
    [Arguments("count(tokenize(',', ','))", "2")]
    public async Task EmptyInput_IsTheEmptySequence(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("tokenize('aXbxc', 'x', 'i')", "a|b|c")]
    [Arguments("tokenize('aXbxc', 'x')", "aXb|c")]
    [Arguments("tokenize('a b', 'a b', 'x')", "a b")]
    [Arguments("tokenize('a1b', '1', 'x')", "a|b")]
    [Arguments("tokenize('a\nb', '.', 's')", "|||")]
    [Arguments("tokenize('a\nb', '.')", "|\n|")]
    [Arguments("tokenize('a\nb', '^b', 'm')", "a\n|")]
    [Arguments("tokenize('a\U0001F600b', '\U0001F600')", "a|b")]
    public async Task Flags_AndCodePoints(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task MoreTokensThanTheSequenceSizeLimit_RaisesTheSequenceSizeError()
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxSequenceSize(3) };
        var tokenize = SequencesHarness.Resolve("tokenize", 2);

        var raised = Raised(() => tokenize.Call(context, [Str("a,b,c,d"), Str(",")]));

        await Assert.That(raised).IsEqualTo(XPathLimitErrors.SequenceSizeExceeded.ToString());
        await Assert.That(tokenize.Call(context, [Str("a,b,c"), Str(",")]).Count).IsEqualTo(3);
    }

    [Test]
    [Arguments("tokenize('a')")]
    [Arguments("tokenize('a', ',', 'i', 'x')")]
    public async Task AnotherNumberOfArguments_IsXPST0017(string expression) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo("error:XPST0017");

    private static string Raised(System.Action action)
    {
        try
        {
            action();
            return string.Empty;
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.QName.ToString();
        }
    }
}
