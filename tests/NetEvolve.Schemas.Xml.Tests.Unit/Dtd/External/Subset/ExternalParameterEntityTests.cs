namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>Section 4.2.2 and 4.4.8: external parameter entities go through the loader like the external subset.</summary>
public class ExternalParameterEntityTests
{
    private const string DocumentBase = SubsetHarness.DocumentUri;
    private const string EntityUri = "http://example.org/doc/pe.ent";

    private static RecordingResolver WithEntity(string text) =>
        new RecordingResolver().Add(EntityUri, LoaderHarness.Utf8(text));

    [Test]
    public async Task ReferenceBetweenDeclarationsOfTheInternalSubset_LoadsTheEntityAgainstTheDocumentBaseUri()
    {
        var resolver = WithEntity("<!ELEMENT a EMPTY>");

        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext; <!ELEMENT b ANY>",
            resolver,
            systemId: string.Empty
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|E:b:ANY|PE:ext - pe.ent");
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests[0].Kind).IsEqualTo(ResourceKind.ExternalParameterEntity);
        await Assert.That(resolver.Requests[0].Reference).IsEqualTo("pe.ent");
        await Assert.That(resolver.Requests[0].BaseUri).IsEqualTo(DocumentBase);
        await Assert.That(resolver.Requests[0].ResolvedUri).IsEqualTo(EntityUri);
    }

    [Test]
    public async Task ExternalEntity_MayHoldReferencesInsideDeclarations()
    {
        var resolver = WithEntity("<!ENTITY % c \"(a|b)\"><!ELEMENT x %c;>");

        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;",
            resolver,
            systemId: string.Empty
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:x:(a|b)|PE:c=\"(a|b)\"|PE:ext - pe.ent");
    }

    [Test]
    public async Task ExternalEntity_TextDeclaration_IsConsumed_AndTheEncodingIsItsOwn()
    {
        var resolver = new RecordingResolver().Add(
            EntityUri,
            LoaderHarness.Utf16Le("<?xml version='1.0' encoding='UTF-16'?><!ELEMENT a EMPTY>")
        );

        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;",
            resolver,
            systemId: string.Empty
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|PE:ext - pe.ent");
    }

    [Test]
    public async Task ReferenceInTheExternalSubset_ResolvesAgainstTheEntityThatContainsTheDeclaration()
    {
        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/sub/ext.dtd", LoaderHarness.Utf8("<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;"))
            .Add("http://example.org/doc/sub/pe.ent", LoaderHarness.Utf8("<!ELEMENT a EMPTY>"));

        var (model, error) = SubsetHarness.Parse(string.Empty, resolver, systemId: "sub/ext.dtd");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|PE:ext - pe.ent");
        await Assert.That(resolver.Requests[1].ResolvedUri).IsEqualTo("http://example.org/doc/sub/pe.ent");
        await Assert.That(resolver.Requests[1].BaseUri).IsEqualTo("http://example.org/doc/sub/ext.dtd");
    }

    [Test]
    public async Task ReferenceInTheExternalSubset_ToAnEntityDeclaredInTheInternalSubset_ResolvesAgainstTheDocument()
    {
        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/sub/ext.dtd", LoaderHarness.Utf8("%ext;"))
            .Add(EntityUri, LoaderHarness.Utf8("<!ELEMENT a EMPTY>"));

        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\">",
            resolver,
            systemId: "sub/ext.dtd"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|PE:ext - pe.ent");
        await Assert.That(resolver.Requests[1].ResolvedUri).IsEqualTo(EntityUri);
    }

    [Test]
    public async Task EachDeclaration_RemembersTheBaseUriOfItsEntity()
    {
        var resolver = new RecordingResolver().Add(
            "http://example.org/doc/sub/ext.dtd",
            LoaderHarness.Utf8("<!ENTITY % fromSubset SYSTEM \"x.ent\">")
        );

        var (model, error) = SubsetHarness.ParseModel(
            "<!ENTITY % fromDocument SYSTEM \"y.ent\">",
            resolver,
            "sub/ext.dtd"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model!.ParameterEntities["fromDocument"].BaseUri).IsEqualTo(DocumentBase);
        await Assert
            .That(model.ParameterEntities["fromSubset"].BaseUri)
            .IsEqualTo("http://example.org/doc/sub/ext.dtd");
    }

    [Test]
    public async Task ExternalEntityInsideAnEntityValue_IsIncludedInTheLiteral()
    {
        var resolver = WithEntity("included")
            .Add(SubsetHarness.SubsetUri, LoaderHarness.Utf8("<!ENTITY g \"a %ext; b\">"));

        var (model, error) = SubsetHarness.Parse("<!ENTITY % ext SYSTEM \"pe.ent\">", resolver);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("GE:g=\"a included b\"|PE:ext - pe.ent");
    }

    [Test]
    public async Task ExternalParameterEntityFlagOff_RaisesXml2002_WithoutCallingTheResolver()
    {
        var resolver = WithEntity("<!ELEMENT a EMPTY>");
        var options = DtdValidationOptions.Default.WithResolver(resolver).WithAllowExternalSubset(true);

        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;",
            resolver,
            systemId: string.Empty,
            options: options
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task MissingExternalEntity_RaisesXml2002()
    {
        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"missing.ent\"> %ext;",
            new RecordingResolver(),
            systemId: string.Empty
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML2002);
    }

    [Test]
    public async Task ExternalEntity_ThatIncludesItself_RaisesXml0020()
    {
        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;",
            WithEntity("<!ELEMENT a EMPTY> %ext;"),
            systemId: string.Empty
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0020);
    }

    [Test]
    public async Task ExternalEntity_ThatIncludesTheSameUriUnderAnotherName_RaisesXml0020()
    {
        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % a SYSTEM \"pe.ent\"><!ENTITY % b SYSTEM \"pe.ent\"> %a;",
            WithEntity("%b;"),
            systemId: string.Empty
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0020);
    }

    [Test]
    public async Task SameExternalEntity_ReferencedTwiceOneAfterTheOther_IsNotRecursion()
    {
        var resolver = WithEntity("<!ENTITY % v 'x'>");

        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext; %ext;",
            resolver,
            systemId: string.Empty
        );

        await Assert.That(error).IsNull();
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExternalEntityRequests_AreBoundedByTheRequestLimit()
    {
        var resolver = WithEntity("<!ENTITY % v 'x'>");
        var options = LoaderHarness
            .Open(resolver)
            .WithDtdLimits(DtdValidationLimits.Default.WithMaxExternalEntityRequests(1));

        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext; %ext;",
            resolver,
            systemId: string.Empty,
            options: options
        );

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Alias).IsEqualTo("XML1117");
    }

    [Test]
    public async Task ExternalEntity_BeyondTheExpansionBudget_RaisesXml1109()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(30);

        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;",
            WithEntity("<!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY>"),
            systemId: string.Empty,
            limits: limits
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1109);
    }

    [Test]
    public async Task ExternalEntity_StreamOfTheResolver_IsDisposedOnSuccessAndOnError()
    {
        var good = WithEntity("<!ELEMENT a EMPTY>");
        var bad = WithEntity("<!ELEMENT a>");
        const string subset = "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;";

        _ = SubsetHarness.Parse(subset, good, systemId: string.Empty);
        _ = SubsetHarness.Parse(subset, bad, systemId: string.Empty);

        await Assert.That(good.Streams[0].IsDisposed).IsTrue();
        await Assert.That(bad.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task ExternalEntity_WithoutAReader_IsSkippedAsBefore()
    {
        var info = new XmlDoctypeInfo(
            "doc".AsSpan(),
            default,
            default,
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext; <!ELEMENT a EMPTY>".AsSpan()
        );

        var error = NetEvolve.Schemas.Xml.Dtd.Declarations.DtdSubsetParser.Parse(
            in info,
            XmlReaderLimits.Default,
            null,
            DocumentBase,
            out var model
        );

        await Assert.That(error).IsNull();
        await Assert.That(model!.Elements.Keys).IsEquivalentTo(["a"]);
    }
}
