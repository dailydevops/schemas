namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:position</c> and <c>fn:last</c> (Functions and Operators 1.0, 16.1 and 16.2): the position and the size of the focus.</summary>
public class ContextFunctionTests
{
    [Test]
    [Arguments(1, 1)]
    [Arguments(1, 9)]
    [Arguments(3, 7)]
    [Arguments(7, 7)]
    public async Task PositionAndLast_ReadThePositionAndTheSizeOfTheFocus(int position, int size)
    {
        var item = XdmItem.FromNode(NodesHarness.Nodes["a"]);

        var actualPosition = NodesHarness.RunWith(c => c.SetContextItem(item, position, size), "position");
        var actualSize = NodesHarness.RunWith(c => c.SetContextItem(item, position, size), "last");

        await Assert.That(actualPosition).IsEqualTo("integer:" + position);
        await Assert.That(actualSize).IsEqualTo("integer:" + size);
    }

    [Test]
    public async Task PositionAndLast_WithAnAtomicContextItem_ReadTheFocusToo()
    {
        var item = XdmItem.FromInteger(42);

        await Assert.That(NodesHarness.RunWith(c => c.SetContextItem(item, 2, 5), "position")).IsEqualTo("integer:2");
        await Assert.That(NodesHarness.RunWith(c => c.SetContextItem(item, 2, 5), "last")).IsEqualTo("integer:5");
    }

    [Test]
    [Arguments("position")]
    [Arguments("last")]
    public async Task PositionAndLast_WithoutContextItem_RaiseXPDY0002(string function) =>
        await Assert.That(NodesHarness.Run(function)).IsEqualTo("error:XPDY0002");

    [Test]
    [Arguments("position")]
    [Arguments("last")]
    public async Task PositionAndLast_AfterResetOfTheContext_RaiseXPDY0002(string function)
    {
        var item = XdmItem.FromNode(NodesHarness.Nodes["a"]);

        var actual = NodesHarness.RunWith(
            c =>
            {
                c.SetContextItem(item, 2, 5);
                c.Reset();
            },
            function
        );

        await Assert.That(actual).IsEqualTo("error:XPDY0002");
    }

    [Test]
    [Arguments("position")]
    [Arguments("last")]
    public async Task PositionAndLast_WithAnArgument_AreNotRegistered(string function) =>
        await Assert.That(NodesHarness.Run(function, "integer:1")).IsEqualTo("error:not-registered");
}
