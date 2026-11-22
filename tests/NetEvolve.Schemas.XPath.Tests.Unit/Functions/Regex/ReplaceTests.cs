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
/// <c>fn:replace</c> (Functions and Operators 1.0, 7.6.3) through the public API: the examples of the specification, the replacement
/// string with <c>$N</c> and its escapes, the flags, the empty input, and the string length limit.
/// </summary>
public class ReplaceTests
{
    [Test]
    [Arguments("replace('abracadabra', 'bra', '*')", "a*cada*")]
    [Arguments("replace('abracadabra', 'a.*a', '*')", "*")]
    [Arguments("replace('abracadabra', 'a.*?a', '*')", "*c*bra")]
    [Arguments("replace('abracadabra', 'a', '')", "brcdbr")]
    [Arguments("replace('abracadabra', 'a(.)', 'a$1$1')", "abbraccaddabbra")]
    [Arguments("replace('AAAA', 'A+', 'b')", "b")]
    [Arguments("replace('AAAA', 'A+?', 'b')", "bbbb")]
    [Arguments("replace('darted', '^(.*?)d(.*)$', '$1c$2')", "carted")]
    [Arguments("replace('abc', '(a)(b)(c)', '$3$2$1')", "cba")]
    [Arguments("replace('abc', 'x', 'y')", "abc")]
    public async Task Examples_AndEdges(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("replace('abc', 'b', '[$0]')", "a[b]c")]
    [Arguments("replace('abc', '(b)', '[$1]')", "a[b]c")]
    [Arguments("replace('abc', '(b)', '[$2]')", "a[]c")]
    [Arguments("replace('abc', '(b)', '[$10]')", "a[b0]c")]
    [Arguments("replace('abc', '(b)', '[$11]')", "a[b1]c")]
    [Arguments("replace('abc', '(b)', '[$01]')", "a[b]c")]
    [Arguments("replace('abcdefghijk', '(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)', '[$10]')", "[j]k")]
    [Arguments("replace('abcdefghijk', '(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)', '[$11]')", "[a1]k")]
    [Arguments("replace('ab', '(x)?b', '[$1]')", "a[]")]
    [Arguments("replace('abc', 'b', '\\$')", "a$c")]
    [Arguments("replace('abc', 'b', '\\\\')", "a\\c")]
    [Arguments("replace('abc', '(b)', '\\\\$1')", "a\\bc")]
    [Arguments("replace('abc', '(b)', '\\$1')", "a$1c")]
    public async Task ReplacementString_ResolvesGroupReferencesAndEscapes(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("replace('AbC', 'b', 'x', 'i')", "AxC")]
    [Arguments("replace('AbC', 'b', 'x')", "AbC")]
    [Arguments("replace('ab', 'a b', 'X', 'x')", "X")]
    [Arguments("replace('a\nb', 'a.b', 'X', 's')", "X")]
    [Arguments("replace('a\nb', '^b$', 'X', 'm')", "a\nX")]
    public async Task Flags_ReachTheMatcher(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("replace('', 'a', 'b')", "")]
    [Arguments("replace((), 'a', 'b')", "")]
    [Arguments("replace('a\U0001F600b', '.', 'x')", "xxx")]
    [Arguments("replace('a\U0001F600b', '\U0001F600', '-')", "a-b")]
    public async Task EmptyInput_AndCodePoints(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task AResultAboveTheStringLengthLimit_RaisesTheStringLengthError()
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxStringLength(10) };
        var replace = SequencesHarness.Resolve("replace", 3);

        var raised = Raised(() => replace.Call(context, [Str("aaaaaaaa"), Str("a"), Str("bbb")]));

        await Assert.That(raised).IsEqualTo(XPathLimitErrors.StringLengthExceeded.ToString());
        await Assert.That(replace.Call(context, [Str("aaa"), Str("a"), Str("bbb")])[0].TryGetString(out var fits)).IsTrue();
        await Assert.That(fits).IsEqualTo("bbbbbbbbb");
    }

    [Test]
    [Arguments("replace('a', 'a')")]
    [Arguments("replace('a', 'a', 'b', 'i', 'x')")]
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
