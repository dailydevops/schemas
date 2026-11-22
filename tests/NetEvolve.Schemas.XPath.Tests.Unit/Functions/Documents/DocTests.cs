namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Documents;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Documents;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:doc</c> and <c>fn:doc-available</c> reach documents only through the resolver of the evaluation context.</summary>
public class DocTests
{
    private const string A = "http://example.com/dir/a.xml";
    private const string Doc = "<r><x id=\"i1\">one</x></r>";

    [Test]
    public async Task Doc_WithoutResolver_RaisesFODC0002AndAvailableIsFalse()
    {
        await Assert.That(DocumentsHarness.Run("doc('" + A + "')")).IsEqualTo("error:FODC0002");
        await Assert.That(DocumentsHarness.Run("doc-available('" + A + "')")).IsEqualTo("false");
    }

    [Test]
    public async Task Doc_WithTheDenyAllResolver_RaisesFODC0002AndAvailableIsFalse()
    {
        var resolver = XPathResourceResolverAdapter.Wrap(DenyAllResourceResolver.Instance);

        await Assert.That(DocumentsHarness.Run("doc('" + A + "')", resolver)).IsEqualTo("error:FODC0002");
        await Assert.That(DocumentsHarness.Run("doc-available('" + A + "')", resolver)).IsEqualTo("false");
    }

    [Test]
    public async Task Doc_TwoUrisThatResolveToTheSameAbsoluteUri_ReturnTheIdenticalNodeAndCallTheResolverOnce()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, Doc);

        var result = DocumentsHarness.Run(
            "doc('http://example.com/dir/a.xml') is doc('http://example.com/dir/sub/../a.xml')",
            resolver
        );

        await Assert.That(result).IsEqualTo("true");
        await Assert.That(resolver.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task Doc_TwoDifferentUris_AreTwoNodes()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, Doc).Add("http://example.com/dir/b.xml", Doc);

        var result = DocumentsHarness.Run(
            "doc('http://example.com/dir/a.xml') is doc('http://example.com/dir/b.xml')",
            resolver
        );

        await Assert.That(result).IsEqualTo("false");
        await Assert.That(resolver.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task Doc_LoadsTheDocumentThroughAnXPathDocumentRequest()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, Doc);

        var result = DocumentsHarness.Run(
            "concat(string(doc('" + A + "')/r/x), '|', string(base-uri(doc('" + A + "'))))",
            resolver
        );

        await Assert.That(result).IsEqualTo("one|" + A);
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests[0]).IsEqualTo((ResourceKind.XPathDocument, A, (string?)null, (string?)A));
    }

    [Test]
    public async Task DocAvailable_ThenDoc_LoadsTheDocumentOnce()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, Doc);
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        await Assert.That(DocumentsHarness.Run("doc-available('" + A + "')", context)).IsEqualTo("true");
        await Assert.That(DocumentsHarness.Run("doc('" + A + "')", context)).IsEqualTo("doc");
        await Assert.That(resolver.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task Doc_ResolverMissAndDenial_RaiseFODC0002AndAvailableIsFalse()
    {
        var resolver = new DocumentsHarness.FakeResolver().Deny(A);

        await Assert.That(DocumentsHarness.Run("doc('" + A + "')", resolver)).IsEqualTo("error:FODC0002");
        await Assert.That(DocumentsHarness.Run("doc-available('" + A + "')", resolver)).IsEqualTo("false");
        await Assert
            .That(DocumentsHarness.Run("doc('http://example.com/missing.xml')", resolver))
            .IsEqualTo("error:FODC0002");
        await Assert
            .That(DocumentsHarness.Run("doc-available('http://example.com/missing.xml')", resolver))
            .IsEqualTo("false");
    }

    [Test]
    public async Task Doc_ResourceThatIsNotWellFormed_RaisesFODC0002AndAvailableIsFalse()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, "<r><unclosed></r>");

        await Assert.That(DocumentsHarness.Run("doc('" + A + "')", resolver)).IsEqualTo("error:FODC0002");
        await Assert.That(DocumentsHarness.Run("doc-available('" + A + "')", resolver)).IsEqualTo("false");
    }

    [Test]
    public async Task Doc_AFailedLoadIsPooledToo_SoTheResolverIsNotCalledAgain()
    {
        var resolver = new DocumentsHarness.FakeResolver();
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        await Assert.That(DocumentsHarness.Run("doc-available('" + A + "')", context)).IsEqualTo("false");
        await Assert.That(DocumentsHarness.Run("doc('" + A + "')", context)).IsEqualTo("error:FODC0002");
        await Assert.That(resolver.Calls).IsEqualTo(1);
    }

    [Test]
    [Arguments("%gg")]
    [Arguments("a b")]
    [Arguments(":/")]
    [Arguments("http://example.com/dir/a.xml#fragment")]
    public async Task Doc_ReferenceThatIsNoUri_RaisesFODC0005_AlsoForDocAvailableAndWithoutAnyResolverCall(
        string reference
    )
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, Doc);

        await Assert.That(DocumentsHarness.Run("doc('" + reference + "')", resolver)).IsEqualTo("error:FODC0005");
        await Assert
            .That(DocumentsHarness.Run("doc-available('" + reference + "')", resolver))
            .IsEqualTo("error:FODC0005");
        await Assert.That(resolver.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task Doc_EmptyArgument_GivesTheEmptySequenceAndAvailableIsFalse()
    {
        var resolver = new DocumentsHarness.FakeResolver();

        await Assert.That(DocumentsHarness.Run("count(doc(()))", resolver)).IsEqualTo("0");
        await Assert.That(DocumentsHarness.Run("doc-available(())", resolver)).IsEqualTo("false");
        await Assert.That(resolver.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task Doc_RelativeReference_ResolvesAgainstTheStaticBaseUriOfTheFunction()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, Doc);
        var function = new DocFunction(new Uri("http://example.com/dir/"));
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        var first = function.Call(context, [XdmSequence.Create(XdmItem.FromString("a.xml"))]);
        var second = function.Call(context, [XdmSequence.Create(XdmItem.FromString("./x/../a.xml"))]);

        _ = first[0].TryGetNode(out var firstNode);
        _ = second[0].TryGetNode(out var secondNode);
        await Assert.That(firstNode.IsSameNode(secondNode)).IsTrue();
        await Assert.That(resolver.Calls).IsEqualTo(1);
        await Assert
            .That(resolver.Requests[0])
            .IsEqualTo((ResourceKind.XPathDocument, "a.xml", (string?)"http://example.com/dir/", (string?)A));
    }

    [Test]
    public async Task Doc_RelativeReferenceWithoutAStaticBaseUri_RaisesFODC0002WithoutAResolverCall()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add("a.xml", Doc);
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        await Assert.That(DocumentsHarness.Run("doc('a.xml')", context)).IsEqualTo("error:FODC0002");
        await Assert.That(DocumentsHarness.Run("doc-available('a.xml')", context)).IsEqualTo("false");
        await Assert.That(resolver.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task Doc_ASeventeenthResource_ExceedsTheResourceLimitAndRaisesFODC0002()
    {
        var resolver = new DocumentsHarness.FakeResolver();
        for (var i = 0; i < 17; i++)
        {
            _ = resolver.Add("http://example.com/d" + i + ".xml", Doc);
        }

        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        for (var i = 0; i < 16; i++)
        {
            await Assert
                .That(DocumentsHarness.Run("doc('http://example.com/d" + i + ".xml')", context))
                .IsEqualTo("doc");
        }

        await Assert
            .That(DocumentsHarness.Run("doc('http://example.com/d16.xml')", context))
            .IsEqualTo("error:FODC0002");
        await Assert.That(resolver.Calls).IsEqualTo(16);
    }

    [Test]
    public async Task Doc_DocumentTypeDeclaration_NeedsTheExplicitReaderOption()
    {
        const string withDtd = "<!DOCTYPE r [<!ELEMENT r ANY>]><r/>";
        var resolver = new DocumentsHarness.FakeResolver().Add(A, withDtd);
        using var secure = new XPathEvaluationContext { ResourceResolver = resolver };
        using var explicitOption = new XPathEvaluationContext { ResourceResolver = resolver };
        var function = new DocFunction(null, XmlReaderOptions.Default.WithDtdProcessing(true));

        await Assert.That(DocumentsHarness.Run("doc('" + A + "')", secure)).IsEqualTo("error:FODC0002");
        await Assert.That(DocumentsHarness.Call(function, explicitOption, A)).IsEqualTo("doc");
    }

    [Test]
    public async Task Pool_LivesAsLongAsTheEvaluationContext_NotAcrossContexts()
    {
        var resolver = new DocumentsHarness.FakeResolver().Add(A, Doc);
        using var one = new XPathEvaluationContext { ResourceResolver = resolver };
        using var two = new XPathEvaluationContext { ResourceResolver = resolver };

        _ = DocumentsHarness.Run("doc('" + A + "')", one);
        one.Reset();
        _ = DocumentsHarness.Run("doc('" + A + "')", one);
        await Assert.That(resolver.Calls).IsEqualTo(1);

        _ = DocumentsHarness.Run("doc('" + A + "')", two);
        await Assert.That(resolver.Calls).IsEqualTo(2);
    }

    [Test]
    [Arguments(XPathVersion.V20)]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task Registration_DocAndDocAvailableTakeOneArgumentInEveryVersion(XPathVersion version)
    {
        foreach (var local in new[] { "doc", "doc-available" })
        {
            await Assert.That(DocumentsHarness.TryResolve(local, 1, out _, version)).IsTrue();
            await Assert.That(DocumentsHarness.TryResolve(local, 0, out _, version)).IsFalse();
            await Assert.That(DocumentsHarness.TryResolve(local, 2, out _, version)).IsFalse();
        }
    }

    [Test]
    [Arguments("collection()")]
    [Arguments("collection('http://example.com/c')")]
    [Arguments("collection(())")]
    [Arguments("fn:collection('urn:none')")]
    public async Task Collection_RaisesFODC0002ForEveryCall_WithoutTouchingTheResolver(string expression)
    {
        var resolver = new DocumentsHarness.FakeResolver().Add("http://example.com/c", Doc);

        await Assert.That(DocumentsHarness.Run(expression, resolver)).IsEqualTo("error:FODC0002");
        await Assert.That(resolver.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task Collection_WithTwoArguments_IsAnUnknownFunction() =>
        await Assert.That(DocumentsHarness.Run("collection('a', 'b')")).IsEqualTo("error:XPST0017");
}
