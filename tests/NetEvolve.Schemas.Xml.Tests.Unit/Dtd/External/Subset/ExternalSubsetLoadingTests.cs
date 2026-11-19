namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System;
using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>Section 2.8 and 4.2.2: the external subset is requested through the loader and its declarations join the model.</summary>
public class ExternalSubsetLoadingTests
{
    [Test]
    public async Task ExternalSubset_IsRequestedAgainstTheDocumentBaseUri_AndItsDeclarationsJoinTheModel()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT a EMPTY><!ENTITY e 'v'>");

        var (model, error) = SubsetHarness.Parse("<!ELEMENT b ANY>", resolver);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|E:b:ANY|GE:e=\"v\"");
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests[0].Kind).IsEqualTo(ResourceKind.ExternalSubset);
        await Assert.That(resolver.Requests[0].Reference).IsEqualTo("ext.dtd");
        await Assert.That(resolver.Requests[0].BaseUri).IsEqualTo(SubsetHarness.DocumentUri);
        await Assert.That(resolver.Requests[0].ResolvedUri).IsEqualTo(SubsetHarness.SubsetUri);
    }

    [Test]
    public async Task ExternalSubset_RequestCarriesThePublicIdentifier_AndTheTextIsReturnedWithItsBaseUri()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT a EMPTY>");
        var reader = new ExternalSubsetReader(LoaderHarness.Open(resolver), XmlReaderLimits.Default);

        var ok = reader.TryOpen(
            ResourceKind.ExternalSubset,
            "doc",
            "-//Example//DTD//EN",
            "ext.dtd",
            SubsetHarness.DocumentUri,
            out var entity,
            out var error
        );
        using var opened = entity;

        await Assert.That(ok).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(entity!.Text).IsEqualTo("<!ELEMENT a EMPTY>");
        await Assert.That(entity.BaseUri).IsEqualTo(SubsetHarness.SubsetUri);
        await Assert.That(resolver.Requests[0].PublicId).IsEqualTo("-//Example//DTD//EN");
    }

    [Test]
    public async Task ExternalSubset_WithoutASystemIdentifier_IsNotRequested()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT a EMPTY>");

        var (model, error) = SubsetHarness.Parse("<!ELEMENT b ANY>", resolver, systemId: string.Empty);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:b:ANY");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task DefaultOptions_DoNotRequestTheExternalSubset_AndRaiseXml2002()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT a EMPTY>");
        var options = DtdValidationOptions.Default.WithResolver(resolver);

        var (_, error) = SubsetHarness.Parse(string.Empty, resolver, options: options);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task OnlyTheExternalSubsetFlagOn_WithTheDenyAllResolver_RaisesXml2002()
    {
        var options = DtdValidationOptions.Default.WithAllowExternalSubset(true);

        var (_, error) = SubsetHarness.Parse(string.Empty, SubsetHarness.Serving(string.Empty), options: options);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML2002);
    }

    [Test]
    public async Task MissingExternalSubset_RaisesXml2002()
    {
        var (_, error) = SubsetHarness.Parse(string.Empty, new RecordingResolver());

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML2002);
    }

    [Test]
    public async Task ExternalSubset_ThatIsNotWellFormed_RaisesTheErrorOfTheDeclaration()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ELEMENT a>");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
        await Assert.That(model).IsEqualTo("<none>");
    }

    [Test]
    public async Task ExternalSubset_TextDeclaration_IsConsumed()
    {
        var (model, error) = SubsetHarness.ParseSubset("<?xml version=\"1.0\" encoding=\"UTF-8\"?><!ELEMENT a EMPTY>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY");
    }

    [Test]
    public async Task ExternalSubset_InUtf16WithATextDeclaration_LoadsIntoAUtf8Document()
    {
        var resolver = new RecordingResolver().Add(
            SubsetHarness.SubsetUri,
            LoaderHarness.Utf16Le("<?xml version='1.0' encoding='UTF-16'?><!ELEMENT a EMPTY>")
        );

        var (model, error) = SubsetHarness.Parse(string.Empty, resolver);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY");
    }

    [Test]
    public async Task ExternalSubset_TextDeclarationWithStandalone_IsRejected()
    {
        var (_, error) = SubsetHarness.ParseSubset("<?xml version=\"1.0\" standalone=\"yes\"?><!ELEMENT a EMPTY>");

        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task ExternalSubset_SecondTextDeclaration_RaisesXml0015()
    {
        var (_, error) = SubsetHarness.ParseSubset(
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><?xml version=\"1.0\" encoding=\"UTF-8\"?><!ELEMENT a EMPTY>"
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0015);
    }

    [Test]
    public async Task ExternalSubset_DoctypeDeclaration_RaisesXml0011()
    {
        var (_, error) = SubsetHarness.ParseSubset("<!DOCTYPE doc [<!ELEMENT doc EMPTY>]>");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task ExternalSubset_ReadInTinyChunks_GivesTheSameModel()
    {
        var text = "<!ELEMENT a (b|c)*><!ATTLIST a x CDATA 'ü€'><!ENTITY e 'v'>";
        var whole = SubsetHarness.Parse(string.Empty, SubsetHarness.Serving(text));
        var tiny = SubsetHarness.Parse(
            string.Empty,
            new RecordingResolver().Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(text), maxRead: 3)
        );

        await Assert.That(tiny).IsEqualTo(whole);
        await Assert.That(whole.Error).IsNull();
    }

    [Test]
    public async Task ExternalSubset_StreamOfTheResolver_IsDisposedOnSuccessAndOnError()
    {
        var good = SubsetHarness.Serving("<!ELEMENT a EMPTY>");
        var bad = SubsetHarness.Serving("<!ELEMENT a>");

        _ = SubsetHarness.Parse(string.Empty, good);
        _ = SubsetHarness.Parse(string.Empty, bad);

        await Assert.That(good.Streams[0].IsDisposed).IsTrue();
        await Assert.That(bad.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task ExternalSubset_BeyondTheExpansionBudget_RaisesXml1109()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(10);

        var (_, error) = SubsetHarness.ParseSubset("<!ELEMENT a EMPTY><!ELEMENT b EMPTY>", limits);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1109);
    }

    [Test]
    public async Task ExternalSubset_WithoutAReader_StaysUnread()
    {
        var info = new XmlDoctypeInfo("doc".AsSpan(), default, "ext.dtd".AsSpan(), "<!ELEMENT b ANY>".AsSpan());

        var error = DtdSubsetParser.Parse(
            in info,
            XmlReaderLimits.Default,
            null,
            SubsetHarness.DocumentUri,
            out var model
        );

        await Assert.That(error).IsNull();
        await Assert.That(model!.Elements.Keys).IsEquivalentTo(["b"]);
    }
}
