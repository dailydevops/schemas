namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The assertions of complex types are compiled and evaluated by the validator that the default builder assembles (XSD-78, #360).</summary>
public class AssertionWiringTests
{
    private static readonly string Schema = AssemblyFixture.WrapSchema(
        """
        <xs:complexType name="base">
          <xs:sequence>
            <xs:element name="a" type="xs:int"/>
            <xs:element name="b" type="xs:int"/>
          </xs:sequence>
          <xs:assert test="a lt b"/>
        </xs:complexType>
        <xs:complexType name="derived">
          <xs:complexContent>
            <xs:extension base="t:base">
              <xs:sequence><xs:element name="c" type="xs:int"/></xs:sequence>
              <xs:assert test="c gt b"/>
            </xs:extension>
          </xs:complexContent>
        </xs:complexType>
        <xs:element name="root" type="t:base"/>
        <xs:element name="sub" type="t:derived"/>
        <xs:element name="anon">
          <xs:complexType>
            <xs:sequence><xs:element name="n" type="xs:int"/></xs:sequence>
            <xs:assert test="n = 5" xpathDefaultNamespace="##targetNamespace"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="plain">
          <xs:complexType><xs:sequence><xs:element name="n" type="xs:int"/></xs:sequence></xs:complexType>
        </xs:element>
        """
    );

    private static ValidationResult Run(string instance) => AssemblyFixture.Run(AssemblyFixture.Build(Schema), instance);

    [Test]
    public async Task Validate_SatisfiedAssertion_IsValid()
    {
        var result = Run("<root xmlns=\"urn:t\"><a>1</a><b>2</b></root>");

        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Validate_ViolatedAssertion_ReportsCvcAssertion()
    {
        var result = Run("<root xmlns=\"urn:t\"><a>3</a><b>2</b></root>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count(i => i.Code == XsdRuleCodes.CvcAssertion)).IsEqualTo(1);
    }

    [Test]
    public async Task Validate_DerivedType_ChecksTheInheritedAndTheOwnAssertion()
    {
        var result = Run("<sub xmlns=\"urn:t\"><a>3</a><b>2</b><c>1</c></sub>");

        await Assert.That(result.Issues.Count(i => i.Code == XsdRuleCodes.CvcAssertion)).IsEqualTo(2);
    }

    [Test]
    public async Task Validate_AnonymousTypeWithXPathDefaultNamespace_IsEvaluated()
    {
        var valid = Run("<anon xmlns=\"urn:t\"><n>5</n></anon>");
        var invalid = Run("<anon xmlns=\"urn:t\"><n>6</n></anon>");

        await Assert.That(valid.Issues).IsEmpty();
        await Assert.That(invalid.Issues.Count(i => i.Code == XsdRuleCodes.CvcAssertion)).IsEqualTo(1);
    }

    [Test]
    public async Task Validate_TypeWithoutAssertion_IsNotAffected()
    {
        var result = Run("<plain xmlns=\"urn:t\"><n>6</n></plain>");

        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Build_AnAssertionThatDoesNotCompile_IsASchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:complexType name=\"c\"><xs:sequence/><xs:assert test=\"1 +\"/></xs:complexType>"
                )
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.AsPropsCorrect)).IsTrue();
    }

    [Test]
    public async Task Build_Version10_StillRejectsTheAssertion()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:complexType name=\"c\"><xs:sequence/><xs:assert test=\"true()\"/></xs:complexType>"
                ),
                XsdVersion.V10
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.Strict10Assert)).IsTrue();
    }

    [Test]
    public async Task Build_XPathDefaultNamespaceOfAnIdentityConstraint_IsStillNotSupported()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:element name=\"e\" type=\"xs:string\"><xs:key name=\"k\"><xs:selector xpath=\".\" xpathDefaultNamespace=\"##local\"/><xs:field xpath=\".\"/></xs:key></xs:element>"
                )
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.NotYetSupportedXPathDefaultNamespace)).IsTrue();
    }
}
