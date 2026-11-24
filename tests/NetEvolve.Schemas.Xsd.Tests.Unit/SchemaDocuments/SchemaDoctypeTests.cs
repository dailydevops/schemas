namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>A schema document may carry a document type declaration; it is read under the limits of the XML reader.</summary>
public class SchemaDoctypeTests
{
    private const string InternalSubset =
        "<!DOCTYPE xs:schema [ <!ENTITY name 'e'> ]>\n"
        + "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:element name=\"&name;\" type=\"xs:string\"/></xs:schema>";

    // The shape of the XML Signature schema bundled with UBL: PUBLIC identifier, system literal, entity declarations.
    private const string SignatureShape =
        "<!DOCTYPE schema PUBLIC \"-//W3C//DTD XMLSchema 200102//EN\" \"http://www.w3.org/2001/XMLSchema.dtd\" [\n"
        + "  <!ATTLIST schema xmlns:ds CDATA #FIXED 'http://www.w3.org/2000/09/xmldsig#'>\n"
        + "  <!ENTITY dsig 'http://www.w3.org/2000/09/xmldsig#'>\n"
        + "]>\n"
        + "<schema xmlns=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"&dsig;\"><element name=\"e\" type=\"string\"/></schema>";

    [Test]
    public async Task Parse_InternalSubsetEntity_IsExpandedInTheSchema()
    {
        var reporter = NewReporter(out _);

        var document = SchemaDocumentParser.Parse(InternalSubset.AsSpan(), SystemId, reporter);

        await Assert.That(document.IsComplete).IsTrue();
        await Assert.That(Dump(document.Root!)).Contains("name=e");
    }

    [Test]
    public async Task Parse_SignatureSchemaShape_IsReadWithoutTheExternalSubset()
    {
        var reporter = NewReporter(out _);

        var document = SchemaDocumentParser.Parse(SignatureShape.AsSpan(), SystemId, reporter);

        await Assert.That(document.IsComplete).IsTrue();
        await Assert.That(Dump(document.Root!)).Contains("http://www.w3.org/2000/09/xmldsig#");
    }

    [Test]
    public async Task Parse_EntityBomb_IsRefusedByTheExpansionLimit()
    {
        var bomb =
            "<!DOCTYPE xs:schema [ <!ENTITY a 'aaaaaaaaaa'> <!ENTITY b '&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;'> "
            + "<!ENTITY c '&b;&b;&b;&b;&b;&b;&b;&b;&b;&b;'> <!ENTITY d '&c;&c;&c;&c;&c;&c;&c;&c;&c;&c;'> "
            + "<!ENTITY e '&d;&d;&d;&d;&d;&d;&d;&d;&d;&d;'> <!ENTITY f '&e;&e;&e;&e;&e;&e;&e;&e;&e;&e;'> "
            + "<!ENTITY g '&f;&f;&f;&f;&f;&f;&f;&f;&f;&f;'> <!ENTITY h '&g;&g;&g;&g;&g;&g;&g;&g;&g;&g;'> ]>\n"
            + "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:annotation><xs:documentation>&h;&h;&h;&h;&h;&h;&h;&h;&h;&h;</xs:documentation></xs:annotation></xs:schema>";
        var (document, _) = Parse(bomb);

        await Assert.That(document.IsComplete).IsFalse();
        await Assert.That(document.ReadError!.Code).IsEqualTo(XmlErrorCodes.XML1109);
    }
}
