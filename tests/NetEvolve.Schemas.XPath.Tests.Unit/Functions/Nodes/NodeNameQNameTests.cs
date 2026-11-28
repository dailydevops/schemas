namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// <c>fn:node-name</c>, <c>fn:nilled</c>, and <c>fn:document-uri</c> (Functions and Operators 1.0, 2.1 to 2.3, 14.x): each takes one
/// <c>node()?</c> argument and gives the empty sequence for the empty sequence and for the kinds of node that have no such property.
/// </summary>
public class NodeNameQNameTests
{
    [Test]
    [Arguments("r", "QName:r{}")]
    [Arguments("a", "QName:p:a{urn:p}")]
    [Arguments("h", "QName:h{urn:d}")]
    [Arguments("a@x", "QName:x{}")]
    [Arguments("a@y", "QName:p:y{urn:p}")]
    [Arguments("r@lang", "QName:xml:lang{http://www.w3.org/XML/1998/namespace}")]
    [Arguments("pi", "QName:target{}")]
    [Arguments("doc", "")]
    [Arguments("a/t", "")]
    [Arguments("cm", "")]
    [Arguments("()", "")]
    public async Task NodeName_GivesTheExpandedNameOfElementsAttributesAndInstructionsOnly(
        string node,
        string expected
    ) => await Assert.That(NodesHarness.Run("node-name", node == "()" ? node : "node:" + node)).IsEqualTo(expected);

    [Test]
    [Arguments("r", "boolean:false")]
    [Arguments("a", "boolean:false")]
    [Arguments("doc", "")]
    [Arguments("a@x", "")]
    [Arguments("a/t", "")]
    [Arguments("cm", "")]
    [Arguments("pi", "")]
    [Arguments("()", "")]
    public async Task Nilled_IsFalseForAnElementWithoutAnnotationAndEmptyForTheOtherKinds(
        string node,
        string expected
    ) => await Assert.That(NodesHarness.Run("nilled", node == "()" ? node : "node:" + node)).IsEqualTo(expected);

    [Test]
    [Arguments("u.doc", "anyURI:http://example.com/u.xml")]
    [Arguments("doc", "")]
    [Arguments("o.doc", "")]
    [Arguments("u", "")]
    [Arguments("r", "")]
    [Arguments("a@x", "")]
    [Arguments("a/t", "")]
    [Arguments("()", "")]
    public async Task DocumentUri_GivesTheUriOfADocumentNodeThatHasOneAndEmptyOtherwise(string node, string expected) =>
        await Assert.That(NodesHarness.Run("document-uri", node == "()" ? node : "node:" + node)).IsEqualTo(expected);

    [Test]
    [Arguments("node-name")]
    [Arguments("nilled")]
    [Arguments("document-uri")]
    public async Task EachFunction_WithANonNodeArgument_IsXPTY0004(string function) =>
        await Assert.That(NodesHarness.Run(function, "integer:1")).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments(XPathVersion.V20, false)]
    [Arguments(XPathVersion.V30, true)]
    [Arguments(XPathVersion.V31, true)]
    public async Task NodeName_WithoutArgument_IsRegisteredFromXPath30(XPathVersion version, bool expected) =>
        await Assert.That(NodesHarness.TryResolve("node-name", 0, out _, version)).IsEqualTo(expected);

    [Test]
    [Arguments("a", "QName:p:a{urn:p}")]
    [Arguments("a@x", "QName:x{}")]
    [Arguments("pi", "QName:target{}")]
    [Arguments("doc", "")]
    [Arguments("cm", "")]
    public async Task NodeName_WithoutArgument_GivesTheNameOfTheContextNode(string contextItem, string expected)
    {
        ArgumentNullException.ThrowIfNull(contextItem);

        _ = NodesHarness.TryResolve("node-name", 0, out var function, XPathVersion.V30);
        using var context = new XPathEvaluationContext();
        context.SetContextItem(NodesHarness.ParseItem("node:" + contextItem));

        await Assert.That(NodesHarness.Show(function!.Call(context, []))).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null, "XPDY0002")]
    [Arguments("integer:1", "XPTY0004")]
    public async Task NodeName_WithoutArgument_RaisesTheContextItemError(string? contextItem, string code)
    {
        _ = NodesHarness.TryResolve("node-name", 0, out var function, XPathVersion.V30);
        using var context = new XPathEvaluationContext();
        if (contextItem is not null)
        {
            context.SetContextItem(NodesHarness.ParseItem(contextItem));
        }

        var failure = Assert.Throws<XPathException>(() => function!.Call(context, []));

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo(code);
    }

    [Test]
    [Arguments("nilled")]
    [Arguments("document-uri")]
    public async Task EachFunction_IsRegisteredForOneArgumentOnlyInEveryLanguageVersion(string local)
    {
        foreach (var version in new[] { XPathVersion.V20, XPathVersion.V30, XPathVersion.V31 })
        {
            await Assert.That(NodesHarness.TryResolve(local, 0, out _, version)).IsFalse();
            await Assert.That(NodesHarness.TryResolve(local, 2, out _, version)).IsFalse();
            await Assert.That(NodesHarness.TryResolve(local, 1, out var function, version)).IsTrue();
            await Assert.That(function!.Versions).IsEqualTo(XPathVersionMask.All);
        }
    }
}
