namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// <c>current()</c> (XSLT 2.0, 16.6.1): the item that was the context item where the expression started, not the context item of a
/// predicate or of the right-hand side of a path step. The evaluator keeps the matched node in the current item slot (design #150
/// EP-13); a predicate changes the focus and leaves the slot as it is. The slot is empty in a function body (G6).
/// </summary>
public class CurrentTests
{
    private static string RunWith(XdmItem? focus, XdmItem? current)
    {
        using var context = new XPathEvaluationContext();
        if (focus is { } item)
        {
            context.SetContextItem(item);
        }

        context.Host.CurrentItem = current;
        return RunIn(Scope(), context, "current");
    }

    [Test]
    public async Task CurrentItem_InAPredicateOfAnAssertTest_IsTheNodeOfTheRule()
    {
        var nodes = Nodes(2);
        using var context = new XPathEvaluationContext();
        context.Host.CurrentItem = XdmItem.FromNode(nodes[0]);
        context.SetContextItem(XdmItem.FromNode(nodes[1]), 2, 3);

        var result = RunIn(Scope(), context, "current");

        await Assert.That(result).IsEqualTo("node");
        await Assert.That(CurrentIs(Scope(), context, nodes[0])).IsTrue();
        await Assert.That(CurrentIs(Scope(), context, nodes[1])).IsFalse();
    }

    [Test]
    public async Task CurrentItem_InAnOutermostExpression_IsTheContextItem()
    {
        var nodes = Nodes(1);
        var item = XdmItem.FromNode(nodes[0]);

        await Assert.That(RunWith(item, item)).IsEqualTo("node");
    }

    [Test]
    public async Task CurrentItem_IsNotFoundUnderAnXpathBinding_AndFoundUnderEveryXsltBinding()
    {
        await Assert.That(Run(Scope("xpath2"), "current")).IsEqualTo("notfound");
        await Assert.That(Run(Scope("xpath"), "current")).IsEqualTo("notfound");
        await Assert.That(Scope("xslt").IsAvailable(Fn("current"), 0)).IsTrue();
        await Assert.That(Scope("xslt1").IsAvailable(Fn("current"), 0)).IsTrue();
        await Assert.That(Scope("xslt2").IsAvailable(Fn("current"), 0)).IsTrue();
        await Assert.That(Scope("xslt3").IsAvailable(Fn("current"), 0)).IsTrue();
    }

    [Test]
    public async Task CurrentItem_WithAnArgument_IsNotFound() =>
        await Assert.That(Run(Scope(), "current", S("x"))).IsEqualTo("notfound");
}
