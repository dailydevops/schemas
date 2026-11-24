namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Documents;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Documents.DocumentsFixture;

/// <summary>
/// <c>document()</c> and the functions bound to the schema base URI (XSLT 2.0, 16.1 and 16.2): loading only through the resolver of the
/// context, relative references against the schema base URI, one pool with <c>fn:doc</c>, and the limits of the core.
/// </summary>
public class DocumentFunctionTests
{
    private const string Codes = "file:///schemas/rules/codes.xml";

    [Test]
    public async Task Document_WithOneArgument_LoadsTheDocument(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = new Resolver().Add(Codes, "<codes/>") };

        await Assert.That(Run("document", context, Functions(), Sequence(Codes))).IsEqualTo("nodes:1");
    }

    [Test]
    public async Task Document_WithABaseNodeAsSecondArgument_ResolvesAgainstTheBaseOfThatNode(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new Resolver().Add("http://other.example/dir/data.xml", "<d/>");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        var anchor = NodeWithBase("http://other.example/dir/anchor.xml");

        var result = Run("document", context, Functions(), Sequence("data.xml"), Sequence(anchor));

        await Assert.That(result).IsEqualTo("nodes:1");
        await Assert.That(resolver.Requests).Contains("http://other.example/dir/data.xml");
    }

    [Test]
    public async Task Document_WithARelativeReference_ResolvesAgainstTheSchemaBaseNotTheInstance(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new Resolver().Add(Codes, "<codes/>");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        context.SetContextItem(XdmItem.FromNode(NodeWithBase("http://instance.example/data/in.xml")));

        var result = Run("document", context, Functions(), Sequence("codes.xml"));

        await Assert.That(result).IsEqualTo("nodes:1");
        await Assert.That(resolver.Requests).IsEquivalentTo([Codes]);
    }

    [Test]
    public async Task Doc_WithARelativeReference_ResolvesAgainstTheSchemaBase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new Resolver().Add(Codes, "<codes/>");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        await Assert.That(Run("doc", context, Functions(), Sequence("codes.xml"))).IsEqualTo("nodes:1");
        await Assert.That(resolver.Requests).IsEquivalentTo([Codes]);
    }

    [Test]
    public async Task Document_UnderTheDefaultResolver_IsACodedError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = Denying() };

        await Assert.That(Run("document", context, Functions(), Sequence("codes.xml"))).IsEqualTo("error:XTRE1160");
    }

    [Test]
    public async Task Doc_UnderTheDefaultResolver_IsACodedError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = Denying() };

        await Assert.That(Run("doc", context, Functions(), Sequence("codes.xml"))).IsEqualTo("error:FODC0002");
    }

    [Test]
    public async Task DocAvailable_UnderTheDefaultResolver_IsFalse(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = Denying() };

        await Assert.That(Run("doc-available", context, Functions(), Sequence("codes.xml"))).IsEqualTo("boolean:false");
    }

    [Test]
    public async Task DocAvailable_ForALoadableResource_IsTrue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = new Resolver().Add(Codes, "<codes/>") };

        await Assert.That(Run("doc-available", context, Functions(), Sequence("codes.xml"))).IsEqualTo("boolean:true");
    }

    [Test]
    public async Task DocumentAndDoc_WithTheSameUri_ReturnTheSameNode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new Resolver().Add(Codes, "<codes/>");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        var functions = Functions();

        _ = Call("document", context, functions, Sequence("codes.xml"))[0].TryGetNode(out var viaDocument);
        _ = Call("doc", context, functions, Sequence(Codes))[0].TryGetNode(out var viaDoc);

        await Assert.That(viaDocument.IsSameNode(in viaDoc)).IsTrue();
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Document_WithTheSameUriTwice_ReturnsOneNode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = new Resolver().Add(Codes, "<codes/>") };

        await Assert
            .That(Run("document", context, Functions(), Sequence("codes.xml", "./codes.xml")))
            .IsEqualTo("nodes:1");
    }

    [Test]
    public async Task Document_WithAnEmptyArgument_IsEmpty(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = Denying() };

        await Assert.That(Run("document", context, Functions(), Sequence())).IsEqualTo("empty");
    }

    [Test]
    public async Task Document_ForAResourceOverTheSizeLimit_IsACodedError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var oversized = new byte[(16 * 1024 * 1024) + 1];
        using var context = new XPathEvaluationContext { ResourceResolver = new Resolver().Add(Codes, oversized) };

        await Assert.That(Run("document", context, Functions(), Sequence(Codes))).IsEqualTo("error:XTRE1160");
        await Assert.That(Run("doc", context, Functions(), Sequence(Codes))).IsEqualTo("error:FODC0002");
    }

    [Test]
    public async Task Document_BeyondTheRequestLimit_IsACodedError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new Resolver();
        for (var i = 0; i <= 16; i++)
        {
            _ = resolver.Add("file:///schemas/rules/d" + i + ".xml", "<d/>");
        }

        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        var functions = Functions();

        for (var i = 0; i < 16; i++)
        {
            await Assert.That(Run("document", context, functions, Sequence("d" + i + ".xml"))).IsEqualTo("nodes:1");
        }

        await Assert.That(Run("document", context, functions, Sequence("d16.xml"))).IsEqualTo("error:XTRE1160");
    }

    [Test]
    public async Task UnparsedText_WithARelativeReference_ResolvesAgainstTheSchemaBase(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext
        {
            ResourceResolver = new Resolver().Add("file:///schemas/rules/t.txt", "hello"),
        };

        await Assert.That(Run("unparsed-text", context, Functions(), Sequence("t.txt"))).IsEqualTo("string:hello");
    }

    [Test]
    public async Task Document_WithANodeReference_ResolvesAgainstTheBaseOfThatNode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new Resolver().Add("http://node.example/dir/data.xml", "<d/>");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        var reference = Reference("http://node.example/dir/ref.xml");

        await Assert.That(Run("document", context, Functions(), Sequence(reference))).IsEqualTo("nodes:1");
        await Assert.That(resolver.Requests).IsEquivalentTo(["http://node.example/dir/data.xml"]);
    }

    [Test]
    public async Task Document_WithANodeReferenceAndABaseNode_UsesTheExplicitBase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new Resolver().Add("http://explicit.example/data.xml", "<d/>");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        var reference = Reference("http://node.example/ref.xml");
        var anchor = NodeWithBase("http://explicit.example/anchor.xml");

        await Assert
            .That(Run("document", context, Functions(), Sequence(reference), Sequence(anchor)))
            .IsEqualTo("nodes:1");
    }

    [Test]
    public async Task Document_WithANonStringItem_IsATypeError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = Denying() };
        var number = XdmSequence.Create(XdmItem.FromInteger(1));

        await Assert.That(Run("document", context, Functions(), number)).IsEqualTo("error:XPTY0004");
    }

    [Test]
    public async Task Document_WithAFragmentIdentifier_IsACodedError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = new Resolver().Add(Codes, "<codes/>") };

        await Assert.That(Run("document", context, Functions(), Sequence("codes.xml#a"))).IsEqualTo("error:XTRE1160");
        await Assert.That(Run("doc", context, Functions(), Sequence("codes.xml#a"))).StartsWith("error:");
    }

    [Test]
    public async Task Document_WithARelativeReferenceAndNoSchemaBase_IsACodedError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = new Resolver().Add(Codes, "<codes/>") };
        var functions = Functions(null);

        await Assert.That(Run("document", context, functions, Sequence("codes.xml"))).IsEqualTo("error:XTRE1160");
        await Assert.That(Run("doc", context, functions, Sequence("codes.xml"))).StartsWith("error:");
        await Assert.That(Run("document", context, functions, Sequence(Codes))).IsEqualTo("nodes:1");
    }

    [Test]
    public async Task Doc_WithAnEmptyArgument_IsEmpty(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext { ResourceResolver = Denying() };

        await Assert.That(Run("doc", context, Functions(), Sequence())).IsEqualTo("empty");
    }

    [Test]
    public async Task UnparsedTextAvailable_WithARelativeReference_UsesTheSchemaBase(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new XPathEvaluationContext
        {
            ResourceResolver = new Resolver().Add("file:///schemas/rules/t.txt", "hello"),
        };

        await Assert
            .That(Run("unparsed-text-available", context, Functions(), Sequence("t.txt")))
            .IsEqualTo("boolean:true");
        await Assert
            .That(Run("unparsed-text-available", context, Functions(), Sequence("missing.txt")))
            .IsEqualTo("boolean:false");
    }

    private static XPathNode Reference(string baseUri) =>
        new(XmlDocumentLoader.Load("<r>data.xml</r>".AsSpan(), null, new Uri(baseUri)).Document);
}
