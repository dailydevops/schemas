namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;

using System;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static AxisFixture;

/// <summary>
/// The child, descendant, and attribute walks of a document tree read its rows; a walk over any other tree goes from node to node.
/// Both deliver the same nodes in the same order for every context node and node test.
/// </summary>
public class AxisRowWalkTests
{
    private static readonly string[] Contexts =
    [
        "doc",
        "a",
        "id",
        "lang",
        "b1",
        "t1",
        "c",
        "pi",
        "b2",
        "t2",
        "i",
        "x",
        "t3",
        "e",
    ];

    [Test]
    [Arguments("child::node()")]
    [Arguments("child::*")]
    [Arguments("child::b")]
    [Arguments("child::p:e")]
    [Arguments("child::*:e")]
    [Arguments("child::p:*")]
    [Arguments("child::nope")]
    [Arguments("child::text()")]
    [Arguments("child::comment()")]
    [Arguments("child::processing-instruction()")]
    [Arguments("child::processing-instruction(pi)")]
    [Arguments("child::processing-instruction(other)")]
    [Arguments("child::document-node()")]
    [Arguments("descendant::node()")]
    [Arguments("descendant::*")]
    [Arguments("descendant::b")]
    [Arguments("descendant::i")]
    [Arguments("descendant::text()")]
    [Arguments("descendant::comment()")]
    [Arguments("descendant::processing-instruction(pi)")]
    [Arguments("descendant::document-node()")]
    [Arguments("descendant-or-self::node()")]
    [Arguments("descendant-or-self::*")]
    [Arguments("descendant-or-self::b")]
    [Arguments("descendant-or-self::a")]
    [Arguments("descendant-or-self::text()")]
    [Arguments("descendant-or-self::document-node()")]
    [Arguments("descendant-or-self::document-node(element(a))")]
    [Arguments("descendant-or-self::document-node(element(b))")]
    [Arguments("attribute::node()")]
    [Arguments("attribute::*")]
    [Arguments("attribute::id")]
    [Arguments("attribute::p:lang")]
    [Arguments("attribute::*:lang")]
    [Arguments("attribute::nope")]
    [Arguments("attribute::text()")]
    public async Task Walk_DocumentTree_DeliversWhatAWalkOverAnotherTreeDelivers(string step)
    {
        var parsed = Parse(step);
        await Assert.That(XPathNodeTest.TryFromSyntax(parsed.Test, parsed.Axis, Statics, out var test)).IsTrue();
        var forwarding = new ForwardingTree((XmlDocumentTree)Node("doc").Tree!);
        foreach (var key in Contexts)
        {
            var context = Node(key);
            var viaRows = Walk(context, parsed.Axis, test);
            var viaNodes = Walk(forwarding.Map(context), parsed.Axis, test);

            await Assert.That(viaRows).IsEqualTo(viaNodes);
        }
    }

    [Test]
    [Arguments("Child")]
    [Arguments("Descendant")]
    [Arguments("DescendantOrSelf")]
    [Arguments("Attribute")]
    public async Task MoveNext_AfterTheEnd_StaysAtTheEnd(string axisName)
    {
        var axis = Enum.Parse<Axis>(axisName);
        var context = Node("b1");
        var iterator = new XPathAxisIterator(context, axis, XPathNodeTest.AnyNode.Bind(context.Tree!.Names));
        while (iterator.MoveNext()) { }

        await Assert.That(iterator.MoveNext()).IsFalse();
        await Assert.That(iterator.MoveNext()).IsFalse();
    }

    private static string Walk(XPathNode context, Axis axis, XPathNodeTest test)
    {
        var iterator = new XPathAxisIterator(context, axis, test.Bind(context.Tree!.Names));
        var text = new StringBuilder();
        while (iterator.MoveNext())
        {
            _ = text.Append(Label(iterator.Current)).Append(' ');
        }

        return text.ToString();
    }
}
