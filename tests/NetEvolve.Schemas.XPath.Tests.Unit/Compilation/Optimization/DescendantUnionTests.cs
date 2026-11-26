namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static OptimizationFixture;

/// <summary>
/// <c>//(a|b)</c> is the union of <c>descendant::a</c> and <c>descendant::b</c> (XPath 2.0, 3.2.4): one walk per operand instead of one
/// union per node of the document. An operand that is not a child step, or that can select by position, keeps the expanded form.
/// </summary>
public class DescendantUnionTests
{
    private const string Xml =
        "<r><x id='1'>a<x id='2'>b</x><y id='3'/></x><x id='4'><y id='5'><x id='6'>a</x></y><z id='9'/></x><!--c--><x id='8'>a</x></r>";

    [Test]
    [Arguments("//(x|y)", 1)]
    [Arguments("//(x|y)/@id", 2)]
    [Arguments("//(x union y)", 1)]
    [Arguments("//(x|y|z)", 1)]
    [Arguments("//(x|(y|z))", 1)]
    [Arguments("//((x|y)|z)", 1)]
    [Arguments("//(x[@id]|y)", 1)]
    [Arguments("//(x[. = 'a']|y[not(z)])", 1)]
    [Arguments("//(x|text())", 1)]
    [Arguments("//(x|comment())", 1)]
    [Arguments("/r//(x|y)", 2)]
    [Arguments("//(x|y)/(z|y)", 2)]
    [Arguments("//(x[1]|y)", 2)]
    [Arguments("//(x|y[last()])", 2)]
    [Arguments("//(x|@id)", 2)]
    [Arguments("//(x|y/z)", 2)]
    [Arguments("//(x intersect y)", 2)]
    [Arguments("//(x except y)", 2)]
    [Arguments("//(x, y)", 2)]
    public async Task Compile_DescendantOrSelfThenUnion_IsOneStep(string expression, int steps)
    {
        var root = Compile(expression, XPathOptimizations.All).Root as PathNode;

        await Assert.That(root).IsNotNull();
        await Assert.That(root!.Steps.Count).IsEqualTo(steps);
    }

    [Test]
    [Arguments("//(x|y)", "(/descendant-or-self::node())/(x|y)")]
    [Arguments("//(y|x)", "(/descendant-or-self::node())/(y|x)")]
    [Arguments("//(x|x)", "(/descendant-or-self::node())/(x|x)")]
    [Arguments("//(x|y|z)", "(/descendant-or-self::node())/(x|y|z)")]
    [Arguments("//(x|(y|z))", "(/descendant-or-self::node())/(x|(y|z))")]
    [Arguments("//(x[@id = '2']|y)", "(/descendant-or-self::node())/(x[@id = '2']|y)")]
    [Arguments("//(x[y]|y[@id])", "(/descendant-or-self::node())/(x[y]|y[@id])")]
    [Arguments("//(x|text())", "(/descendant-or-self::node())/(x|text())")]
    [Arguments("//(x|comment())", "(/descendant-or-self::node())/(x|comment())")]
    [Arguments("//(x|y)/@id", "(/descendant-or-self::node())/(x|y)/@id")]
    [Arguments("//(x|y)/(z|y)", "(/descendant-or-self::node())/(x|y)/(z|y)")]
    [Arguments("//(x[1]|y)", "(/descendant-or-self::node())/(x[1]|y)")]
    [Arguments("//(x|y[last()])", "(/descendant-or-self::node())/(x|y[last()])")]
    [Arguments("/r//(x|y)", "/r/(descendant-or-self::node())/(x|y)")]
    [Arguments("//x//(x|y)", "//x/(descendant-or-self::node())/(x|y)")]
    [Arguments("//(nope|none)", "(/descendant-or-self::node())/(nope|none)")]
    public async Task Evaluate_DescendantUnion_SelectsWhatTheExpandedFormSelects(string shortcut, string expanded)
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());
        var actual = Evaluate(
            "string-join(for $n in " + shortcut + " return string(($n/@id, name($n))[1]), ',')",
            tree.Document
        );
        var wanted = Evaluate(
            "string-join(for $n in " + expanded + " return string(($n/@id, name($n))[1]), ',')",
            tree.Document
        );

        await Assert.That(actual).IsEqualTo(wanted);
    }

    [Test]
    public async Task Evaluate_UnionFromAnElement_SelectsWhatTheExpandedFormSelects()
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());
        var element = tree.Document.FirstChild.FirstChild; // the first x
        var actual = Evaluate("string-join(for $n in .//(x|y) return string($n/@id), ',')", element);
        var wanted = Evaluate(
            "string-join(for $n in ./(descendant-or-self::node())/(x|y) return string($n/@id), ',')",
            element
        );

        await Assert.That(actual).IsEqualTo(wanted);
        await Assert.That(actual).IsEqualTo("2,3");
    }

    private static string Evaluate(string expression, XmlNode node)
    {
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(node)));
        var item = Compile(expression, XPathOptimizations.All).Evaluate(context)[0];

        return item.TryGetString(out var text) ? text ?? string.Empty : throw new InvalidOperationException(expression);
    }
}
