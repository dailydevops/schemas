namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System.Threading.Tasks;

/// <summary><c>fn:root</c> (Functions and Operators 1.0, 14.6): the topmost ancestor of a node, of the context item, or the empty sequence.</summary>
public class RootTests
{
    [Test]
    [Arguments("doc", "node:doc")]
    [Arguments("r", "node:doc")]
    [Arguments("a/t", "node:doc")]
    [Arguments("a@x", "node:doc")]
    [Arguments("cm", "node:doc")]
    [Arguments("pi", "node:doc")]
    [Arguments("o", "node:o.doc")]
    [Arguments("q", "node:o.doc")]
    [Arguments("o.doc", "node:o.doc")]
    public async Task Root_WithExplicitNode_ReturnsTheDocumentNodeOfItsTree(string node, string expected)
    {
        await Assert.That(NodesHarness.Run("root", "node:" + node)).IsEqualTo(expected);
        await Assert.That(NodesHarness.RunWithContextItem("node:o", "root", "node:" + node)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("c", "node:doc")]
    [Arguments("q", "node:o.doc")]
    [Arguments("b@lang", "node:doc")]
    public async Task Root_WithoutArgument_UsesTheContextItem(string node, string expected) =>
        await Assert.That(NodesHarness.RunWithContextItem("node:" + node, "root")).IsEqualTo(expected);

    [Test]
    public async Task Root_WithTheEmptySequence_ReturnsTheEmptySequence()
    {
        await Assert.That(NodesHarness.Run("root", "()")).IsEqualTo(string.Empty);
        await Assert.That(NodesHarness.RunWithContextItem("node:a", "root", "()")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Root_WithoutArgumentAndWithoutContextItem_RaisesXPDY0002() =>
        await Assert.That(NodesHarness.Run("root")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task Root_WithNonNodeContextItem_RaisesXPTY0004() =>
        await Assert.That(NodesHarness.RunWithContextItem("string:a", "root")).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("integer:5")]
    [Arguments("node:a|node:b")]
    public async Task Root_WithNonNodeOrSeveralItems_RaisesXPTY0004(string argument) =>
        await Assert.That(NodesHarness.Run("root", argument)).IsEqualTo("error:XPTY0004");
}
