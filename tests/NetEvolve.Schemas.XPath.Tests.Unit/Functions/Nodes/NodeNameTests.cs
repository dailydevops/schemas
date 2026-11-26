namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;

/// <summary>
/// <c>fn:name</c>, <c>fn:local-name</c>, and <c>fn:namespace-uri</c> (Functions and Operators 1.0, 14.1 to 14.3): the lexical name,
/// the local part, and the namespace of a node, each with the explicit node, the context item, the empty sequence, and a missing or
/// non-node context item.
/// </summary>
public class NodeNameTests
{
    [Test]
    [Arguments("name", "r", "string:r")]
    [Arguments("name", "a", "string:p:a")]
    [Arguments("name", "a@x", "string:x")]
    [Arguments("name", "a@y", "string:p:y")]
    [Arguments("name", "h", "string:h")]
    [Arguments("name", "pi", "string:target")]
    [Arguments("name", "doc", "string:")]
    [Arguments("name", "a/t", "string:")]
    [Arguments("name", "cm", "string:")]
    [Arguments("local-name", "r", "string:r")]
    [Arguments("local-name", "a", "string:a")]
    [Arguments("local-name", "a@y", "string:y")]
    [Arguments("local-name", "h", "string:h")]
    [Arguments("local-name", "pi", "string:target")]
    [Arguments("local-name", "doc", "string:")]
    [Arguments("local-name", "a/t", "string:")]
    [Arguments("local-name", "cm", "string:")]
    [Arguments("namespace-uri", "a", "anyURI:urn:p")]
    [Arguments("namespace-uri", "a@y", "anyURI:urn:p")]
    [Arguments("namespace-uri", "h", "anyURI:urn:d")]
    [Arguments("namespace-uri", "r", "anyURI:")]
    [Arguments("namespace-uri", "a@x", "anyURI:")]
    [Arguments("namespace-uri", "pi", "anyURI:")]
    [Arguments("namespace-uri", "doc", "anyURI:")]
    [Arguments("namespace-uri", "cm", "anyURI:")]
    public async Task Function_WithExplicitNode_ReturnsTheNamePart(string function, string node, string expected)
    {
        await Assert.That(NodesHarness.Run(function, "node:" + node)).IsEqualTo(expected);
        await Assert.That(NodesHarness.RunWithContextItem("node:doc", function, "node:" + node)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("name", "a", "string:p:a")]
    [Arguments("name", "a@y", "string:p:y")]
    [Arguments("name", "doc", "string:")]
    [Arguments("local-name", "a", "string:a")]
    [Arguments("local-name", "pi", "string:target")]
    [Arguments("namespace-uri", "h", "anyURI:urn:d")]
    [Arguments("namespace-uri", "r", "anyURI:")]
    public async Task Function_WithoutArgument_UsesTheContextItem(string function, string node, string expected) =>
        await Assert.That(NodesHarness.RunWithContextItem("node:" + node, function)).IsEqualTo(expected);

    // The lexical name of a prefixed node is built once per name table, so a rule that reads name() on every node does not allocate a string per call.
    [Test]
    public async Task Name_OfAPrefixedNode_ReturnsTheSameStringInstanceOnEveryCall()
    {
        _ = NodesHarness.TryResolve("name", 1, out var function);
        using var context = new XPathEvaluationContext();
        var argument = NodesHarness.Parse("node:a");

        _ = function!.Call(context, [argument])[0].TryGetString(out var first);
        _ = function.Call(context, [argument])[0].TryGetString(out var second);

        await Assert.That(first).IsEqualTo("p:a");
        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    [Arguments("name", "string:")]
    [Arguments("local-name", "string:")]
    [Arguments("namespace-uri", "anyURI:")]
    public async Task Function_WithTheEmptySequence_ReturnsTheZeroLengthValue(string function, string expected)
    {
        await Assert.That(NodesHarness.Run(function, "()")).IsEqualTo(expected);
        await Assert.That(NodesHarness.RunWithContextItem("node:a", function, "()")).IsEqualTo(expected);
    }

    [Test]
    [Arguments("name")]
    [Arguments("local-name")]
    [Arguments("namespace-uri")]
    public async Task Function_WithoutArgumentAndWithoutContextItem_RaisesXPDY0002(string function) =>
        await Assert.That(NodesHarness.Run(function)).IsEqualTo("error:XPDY0002");

    [Test]
    [Arguments("name")]
    [Arguments("local-name")]
    [Arguments("namespace-uri")]
    public async Task Function_WithNonNodeContextItem_RaisesXPTY0004(string function) =>
        await Assert.That(NodesHarness.RunWithContextItem("integer:5", function)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("name", "integer:5")]
    [Arguments("local-name", "string:a")]
    [Arguments("namespace-uri", "boolean:true")]
    [Arguments("name", "node:a|node:b")]
    public async Task Function_WithNonNodeOrSeveralItems_RaisesXPTY0004(string function, string argument) =>
        await Assert.That(NodesHarness.Run(function, argument)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("name")]
    [Arguments("local-name")]
    [Arguments("namespace-uri")]
    public async Task Function_WithTwoArguments_IsNotRegistered(string function) =>
        await Assert.That(NodesHarness.Run(function, "node:a", "node:b")).IsEqualTo("error:not-registered");
}
