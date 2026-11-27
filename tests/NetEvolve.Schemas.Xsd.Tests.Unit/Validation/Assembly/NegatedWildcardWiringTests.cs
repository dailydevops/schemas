namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>
/// The wildcards of XSD 1.1 with <c>notNamespace</c> and <c>notQName</c> (including <c>##defined</c> and <c>##definedSibling</c>) are
/// compiled and validated by the assembled validator (XSD-78, #360).
/// </summary>
public class NegatedWildcardWiringTests
{
    private const string Prefixes = "xmlns:x=\"urn:x\" xmlns:y=\"urn:y\"";

    private static string Schema(string body) => AssemblyFixture.WrapSchema(body, Prefixes);

    private static string ElementRoot(string any) =>
        Schema(
            "<xs:element name=\"root\"><xs:complexType><xs:sequence>"
                + any
                + "</xs:sequence></xs:complexType></xs:element>"
                + "<xs:element name=\"g\" type=\"xs:string\"/>"
        );

    private static string AttributeRoot(string anyAttribute) =>
        Schema(
            "<xs:element name=\"root\"><xs:complexType>"
                + anyAttribute
                + "</xs:complexType></xs:element>"
                + "<xs:attribute name=\"g\" type=\"xs:string\"/>"
        );

    private static async Task AssertValid(string schema, string instance) =>
        await Assert.That(AssemblyFixture.Run(AssemblyFixture.Build(schema), instance).Issues).IsEmpty();

    private static async Task AssertInvalid(string schema, string instance)
    {
        var result = AssemblyFixture.Run(AssemblyFixture.Build(schema), instance);
        await Assert.That(result.IsValid).IsFalse();
    }

    private const string Skip = " processContents=\"skip\"";

    [Test]
    public async Task Element_NotNamespaceWithANamespaceName_RejectsThatNamespaceOnly()
    {
        var schema = ElementRoot("<xs:any notNamespace=\"urn:x\"" + Skip + "/>");

        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:x\"/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:y\"/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"\"/></root>");
    }

    [Test]
    public async Task Element_NotNamespaceLocal_RejectsTheAbsentNamespace()
    {
        var schema = ElementRoot("<xs:any notNamespace=\"##local\"" + Skip + "/>");

        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"\"/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:x\"/></root>");
    }

    [Test]
    public async Task Element_NotNamespaceTargetNamespace_RejectsTheTargetNamespace()
    {
        var schema = ElementRoot("<xs:any notNamespace=\"##targetNamespace\"" + Skip + "/>");

        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><a/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:x\"/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"\"/></root>");
    }

    [Test]
    public async Task Element_NotQNameWithNames_RejectsOnlyThoseNames()
    {
        var schema = ElementRoot("<xs:any notQName=\"x:a y:b\"" + Skip + "/>");

        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:x\"/></root>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><b xmlns=\"urn:y\"/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><b xmlns=\"urn:x\"/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:y\"/></root>");
    }

    [Test]
    public async Task Element_NotQNameDefined_RejectsTheNamesOfGlobalElementDeclarations()
    {
        var schema = ElementRoot("<xs:any notQName=\"##defined\"" + Skip + "/>");

        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><g/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><other/></root>");
    }

    [Test]
    public async Task Element_NotQNameDefinedSibling_RejectsTheElementsOfTheSameContentModel()
    {
        var schema = Schema(
            "<xs:element name=\"root\"><xs:complexType><xs:sequence>"
                + "<xs:element name=\"s\" type=\"xs:string\" minOccurs=\"0\"/>"
                + "<xs:any notQName=\"##definedSibling\" minOccurs=\"0\" maxOccurs=\"unbounded\""
                + Skip
                + "/>"
                + "</xs:sequence></xs:complexType></xs:element>"
        );

        await AssertValid(schema, "<root xmlns=\"urn:t\"><s/><z/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><z/><z/></root>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><s/><s/></root>");
    }

    [Test]
    public async Task Element_NegatedWildcardInAllGroup_IsMatched()
    {
        var schema = Schema(
            "<xs:element name=\"root\"><xs:complexType><xs:all>"
                + "<xs:element name=\"s\" type=\"xs:string\" minOccurs=\"0\"/>"
                + "<xs:any notQName=\"##definedSibling\" notNamespace=\"urn:x\" minOccurs=\"0\""
                + Skip
                + "/>"
                + "</xs:all></xs:complexType></xs:element>"
        );

        await AssertValid(schema, "<root xmlns=\"urn:t\"><s/><z/></root>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><s/><z xmlns=\"urn:x\"/></root>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><s/><s/></root>");
    }

    [Test]
    public async Task Attribute_NotNamespace_RejectsThatNamespaceOnly()
    {
        var schema = AttributeRoot("<xs:anyAttribute notNamespace=\"urn:x ##local\"" + Skip + "/>");

        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:x=\"urn:x\" x:a=\"1\"/>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\" a=\"1\"/>");
        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:y=\"urn:y\" y:a=\"1\"/>");
    }

    [Test]
    public async Task Attribute_NotQNameWithNamesAndDefined_RejectsThem()
    {
        var schema = AttributeRoot("<xs:anyAttribute notQName=\"x:a ##defined\"" + Skip + "/>");

        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:x=\"urn:x\" x:a=\"1\"/>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:t=\"urn:t\" t:g=\"1\"/>");
        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:x=\"urn:x\" x:b=\"1\"/>");
        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:t=\"urn:t\" t:other=\"1\"/>");
    }

    [Test]
    public async Task Attribute_IntersectionOfNegatedWildcards_RejectsBothSets()
    {
        var schema = Schema(
            "<xs:attributeGroup name=\"ax\"><xs:anyAttribute notNamespace=\"urn:x\""
                + Skip
                + "/></xs:attributeGroup>"
                + "<xs:attributeGroup name=\"ay\"><xs:anyAttribute notNamespace=\"urn:y\""
                + Skip
                + "/></xs:attributeGroup>"
                + "<xs:element name=\"root\"><xs:complexType>"
                + "<xs:attributeGroup ref=\"t:ax\"/><xs:attributeGroup ref=\"t:ay\"/>"
                + "</xs:complexType></xs:element>"
        );

        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:x=\"urn:x\" x:a=\"1\"/>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:y=\"urn:y\" y:a=\"1\"/>");
        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:z=\"urn:z\" z:a=\"1\"/>");
    }

    [Test]
    public async Task Attribute_IntersectionWithAPlainWildcard_KeepsTheNegation()
    {
        var schema = Schema(
            "<xs:attributeGroup name=\"ax\"><xs:anyAttribute notNamespace=\"urn:x\""
                + Skip
                + "/></xs:attributeGroup>"
                + "<xs:element name=\"root\"><xs:complexType>"
                + "<xs:attributeGroup ref=\"t:ax\"/><xs:anyAttribute namespace=\"urn:x urn:y\""
                + Skip
                + "/>"
                + "</xs:complexType></xs:element>"
        );

        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:x=\"urn:x\" x:a=\"1\"/>");
        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:y=\"urn:y\" y:a=\"1\"/>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:z=\"urn:z\" z:a=\"1\"/>");
    }

    [Test]
    public async Task Attribute_ExtensionUnionOfNegatedWildcards_AllowsBothSets()
    {
        var schema = Schema(
            "<xs:complexType name=\"b\"><xs:anyAttribute notNamespace=\"urn:x\""
                + Skip
                + "/></xs:complexType>"
                + "<xs:complexType name=\"d\"><xs:complexContent><xs:extension base=\"t:b\">"
                + "<xs:anyAttribute notNamespace=\"urn:y\""
                + Skip
                + "/>"
                + "</xs:extension></xs:complexContent></xs:complexType>"
                + "<xs:element name=\"root\" type=\"t:d\"/>"
        );

        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:x=\"urn:x\" x:a=\"1\"/>");
        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:y=\"urn:y\" y:a=\"1\"/>");
    }

    [Test]
    public async Task Attribute_RestrictionToASubsetOfNegatedWildcard_IsValid()
    {
        var schema = Schema(
            "<xs:complexType name=\"b\"><xs:anyAttribute notNamespace=\"urn:x\""
                + Skip
                + "/></xs:complexType>"
                + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\">"
                + "<xs:anyAttribute notNamespace=\"urn:x urn:y\""
                + Skip
                + "/>"
                + "</xs:restriction></xs:complexContent></xs:complexType>"
                + "<xs:element name=\"root\" type=\"t:d\"/>"
        );

        await AssertInvalid(schema, "<root xmlns=\"urn:t\" xmlns:y=\"urn:y\" y:a=\"1\"/>");
        await AssertValid(schema, "<root xmlns=\"urn:t\" xmlns:z=\"urn:z\" z:a=\"1\"/>");
    }

    [Test]
    public async Task Attribute_RestrictionToAWildcardThatIsNoSubset_IsASchemaError()
    {
        var schema = Schema(
            "<xs:complexType name=\"b\"><xs:anyAttribute notNamespace=\"urn:x\""
                + Skip
                + "/></xs:complexType>"
                + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\">"
                + "<xs:anyAttribute namespace=\"##any\""
                + Skip
                + "/>"
                + "</xs:restriction></xs:complexContent></xs:complexType>"
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Element_PlainWildcardRestrictedToANegatedOne_IsValid()
    {
        var schema = Schema(
            "<xs:complexType name=\"b\"><xs:sequence><xs:any"
                + Skip
                + "/></xs:sequence></xs:complexType>"
                + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence>"
                + "<xs:any notNamespace=\"urn:x\""
                + Skip
                + "/>"
                + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
                + "<xs:element name=\"root\" type=\"t:d\"/>"
        );

        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:x\"/></root>");
        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:y\"/></root>");
    }

    [Test]
    public async Task Element_NegatedWildcardThatIsNoSubsetOfTheBase_IsASchemaError()
    {
        var schema = Schema(
            "<xs:complexType name=\"b\"><xs:sequence><xs:any notNamespace=\"urn:x\""
                + Skip
                + "/></xs:sequence></xs:complexType>"
                + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence>"
                + "<xs:any notNamespace=\"urn:y\""
                + Skip
                + "/>"
                + "</xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Element_OverlappingNegatedWildcards_AreAmbiguous()
    {
        var schema = ElementRoot(
            "<xs:any notNamespace=\"urn:x\" minOccurs=\"0\"" + Skip + "/><xs:any notNamespace=\"urn:y\"" + Skip + "/>"
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Element_DisjointNegatedWildcards_AreDeterministic()
    {
        var schema = ElementRoot(
            "<xs:any namespace=\"urn:x\" minOccurs=\"0\"" + Skip + "/><xs:any notNamespace=\"urn:x\"" + Skip + "/>"
        );

        await AssertValid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:x\"/><b xmlns=\"urn:y\"/></root>");
        await AssertInvalid(schema, "<root xmlns=\"urn:t\"><a xmlns=\"urn:x\"/></root>");
    }

    [Test]
    [Arguments("<xs:any namespace=\"urn:x\" notNamespace=\"urn:y\"/>")]
    [Arguments("<xs:any notNamespace=\"##any\"/>")]
    [Arguments("<xs:any notQName=\"nope:a\"/>")]
    [Arguments("<xs:any notNamespace=\"urn:x\" notQName=\"y:a\"/>")]
    public async Task Element_InvalidCombinations_AreLocatedSchemaErrors(string any)
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                Schema(
                    "<xs:element name=\"root\"><xs:complexType><xs:sequence>"
                        + any
                        + "</xs:sequence></xs:complexType></xs:element>"
                )
            )
        );

        var issue = exception.Issues[0];
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
        await Assert.That(issue.Location.Source).IsEqualTo(AssemblyFixture.SchemaId);
    }

    [Test]
    [Arguments("<xs:anyAttribute namespace=\"urn:x\" notNamespace=\"urn:y\"/>")]
    [Arguments("<xs:anyAttribute notQName=\"##definedSibling\"/>")]
    public async Task Attribute_InvalidCombinations_AreLocatedSchemaErrors(string any)
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(Schema("<xs:complexType name=\"c\">" + any + "</xs:complexType>"))
        );

        var issue = exception.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(XsdRuleCodes.SrcWildcard);
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
        await Assert.That(issue.Location.Source).IsEqualTo(AssemblyFixture.SchemaId);
    }

    [Test]
    public async Task Version10_NegatedWildcard_KeepsTheStrictCode()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                Schema(
                    "<xs:complexType name=\"c\"><xs:sequence><xs:any notNamespace=\"urn:x\"/></xs:sequence></xs:complexType>"
                ),
                XsdVersion.V10
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.Strict10NotNamespace)).IsTrue();
    }
}
