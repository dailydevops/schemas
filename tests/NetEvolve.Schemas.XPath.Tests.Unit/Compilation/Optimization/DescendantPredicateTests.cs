namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

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
/// <c>//x[P]</c> is <c>descendant::x[P]</c> when <c>P</c> never selects by position (XPath 2.0, 3.2.4): one walk over the descendants
/// instead of one child step per node. A predicate that can select by position keeps the expanded form.
/// </summary>
public class DescendantPredicateTests
{
    private const string Xml =
        "<r><x id='1'>a<x id='2'>b</x><y id='3'/></x><x id='4'><y id='5'><x id='6'>a</x></y><x id='7'/></x><x id='8'>a</x></r>";

    [Test]
    [Arguments("//x[y]")]
    [Arguments("//x[@id]")]
    [Arguments("//x[. = 'a']")]
    [Arguments("//x[not(y)]")]
    [Arguments("//x[y and @id]")]
    [Arguments("//x[@id = '2' or @id = '7']")]
    [Arguments("//x[string-length(.) > 0]")]
    [Arguments("//x[exists(x)][@id]")]
    public async Task Compile_PredicateThatNeverSelectsByPosition_IsOneDescendantStep(string expression)
    {
        var root = Compile(expression, XPathOptimizations.All).Root as PathNode;

        await Assert.That(root).IsNotNull();
        await Assert.That(root!.Steps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_PredicateInTheMiddleOfAPath_IsOneDescendantStep()
    {
        var root = Compile("/r//x[y]", XPathOptimizations.All).Root as PathNode;

        await Assert.That(root).IsNotNull();
        await Assert.That(root!.Steps.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments("//x[1]")]
    [Arguments("//x[last()]")]
    [Arguments("//x[position() = 2]")]
    [Arguments("//x[position() = last()]")]
    [Arguments("//x[1 + 1]")]
    [Arguments("//x[count(y)]")]
    [Arguments("//x[y/string-length(@id)]")]
    [Arguments("//x[@id][2]")]
    [Arguments("//x[(position())]")]
    public async Task Compile_PredicateThatMaySelectByPosition_KeepsTheExpandedForm(string expression)
    {
        var root = Compile(expression, XPathOptimizations.All).Root as PathNode;

        await Assert.That(root).IsNotNull();
        await Assert.That(root!.Steps.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments("y")]
    [Arguments("@id")]
    [Arguments(". = 'a'")]
    [Arguments("not(y)")]
    [Arguments("y and @id")]
    [Arguments("@id = '2' or @id = '7'")]
    [Arguments("string-length(.) > 0")]
    [Arguments("exists(x)][@id")]
    [Arguments("1")]
    [Arguments("last()")]
    [Arguments("position() = 2")]
    [Arguments("position() = last()")]
    [Arguments("count(y)")]
    [Arguments("@id][2")]
    [Arguments("(position())")]
    public async Task Evaluate_DescendantStep_SelectsWhatTheExpandedFormSelects(string predicate)
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());
        var shortcut = Evaluate("string-join(//x[" + predicate + "]/@id, ',')", tree.Document);
        var expanded = Evaluate(
            "string-join((/descendant-or-self::node())/child::x[" + predicate + "]/@id, ',')",
            tree.Document
        );

        await Assert.That(shortcut).IsEqualTo(expanded);
    }

    private static string Evaluate(string expression, XmlNode document)
    {
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(document)));
        return Compile(expression, XPathOptimizations.All).Evaluate(context)[0].ToString() ?? string.Empty;
    }
}
