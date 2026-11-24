namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.AnalyzeString;

using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:analyze-string</c> (Functions and Operators 3.1, 5.6.5): the result tree for no match, nested groups, adjacent matches, and the
/// empty input; the string value of the result; the errors; and the XPath 3.0 version gate.
/// </summary>
public class AnalyzeStringTests
{
    private const string Root = "analyze-string-result";

    [Test]
    [Arguments("analyze-string('abc', 'x')", "<analyze-string-result><non-match>abc</non-match></analyze-string-result>")]
    [Arguments(
        "analyze-string('abc', 'b')",
        "<analyze-string-result><non-match>a</non-match><match>b</match><non-match>c</non-match></analyze-string-result>"
    )]
    [Arguments(
        "analyze-string('aa', 'a')",
        "<analyze-string-result><match>a</match><match>a</match></analyze-string-result>"
    )]
    [Arguments(
        "analyze-string('abcd', '(a(b)c)')",
        "<analyze-string-result><match><group nr=\"1\">a<group nr=\"2\">b</group>c</group></match><non-match>d</non-match></analyze-string-result>"
    )]
    [Arguments(
        "analyze-string('ab', '(a)(b)')",
        "<analyze-string-result><match><group nr=\"1\">a</group><group nr=\"2\">b</group></match></analyze-string-result>"
    )]
    [Arguments(
        "analyze-string('xaby', 'x(a)?(c)?(b)y')",
        "<analyze-string-result><match>x<group nr=\"1\">a</group><group nr=\"3\">b</group>y</match></analyze-string-result>"
    )]
    [Arguments(
        "analyze-string('ab', 'a()b')",
        "<analyze-string-result><match>a<group nr=\"1\"></group>b</match></analyze-string-result>"
    )]
    [Arguments("analyze-string('', 'a')", "<analyze-string-result></analyze-string-result>")]
    [Arguments("analyze-string((), 'a')", "<analyze-string-result></analyze-string-result>")]
    [Arguments(
        "analyze-string('aXb', 'x', 'i')",
        "<analyze-string-result><non-match>a</non-match><match>X</match><non-match>b</non-match></analyze-string-result>"
    )]
    public async Task Result_IsTheTreeOfMatchesNonMatchesAndGroups(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("string(analyze-string('The quick brown fox', '\w+'))", "The quick brown fox")]
    [Arguments("string(analyze-string('abcd', '(a(b)c)'))", "abcd")]
    [Arguments("string(analyze-string('', 'a'))", "")]
    [Arguments("count(analyze-string('a-b-c', '-')/*)", "5")]
    [Arguments("analyze-string('a-b', '-')/*/name()", "non-match|match|non-match")]
    [Arguments("analyze-string('ab', '(a)(b)')/*/*/@nr/string()", "1|2")]
    [Arguments("namespace-uri(analyze-string('a', 'a'))", ApiFixture.FunctionNamespace)]
    public async Task Result_CanBeNavigated_AndKeepsTheInput(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression, statics: Statics())).IsEqualTo(expected);

    [Test]
    [Arguments("analyze-string('abc', 'a*')", "error:FORX0003")]
    [Arguments("analyze-string('', 'a*')", "error:FORX0003")]
    [Arguments("analyze-string('abc', 'b', 'z')", "error:FORX0001")]
    [Arguments("analyze-string('abc', '(')", "error:FORX0002")]
    [Arguments("analyze-string('abc')", "error:XPST0017")]
    [Arguments("analyze-string('abc', 'b', 'i', 'x')", "error:XPST0017")]
    public async Task Errors_AreReported(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression, statics: Statics())).IsEqualTo(expected);

    [Test]
    public async Task InXPath20_TheFunctionIsUnknown() =>
        await Assert
            .That(ApiFixture.Run("analyze-string('abc', 'b')", statics: Statics(XPathVersion.V20)))
            .IsEqualTo("error:XPST0017");

    private static XPathStaticContext Statics(XPathVersion version = XPathVersion.V31) =>
        ApiFixture.Builder().WithVersion(version).Build();

    private static string Run(string expression)
    {
        var compiled = ApiFixture.Compile(expression, Statics());
        using var context = new XPathEvaluationContext();
        var outcome = compiled.TryEvaluate(context);
        return outcome.Status == XPathEvaluationStatus.Ok && outcome.Value.Count == 1 && outcome.Value[0].TryGetNode(out var node)
            ? Render(node.Node)
            : "error:" + outcome.Diagnostic.Code;
    }

    private static string Render(XmlNode node)
    {
        var text = new StringBuilder();
        Append(text, node);
        return text.ToString();
    }

    private static void Append(StringBuilder text, XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Text)
        {
            _ = text.Append(node.StringValue);
            return;
        }

        var name = node.Tree!.Names.GetString(node.Name.Local);
        _ = text.Append('<').Append(name);
        for (var attribute = node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            _ = text.Append(' ')
                .Append(node.Tree.Names.GetString(attribute.Name.Local))
                .Append("=\"")
                .Append(attribute.StringValue)
                .Append('"');
        }

        _ = text.Append('>');
        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            Append(text, child);
        }

        _ = text.Append("</").Append(name).Append('>');
    }
}
