namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static AxisFixture;

/// <summary>
/// A descendant walk for the elements of one local name over a large subtree reads the element index of the document tree, which is
/// built once enough walks asked for it. It delivers the same nodes in the same order as a walk over any other tree, before and after
/// the index exists, for every context node.
/// </summary>
public class AxisElementIndexTests
{
    // 10 * 11 + 8 element, text, and other rows below the root element: large enough for the index to apply to the root.
    private const int FanOut = 10;

    [Test]
    [Arguments("descendant::i")]
    [Arguments("descendant::b")]
    [Arguments("descendant::e")]
    [Arguments("descendant::p:e")]
    [Arguments("descendant::*:i")]
    [Arguments("descendant::nope")]
    [Arguments("descendant::*")]
    [Arguments("descendant::p:*")]
    [Arguments("descendant::text()")]
    public async Task Walk_DescendantsByName_DeliverWhatAWalkOverAnotherTreeDelivers(string step)
    {
        var parsed = Parse(step);
        await Assert.That(XPathNodeTest.TryFromSyntax(parsed.Test, parsed.Axis, Statics, out var test)).IsTrue();
        var tree = Build(FanOut);
        var forwarding = new ForwardingTree(tree);

        // The first walks read the rows; the later ones the index. All of them agree.
        for (var round = 0; round < 4; round++)
        {
            for (var row = 0; row < tree.Count; row++)
            {
                var context = new XPathNode(new XmlNode(tree, row));
                var expected = Walk(forwarding.Map(context), test, forwarding);

                await Assert.That(Walk(context, test, tree)).IsEqualTo(expected);
            }
        }
    }

    [Test]
    public async Task Walk_ConcurrentWalks_AgreeWhileTheIndexIsBuilt()
    {
        var tree = Build(FanOut);
        var forwarding = new ForwardingTree(tree);
        var parsed = Parse("descendant::i");
        _ = XPathNodeTest.TryFromSyntax(parsed.Test, parsed.Axis, Statics, out var test);
        var expected = Walk(forwarding.Map(new XPathNode(tree.Document)), test, forwarding);
        var results = new string[32];

        await Task.WhenAll(
            Enumerable
                .Range(0, results.Length)
                .Select(n => Task.Run(() => results[n] = Walk(new XPathNode(tree.Document), test, tree)))
        );

        await Assert.That(results).All().Satisfy(result => result.IsEqualTo(expected));
    }

    [Test]
    public async Task MoveNext_IndexedWalkAfterTheEnd_StaysAtTheEnd()
    {
        var tree = Build(FanOut);
        var parsed = Parse("descendant::i");
        _ = XPathNodeTest.TryFromSyntax(parsed.Test, parsed.Axis, Statics, out var test);
        var document = new XPathNode(tree.Document);
        _ = Walk(document, test, tree);
        _ = Walk(document, test, tree);

        var iterator = new XPathAxisIterator(document, parsed.Axis, test.Bind(tree.Names));
        while (iterator.MoveNext()) { }

        await Assert.That(iterator.MoveNext()).IsFalse();
        await Assert.That(iterator.MoveNext()).IsFalse();
    }

    private static string Walk(XPathNode context, XPathNodeTest test, XmlTree tree)
    {
        var iterator = new XPathAxisIterator(context, Axis.Descendant, test.Bind(tree.Names));
        var text = new StringBuilder();
        while (iterator.MoveNext())
        {
            _ = text.Append(Label(iterator.Current)).Append('#').Append(iterator.Current.Node.Index).Append(' ');
        }

        return text.ToString();
    }
}
