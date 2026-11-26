namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using System.Linq;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>The document checks of the pipeline: the order and number of the children, the id attributes, and the normalized values.</summary>
public class StructureTests
{
    private static SchemaSyntaxNode Parse(string body)
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        return SchemaDocumentParser.Parse(Schema(body).AsSpan(), RootUri, new XsdIssueReporter(collector)).Root!;
    }

    private static bool IsValid(string body) => AllValid(Parse(body));

    private static bool AllValid(SchemaSyntaxNode node)
    {
        var valid =
            !string.Equals(node.Name.NamespaceName, Xs, StringComparison.Ordinal) || SchemaStructure.IsValid(node);
        for (var i = 0; valid && i < node.ChildCount; i++)
        {
            valid = AllValid(node.GetChildNode(i));
        }

        return valid;
    }

    [Test]
    [Arguments(62, true)]
    [Arguments(63, true)]
    [Arguments(64, true)]
    [Arguments(65, true)]
    [Arguments(200, true)]
    [Arguments(63, false)]
    [Arguments(64, false)]
    [Arguments(200, false)]
    public async Task IsValid_ForALongListOfChildren_FollowsTheModel(int count, bool valid)
    {
        // The children of the schema element cross the 64 positions that a position set holds inline. An include after the
        // declarations breaks the order of the model at the far end of the list.
        var declarations = string.Concat(Enumerable.Repeat("<xs:element name=\"e\"/>", count));
        var body = valid ? declarations : declarations + "<xs:include schemaLocation=\"a.xsd\"/>";

        await Assert.That(IsValid(body)).IsEqualTo(valid);
    }

    [Test]
    [Arguments(63, 1, true)]
    [Arguments(64, 64, true)]
    [Arguments(127, 1, true)]
    [Arguments(128, 129, true)]
    [Arguments(191, 2, true)]
    [Arguments(192, 192, true)]
    [Arguments(300, 5, true)]
    [Arguments(64, 64, false)]
    [Arguments(128, 129, false)]
    [Arguments(192, 192, false)]
    public async Task IsValid_ForChildrenThatCrossWordBoundaries_FollowsTheModel(
        int imports,
        int declarations,
        bool valid
    )
    {
        // A step of the model reaches neighbors of a position, so a set usually sits in one 64-bit word; the sets here straddle words
        // (an import list followed by declarations, with an include that breaks the order after the declarations when invalid).
        var body =
            string.Concat(Enumerable.Repeat("<xs:import namespace=\"urn:a\"/>", imports))
            + string.Concat(Enumerable.Repeat("<xs:element name=\"e\"/><xs:annotation/>", declarations))
            + (valid ? string.Empty : "<xs:include schemaLocation=\"a.xsd\"/>");

        await Assert.That(IsValid(body)).IsEqualTo(valid);
    }

    [Test]
    public async Task GetChild_ForAnElementWithoutChildren_Throws()
    {
        var root = Parse("<xs:element name=\"e\"/>");
        var leaf = root.GetChildNode(0);

        await Assert.That(leaf.ChildCount).IsEqualTo(0);
        await Assert.That(leaf.CountChildren("annotation")).IsEqualTo(0);
        await Assert.That(() => leaf.GetChildNode(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => leaf.GetChild(0)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Parse_Attributes_AreKeptInDocumentOrderAndFoundByName()
    {
        var root = Parse("<xs:element name=\"e\" type=\"xs:int\" minOccurs=\"0\"/>");
        var element = root.GetChildNode(0);

        await Assert.That(element.AttributeCount).IsEqualTo(3);
        await Assert.That(element.GetAttribute(0).Name.LocalName).IsEqualTo("name");
        await Assert.That(element.GetAttribute(2).Value).IsEqualTo("0");
        await Assert.That(element.FindAttribute("type")).IsEqualTo("xs:int");
        await Assert.That(element.FindAttribute("ref")).IsNull();
        await Assert.That(root.AttributeCount).IsGreaterThan(0);
    }

    [Test]
    [Arguments("<xs:simpleType name=\"s\"><xs:annotation/><xs:restriction base=\"xs:int\"/></xs:simpleType>")]
    [Arguments(
        "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:annotation/><xs:minInclusive value=\"1\"/><xs:pattern value=\"1\"/><xs:pattern value=\"2\"/></xs:restriction></xs:simpleType>"
    )]
    [Arguments(
        "<xs:simpleType name=\"s\"><xs:list><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:list></xs:simpleType>"
    )]
    [Arguments("<xs:simpleType name=\"s\"><xs:union memberTypes=\"xs:int\"/></xs:simpleType>")]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:annotation/><xs:sequence/><xs:attribute name=\"a\"/><xs:attributeGroup ref=\"t:g\"/><xs:anyAttribute/></xs:complexType>"
    )]
    [Arguments("<xs:complexType name=\"c\"><xs:attribute name=\"a\"/></xs:complexType>")]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:simpleContent><xs:extension base=\"xs:int\"><xs:attribute name=\"a\"/></xs:extension></xs:simpleContent></xs:complexType>"
    )]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:simpleContent><xs:restriction base=\"t:b\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType><xs:maxInclusive value=\"3\"/><xs:attribute name=\"a\"/></xs:restriction></xs:simpleContent></xs:complexType>"
    )]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"t:b\"><xs:choice/><xs:anyAttribute/></xs:extension></xs:complexContent></xs:complexType>"
    )]
    [Arguments(
        "<xs:element name=\"e\"><xs:complexType/><xs:unique name=\"u\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@b\"/><xs:field xpath=\"@c\"/></xs:unique></xs:element>"
    )]
    [Arguments(
        "<xs:group name=\"g\"><xs:choice><xs:element name=\"a\"/><xs:any/><xs:group ref=\"t:h\"/></xs:choice></xs:group>"
    )]
    [Arguments(
        "<xs:attributeGroup name=\"g\"><xs:attribute name=\"a\"/><xs:attributeGroup ref=\"t:h\"/><xs:anyAttribute/></xs:attributeGroup>"
    )]
    [Arguments(
        "<xs:attribute name=\"a\"><xs:annotation/><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:attribute>"
    )]
    [Arguments(
        "<xs:redefine schemaLocation=\"a.xsd\"><xs:annotation/><xs:simpleType name=\"s\"><xs:restriction base=\"t:s\"/></xs:simpleType></xs:redefine>"
    )]
    [Arguments("<xs:notation name=\"n\" public=\"p\"/>")]
    [Arguments("<xs:include schemaLocation=\"a.xsd\"><xs:annotation/></xs:include>")]
    public async Task IsValid_ForChildrenInTheOrderOfTheModel_IsTrue(string element) =>
        await Assert.That(IsValid(element)).IsTrue();

    [Test]
    [Arguments("<xs:simpleType name=\"s\"/>")]
    [Arguments(
        "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"/><xs:list itemType=\"xs:int\"/></xs:simpleType>"
    )]
    [Arguments(
        "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:minInclusive value=\"1\"/><xs:simpleType/></xs:restriction></xs:simpleType>"
    )]
    [Arguments("<xs:complexType name=\"c\"><xs:attribute name=\"a\"/><xs:sequence/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:anyAttribute/><xs:anyAttribute/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:sequence/><xs:sequence/></xs:complexType>")]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:simpleContent><xs:extension base=\"xs:int\"/></xs:simpleContent><xs:attribute name=\"a\"/></xs:complexType>"
    )]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"t:b\"/><xs:restriction base=\"t:b\"/></xs:complexContent></xs:complexType>"
    )]
    [Arguments(
        "<xs:element name=\"e\"><xs:unique name=\"u\"><xs:field xpath=\"a\"/><xs:selector xpath=\"b\"/></xs:unique></xs:element>"
    )]
    [Arguments("<xs:element name=\"e\"><xs:unique name=\"u\"><xs:selector xpath=\"a\"/></xs:unique></xs:element>")]
    [Arguments("<xs:group name=\"g\"/>")]
    [Arguments("<xs:attribute name=\"a\"><xs:simpleType/><xs:simpleType/></xs:attribute>")]
    public async Task IsValid_ForChildrenThatBreakTheModel_IsFalse(string element) =>
        await Assert.That(IsValid(element)).IsFalse();

    [Test]
    public async Task IsValid_ForAnElementWithoutAModelOrWithChildrenOfAnotherNamespace_IsTrue()
    {
        var foreign = Parse(
                "<xs:complexType name=\"c\"><xs:sequence xmlns:o=\"urn:o\"><o:x/><xs:element name=\"a\"/></xs:sequence></xs:complexType>"
            )
            .GetChildNode(0);
        var unknown = new SchemaSyntaxNode(new NetEvolve.Schemas.XPath.Api.XPathQName(Xs, "bogus"), default, null);

        await Assert.That(SchemaStructure.IsValid(foreign.GetChildNode(0))).IsTrue();
        await Assert.That(SchemaStructure.IsValid(unknown)).IsTrue();
        await Assert.That(SchemaStructure.DescribeModel(unknown)).IsNull();
        await Assert.That(SchemaStructure.DescribeModel(foreign.GetChildNode(0))).Contains("annotation?");
        await Assert.That(() => SchemaStructure.IsValid(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => SchemaStructure.DescribeModel(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Pipeline_ComplexTypeWithTwoContents_IsOneErrorOfTheRuleOfTheElement()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:sequence/><xs:choice/></xs:complexType>"
                    + "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:simpleType/><xs:simpleType/></xs:restriction></xs:simpleType>"
            )
        );

        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.SrcCt);
        await Assert
            .That(compilation.Result.Issues.Any(i => i.Message.Contains("children must be", StringComparison.Ordinal)))
            .IsTrue();
        await Assert.That(compilation.Result.Issues[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    [Arguments("<xs:element name=\"a\" id=\"x\"/><xs:element name=\"b\" id=\"x\"/>")]
    [Arguments("<xs:element name=\"a\" id=\"1x\"/>")]
    [Arguments("<xs:element name=\"a\" id=\"\"/>")]
    [Arguments("<xs:element name=\"a\" id=\"p:q\"/>")]
    public async Task Pipeline_AnIdThatIsNotAnIdOrIsUsedTwice_IsOneError(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(1);
        await Assert.That(compilation.Result.Issues[0].Code).IsEqualTo(XsdRuleCodes.SrcElement);
        await Assert.That(compilation.Result.Issues[0].Message).Contains("id");
    }

    [Test]
    public async Task Pipeline_ValidIdsOfDifferentElements_AreAccepted()
    {
        var compilation = Compile(
            Schema("<xs:element name=\"a\" id=\"x1\"/><xs:element name=\"b\" id=\"x2\"/><xs:annotation id=\"x3\"/>")
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }

    [Test]
    public async Task Pipeline_ValuesOfTheQNameTypes_AreNormalizedBeforeTheyAreResolved()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"u\"><xs:union memberTypes=\"  xs:int \n  t:code \"/></xs:simpleType>"
                    + "<xs:simpleType name=\"code\"><xs:restriction base=\"\t xs:string \"/></xs:simpleType>"
                    + "<xs:element name=\"e\" type=\"  t:u  \"/>"
            )
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }

    [Test]
    public async Task Pipeline_ValuesOfTheQNameTypes_AreNotTrimmedOfNonXmlWhitespace()
    {
        // Only #x20, #x9, #xA and #xD collapse; a non-breaking space stays part of the QName value.
        var root = Parse("<xs:element name=\"e\" type=\"" + (char)0xA0 + "xs:string\"/>");
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));

        DocumentPreparation.Prepare(root, XsdVersion.V10, reporter);

        await Assert.That(root.GetChildNode(0).FindAttribute("type")).IsEqualTo((char)0xA0 + "xs:string");
    }

    [Test]
    public async Task Pipeline_TheXmlPrefix_IsDeclaredWithoutADeclaration()
    {
        var compilation = Compile(
            Schema("<xs:complexType name=\"c\"><xs:attribute ref=\"xml:lang\"/></xs:complexType>")
        );

        // The prefix resolves, so the unresolved attribute names the XML namespace and no QName error is reported.
        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(1);
        await Assert.That(compilation.Result.Issues[0].Message).Contains("http://www.w3.org/XML/1998/namespace");
    }

    [Test]
    public async Task Normalize_GuardsItsArgument()
    {
        await Assert
            .That(() =>
                DocumentPreparation.Prepare(
                    null!,
                    XsdVersion.V10,
                    new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DocumentPreparation.Prepare(Parse(string.Empty), XsdVersion.V10, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task SyntaxValues_ReadTheValuesOfAttributesAndChildren()
    {
        var root = Parse(
            "<xs:complexType name=\"c\" final=\"extension\"><xs:annotation/><xs:sequence/></xs:complexType><xs:foo/>"
        );
        var type = root.GetChildNode(0);

        await Assert.That(SyntaxValues.IsSchemaElement(type, "complexType")).IsTrue();
        await Assert.That(SyntaxValues.IsSchemaElement(type, "simpleType")).IsFalse();
        await Assert.That(SyntaxValues.FindChild(type, "sequence")).IsNotNull();
        await Assert.That(SyntaxValues.FindChild(type, "choice")).IsNull();
        await Assert.That(SyntaxValues.Root(type)).IsSameReferenceAs(root);
        await Assert.That(SyntaxValues.IsTrue("true")).IsTrue();
        await Assert.That(SyntaxValues.IsTrue("1")).IsTrue();
        await Assert.That(SyntaxValues.IsTrue("false")).IsFalse();
        await Assert.That(SyntaxValues.IsTrue(null)).IsFalse();
        await Assert.That(SyntaxValues.Split(" a  b\tc\n")).IsEquivalentTo(["a", "b", "c"]);
        await Assert.That(() => SyntaxValues.Split(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => SyntaxValues.IsSchemaElement(null!, "a")).Throws<ArgumentNullException>();
        await Assert.That(() => SyntaxValues.FindChild(null!, "a")).Throws<ArgumentNullException>();
        await Assert.That(() => SyntaxValues.Root(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(null, 0)]
    [Arguments("#all", 3)]
    [Arguments("extension", 1)]
    [Arguments("restriction extension", 3)]
    [Arguments("list union", 0)]
    [Arguments("substitution nonsense", 0)]
    public async Task ParseMethods_ReadsTheDerivationSetThatTheMaskAllows(string? value, int expected) =>
        await Assert
            .That(
                (int)SyntaxValues.ParseMethods(value, XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction)
            )
            .IsEqualTo(expected);

    [Test]
    public async Task ParseMethods_ReadsEveryNamedMethodWhenTheMaskAllowsAll()
    {
        const XsdDerivationMethod all =
            XsdDerivationMethod.Extension
            | XsdDerivationMethod.Restriction
            | XsdDerivationMethod.List
            | XsdDerivationMethod.Union
            | XsdDerivationMethod.Substitution;

        await Assert
            .That(SyntaxValues.ParseMethods("extension restriction list union substitution", all))
            .IsEqualTo(all);
    }
}
