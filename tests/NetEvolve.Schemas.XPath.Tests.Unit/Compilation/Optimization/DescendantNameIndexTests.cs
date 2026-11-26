namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static OptimizationFixture;

/// <summary>
/// <c>//name</c> and <c>//@name</c> over a large document read the name indexes of the tree, which are built once enough paths
/// asked for them. Every path gives what its expanded form gives, before and after the indexes exist and for names that share an
/// index bucket.
/// </summary>
public class DescendantNameIndexTests
{
    private static readonly string Xml = Document(3, 40);

    [Test]
    [Arguments("//x", "(/descendant-or-self::node())/child::x")]
    [Arguments("//y", "(/descendant-or-self::node())/child::y")]
    [Arguments("//*:y", "(/descendant-or-self::node())/child::*:y")]
    [Arguments("//nope", "(/descendant-or-self::node())/child::nope")]
    [Arguments("/r//x", "/r/(descendant-or-self::node())/child::x")]
    [Arguments("/r/x[1]//y", "/r/x[1]/(descendant-or-self::node())/child::y")]
    [Arguments("//@id", "(/descendant-or-self::node())/@id")]
    [Arguments("//@k", "(/descendant-or-self::node())/@k")]
    [Arguments("//@*:k", "(/descendant-or-self::node())/@*:k")]
    [Arguments("//@nope", "(/descendant-or-self::node())/@nope")]
    [Arguments("/r/x[2]//@id", "/r/x[2]/(descendant-or-self::node())/@id")]
    [Arguments("//y//@k", "//y/(descendant-or-self::node())/@k")]
    public async Task Evaluate_NamedDescendants_SelectWhatTheExpandedFormSelects(string shortcut, string expanded)
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());

        // The first evaluations read the rows, the later ones the indexes.
        for (var round = 0; round < 4; round++)
        {
            var actual = Evaluate(
                "string-join(for $n in "
                    + shortcut
                    + " return concat(name($n), count($n/preceding::*), count($n/ancestor::*)), ',')",
                tree.Document
            );
            var wanted = Evaluate(
                "string-join(for $n in "
                    + expanded
                    + " return concat(name($n), count($n/preceding::*), count($n/ancestor::*)), ',')",
                tree.Document
            );

            await Assert.That(actual).IsEqualTo(wanted);
        }
    }

    [Test]
    public async Task Evaluate_NamesThatShareABucket_AreNotMixedUp()
    {
        var names = Enumerable.Range(0, 200).Select(n => "e" + n).ToArray();
        var builder = new StringBuilder("<r>");
        foreach (var name in names)
        {
            _ = builder.Append("<g ").Append(name).Append("='v'><").Append(name).Append("/></g>");
        }

        var tree = XmlDocumentLoader.Load((builder.Append("</r>").ToString()).AsSpan());
        for (var round = 0; round < 4; round++)
        {
            foreach (var name in names)
            {
                await Assert.That(Evaluate($"string(count(//{name}))", tree.Document)).IsEqualTo("1");
                await Assert.That(Evaluate($"string(count(//@{name}))", tree.Document)).IsEqualTo("1");
                await Assert.That(Evaluate($"string(local-name(//{name}/..))", tree.Document)).IsEqualTo("g");
            }
        }
    }

    [Test]
    public async Task Evaluate_SmallSubtreeAndNonElementContext_AreUnchanged()
    {
        var tree = XmlDocumentLoader.Load(Xml.AsSpan());
        var element = tree.Document.FirstChild.FirstChild; // the first x: a subtree below the index threshold
        var attribute = tree.Document.FirstChild.FirstAttribute;
        for (var round = 0; round < 4; round++)
        {
            await Assert.That(Evaluate("string(count(.//y))", element)).IsEqualTo("3");
            await Assert.That(Evaluate("string(count(.//@k))", element)).IsEqualTo("4");
            await Assert.That(Evaluate("string(count(//y))", attribute)).IsEqualTo("120");
            await Assert.That(Evaluate("string(count(descendant::y))", attribute)).IsEqualTo("0");
            await Assert.That(Evaluate("string(count(descendant-or-self::node()/@k))", attribute)).IsEqualTo("0");
        }
    }

    private static string Document(int children, int items)
    {
        var builder = new StringBuilder("<r id='0'>");
        for (var item = 0; item < items; item++)
        {
            _ = builder.Append("<x id='").Append(item).Append("' k='v'>");
            for (var child = 0; child < children; child++)
            {
                _ = builder.Append("<y id='").Append(item).Append('_').Append(child).Append("' k='w'>t</y>");
            }

            _ = builder.Append("</x>");
        }

        return builder.Append("</r>").ToString();
    }

    private static string Evaluate(string expression, XmlNode node)
    {
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(node)));
        var item = Compile(expression, XPathOptimizations.All).Evaluate(context)[0];

        return item.TryGetString(out var text) ? text ?? string.Empty : throw new InvalidOperationException(expression);
    }
}
