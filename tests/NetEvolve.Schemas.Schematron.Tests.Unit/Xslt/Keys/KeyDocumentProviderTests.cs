namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys.KeyFixture;

/// <summary>
/// The provider as the key provider of the XPath package (<see cref="IXPathKeyProvider"/>, #777): the lookup names the node
/// from which the document is reached, a problem of the lookup is an <see cref="XPathException"/> with the code of the problem, and
/// the function <c>key</c> (#776) reaches the provider through the slot of the evaluation context.
/// </summary>
public class KeyDocumentProviderTests
{
    private static (IXPathKeyProvider? Contract, KeyDocument Document) Create()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));
        return (((object)provider) as IXPathKeyProvider, new KeyDocument());
    }

    [Test]
    public async Task Lookup_TwoArgumentForm_SearchesTheDocumentOfTheNode()
    {
        var (contract, document) = Create();

        var found = contract!.TryLookup(Key("k"), Str("a"), document["i3"], false, out var nodes);

        await Assert.That(found).IsTrue();
        await Assert.That(document.Keys(Nodes(nodes))).IsEqualTo("i1 o1");
    }

    [Test]
    public async Task Lookup_ThreeArgumentForm_SearchesTheSubtreeOfTheTopNode()
    {
        var (contract, document) = Create();

        var found = contract!.TryLookup(Key("k"), Str("a"), document["i3"], true, out var nodes);

        await Assert.That(found).IsTrue();
        await Assert.That(nodes.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Lookup_UndeclaredKey_IsAnExceptionWithXTDE1260()
    {
        var (contract, document) = Create();

        var failure = await Assert
            .That(() => contract!.TryLookup(Key("missing"), Str("a"), document["doc"], false, out _))
            .Throws<XPathException>();

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo("XTDE1260");
    }

    [Test]
    public async Task Lookup_TreeWithoutADocumentNode_IsAnExceptionWithXTDE1270()
    {
        var (contract, _) = Create();
        using var builder = new XmlTreeBuilder(new KeyFixture().Names, null, false);
        builder.Text("text", XmlTextKind.Text);
        var text = new XPathNode(new XmlNode(builder.Complete(), 0));

        var failure = await Assert
            .That(() => contract!.TryLookup(Key("k"), Str("a"), text, false, out _))
            .Throws<XPathException>();

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo("XTDE1270");
    }

    // A problem without a code of the XSLT specification (here: a slot that cannot be bound) is an exception without one.
    [Test]
    public async Task Lookup_ProblemWithoutACode_IsAnExceptionWithTheMessage()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));
        fixture.Binder.Fail = true;
        var contract = (IXPathKeyProvider)(object)provider;

        var failure = await Assert
            .That(() => contract.TryLookup(Key("k"), Str("a"), document["doc"], false, out _))
            .Throws<XPathException>();

        await Assert.That(failure!.Message).IsNotEmpty();
    }

    // Over real XPath: the function reads the slot, and the provider answers from the document of the context node.
    [Test]
    [Arguments("key('k', 'a')", "doc", "i1 o1")]
    [Arguments("key('k', ('a', 'b'))", "i3", "i1 i2 o1")]
    [Arguments("key('k', 'a', /catalog)", "doc", "i1 o1")]
    [Arguments("key('k', 'x', /catalog/item[@id = 'i3'])", "doc", "s1")]
    [Arguments("key('k', 'a', /catalog/item[@id = 'i3'])", "doc", "")]
    public async Task KeyFunction_OverTheProvider_FindsTheNodes(string text, string from, string expected)
    {
        var (contract, document) = Create();
        var compiled = XPathExpression.TryCompile(
            text,
            XPathStaticContext.Default,
            XPathHostRequirements.Xslt20Host | XPathHostRequirements.Keys
        );
        using var context = new XPathEvaluationContext();
        context.Host.Keys = contract;
        context.SetContextItem(XdmItem.FromNode(document[from]));

        await Assert.That(compiled.Success).IsTrue().Because(compiled.Diagnostic.Message);
        var result = compiled.Expression!.TryEvaluate(context);

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Ok).Because(result.Diagnostic.Message);
        await Assert.That(document.Keys(Nodes(result.Value))).IsEqualTo(expected);
    }

    // One run, two documents, the same key name: each node gets the nodes of its own document, and an index is built once per document.
    [Test]
    public async Task Lookup_TwoDocuments_UseTheirOwnIndex()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));
        var contract = (IXPathKeyProvider)(object)provider;
        var first = new KeyDocument();
        var second = new KeyDocument();

        _ = contract.TryLookup(Key("k"), Str("a"), first["i3"], false, out var fromFirst);
        _ = contract.TryLookup(Key("k"), Str("a"), second["i3"], false, out var fromSecond);
        _ = contract.TryLookup(Key("k"), Str("a"), first["i2"], false, out var again);

        await Assert.That(first.Keys(Nodes(fromFirst))).IsEqualTo("i1 o1");
        await Assert.That(first.Keys(Nodes(fromSecond))).IsEqualTo("? ?");
        await Assert.That(second.Keys(Nodes(fromSecond))).IsEqualTo("i1 o1");
        await Assert.That(first.Keys(Nodes(again))).IsEqualTo("i1 o1");
        await Assert.That(provider.IndexBuilds).IsEqualTo(2);
    }

    // The top node restricts to its subtree, itself included; a top in another document uses the index of that document.
    [Test]
    public async Task Lookup_ThreeArgumentForm_IncludesTopItselfAndUsesTheDocumentOfTop()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));
        var contract = (IXPathKeyProvider)(object)provider;
        var first = new KeyDocument();
        var second = new KeyDocument();

        _ = contract.TryLookup(Key("k"), Str("A"), first["i3"], true, out var withTop);
        _ = contract.TryLookup(Key("k"), Str("x"), second["i3"], true, out var inSecond);

        await Assert.That(first.Keys(Nodes(withTop))).IsEqualTo("i3");
        await Assert.That(second.Keys(Nodes(inSecond))).IsEqualTo("s1");
    }
}
