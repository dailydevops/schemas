namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using System;
using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General.GeneralHarness;

/// <summary>Unparsed entity declarations are kept in the declaration model with their identifiers and notation (XML-62).</summary>
public class UnparsedEntityDeclarationTests
{
    private static DtdEntityDeclaration Declared(string xml, string name, RecordingResolver? resolver = null)
    {
        using var reader = XmlPipelineReader.Create(
            xml.AsSpan(),
            Options(LoaderHarness.Open(resolver ?? new RecordingResolver()))
        );
        var sink = new GeneralSink();
        _ = reader.Pump(ref sink);

        return reader.Declarations!.GeneralEntities[name];
    }

    [Test]
    public async Task Declaration_WithASystemIdentifier_KeepsTheIdentifierAndTheNotation(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = Declared(Doc("<!ENTITY u SYSTEM \"u.png\" NDATA png>", "x"), "u");

        await Assert.That(entity.NotationName).IsEqualTo("png");
        await Assert.That(entity.SystemId).IsEqualTo("u.png");
        await Assert.That(entity.PublicId).IsNull();
        await Assert.That(entity.Value).IsNull();
        await Assert.That(entity.IsParameter).IsFalse();
    }

    [Test]
    public async Task Declaration_WithAPublicAndASystemIdentifier_KeepsBothAndTheNotation(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = Declared(Doc("<!ENTITY u PUBLIC \"-//Example//PNG//EN\" \"u.png\" NDATA png>", "x"), "u");

        await Assert.That(entity.NotationName).IsEqualTo("png");
        await Assert.That(entity.SystemId).IsEqualTo("u.png");
        await Assert.That(entity.PublicId).IsEqualTo("-//Example//PNG//EN");
    }

    [Test]
    public async Task Declaration_InTheExternalSubset_KeepsTheIdentifiersTheNotationAndTheBaseUri(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add(
            "http://example.org/doc/ext.dtd",
            LoaderHarness.Utf8("<!ENTITY u SYSTEM \"u.png\" NDATA png>")
        );

        var entity = Declared("<!DOCTYPE doc SYSTEM \"ext.dtd\">\n<doc/>", "u", resolver);

        await Assert.That(entity.NotationName).IsEqualTo("png");
        await Assert.That(entity.SystemId).IsEqualTo("u.png");
    }

    [Test]
    public async Task Declaration_OfAnExternalParsedEntity_HasNoNotation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = Declared(Doc(ExternalE, "x"), "e");

        await Assert.That(entity.NotationName).IsNull();
        await Assert.That(entity.SystemId).IsEqualTo("e.ent");
        await Assert.That(entity.Value).IsNull();
    }
}
