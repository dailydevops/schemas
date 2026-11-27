namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The type alternatives and the inheritable attributes are compiled and evaluated by the validator that the default builder assembles (XSD-78, #360, #1428).</summary>
public class TypeAlternativeWiringTests
{
    private const string XsiDeclarations =
        "xmlns=\"urn:t\" xmlns:t=\"urn:t\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

    private static readonly string Schema = AssemblyFixture.WrapSchema(
        """
        <xs:complexType name="numBase">
          <xs:simpleContent><xs:extension base="xs:integer"><xs:attribute name="kind" type="xs:string"/></xs:extension></xs:simpleContent>
        </xs:complexType>
        <xs:complexType name="small">
          <xs:simpleContent><xs:restriction base="t:numBase"><xs:maxInclusive value="9"/></xs:restriction></xs:simpleContent>
        </xs:complexType>
        <xs:complexType name="tiny">
          <xs:simpleContent><xs:restriction base="t:small"><xs:maxInclusive value="3"/></xs:restriction></xs:simpleContent>
        </xs:complexType>
        <xs:complexType name="byteNum">
          <xs:simpleContent><xs:restriction base="t:numBase"><xs:maxInclusive value="127"/></xs:restriction></xs:simpleContent>
        </xs:complexType>
        <xs:complexType name="flagged">
          <xs:simpleContent><xs:extension base="xs:int"><xs:attribute name="flag" type="xs:string"/></xs:extension></xs:simpleContent>
        </xs:complexType>
        <xs:element name="num" type="t:numBase">
          <xs:alternative test="@kind = 'small'" type="t:small"/>
          <xs:alternative test="@kind = 'byte'" type="t:byteNum"/>
        </xs:element>
        <xs:element name="withDefault" type="t:numBase">
          <xs:alternative test="@kind = 'byte'" type="t:byteNum"/>
          <xs:alternative type="t:small"/>
        </xs:element>
        <xs:element name="inline" type="t:numBase">
          <xs:alternative test="@kind = 'a'">
            <xs:complexType>
              <xs:simpleContent><xs:restriction base="t:numBase"><xs:maxInclusive value="3"/></xs:restriction></xs:simpleContent>
            </xs:complexType>
          </xs:alternative>
        </xs:element>
        <xs:element name="poly">
          <xs:alternative test="@kind = 'c'">
            <xs:complexType><xs:sequence><xs:element name="x" type="xs:int"/></xs:sequence><xs:attribute name="kind"/></xs:complexType>
          </xs:alternative>
        </xs:element>
        <xs:element name="holder">
          <xs:alternative test="child = 'x'" type="xs:int" xpathDefaultNamespace="##targetNamespace"/>
        </xs:element>
        <xs:element name="attrs">
          <xs:alternative test="@flag = 'y'" type="t:flagged"/>
        </xs:element>
        <xs:element name="dynamic" type="t:numBase">
          <xs:alternative test="xs:integer(@kind) = 1" type="t:tiny"/>
        </xs:element>
        <xs:element name="outer">
          <xs:complexType>
            <xs:sequence><xs:element ref="t:innerInherited"/></xs:sequence>
            <xs:attribute name="kind" type="xs:string" inheritable="true"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="innerInherited" type="t:numBase">
          <xs:alternative test="@kind = 'small'" type="t:small"/>
        </xs:element>
        <xs:attribute name="mode" type="xs:string" inheritable="true"/>
        <xs:element name="innerMode" type="t:numBase">
          <xs:alternative test="@t:mode = 'small'" type="t:small"/>
        </xs:element>
        <xs:element name="byRef">
          <xs:complexType>
            <xs:sequence><xs:element ref="t:innerMode"/></xs:sequence>
            <xs:attribute ref="t:mode"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="byWildcard">
          <xs:complexType>
            <xs:sequence><xs:element ref="t:innerMode"/></xs:sequence>
            <xs:anyAttribute namespace="##targetNamespace" processContents="lax"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="bySkip">
          <xs:complexType>
            <xs:sequence><xs:element ref="t:innerMode"/></xs:sequence>
            <xs:anyAttribute namespace="##targetNamespace" processContents="skip"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="plainInt" type="xs:int">
          <xs:alternative test="true()" type="xs:short"/>
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
        var other = Run("<attrs xmlns=\"urn:t\" flag=\"n\">abc</attrs>");

        await Assert.That(children.IsValid).IsTrue();
        await Assert.That(withAttribute.IsValid).IsTrue();
        await Assert.That(withBadValue.IsValid).IsFalse();
        await Assert.That(other.IsValid).IsTrue();
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
    [Arguments("byRef", false)]
    [Arguments("byWildcard", false)]
    [Arguments("bySkip", true)]
    public async Task Validate_TheInheritableAttributeOfAGlobalDeclaration_IsVisibleThroughAReferenceOrALaxWildcard(
        string root,
        bool expected
    )
    {
        var result = Run(
            "<" + root + " xmlns=\"urn:t\" xmlns:t=\"urn:t\" t:mode=\"small\"><innerMode>50</innerMode></" + root + ">"
        );

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Validate_AnAttributeOfASimpleTypeElement_IsNotInherited()
    {
        var result = Run("<plainInt xmlns=\"urn:t\" foo=\"1\">5</plainInt>");

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    [Arguments(" kind=\"small\" xsi:type=\"t:tiny\">3</num>", true)]
    [Arguments(" kind=\"small\" xsi:type=\"t:tiny\">5</num>", false)]
    [Arguments(" kind=\"small\" xsi:type=\"t:byteNum\">5</num>", false)]
    [Arguments(" xsi:type=\"t:byteNum\">5</num>", true)]
    public async Task Validate_XsiTypeMustBeDerivedFromTheTypeThatTheTableSelected(string rest, bool expected)
    {
        var result = Run("<num " + XsiDeclarations + rest);

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Validate_ADynamicErrorInTheTest_MakesItFalse()
    {
        var dynamicError = Run("<dynamic xmlns=\"urn:t\" kind=\"abc\">5</dynamic>");
        var selected = Run("<dynamic xmlns=\"urn:t\" kind=\"1\">5</dynamic>");

        await Assert.That(dynamicError.IsValid).IsTrue();
        await Assert.That(selected.IsValid).IsFalse();
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
