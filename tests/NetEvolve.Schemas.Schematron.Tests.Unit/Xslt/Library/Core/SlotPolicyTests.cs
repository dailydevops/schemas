namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// The rows of the slot policy table of design #150 (EP-13) that belong to #285: <c>current()</c> reads the current item slot, which a
/// predicate does not change (the focus does change), and which is empty in the body of a stylesheet function (G6).
/// </summary>
public class SlotPolicyTests
{
    [Test]
    public async Task CurrentItem_InRuleContextAndTest_IsMatchedNode()
    {
        var nodes = Nodes(3);
        using var context = new XPathEvaluationContext();
        context.Host.CurrentItem = XdmItem.FromNode(nodes[0]);

        // A predicate of the rule context: the focus is a candidate node, one of three.
        context.SetContextItem(XdmItem.FromNode(nodes[1]), 1, 3);
        var inContext = CurrentIs(Scope(), context, nodes[0]);

        // A predicate of an assert test: the focus is a node of the path, in another position.
        context.SetContextItem(XdmItem.FromNode(nodes[2]), 3, 3);
        var inTest = CurrentIs(Scope(), context, nodes[0]);

        await Assert.That(inContext).IsTrue();
        await Assert.That(inTest).IsTrue();
    }

    [Test]
    public async Task Function_Body_CurrentItem_IsAbsent()
    {
        var nodes = Nodes(2);
        using var context = new XPathEvaluationContext();
        context.Host.CurrentItem = XdmItem.FromNode(nodes[0]);
        context.SetContextItem(XdmItem.FromNode(nodes[1]));
        var inRule = RunIn(Scope(), context, "current");

        // Entering a function body clears the slot (the run saves and restores it); the call then has no current item.
        context.Host.CurrentItem = null;
        var inFunction = RunIn(Scope(), context, "current");
        context.Reset();
        var withoutFocus = RunIn(Scope(), context, "current");

        await Assert.That(inRule).IsEqualTo("node");
        await Assert.That(inFunction).IsEqualTo("error:XPDY0002");
        await Assert.That(withoutFocus).IsEqualTo("error:XPDY0002");
    }
}
