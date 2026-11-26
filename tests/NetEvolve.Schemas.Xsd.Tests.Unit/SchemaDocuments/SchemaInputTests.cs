namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using System.IO;
using System.Text;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>The same schema as characters, bytes, and a stream yields the same model and the same issues.</summary>
public class SchemaInputTests
{
    private const string Schema =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"urn:t\">\n"
        + "  <xs:element name=\"e\" type=\"xs:string\" minOccurs=\"zero\"/>\n"
        + "  <xs:complexType name=\"c\"><xs:sequence><xs:any namespace=\"##other\"/></xs:sequence><xs:anyAttribute/></xs:complexType>\n"
        + "  <xs:annotation><xs:documentation>Grüße €</xs:documentation></xs:annotation>\n"
        + "</xs:schema>";

    private static string Describe(
        SchemaDocument document,
        System.Collections.Generic.IReadOnlyList<NetEvolve.Schemas.ValidationIssue> issues
    )
    {
        var builder = new StringBuilder(Dump(document.Root!));
        foreach (var issue in issues)
        {
            builder.Append('\n').Append(issue);
        }

        return builder.ToString();
    }

    private static string FromChars()
    {
        var reporter = NewReporter(out var collector);
        var document = SchemaDocumentParser.Parse(Schema.AsSpan(), SystemId, reporter);
        return Describe(document, collector.ToResult().Issues);
    }

    [Test]
    public async Task Model_FromBytes_EqualsTheModelFromChars()
    {
        var bytes = new UTF8Encoding(false).GetBytes(Schema);
        var reporter = NewReporter(out var collector);

        var document = SchemaDocumentParser.Parse(bytes.AsSpan(), SystemId, reporter);

        await Assert.That(Describe(document, collector.ToResult().Issues)).IsEqualTo(FromChars());
    }

    [Test]
    public async Task Model_FromBytesWithByteOrderMark_EqualsTheModelFromChars()
    {
        var bytes = new UTF8Encoding(true).GetPreamble();
        bytes = [.. bytes, .. new UTF8Encoding(false).GetBytes(Schema)];
        var reporter = NewReporter(out var collector);

        var document = SchemaDocumentParser.Parse(bytes.AsSpan(), SystemId, reporter);

        await Assert.That(Describe(document, collector.ToResult().Issues)).IsEqualTo(FromChars());
    }

    [Test]
    public async Task Model_FromStream_EqualsTheModelFromChars()
    {
        using var stream = new MemoryStream(new UTF8Encoding(false).GetBytes(Schema));
        var reporter = NewReporter(out var collector);

        var document = SchemaDocumentParser.Parse(stream, SystemId, reporter);

        await Assert.That(Describe(document, collector.ToResult().Issues)).IsEqualTo(FromChars());
        await Assert.That(stream.CanRead).IsTrue();
    }

    [Test]
    public async Task Model_FromUtf16Bytes_EqualsTheModelFromChars()
    {
        var utf16 = Schema.Replace("encoding=\"UTF-8\"", "encoding=\"UTF-16\"", StringComparison.Ordinal);
        var bytes = new UnicodeEncoding(false, true).GetPreamble();
        bytes = [.. bytes, .. new UnicodeEncoding(false, false).GetBytes(utf16)];
        var reporter = NewReporter(out var collector);
        var document = SchemaDocumentParser.Parse(bytes.AsSpan(), SystemId, reporter);
        var charReporter = NewReporter(out var charCollector);
        var fromChars = SchemaDocumentParser.Parse(utf16.AsSpan(), SystemId, charReporter);

        await Assert
            .That(Describe(document, collector.ToResult().Issues))
            .IsEqualTo(Describe(fromChars, charCollector.ToResult().Issues));
    }

    [Test]
    public async Task Model_FromChars_ContainsTheIssueOfTheInvalidAttributeInEveryInput()
    {
        var description = FromChars();

        await Assert.That(description).Contains("NES4101");
        await Assert.That(description).Contains(SystemId + "(3,41)");
    }
}
