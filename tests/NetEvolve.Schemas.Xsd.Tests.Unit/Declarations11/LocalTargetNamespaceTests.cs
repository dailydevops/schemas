namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11.SyntaxFixture;

public class LocalTargetNamespaceTests
{
    private const string Element = "<xs:element name=\"e\" targetNamespace=\"urn:o\"/>";
    private const string Attribute = "<xs:attribute name=\"a\" targetNamespace=\"urn:o\"/>";

    private static string InRestriction(string declaration, string schema = WithNamespace) =>
        schema
        + "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"b:t\"><xs:sequence>"
        + declaration
        + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
        + EndOfSchema;

    private static string AttributeInRestriction(string declaration, string schema = WithNamespace) =>
        schema
        + "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"b:t\">"
        + declaration
        + "</xs:restriction></xs:complexContent></xs:complexType>"
        + EndOfSchema;

    // The caret marks the declaration that the check must report, with the clause and the rule it reports.
    [Test]
    [Arguments("  ^" + Element, "src-element.4.3.1", 4101)]
    [Arguments(
        "  <xs:group name=\"g\"><xs:sequence>^" + Element + "</xs:sequence></xs:group>",
        "src-element.4.3.1",
        4101
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:sequence>^" + Element + "</xs:sequence></xs:complexType>",
        "src-element.4.3.2",
        4101
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"b:t\"><xs:sequence>^"
            + Element
            + "</xs:sequence></xs:extension></xs:complexContent></xs:complexType>",
        "src-element.4.3.2",
        4101
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"xs:anyType\"><xs:sequence>^"
            + Element
            + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType>",
        "src-element.4.3.2",
        4101
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"b:t\"><xs:sequence>"
            + "<xs:element name=\"outer\"><xs:complexType><xs:complexContent><xs:extension base=\"b:t\"><xs:sequence>^"
            + Element
            + "</xs:sequence></xs:extension></xs:complexContent></xs:complexType></xs:element>"
            + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType>",
        "src-element.4.3.2",
        4101
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"b:t\"><xs:sequence>^<xs:element ref=\"b:x\" targetNamespace=\"urn:o\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>",
        "src-element.4.1",
        4101
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"b:t\"><xs:sequence>^<xs:element name=\"e\" form=\"qualified\" targetNamespace=\"urn:o\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>",
        "src-element.4.2",
        4101
    )]
    [Arguments("  ^" + Attribute, "src-attribute.6.3.1", 4100)]
    [Arguments("  <xs:attributeGroup name=\"g\">^" + Attribute + "</xs:attributeGroup>", "src-attribute.6.3.1", 4100)]
    [Arguments("  <xs:complexType name=\"c\">^" + Attribute + "</xs:complexType>", "src-attribute.6.3.2", 4100)]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:simpleContent><xs:extension base=\"b:t\">^"
            + Attribute
            + "</xs:extension></xs:simpleContent></xs:complexType>",
        "src-attribute.6.3.2",
        4100
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"xs:anyType\">^"
            + Attribute
            + "</xs:restriction></xs:complexContent></xs:complexType>",
        "src-attribute.6.3.2",
        4100
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"b:t\">^<xs:attribute ref=\"b:a\" targetNamespace=\"urn:o\"/></xs:restriction></xs:complexContent></xs:complexType>",
        "src-attribute.6.1",
        4100
    )]
    [Arguments(
        "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"b:t\">^<xs:attribute name=\"a\" form=\"qualified\" targetNamespace=\"urn:o\"/></xs:restriction></xs:complexContent></xs:complexType>",
        "src-attribute.6.2",
        4100
    )]
    public async Task Check_Version11_ViolationIsALocatedSchemaError(string marked, string clause, int code)
    {
        ArgumentNullException.ThrowIfNull(marked);
        var (root, line, column) = ParseMarked(WithNamespace + marked + EndOfSchema);
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(root, XsdVersion.V11, reporter);

        var issues = Issues(collector);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(new DiagnosticCode(code));
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo(clause);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task Check_Version11_AnElementInTheRestrictionOfAnotherNamespace_IsAccepted()
    {
        var root = ParseDocument(InRestriction(Element));
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(root, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Version11_AnAttributeInAComplexContentRestriction_IsAccepted()
    {
        var root = ParseDocument(AttributeInRestriction(Attribute));
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(root, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Version11_AnAttributeInASimpleContentRestriction_IsAccepted()
    {
        var text =
            WithNamespace
            + "  <xs:complexType name=\"c\"><xs:simpleContent><xs:restriction base=\"b:t\">"
            + Attribute
            + "</xs:restriction></xs:simpleContent></xs:complexType>"
            + EndOfSchema;
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(ParseDocument(text), XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Version11_AnElementInASchemaWithoutNamespace_IsAcceptedInARestriction()
    {
        var root = ParseDocument(InRestriction(Element, WithoutNamespace));
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(root, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Version11_TheNamespaceOfTheSchema_IsAcceptedWhereverTheDeclarationIs()
    {
        var text =
            WithNamespace
            + "  <xs:element name=\"g\" targetNamespace=\"urn:s\"/>\n"
            + "  <xs:attribute name=\"ga\" targetNamespace=\"urn:s\"/>\n"
            + "  <xs:complexType name=\"c\"><xs:sequence><xs:element name=\"e\" targetNamespace=\"urn:s\"/></xs:sequence>"
            + "<xs:attribute name=\"a\" targetNamespace=\"urn:s\"/></xs:complexType>"
            + EndOfSchema;
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(ParseDocument(text), XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Version11_ABaseWithAnUndeclaredPrefix_DoesNotMatchAnyType()
    {
        var text =
            WithNamespace
            + "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"q:t\"><xs:sequence>"
            + Element
            + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            + EndOfSchema;
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(ParseDocument(text), XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Version11_ARestrictionWithoutABase_IsNotAnyType_AndItsMissingBaseIsNotReportedHere()
    {
        var text =
            WithNamespace
            + "  <xs:complexType name=\"c\"><xs:complexContent><xs:restriction><xs:sequence>"
            + Element
            + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            + EndOfSchema;
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(SchemaDocuments.SchemaFixture.Parse(text).Document.Root!, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Check_Version11_AnUnprefixedBase_IsResolvedWithTheDefaultNamespace()
    {
        var anyType =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"urn:s\">"
            + "<xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"anyType\"><xs:sequence>"
            + Element
            + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType></xs:schema>";
        var other = anyType.Replace("base=\"anyType\"", "base=\"t\"", StringComparison.Ordinal);
        var reporter = Reporter(out var collector);

        LocalTargetNamespace.Check(ParseDocument(anyType), XsdVersion.V11, reporter);
        var anyTypeIssues = Issues(collector).Count;
        LocalTargetNamespace.Check(ParseDocument(other), XsdVersion.V11, reporter);

        await Assert.That(anyTypeIssues).IsEqualTo(1);
        await Assert.That(Issues(collector).Count).IsEqualTo(1);
    }

    [Test]
    public async Task Check_Strict10_ReportsNothing_BecauseTheConstructTableRejectsTheAttribute()
    {
        var root = ParseDocument(InRestriction(Element));
        var reporter = Reporter(out var collector);
        var constructReporter = Reporter(out var constructCollector);

        LocalTargetNamespace.Check(root, XsdVersion.V10, reporter);
        Strict10Constructs.Check(root, XsdVersion.V10, constructReporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
        await Assert.That(Issues(constructCollector).Count).IsEqualTo(1);
        await Assert.That(Issues(constructCollector)[0].Code).IsEqualTo(XsdRuleCodes.Strict10LocalTargetNamespace);
    }

    [Test]
    public async Task TryGetName_ADeclarationWithATargetNamespace_GivesTheDeclaredExpandedName()
    {
        var root = ParseDocument(InRestriction(Element));
        var declaration = FindNode(root, "complexType/complexContent/restriction/sequence/element");

        var found = LocalTargetNamespace.TryGetName(declaration, XsdVersion.V11, out var name);

        await Assert.That(found).IsTrue();
        await Assert.That(name).IsEqualTo(new XPathQName("urn:o", "e"));
    }

    [Test]
    public async Task TryGetName_AnAttribute_GivesTheNameOfTheDeclarationFactory()
    {
        var root = ParseDocument(AttributeInRestriction(Attribute));
        var declaration = FindNode(root, "complexType/complexContent/restriction/attribute");
        var reporter = Reporter(out var collector);
        var type = TypeFixture.Xs("string");

        var found = LocalTargetNamespace.TryGetName(declaration, XsdVersion.V11, out var name);
        var built = AttributeDeclaration.Local(
            name.LocalName,
            name.NamespaceName,
            AttributeForm.Qualified,
            declaration.Location,
            type,
            default,
            reporter
        );

        await Assert.That(found).IsTrue();
        await Assert.That(built!.Name).IsEqualTo(new XPathQName("urn:o", "a"));
        await Assert.That(Issues(collector).Count).IsEqualTo(0);
    }

    [Test]
    public async Task TryGetName_Version10_AttributeIsNotPartOfTheLanguage()
    {
        var root = ParseDocument(InRestriction(Element));
        var declaration = FindNode(root, "complexType/complexContent/restriction/sequence/element");

        var found = LocalTargetNamespace.TryGetName(declaration, XsdVersion.V10, out var name);

        await Assert.That(found).IsFalse();
        await Assert.That(name).IsEqualTo(default(XPathQName));
    }

    [Test]
    [Arguments("<xs:element name=\"e\"/>")]
    [Arguments("<xs:element ref=\"b:x\" targetNamespace=\"urn:o\"/>")]
    public async Task TryGetName_WithoutTheAttributeOrTheName_GivesNoName(string declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        var root = ParseDocument(InRestriction(declaration));
        var node = FindNode(root, "complexType/complexContent/restriction/sequence/element");

        var found = LocalTargetNamespace.TryGetName(node, XsdVersion.V11, out var name);

        await Assert.That(found).IsFalse();
        await Assert.That(name).IsEqualTo(default(XPathQName));
    }

    [Test]
    public async Task Check_NullArguments_Throw()
    {
        var root = ParseDocument(InRestriction(Element));
        var reporter = Reporter(out _);

        await Assert
            .That(() => LocalTargetNamespace.Check(null!, XsdVersion.V11, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => LocalTargetNamespace.Check(root, XsdVersion.V11, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => LocalTargetNamespace.TryGetName(null!, XsdVersion.V11, out _))
            .Throws<ArgumentNullException>();
    }
}
