namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// <c>snapshot()</c> and <c>copy-of()</c> (XSLT 3.0, 22.1 and 22.2): registered for the <c>xslt3</c> binding only; atomic values pass
/// unchanged; a node is deep copied with a new identity; <c>snapshot</c> keeps the ancestors and their attributes and drops their
/// other children, <c>copy-of</c> keeps no ancestor.
/// </summary>
public class SnapshotTests
{
    private const string Xml =
        "<root a=\"1\" xmlns:p=\"urn:p\"><before/><target b=\"2\"><c>t</c></target><after/></root>";

    [Test]
    [Arguments("snapshot")]
    [Arguments("copy-of")]
    public async Task Function_IsRegisteredForXslt3Only(string local)
    {
        await Assert.That(Scope("xslt3").TryResolve(Fn(local), 1, out _)).IsTrue();
        await Assert.That(Scope("xslt2").TryResolve(Fn(local), 1, out _)).IsFalse();
        await Assert.That(Scope("xpath2").TryResolve(Fn(local), 1, out _)).IsFalse();
    }

    [Test]
    [Arguments("snapshot")]
    [Arguments("copy-of")]
    public async Task Function_PassesAtomicValuesUnchanged(string local)
    {
        await Assert.That(Run(Scope("xslt3"), local, S("x"))).IsEqualTo("string:x");
        await Assert.That(Run(Scope("xslt3"), local, I(7))).IsEqualTo("other");
    }

    [Test]
    public async Task Snapshot_KeepsAncestorsAndTheirAttributes_AndDropsSiblings()
    {
        var target = Target();
        var copy = Call("snapshot", target);

        await Assert.That(copy.Node.Equals(target.Node)).IsFalse();
        await Assert.That(copy.Node.Name.Local.Value).IsEqualTo(target.Node.Name.Local.Value);
        await Assert.That(copy.Node.StringValue).IsEqualTo("t");
        await Assert.That(copy.Node.FirstAttribute.StringValue).IsEqualTo("2");

        var root = copy.Node.Parent;
        await Assert.That(root.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(root.FirstAttribute.StringValue).IsEqualTo("1");
        await Assert.That(root.FirstChild.Equals(copy.Node)).IsTrue();
        await Assert.That(copy.Node.NextSibling.IsEmpty).IsTrue();
        await Assert.That(root.Parent.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(ReferenceEquals(root.Tree, target.Node.Tree)).IsFalse();
    }

    [Test]
    public async Task Snapshot_OfAnAttribute_ReturnsTheCopiedAttributeOfTheCopiedParent()
    {
        var attribute = new XPathNode(Target().Node.FirstAttribute);
        var copy = Call("snapshot", attribute);

        await Assert.That(copy.Node.Kind).IsEqualTo(XmlNodeKind.Attribute);
        await Assert.That(copy.Node.StringValue).IsEqualTo("2");
        await Assert.That(copy.Node.Parent.Name.Local.Value).IsEqualTo(attribute.Node.Parent.Name.Local.Value);
        await Assert.That(copy.Node.Parent.Parent.FirstAttribute.StringValue).IsEqualTo("1");
    }

    [Test]
    public async Task CopyOf_KeepsNoAncestor_AndCopiesTheSubtree()
    {
        var target = Target();
        var copy = Call("copy-of", target);

        await Assert.That(copy.Node.Equals(target.Node)).IsFalse();
        await Assert.That(copy.Node.Name.Local.Value).IsEqualTo(target.Node.Name.Local.Value);
        await Assert.That(copy.Node.StringValue).IsEqualTo("t");
        await Assert.That(copy.Node.Parent.Kind).IsEqualTo(XmlNodeKind.Document);
    }

    private static XPathNode Target() =>
        new(XmlDocumentLoader.Load(Xml.AsSpan()).Document.FirstChild.FirstChild.NextSibling);

    private static XPathNode Call(string local, XPathNode node)
    {
        _ = Scope("xslt3").TryResolve(Fn(local), 1, out var function);
        using var context = new XPathEvaluationContext();
        var call = new XPathFunctionCall(context, [XdmSequence.Create(XdmItem.FromNode(node))]);
        _ = function!.Invoke(ref call);
        _ = call.Result[0].TryGetNode(out var result);
        return result;
    }
}
