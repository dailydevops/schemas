namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The type alternatives and the inheritable attributes are compiled and evaluated by the validator that the default builder assembles (XSD-78, #360, #1428).</summary>
public class TypeAlternativeWiringTests
{
    private const string XsiDeclarations =
        "xmlns=\"urn:t\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

    private static readonly string Schema = AssemblyFixture.WrapSchema(
        """
        <xs:simpleType name="small"><xs:restriction base="xs:int"><xs:maxInclusive value="9"/></xs:restriction></xs:simpleType>
        <xs:simpleType name="tiny"><xs:restriction base="t:small"><xs:maxInclusive value="3"/></xs:restriction></xs:simpleType>
        <xs:element name="num" type="xs:integer">
          <xs:alternative test="@kind = 'small'" type="t:small"/>
          <xs:alternative test="@kind = 'byte'" type="xs:byte"/>
        </xs:element>
        <xs:element name="withDefault" type="xs:integer">
          <xs:alternative test="@kind = 'byte'" type="xs:byte"/>
          <xs:alternative type="t:small"/>
        </xs:element>
        <xs:element name="inline" type="xs:integer">
          <xs:alternative test="@kind = 'a'">
            <xs:simpleType><xs:restriction base="xs:integer"><xs:maxInclusive value="3"/></xs:restriction></xs:simpleType>
          </xs:alternative>
        </xs:element>
        <xs:element name="poly">
          <xs:alternative test="@kind = 'c'">
            <xs:complexType><xs:sequence><xs:element name="x" type="xs:int"/></xs:sequence></xs:complexType>
          </xs:alternative>
        </xs:element>
        <xs:element name="holder">
          <xs:alternative test="child = 'x'" type="xs:int" xpathDefaultNamespace="##targetNamespace"/>
        </xs:element>
        <xs:element name="attrs">
          <xs:alternative test="@flag = 'y'" type="xs:int"/>
        </xs:element>
        <xs:element name="dynamic" type="xs:string">
          <xs:alternative test="xs:integer(@kind) = 1" type="xs:token"/>
        </xs:element>
        <xs:element name="outer">
          <xs:complexType>
            <xs:sequence><xs:element ref="t:innerInherited"/></xs:sequence>
            <xs:attribute name="kind" type="xs:string" inheritable="true"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="innerInherited" type="xs:integer">
          <xs:alternative test="@kind = 'small'" type="t:small"/>
        </xs:element>
        <xs:element name="outerPlain">
          <xs:complexType>
            <xs:sequence><xs:element ref="t:innerInherited"/></xs:sequence>
            <xs:attribute name="kind" type="xs:string"/>
          </xs:complexType>
        </xs:element>
        """
    );

    private static ValidationResult Run(string instance) =>
        AssemblyFixture.Run(AssemblyFixture.Build(Schema), instance);

    [Test]
    [Arguments("<num xmlns=\"urn:t\" kind=\"small\">5</num>", true)]
    [Arguments("<num xmlns=\"urn:t\" kind=\"small\">50</num>", false)]
    [Arguments("<num xmlns=\"urn:t\" kind=\"byte\">100</num>", true)]
    [Arguments("<num xmlns=\"urn:t\" kind=\"byte\">200</num>", false)]
    [Arguments("<num xmlns=\"urn:t\" kind=\"other\">50000</num>", true)]
    [Arguments("<num xmlns=\"urn:t\">50000</num>", true)]
    public async Task Validate_FirstTrueAlternativeSelectsTheType_NoMatchSelectsTheDeclaredType(
        string instance,
        bool expected
    )
    {
        var result = Run(instance);

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<withDefault xmlns=\"urn:t\" kind=\"byte\">100</withDefault>", true)]
    [Arguments("<withDefault xmlns=\"urn:t\" kind=\"byte\">200</withDefault>", false)]
    [Arguments("<withDefault xmlns=\"urn:t\" kind=\"x\">5</withDefault>", true)]
    [Arguments("<withDefault xmlns=\"urn:t\" kind=\"x\">50</withDefault>", false)]
    [Arguments("<withDefault xmlns=\"urn:t\">50</withDefault>", false)]
    public async Task Validate_AlternativeWithoutTest_IsTheDefault(string instance, bool expected)
    {
        var result = Run(instance);

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<inline xmlns=\"urn:t\" kind=\"a\">3</inline>", true)]
    [Arguments("<inline xmlns=\"urn:t\" kind=\"a\">4</inline>", false)]
    [Arguments("<poly xmlns=\"urn:t\" kind=\"c\"><x>1</x></poly>", true)]
    [Arguments("<poly xmlns=\"urn:t\" kind=\"c\"><x>z</x></poly>", false)]
    public async Task Validate_AlternativeWithAnAnonymousType_UsesIt(string instance, bool expected)
    {
        var result = Run(instance);

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Validate_TheTestSeesTheAttributesOfTheElementAndNotItsChildren()
    {
        var children = Run("<holder xmlns=\"urn:t\"><child>x</child></holder>");
        var withAttribute = Run("<attrs xmlns=\"urn:t\" flag=\"y\">5</attrs>");
        var withBadValue = Run("<attrs xmlns=\"urn:t\" flag=\"y\">abc</attrs>");

        await Assert.That(children.IsValid).IsTrue();
        await Assert.That(withAttribute.IsValid).IsTrue();
        await Assert.That(withBadValue.IsValid).IsFalse();
    }

    [Test]
    [Arguments("<outer xmlns=\"urn:t\" kind=\"small\"><innerInherited>50</innerInherited></outer>", false)]
    [Arguments("<outer xmlns=\"urn:t\" kind=\"small\"><innerInherited>5</innerInherited></outer>", true)]
    [Arguments("<outer xmlns=\"urn:t\" kind=\"big\"><innerInherited>50</innerInherited></outer>", true)]
    [Arguments("<outerPlain xmlns=\"urn:t\" kind=\"small\"><innerInherited>50</innerInherited></outerPlain>", true)]
    public async Task Validate_AnInheritableAttributeIsVisibleToTheTestOfADescendant(string instance, bool expected)
    {
        var result = Run(instance);

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    [Arguments(" kind=\"small\" xsi:type=\"t:tiny\">3</num>", true)]
    [Arguments(" kind=\"small\" xsi:type=\"t:tiny\">5</num>", false)]
    [Arguments(" kind=\"small\" xsi:type=\"xs:byte\">5</num>", false)]
    [Arguments(" xsi:type=\"xs:long\">5</num>", true)]
    public async Task Validate_XsiTypeMustBeDerivedFromTheTypeThatTheTableSelected(string rest, bool expected)
    {
        var result = Run("<num " + XsiDeclarations + rest);

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Validate_ADynamicErrorInTheTest_MakesItFalse()
    {
        var result = Run("<dynamic xmlns=\"urn:t\" kind=\"abc\">  a  </dynamic>");

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Build_AnAlternativeThatIsNotDerivedFromTheDeclaredType_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:element name=\"e\" type=\"xs:int\"><xs:alternative test=\"true()\" type=\"xs:string\"/></xs:element>"
                )
            )
        );

        var issue = exception.Issues.First(i => i.Code == XsdRuleCodes.TaPropsCorrect);
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_ATestThatDoesNotCompile_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:element name=\"e\" type=\"xs:string\"><xs:alternative test=\"1 +\" type=\"xs:string\"/></xs:element>"
                )
            )
        );

        var issue = exception.Issues.First(i => i.Code == XsdRuleCodes.XpathValid);
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_AnAlternativeWithAnUnknownType_IsAnUnresolvedReference()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:element name=\"e\" type=\"xs:string\"><xs:alternative test=\"true()\" type=\"t:missing\"/></xs:element>"
                )
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.SrcResolve)).IsTrue();
    }

    [Test]
    public async Task Build_Version10_StillRejectsTheAlternativeAndTheInheritableAttribute()
    {
        var alternative = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema(
                    "<xs:element name=\"e\" type=\"xs:string\"><xs:alternative test=\"true()\" type=\"xs:string\"/></xs:element>"
                ),
                XsdVersion.V10
            )
        );
        var inheritable = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                AssemblyFixture.WrapSchema("<xs:attribute name=\"a\" type=\"xs:string\" inheritable=\"true\"/>"),
                XsdVersion.V10
            )
        );

        await Assert.That(alternative.Issues.Any(i => i.Code == XsdRuleCodes.Strict10Alternative)).IsTrue();
        await Assert.That(inheritable.Issues.Any(i => i.Code == XsdRuleCodes.Strict10Inheritable)).IsTrue();
    }

    [Test]
    public async Task Build_XPathDefaultNamespaceOfATypeAlternative_IsAccepted()
    {
        var validator = AssemblyFixture.Build(
            AssemblyFixture.WrapSchema(
                "<xs:element name=\"e\" type=\"xs:string\"><xs:alternative test=\"true()\" type=\"xs:string\" xpathDefaultNamespace=\"##local\"/></xs:element>"
            )
        );

        await Assert.That(validator.Version).IsEqualTo(XsdVersion.V11);
    }
}
