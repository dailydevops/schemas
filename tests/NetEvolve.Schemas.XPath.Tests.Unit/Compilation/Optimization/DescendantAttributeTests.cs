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
/// <c>//@a</c> is one walk over the attributes of the descendants: the node list of <c>descendant-or-self::node()</c> and one attribute
/// step per node are not needed (XPath 2.0, 3.2.4). A predicate on the attribute step keeps the expanded form.
/// </summary>
public class DescendantAttributeTests
{
    private const string Xml =
        "<r id='0' k='v'><x id='1' p:a='z' xmlns:p='urn:p'>a<x id='2'>b</x><y id='3'/></x><x id='4'><y id='5' k='w'><x id='6'>a</x></y><x id='7'/></x><!--c--><x id='8'>a</x></r>";

    [Test]
    [Arguments("//@id", 1)]
    [Arguments("//@*", 1)]
    [Arguments("//@*:id", 1)]
    [Arguments("//attribute::id", 1)]
    [Arguments("/r//@id", 2)]
    [Arguments("/r/x//@id", 3)]
    [Arguments("//x//@id", 2)]
    [Arguments("//@id/..", 2)]
    [Arguments("//@id[. = '1']", 2)]
    [Arguments("//@id[1]", 2)]
    [Arguments("//node()/@id", 2)]
    [Arguments("//self::x/@id", 3)]
    public async Task Compile_DescendantOrSelfThenAttribute_IsOneStep(string expression, int steps)
    {
        var root = Compile(expression, XPathOptimizations.All).Root as PathNode;

        await Assert.That(root).IsNotNull();
        await Assert.That(root!.Steps.Count).IsEqualTo(steps);
    }

    [Test]
    [Arguments("//@id", "(/descendant-or-self::node())/@id")]
    [Arguments("//@*", "(/descendant-or-self::node())/@*")]
    [Arguments("//@*:a", "(/descendant-or-self::node())/@*:a")]
    [Arguments("//@k", "(/descendant-or-self::node())/@k")]
    [Arguments("//@nope", "(/descendant-or-self::node())/@nope")]
    [Arguments("/r//@id", "/r/(descendant-or-self::node())/@id")]
    [Arguments("//x//@id", "//x/(descendant-or-self::node())/@id")]
    [Arguments("/r/x//@id", "/r/x/(descendant-or-self::node())/@id")]
    [Arguments("//y//@*", "//y/(descendant-or-self::node())/@*")]
    [Arguments("//@id/..", "(/descendant-or-self::node())/@id/..")]
    public async Task Evaluate_DescendantAttributeStep_SelectsWhatTheExpandedFormSelects(
        string shortcut,
        string expanded
    )
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());
        var actual = Evaluate("string-join(for $n in " + shortcut + " return string($n), ',')", tree.Document);
        var wanted = Evaluate("string-join(for $n in " + expanded + " return string($n), ',')", tree.Document);

        await Assert.That(actual).IsEqualTo(wanted);
    }

    [Test]
    public async Task Evaluate_AttributesOfTheDescendants_AreInDocumentOrder()
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());

        await Assert
            .That(Evaluate("string-join(for $n in //@id return string($n), ',')", tree.Document))
            .IsEqualTo("0,1,2,3,4,5,6,7,8");
        await Assert
            .That(Evaluate("string-join(for $n in //x//@id return string($n), ',')", tree.Document))
            .IsEqualTo("1,2,3,4,5,6,7,8");
    }

    [Test]
    public async Task Evaluate_RelativeFromAnElement_SelectsWhatTheExpandedFormSelects()
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());
        var element = tree.Document.FirstChild; // r
        var shortcut = Evaluate("string-join(for $n in x//@id return string($n), ',')", element);
        var expanded = Evaluate(
            "string-join(for $n in x/(descendant-or-self::node())/@id return string($n), ',')",
            element
        );

        await Assert.That(shortcut).IsEqualTo(expanded);
        await Assert.That(shortcut).IsEqualTo("1,2,3,4,5,6,7,8");
    }

    [Test]
    public async Task Evaluate_FromAnAttributeOrTextNode_IsEmpty()
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());
        var attribute = tree.Document.FirstChild.FirstAttribute;
        var text = tree.Document.FirstChild.FirstChild.FirstChild;

        await Assert.That(Evaluate("string(count(descendant-or-self::node()/@*))", attribute)).IsEqualTo("0");
        await Assert.That(Evaluate("string(count(descendant-or-self::node()/@*))", text)).IsEqualTo("0");
    }

    private static string Evaluate(string expression, XmlNode node)
    {
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(node)));
        var item = Compile(expression, XPathOptimizations.All).Evaluate(context)[0];

        return item.TryGetString(out var text) ? text ?? string.Empty : throw new InvalidOperationException(expression);
    }
}
