namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The default attribute group of a schema and <c>defaultAttributesApply</c> of a complex type are compiled and evaluated (XSD-78, #360, #1426).</summary>
public class DefaultAttributeWiringTests
{
    private const string Groups = """
        <xs:attributeGroup name="dg">
          <xs:attribute name="req" type="xs:string" use="required"/>
          <xs:attribute name="dflt" type="xs:string" default="d"/>
        </xs:attributeGroup>
        """;

    private static readonly string Schema = AssemblyFixture.WrapSchema(
        Groups
            + """
            <xs:element name="implicit"><xs:complexType/></xs:element>
            <xs:element name="optOut"><xs:complexType defaultAttributesApply="false"/></xs:element>
            <xs:element name="optIn"><xs:complexType defaultAttributesApply="true"/></xs:element>
            <xs:complexType name="base"><xs:attribute name="own" type="xs:string"/></xs:complexType>
            <xs:element name="ext">
              <xs:complexType><xs:complexContent><xs:extension base="t:base"/></xs:complexContent></xs:complexType>
            </xs:element>
            <xs:element name="res">
              <xs:complexType><xs:complexContent><xs:restriction base="t:base"/></xs:complexContent></xs:complexType>
            </xs:element>
            <xs:element name="simple">
              <xs:complexType><xs:simpleContent><xs:extension base="xs:int"/></xs:simpleContent></xs:complexType>
            </xs:element>
            <xs:element name="simpleOptOut">
              <xs:complexType defaultAttributesApply="false">
                <xs:simpleContent><xs:extension base="xs:int"/></xs:simpleContent>
              </xs:complexType>
            </xs:element>
            """,
        "defaultAttributes=\"t:dg\""
    );

    private static ValidationResult Run(string instance) =>
        AssemblyFixture.Run(AssemblyFixture.Build(Schema, XsdVersion.V11), instance);

    [Test]
    [Arguments("<implicit xmlns=\"urn:t\" req=\"a\"/>", true)]
    [Arguments("<implicit xmlns=\"urn:t\"/>", false)]
    [Arguments("<optIn xmlns=\"urn:t\"/>", false)]
    [Arguments("<optIn xmlns=\"urn:t\" req=\"a\"/>", true)]
    [Arguments("<optOut xmlns=\"urn:t\"/>", true)]
    [Arguments("<optOut xmlns=\"urn:t\" req=\"a\"/>", false)]
    [Arguments("<ext xmlns=\"urn:t\" req=\"a\" own=\"b\"/>", true)]
    [Arguments("<ext xmlns=\"urn:t\" own=\"b\"/>", false)]
    [Arguments("<res xmlns=\"urn:t\" req=\"a\"/>", true)]
    [Arguments("<res xmlns=\"urn:t\"/>", false)]
    [Arguments("<simple xmlns=\"urn:t\" req=\"a\">5</simple>", true)]
    [Arguments("<simple xmlns=\"urn:t\">5</simple>", false)]
    [Arguments("<simpleOptOut xmlns=\"urn:t\">5</simpleOptOut>", true)]
    [Arguments("<simpleOptOut xmlns=\"urn:t\" req=\"a\">5</simpleOptOut>", false)]
    public async Task Validate_TheDefaultGroupIsAddedToTheApplicableTypes(string instance, bool expected)
    {
        var result = Run(instance);

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Validate_TheDefaultOfAnAttributeOfTheGroupIsApplied()
    {
        var withValue = Run("<implicit xmlns=\"urn:t\" req=\"a\" dflt=\"x\"/>");
        var withoutValue = Run("<implicit xmlns=\"urn:t\" req=\"a\"/>");

        await Assert.That(withValue.IsValid).IsTrue();
        await Assert.That(withoutValue.IsValid).IsTrue();
    }

    [Test]
    public async Task Build_TheGroupWildcardIsIntersectedWithTheOneOfTheType()
    {
        var schema = AssemblyFixture.WrapSchema(
            """
            <xs:attributeGroup name="dg"><xs:anyAttribute namespace="##any" processContents="skip"/></xs:attributeGroup>
            <xs:element name="open"><xs:complexType/></xs:element>
            <xs:element name="closed"><xs:complexType defaultAttributesApply="false"/></xs:element>
            """,
            "defaultAttributes=\"t:dg\""
        );
        var validator = AssemblyFixture.Build(schema, XsdVersion.V11);

        await Assert.That(AssemblyFixture.Run(validator, "<open xmlns=\"urn:t\" any=\"1\"/>").IsValid).IsTrue();
        await Assert.That(AssemblyFixture.Run(validator, "<closed xmlns=\"urn:t\" any=\"1\"/>").IsValid).IsFalse();
    }

    [Test]
    public async Task Build_AnUnresolvedDefaultAttributesReference_IsALocatedSchemaError()
    {
        var schema = AssemblyFixture.WrapSchema(
            "<xs:element name=\"a\"><xs:complexType/></xs:element><xs:element name=\"b\"><xs:complexType/></xs:element>",
            "defaultAttributes=\"t:missing\""
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema, XsdVersion.V11));

        var issues = exception.Issues.Where(i => i.Code == XsdRuleCodes.SrcResolve).ToArray();
        await Assert.That(issues.Length).IsEqualTo(1);
        await Assert.That(issues[0].Location.Line).IsGreaterThan(0);
        await Assert.That(issues[0].Message).Contains("t:missing");
    }

    [Test]
    public async Task Build_ADuplicateAttributeUseOfTheTypeAndTheGroup_IsReported()
    {
        var schema = AssemblyFixture.WrapSchema(
            Groups
                + "<xs:element name=\"e\"><xs:complexType><xs:attribute name=\"req\"/></xs:complexType></xs:element>",
            "defaultAttributes=\"t:dg\""
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema, XsdVersion.V11));

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Build_AnIncludedDocumentDoesNotInheritTheDefaultOfTheIncludingOne(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var included = AssemblyFixture.WrapSchema("<xs:element name=\"inc\"><xs:complexType/></xs:element>");
        var root = AssemblyFixture.WrapSchema(
            "<xs:include schemaLocation=\"inc.xsd\"/>" + Groups,
            "defaultAttributes=\"t:dg\""
        );
        var validator = await SchemaValidator
            .ForXsd()
            .WithVersion(XsdVersion.V11)
            .WithResolver(new MapResolver().Add("file:///inc.xsd", included))
            .AddSchema(root.AsMemory(), AssemblyFixture.SchemaId)
            .BuildAsync(cancellationToken);

        await Assert.That(AssemblyFixture.Run(validator, "<inc xmlns=\"urn:t\"/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Build_Version10_RejectsTheAttributesAndKeepsTheStrictCodes()
    {
        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(Schema, XsdVersion.V10));

        await Assert.That(exception.Issues.Any(i => i.Code.Number is 4026 or 4027)).IsTrue();
    }
}
