namespace NetEvolve.Schemas.Xsd.Tests.Unit.Versioning.Constructs;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>Every XSD 1.1 construct of the table is a located schema error in strict 1.0 mode and none in 1.1 mode.</summary>
public class Strict10ConstructTests
{
    private const string Xmlns = "xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

    // The schema elements and attributes of XML Schema 1.0 Part 1, section 3 (the oracle for "marked 1.1").
    private static readonly string[] Xsd10Elements =
    [
        "all",
        "annotation",
        "any",
        "anyAttribute",
        "appinfo",
        "attribute",
        "attributeGroup",
        "choice",
        "complexContent",
        "complexType",
        "documentation",
        "element",
        "enumeration",
        "extension",
        "field",
        "fractionDigits",
        "group",
        "import",
        "include",
        "key",
        "keyref",
        "length",
        "list",
        "maxExclusive",
        "maxInclusive",
        "maxLength",
        "minExclusive",
        "minInclusive",
        "minLength",
        "notation",
        "pattern",
        "redefine",
        "restriction",
        "schema",
        "selector",
        "sequence",
        "simpleContent",
        "simpleType",
        "totalDigits",
        "union",
        "unique",
        "whiteSpace",
    ];

    private static readonly string[] Xsd10Attributes =
    [
        "id",
        "targetNamespace",
        "version",
        "attributeFormDefault",
        "elementFormDefault",
        "blockDefault",
        "finalDefault",
        "schemaLocation",
        "namespace",
        "name",
        "final",
        "base",
        "itemType",
        "memberTypes",
        "value",
        "fixed",
        "mixed",
        "abstract",
        "block",
        "processContents",
        "minOccurs",
        "maxOccurs",
        "ref",
        "type",
        "use",
        "default",
        "form",
        "substitutionGroup",
        "nillable",
        "refer",
        "xpath",
        "public",
        "system",
        "source",
    ];

    [Test]
    [Arguments(Open + "  <xs:complexType name=\"c\">^<xs:assert test=\"a &lt; b\"/></xs:complexType>", 4020)]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\">^<xs:assertion test=\"$value gt 0\"/></xs:restriction></xs:simpleType>",
        4021
    )]
    [Arguments(Open + "  <xs:element name=\"e\">^<xs:alternative test=\"@k = 1\" type=\"t\"/></xs:element>", 4022)]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:openContent mode=\"suffix\"><xs:any/></xs:openContent></xs:complexType>",
        4023
    )]
    [Arguments(Open + "  ^<xs:defaultOpenContent><xs:any/></xs:defaultOpenContent>", 4024)]
    [Arguments(Open + "  ^<xs:override schemaLocation=\"a.xsd\"/>", 4025)]
    [Arguments("<xs:schema " + Xmlns + " ^defaultAttributes=\"ag\"/>", 4026)]
    [Arguments(Open + "  <xs:complexType name=\"c\" ^defaultAttributesApply=\"false\"/>", 4027)]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:sequence><xs:any ^notNamespace=\"##local\"/></xs:sequence></xs:complexType>",
        4028
    )]
    [Arguments(Open + "  <xs:complexType name=\"c\"><xs:anyAttribute ^notQName=\"##defined\"/></xs:complexType>", 4029)]
    [Arguments("<xs:schema " + Xmlns + " ^xpathDefaultNamespace=\"##local\"/>", 4030)]
    [Arguments(
        Open + "  <xs:complexType name=\"c\"><xs:attribute name=\"a\" ^inheritable=\"true\"/></xs:complexType>",
        4031
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:sequence><xs:element name=\"e\" ^targetNamespace=\"urn:o\"/></xs:sequence></xs:complexType>",
        4032
    )]
    [Arguments(
        Open + "  <xs:complexType name=\"c\"><xs:attribute name=\"a\" ^targetNamespace=\"urn:o\"/></xs:complexType>",
        4032
    )]
    [Arguments(Open + "  <xs:element name=\"e\" ^targetNamespace=\"urn:o\"/>", 4032)]
    [Arguments(Open + "  <xs:attribute name=\"a\" ^targetNamespace=\"urn:o\"/>", 4032)]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:dateTime\">^<xs:explicitTimezone value=\"required\"/></xs:restriction></xs:simpleType>",
        4033
    )]
    public async Task Check_Strict10_ConstructIsLocatedSchemaError(string marked, int code)
    {
        ArgumentNullException.ThrowIfNull(marked);
        var (text, line, column) = Unmark(marked);
        var document = Parse(text).Document;

        var issues = Run(document.Root!, XsdVersion.V10);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(new DiagnosticCode(code));
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsEqualTo(column);
    }

    [Test]
    [Arguments(Open + "  <xs:complexType name=\"c\"><xs:assert test=\"a &lt; b\"/></xs:complexType>")]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\"><xs:assertion test=\"$value gt 0\"/></xs:restriction></xs:simpleType>"
    )]
    [Arguments(Open + "  <xs:element name=\"e\"><xs:alternative test=\"@k = 1\" type=\"t\"/></xs:element>")]
    [Arguments(Open + "  <xs:complexType name=\"c\"><xs:openContent><xs:any/></xs:openContent></xs:complexType>")]
    [Arguments(Open + "  <xs:defaultOpenContent><xs:any/></xs:defaultOpenContent>")]
    [Arguments(Open + "  <xs:override schemaLocation=\"a.xsd\"/>")]
    [Arguments("<xs:schema " + Xmlns + " defaultAttributes=\"ag\" xpathDefaultNamespace=\"##local\"/>")]
    [Arguments(Open + "  <xs:complexType name=\"c\" defaultAttributesApply=\"false\"/>")]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:sequence><xs:any notNamespace=\"##local\"/></xs:sequence><xs:anyAttribute notQName=\"##defined\"/></xs:complexType>"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:sequence><xs:element name=\"e\" targetNamespace=\"urn:o\"/></xs:sequence><xs:attribute name=\"a\" inheritable=\"true\" targetNamespace=\"urn:o\"/></xs:complexType>"
    )]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:dateTime\"><xs:explicitTimezone value=\"required\"/></xs:restriction></xs:simpleType>"
    )]
    public async Task Check_Version11_ReportsNothing(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var document = Parse(text.StartsWith(Open, StringComparison.Ordinal) ? text + Close : text).Document;

        var issues = Run(document.Root!, XsdVersion.V11);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_AssertionFacet_Strict10_IsLocatedSchemaError_Version11_Nothing()
    {
        var facet = new FakeSyntaxElement("assertion", 7).WithAttribute("test", "$value gt 0");
        var root = new FakeSyntaxElement("schema", 1).WithChild(
            new FakeSyntaxElement("simpleType", 3)
                .WithAttribute("name", "t")
                .WithChild(new FakeSyntaxElement("restriction", 5).WithAttribute("base", "xs:int").WithChild(facet))
        );

        var strict = Run(root, XsdVersion.V10);
        var lax = Run(root, XsdVersion.V11);

        await Assert.That(strict.Count).IsEqualTo(1);
        await Assert.That(strict[0].Code).IsEqualTo(new DiagnosticCode(4021));
        await Assert.That(strict[0].Location.Line).IsEqualTo(7);
        await Assert.That(lax.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Strict10_GlobalDeclarationsAndOneZeroSyntax_ReportNothing()
    {
        var text =
            Open
            + "  <xs:element name=\"g\" type=\"xs:string\"/>\n  <xs:attribute name=\"ga\" type=\"xs:string\"/>\n"
            + "  <xs:complexType name=\"c\"><xs:sequence><xs:any namespace=\"##other\"/></xs:sequence></xs:complexType>"
            + Close;
        var document = Parse(text).Document;

        await Assert.That(Run(document.Root!, XsdVersion.V10).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Strict10_VersionControlAttributesAreIgnoredAndTheElementIsProcessed()
    {
        var text =
            "<xs:schema "
            + Xmlns
            + " xmlns:vc=\"http://www.w3.org/2007/XMLSchema-versioning\" vc:minVersion=\"1.1\" vc:maxVersion=\"1.0\" vc:typeAvailable=\"xs:foo\" vc:typeUnavailable=\"xs:bar\" vc:facetAvailable=\"xs:baz\" vc:facetUnavailable=\"xs:qux\">"
            + "<xs:element name=\"e\" vc:minVersion=\"1.1\" type=\"xs:string\"/></xs:schema>";
        var (document, parseIssues) = Parse(text);

        var issues = Run(document.Root!, XsdVersion.V10);

        await Assert.That(parseIssues.Count).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Find(document, "element").FindAttribute("type")).IsEqualTo("xs:string");
    }

    [Test]
    public async Task Check_Strict10_ReportsEveryConstructInDocumentOrder()
    {
        var text =
            Open
            + "  <xs:complexType name=\"c\">\n    <xs:assert test=\"true()\"/>\n    <xs:openContent><xs:any notNamespace=\"##local\"/></xs:openContent>\n  </xs:complexType>"
            + Close;
        var document = Parse(text).Document;

        var issues = Run(document.Root!, XsdVersion.V10);

        await Assert.That(issues.Count).IsEqualTo(3);
        await Assert.That(issues[0].Code).IsEqualTo(new DiagnosticCode(4020));
        await Assert.That(issues[1].Code).IsEqualTo(new DiagnosticCode(4023));
        await Assert.That(issues[2].Code).IsEqualTo(new DiagnosticCode(4028));
    }

    [Test]
    public async Task Table_IsTheSingleSource_EveryGrammarConstructOutsideXsd10HasAnEntry()
    {
        var missing = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<(string Name, string Parent)>();
        queue.Enqueue(("schema", string.Empty));
        while (queue.Count > 0)
        {
            var (name, parent) = queue.Dequeue();
            if (!seen.Add(name + "@" + parent))
            {
                continue;
            }

            var rule = SchemaGrammar.Find(name, parent);
            if (Array.IndexOf(Xsd10Elements, name) < 0)
            {
                if (!Strict10ConstructTable.HasElement(name))
                {
                    missing.Add("element " + name);
                }
            }
            else
            {
                foreach (var attribute in rule.Attributes)
                {
                    if (
                        Array.IndexOf(Xsd10Attributes, attribute.Name) < 0
                        && !Strict10ConstructTable.HasAttribute(attribute.Name)
                    )
                    {
                        missing.Add("attribute " + attribute.Name + " on " + name);
                    }
                }
            }

            foreach (var child in rule.Children)
            {
                queue.Enqueue((child, name));
            }
        }

        await Assert.That(string.Join(", ", missing)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Table_EveryEntryHasAStrict10Rule()
    {
        foreach (var entry in Strict10ConstructTable.Entries)
        {
            await Assert.That(entry.Rule.Kind).IsEqualTo(XsdRuleKind.Strict10Rejection);
        }
    }

    [Test]
    public async Task Check_NullArguments_Throw()
    {
        var reporter = NewReporter(out _);
        var root = new FakeSyntaxElement("schema", 1);

        await Assert
            .That(() => Strict10Constructs.Check(null!, XsdVersion.V10, reporter))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Strict10Constructs.Check(root, XsdVersion.V10, null!)).Throws<ArgumentNullException>();
    }

    private static IReadOnlyList<ValidationIssue> Run(ISchemaSyntaxElement root, XsdVersion version)
    {
        var reporter = NewReporter(out var collector);
        Strict10Constructs.Check(root, version, reporter);
        return collector.ToResult().Issues;
    }
}
