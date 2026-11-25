namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using System.IO;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>Structural errors of a schema document are located schema errors with the rule code of their element.</summary>
public class SchemaErrorTests
{
    private const string Xmlns = "xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

    private static DiagnosticCode CodeOf(string ruleId)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
            {
                return rule.Code;
            }
        }

        throw new InvalidOperationException("No rule " + ruleId);
    }

    [Test]
    // Misplaced elements: not allowed in the parent, foreign, unknown, or annotation after other children.
    [Arguments(Open + "  <xs:element name=\"e\">^<xs:attribute name=\"a\"/></xs:element>", "src-element")]
    [Arguments(Open + "  <xs:attribute name=\"a\">^<xs:element name=\"e\"/></xs:attribute>", "src-attribute")]
    [Arguments(Open + "  ^<foreign/>", "sch-props-correct")]
    [Arguments(Open + "  ^<xs:frob/>", "sch-props-correct")]
    [Arguments(
        Open
            + "  <xs:element name=\"e\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType>^<xs:annotation/></xs:element>",
        "src-element"
    )]
    [Arguments(Open + "  <xs:complexType name=\"c\">^<xs:element name=\"e\"/></xs:complexType>", "src-ct")]
    [Arguments(
        Open + "  <xs:complexType name=\"c\"><xs:sequence>^<xs:attribute name=\"a\"/></xs:sequence></xs:complexType>",
        "src-model_group"
    )]
    [Arguments(Open + "  <xs:simpleType name=\"t\">^<xs:sequence/></xs:simpleType>", "src-simple-type")]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\">^<xs:sequence/></xs:restriction></xs:simpleType>",
        "src-simple-type"
    )]
    [Arguments(Open + "  <xs:annotation>^<xs:element name=\"e\"/></xs:annotation>", "src-annotation")]
    // Unknown attributes in no namespace and in the schema namespace.
    [Arguments(Open + "  ^<xs:element name=\"e\" bogus=\"1\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" xs:name=\"1\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:attribute name=\"a\" minOccurs=\"1\"/>", "src-attribute")]
    [Arguments(Open + "  ^<xs:complexType name=\"c\" nillable=\"true\"/>", "src-ct")]
    [Arguments(Open + "  ^<xs:simpleType name=\"t\" mixed=\"true\"/>", "src-simple-type")]
    [Arguments(
        Open + "  <xs:complexType name=\"c\"><xs:sequence>^<xs:any bogus=\"1\"/></xs:sequence></xs:complexType>",
        "src-wildcard"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\">^<xs:key name=\"k\" refer=\"x\"><xs:selector xpath=\"a\"/><xs:field xpath=\"b\"/></xs:key></xs:element>",
        "src-identity-constraint"
    )]
    [Arguments(Open + "  ^<xs:notation name=\"n\" bogus=\"1\"/>", "src-notation")]
    [Arguments(Open + "  ^<xs:annotation bogus=\"1\"/>", "src-annotation")]
    // Invalid attribute values.
    [Arguments(Open + "  ^<xs:element name=\"e\" minOccurs=\"abc\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" minOccurs=\"-1\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" maxOccurs=\"many\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" nillable=\"yes\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" form=\"other\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"1e\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"a:b\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" type=\"a:b:c\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" type=\"\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:element name=\"e\" block=\"foo\"/>", "src-element")]
    [Arguments(Open + "  ^<xs:attribute name=\"a\" use=\"mandatory\"/>", "src-attribute")]
    [Arguments(
        Open + "  <xs:complexType name=\"c\">^<xs:anyAttribute processContents=\"maybe\"/></xs:complexType>",
        "src-wildcard"
    )]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:string\">^<xs:whiteSpace value=\"squeeze\"/></xs:restriction></xs:simpleType>",
        "src-simple-type"
    )]
    [Arguments(Open + "  <xs:complexType name=\"c\">^<xs:openContent mode=\"first\"/></xs:complexType>", "src-ct")]
    [Arguments(
        Open + "  <xs:complexType name=\"c\">^<xs:openContent mode=\"interleaved\"/></xs:complexType>",
        "src-ct"
    )]
    [Arguments(Open + "  ^<xs:defaultOpenContent mode=\"interleaved\"><xs:any/></xs:defaultOpenContent>", "src-ct")]
    [Arguments(Open + "  ^<xs:complexType name=\"c\" mixed=\"2\"/>", "src-ct")]
    [Arguments(Open + "  <xs:simpleType name=\"t\">^<xs:union memberTypes=\"1x\"/></xs:simpleType>", "src-simple-type")]
    [Arguments("^<xs:schema " + Xmlns + " elementFormDefault=\"both\"/>", "sch-props-correct")]
    [Arguments("^<xs:schema " + Xmlns + " targetNamespace=\"\"/>", "sch-props-correct")]
    [Arguments("^<xs:schema " + Xmlns + " targetNamespace=\" \"/>", "sch-props-correct")]
    // Missing required attributes.
    [Arguments(Open + "  ^<xs:include/>", "src-include")]
    [Arguments(Open + "  ^<xs:redefine/>", "src-redefine")]
    [Arguments(Open + "  ^<xs:override/>", "src-override")]
    [Arguments(
        Open
            + "  <xs:element name=\"e\"><xs:key name=\"k\">^<xs:selector/><xs:field xpath=\"a\"/></xs:key></xs:element>",
        "src-identity-constraint"
    )]
    [Arguments(Open + "  ^<xs:notation/>", "src-notation")]
    [Arguments(
        Open + "  <xs:complexType name=\"c\"><xs:complexContent>^<xs:extension/></xs:complexContent></xs:complexType>",
        "src-ct"
    )]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\">^<xs:length/></xs:restriction></xs:simpleType>",
        "src-simple-type"
    )]
    [Arguments(Open + "  <xs:complexType name=\"c\">^<xs:assert/></xs:complexType>", "src-ct")]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\">^<xs:assertion/></xs:restriction></xs:simpleType>",
        "src-simple-type"
    )]
    // assert is the complex type assertion, not a facet; assertion is the facet, not a complex type child.
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\">^<xs:assert test=\"$value gt 0\"/></xs:restriction></xs:simpleType>",
        "src-simple-type"
    )]
    [Arguments(Open + "  <xs:complexType name=\"c\">^<xs:assertion test=\"1\"/></xs:complexType>", "src-ct")]
    // Either or both of an attribute and a simpleType child.
    [Arguments(
        Open + "  <xs:simpleType name=\"t\">^<xs:restriction/></xs:simpleType>",
        "src-restriction-base-or-simpleType"
    )]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\">^<xs:restriction base=\"xs:int\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:restriction></xs:simpleType>",
        "src-restriction-base-or-simpleType"
    )]
    [Arguments(Open + "  <xs:simpleType name=\"t\">^<xs:list/></xs:simpleType>", "src-list-itemType-or-simpleType")]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\">^<xs:list itemType=\"xs:int\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:list></xs:simpleType>",
        "src-list-itemType-or-simpleType"
    )]
    [Arguments(
        Open + "  <xs:simpleType name=\"t\">^<xs:union/></xs:simpleType>",
        "src-union-memberTypes-or-simpleTypes"
    )]
    public async Task Error_IsReportedAtTheElementWithTheRuleCode(string marked, string ruleId)
    {
        ArgumentNullException.ThrowIfNull(marked);

        var (text, line, column) = Unmark(marked);

        var (_, issues) = Parse(text);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf(ruleId));
        await Assert.That(issues[0].Constraint).IsEqualTo(ruleId);
        await Assert.That(issues[0].Location.Source).IsEqualTo(SystemId);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task Error_MisplacedElement_SkipsItsSubtreeWithoutFollowUpIssues()
    {
        var (document, issues) = Parse(
            Open
                + "<xs:attribute name=\"a\"><xs:sequence><xs:foo bar=\"1\"/><xs:any/></xs:sequence></xs:attribute>"
                + Close
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(Find(document, "attribute").ChildCount).IsEqualTo(0);
    }

    [Test]
    public async Task Error_MultipleProblems_AreAllReportedInDocumentOrder()
    {
        var (_, issues) = Parse(Open + "<xs:element name=\"e\" bogus=\"1\" minOccurs=\"x\"/>\n<xs:include/>" + Close);

        await Assert.That(issues.Count).IsEqualTo(3);
        await Assert.That(issues[0].Constraint).IsEqualTo("src-element");
        await Assert.That(issues[1].Constraint).IsEqualTo("src-element");
        await Assert.That(issues[2].Constraint).IsEqualTo("src-include");
        await Assert.That(issues[2].Location.Line).IsEqualTo(3);
    }

    [Test]
    [Arguments("<xs:element " + Xmlns + " name=\"e\"/>")]
    [Arguments("<schema xmlns=\"urn:other\"/>")]
    [Arguments("<schema/>")]
    public async Task Error_RootThatIsNotTheSchemaElement_IsRejectedAndSkipped(string text)
    {
        var (document, issues) = Parse(text);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("sch-props-correct");
        await Assert.That(issues[0].Location.Line).IsEqualTo(1);
        await Assert.That(issues[0].Location.Column).IsEqualTo(1);
        await Assert.That(document.Root).IsNull();
        await Assert.That(document.IsComplete).IsTrue();
    }

    [Test]
    public async Task Error_IllFormedDocument_ReturnsTheReaderErrorAndNoRoot()
    {
        var (document, _) = Parse(Open + "<xs:element name=\"e\">" + Close);

        await Assert.That(document.IsComplete).IsFalse();
        await Assert.That(document.Root).IsNull();
        await Assert.That(document.ReadError).IsNotNull();
    }

    [Test]
    public async Task Parse_NullReporter_Throws()
    {
        await Assert.That(() => SchemaDocumentParser.Parse(Open.AsSpan(), null, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => SchemaDocumentParser.Parse(new MemoryStream(), null, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => SchemaDocumentParser.Parse((Stream)null!, null, NewReporter(out _)))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => SchemaDocumentParser.Parse((IXmlReader)null!, null, NewReporter(out _)))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Rules_UsedByTheParser_ExistInTheCatalog()
    {
        await Assert.That(FakeRules.Find(XsdRuleCodes.SrcElement).Id).IsEqualTo("src-element");
        await Assert.That(CodeOf("sch-props-correct")).IsEqualTo(XsdRuleCodes.SchPropsCorrect);
    }
}
