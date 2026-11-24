namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.AnalyzeString;

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:analyze-string</c> (Functions and Operators 3.1, 5.6.5): the result tree for no match, nested groups, adjacent matches, and the
/// empty input; the string value of the result; the errors; and the XPath 3.0 version gate. The function is called through the
/// default catalog, because the compiler enables XPath 2.0 only until the XPath 3.0 syntax lands.
/// </summary>
public class AnalyzeStringTests
{
    private const string ResultOpen = "<analyze-string-result>";
    private const string ResultClose = "</analyze-string-result>";

    [Test]
    [Arguments("abc", "x", "", ResultOpen + "<non-match>abc</non-match>" + ResultClose)]
    [Arguments(
        "abc",
        "b",
        "",
        ResultOpen + "<non-match>a</non-match><match>b</match><non-match>c</non-match>" + ResultClose
    )]
    [Arguments("aa", "a", "", ResultOpen + "<match>a</match><match>a</match>" + ResultClose)]
    [Arguments(
        "abcd",
        "(a(b)c)",
        "",
        ResultOpen
            + "<match><group nr=\"1\">a<group nr=\"2\">b</group>c</group></match><non-match>d</non-match>"
            + ResultClose
    )]
    [Arguments(
        "ab",
        "(a)(b)",
        "",
        ResultOpen + "<match><group nr=\"1\">a</group><group nr=\"2\">b</group></match>" + ResultClose
    )]
    [Arguments(
        "xaby",
        "x(a)?(c)?(b)y",
        "",
        ResultOpen + "<match>x<group nr=\"1\">a</group><group nr=\"3\">b</group>y</match>" + ResultClose
    )]
    [Arguments("ab", "a()b", "", ResultOpen + "<match>a<group nr=\"1\"></group>b</match>" + ResultClose)]
    [Arguments(
        "xabab",
        "(ab)",
        "",
        ResultOpen
            + "<non-match>x</non-match><match><group nr=\"1\">ab</group></match><match><group nr=\"1\">ab</group></match>"
            + ResultClose
    )]
    [Arguments("", "a", "", ResultOpen + ResultClose)]
    [Arguments(
        "aXb",
        "x",
        "i",
        ResultOpen + "<non-match>a</non-match><match>X</match><non-match>b</non-match>" + ResultClose
    )]
    public async Task Result_IsTheTreeOfMatchesNonMatchesAndGroups(
        string input,
        string pattern,
        string flags,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Call(Str(input), Str(pattern), Str(flags));

        await Assert.That(Render(result)).IsEqualTo(expected);
    }

    [Test]
    public async Task TwoArgumentForm_AndTheEmptySequence_AreTheSameAsTheirDefaults(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Render(Call(XdmSequence.Empty, Str("a"))))
            .IsEqualTo(Render(Call(Str(string.Empty), Str("a"), Str(string.Empty))));
        await Assert
            .That(Render(Call(Str("abc"), Str("b"))))
            .IsEqualTo(Render(Call(Str("abc"), Str("b"), Str(string.Empty))));
    }

    [Test]
    [Arguments("The quick brown fox", "[a-z]+")]
    [Arguments("abcd", "(a(b)c)")]
    [Arguments("a-b-c", "-")]
    [Arguments("", "a")]
    public async Task StringValueOfTheResult_IsTheInput(
        string input,
        string pattern,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Call(Str(input), Str(pattern), Str("i"));

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetNode(out var node)).IsTrue();
        await Assert.That(node.Node.StringValue).IsEqualTo(input);
    }

    [Test]
    public async Task TheResultElements_AreInTheFunctionNamespace(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = Call(Str("a"), Str("(a)"))[0].TryGetNode(out var root);
        var tree = root.Node.Tree!;
        var match = root.Node.FirstChild;
        var group = match.FirstChild;

        await Assert.That(tree.Names.GetString(root.Node.Name.NamespaceUri)).IsEqualTo(ApiFixture.FunctionNamespace);
        await Assert.That(tree.Names.GetString(match.Name.NamespaceUri)).IsEqualTo(ApiFixture.FunctionNamespace);
        await Assert.That(tree.Names.GetString(group.Name.NamespaceUri)).IsEqualTo(ApiFixture.FunctionNamespace);
        await Assert.That(group.FirstAttribute.Name.NamespaceUri.IsEmpty).IsTrue();
    }

    [Test]
    [Arguments("abc", "a*", "", "FORX0003")]
    [Arguments("", "a*", "", "FORX0003")]
    [Arguments("abc", "b", "z", "FORX0001")]
    [Arguments("abc", "(", "", "FORX0002")]
    public async Task Errors_AreReported(
        string input,
        string pattern,
        string flags,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var raised = string.Empty;
        try
        {
            _ = Call(Str(input), Str(pattern), Str(flags));
        }
        catch (XPathException failure)
        {
            raised = failure.Diagnostic.Code;
        }

        await Assert.That(raised).IsEqualTo(code);
    }

    [Test]
    public async Task Registry_ResolvesInXPath30AndLater_ButNotInXPath20(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(TryResolve(2, XPathVersion.V20, out _)).IsFalse();
        await Assert.That(TryResolve(2, XPathVersion.V30, out _)).IsTrue();
        await Assert.That(TryResolve(3, XPathVersion.V31, out _)).IsTrue();
        await Assert.That(TryResolve(1, XPathVersion.V31, out _)).IsFalse();
        await Assert.That(TryResolve(4, XPathVersion.V31, out _)).IsFalse();
    }

    private static XdmSequence Str(string value) => XdmSequence.Create(XdmItem.FromString(value));

    private static bool TryResolve(int arity, XPathVersion version, out XPathFunction? function) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(ApiFixture.FunctionNamespace, "analyze-string"),
            arity,
            XPathHostRequirements.None,
            out function
        );

    private static XdmSequence Call(params XdmSequence[] arguments)
    {
        _ = TryResolve(arguments.Length, XPathVersion.V31, out var function);
        using var context = new XPathEvaluationContext();
        return function!.Call(context, arguments);
    }

    private static string Render(XdmSequence result)
    {
        _ = result[0].TryGetNode(out var node);
        var text = new StringBuilder();
        Append(text, node.Node);
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
