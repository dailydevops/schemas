namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The assertion facet of a simple type is compiled and evaluated by the validator that the default builder assembles (XSD-78, #1191).</summary>
public class FacetAssertionWiringTests
{
    private static readonly string Schema = AssemblyFixture.WrapSchema(
        """
        <xs:simpleType name="positive">
          <xs:restriction base="xs:integer"><xs:assertion test="$value gt 0"/></xs:restriction>
        </xs:simpleType>
        <xs:simpleType name="small">
          <xs:restriction base="t:positive"><xs:assertion test="$value lt 10"/></xs:restriction>
        </xs:simpleType>
        <xs:simpleType name="pair">
          <xs:restriction><xs:simpleType><xs:list itemType="xs:integer"/></xs:simpleType>
            <xs:assertion test="count($value) eq 2" xpathDefaultNamespace="##targetNamespace"/>
          </xs:restriction>
        </xs:simpleType>
        <xs:simpleType name="contextual">
          <xs:restriction base="xs:integer"><xs:assertion test=". gt 0"/></xs:restriction>
        </xs:simpleType>
        <xs:simpleType name="plainInt"><xs:restriction base="xs:integer"/></xs:simpleType>
        <xs:complexType name="holder">
          <xs:simpleContent>
            <xs:restriction base="t:plainHolder">
              <xs:assertion test="$value gt 0"/>
            </xs:restriction>
          </xs:simpleContent>
        </xs:complexType>
        <xs:complexType name="plainHolder">
          <xs:simpleContent><xs:extension base="xs:integer"/></xs:simpleContent>
        </xs:complexType>
        <xs:element name="p" type="t:positive"/>
        <xs:element name="s" type="t:small"/>
        <xs:element name="pair" type="t:pair"/>
        <xs:element name="ctx" type="t:contextual"/>
        <xs:element name="plain" type="t:plainInt"/>
        <xs:element name="h" type="t:holder"/>
        <xs:element name="wrap">
          <xs:complexType>
            <xs:sequence><xs:element name="n" type="t:positive"/></xs:sequence>
            <xs:attribute name="a" type="t:positive"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="any" type="xs:anyType"/>
        """
    );

    private static int Count(string instance) =>
        AssemblyFixture
            .Run(AssemblyFixture.Build(Schema), instance)
            .Issues.Count(i => i.Code == XsdRuleCodes.CvcAssertionsValid);

    [Test]
    public async Task Validate_SatisfiedAssertion_IsValid()
    {
        var result = AssemblyFixture.Run(AssemblyFixture.Build(Schema), "<p xmlns=\"urn:t\">5</p>");

        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Validate_ViolatedAssertion_ReportsCvcAssertionsValid()
    {
        var result = AssemblyFixture.Run(AssemblyFixture.Build(Schema), "<p xmlns=\"urn:t\">-5</p>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count(i => i.Code == XsdRuleCodes.CvcAssertionsValid)).IsEqualTo(1);
    }

    [Test]
    public async Task Validate_DerivedType_ChecksTheInheritedAndTheOwnAssertion()
    {
        await Assert.That(Count("<s xmlns=\"urn:t\">5</s>")).IsEqualTo(0);
        await Assert.That(Count("<s xmlns=\"urn:t\">-5</s>")).IsEqualTo(1);
        await Assert.That(Count("<s xmlns=\"urn:t\">50</s>")).IsEqualTo(1);
    }

    [Test]
    public async Task Validate_ListType_BindsTheItemsAndReadsXPathDefaultNamespace()
    {
        await Assert.That(Count("<pair xmlns=\"urn:t\">1 2</pair>")).IsEqualTo(0);
        await Assert.That(Count("<pair xmlns=\"urn:t\">1 2 3</pair>")).IsEqualTo(1);
    }

    [Test]
    public async Task Validate_TestThatUsesTheContextItem_IsADynamicErrorAndInvalid() =>
        await Assert.That(Count("<ctx xmlns=\"urn:t\">5</ctx>")).IsEqualTo(1);

    [Test]
    public async Task Validate_SimpleContentRestriction_EvaluatesTheAssertionFacet()
    {
        await Assert.That(Count("<h xmlns=\"urn:t\">5</h>")).IsEqualTo(0);
        await Assert.That(Count("<h xmlns=\"urn:t\">-5</h>")).IsEqualTo(1);
    }

    [Test]
    public async Task Validate_ChildElementAndAttribute_AreEvaluated()
    {
        await Assert.That(Count("<wrap xmlns=\"urn:t\" a=\"1\"><n>1</n></wrap>")).IsEqualTo(0);
        await Assert.That(Count("<wrap xmlns=\"urn:t\" a=\"-1\"><n>-1</n></wrap>")).IsEqualTo(2);
    }

    [Test]
    public async Task Validate_XsiType_EvaluatesTheAssertionOfTheActualType()
    {
        const string Xsi = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:t=\"urn:t\"";

        await Assert.That(Count("<any xmlns=\"urn:t\"" + Xsi + " xsi:type=\"t:positive\">-1</any>")).IsEqualTo(1);
        await Assert.That(Count("<any xmlns=\"urn:t\"" + Xsi + " xsi:type=\"t:positive\">1</any>")).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_XsiTypeOfABuiltInType_HasNoAssertionToEvaluate()
    {
        const string Xsi =
            " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

        await Assert.That(Count("<any xmlns=\"urn:t\"" + Xsi + " xsi:type=\"xs:int\">-1</any>")).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_ValueThatIsInvalidForTheDatatype_ReportsNoAssertion()
    {
        var result = AssemblyFixture.Run(AssemblyFixture.Build(Schema), "<p xmlns=\"urn:t\">x</p>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcAssertionsValid)).IsFalse();
    }

    [Test]
    public async Task Validate_TypeWithoutAssertion_IsNotAffected() =>
        await Assert.That(Count("<plain xmlns=\"urn:t\">-5</plain>")).IsEqualTo(0);

    [Test]
    public async Task Build_AnAssertionThatDoesNotCompile_IsASchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:integer\"><xs:assertion test=\"1 +\"/></xs:restriction></xs:simpleType>"
                )
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.XpathValid)).IsTrue();
    }

    [Test]
    public async Task Build_Version10_StillRejectsTheAssertionFacet()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:integer\"><xs:assertion test=\"$value gt 0\"/></xs:restriction></xs:simpleType>"
                ),
                XsdVersion.V10
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.Strict10AssertionFacet)).IsTrue();
    }

    [Test]
    public async Task Build_XPathDefaultNamespaceOfTheAssertion_IsNotAFurtherNotYetSupportedError()
    {
        var validator = AssemblyFixture.Build(Schema);

        await Assert.That(validator).IsNotNull();
    }
}
