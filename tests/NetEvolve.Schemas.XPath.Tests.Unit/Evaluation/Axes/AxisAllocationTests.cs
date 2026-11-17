namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static AxisFixture;

public class AxisAllocationTests
{
    // The fixture plus 100 elements with 100 children each: more than 10,000 nodes.
    private static readonly XmlDocumentTree Big = Build(fanOut: 100);

    [Test]
    public async Task Walk_DescendantOverTenThousandNodes_AllocatesZeroBytes()
    {
        var root = new XPathNode(Big.Document);
        var matcher = XPathNodeTest.AnyNode.Bind(Big.Names);
        var count = 0;

        AllocationAssert.NoAllocation(() =>
        {
            count = 0;
            var iterator = new XPathAxisIterator(root, Axis.Descendant, matcher);
            while (iterator.MoveNext())
            {
                count++;
            }
        });

        await Assert.That(count).IsGreaterThanOrEqualTo(10_000);
    }

    [Test]
    [Arguments("Child")]
    [Arguments("DescendantOrSelf")]
    [Arguments("Attribute")]
    [Arguments("Self")]
    [Arguments("FollowingSibling")]
    [Arguments("Following")]
    [Arguments("Parent")]
    [Arguments("Ancestor")]
    [Arguments("AncestorOrSelf")]
    [Arguments("PrecedingSibling")]
    [Arguments("Preceding")]
    public async Task Walk_EveryAxis_AllocatesZeroBytes(string axisName)
    {
        var axis = Enum.Parse<Axis>(axisName);

        // A fan-out element deep in the tree: siblings on both sides and a large preceding side.
        var context = new XPathNode(Big.Document).FirstChild().FirstChild();
        for (var n = 0; n < 20; n++)
        {
            context = context.NextSibling();
        }

        var matcher = XPathNodeTest.AnyNode.Bind(Big.Names);
        var count = 0;

        AllocationAssert.NoAllocation(() =>
        {
            count = 0;
            var iterator = new XPathAxisIterator(context, axis, matcher);
            while (iterator.MoveNext())
            {
                count++;
            }
        });

        await Assert.That(count).IsGreaterThanOrEqualTo(axis == Axis.Attribute ? 0 : 1);
    }
}
