namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>A schema document that declares XML 1.1 is read; its content is the XML 1.0 compatible subset (#861).</summary>
public class SchemaXml11DeclarationTests
{
    private const string Schema =
        "<?xml version=\"1.1\"?><xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:element name=\"e\"/></xs:schema>";

    [Test]
    public async Task Chars_WithVersion11_AreRead(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reporter = NewReporter(out _);
        var document = SchemaDocumentParser.Parse(Schema.AsSpan(), SystemId, reporter);

        await Assert.That(document.ReadError).IsNull();
        await Assert.That(document.Root).IsNotNull();
    }

    [Test]
    public async Task Bytes_WithVersion11_AreRead(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reporter = NewReporter(out _);
        var document = SchemaDocumentParser.Parse(
            new UTF8Encoding(false).GetBytes(Schema).AsSpan(),
            SystemId,
            reporter
        );

        await Assert.That(document.ReadError).IsNull();
        await Assert.That(document.Root).IsNotNull();
    }

    [Test]
    public async Task Chars_WithAnotherVersion_AreStillRejected(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reporter = NewReporter(out _);
        var document = SchemaDocumentParser.Parse(
            Schema.Replace("1.1", "1.7", StringComparison.Ordinal).AsSpan(),
            SystemId,
            reporter
        );

        await Assert.That(document.ReadError!.Code.Alias).IsEqualTo("XML0010");
    }
}
