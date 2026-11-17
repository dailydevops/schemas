namespace NetEvolve.Schemas.Xsd.Tests.Unit.Elements;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;

public class ElementDeclarationTests
{
    private const XsdDerivationMethod AllBlock =
        XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction | XsdDerivationMethod.Substitution;

    [Test]
    public async Task GlobalElement_IsRegisteredInTheTargetNamespace_AndBindsItsType()
    {
        var fixture = ElementFixture.Build("<xs:element name=\"a\" type=\"xs:string\"/>").Resolve();

        var element = fixture.Element("a");

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(element.IsGlobal).IsTrue();
        await Assert.That(element.Kind).IsEqualTo(XsdComponentKind.ElementDeclaration);
        await Assert.That(element.Name.NamespaceName).IsEqualTo("urn:test");
        await Assert.That(element.Name.LocalName).IsEqualTo("a");
        await Assert.That(element.TypeDefinition!.Name.LocalName).IsEqualTo("string");
    }

    [Test]
    public async Task GlobalElement_WithoutType_HasAnyType()
    {
        var fixture = ElementFixture.Build("<xs:element name=\"a\"/>").Resolve();

        await Assert.That(fixture.Element("a").TypeDefinition!.Name.LocalName).IsEqualTo("anyType");
    }

    [Test]
    public async Task GlobalElement_ReferencingAnUndeclaredType_IsAResolveError()
    {
        var fixture = ElementFixture.Build("<xs:element name=\"a\" type=\"t:missing\"/>").Resolve();

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-resolve"));
    }

    [Test]
    [Arguments("", "", false)]
    [Arguments("", "qualified", true)]
    [Arguments("", "unqualified", false)]
    [Arguments("elementFormDefault=\"qualified\"", "", true)]
    [Arguments("elementFormDefault=\"qualified\"", "unqualified", false)]
    [Arguments("elementFormDefault=\"unqualified\"", "qualified", true)]
    public async Task LocalElement_FormDecidesTheNamespaceOfTheName(
        string schemaAttributes,
        string form,
        bool qualified
    )
    {
        var formAttribute = form.Length == 0 ? string.Empty : " form=\"" + form + "\"";
        var fixture = ElementFixture
            .Build(
                "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:string\""
                    + formAttribute
                    + "/></xs:sequence></xs:group>",
                schemaAttributes
            )
            .Resolve();

        var local = (ElementDeclaration)fixture.Group("g").ModelGroup.Particles[0].Term!;

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(local.IsGlobal).IsFalse();
        await Assert.That(local.Name.LocalName).IsEqualTo("a");
        await Assert.That(local.Name.NamespaceName).IsEqualTo(qualified ? "urn:test" : string.Empty);
        await Assert.That(local.TypeDefinition!.Name.LocalName).IsEqualTo("string");
    }

    [Test]
    public async Task Element_ExposesNillableDefaultAndFixed()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"n\" nillable=\"true\"/>"
                + "<xs:element name=\"d\" type=\"xs:string\" default=\"x\"/>"
                + "<xs:element name=\"f\" type=\"xs:string\" fixed=\"y\"/>"
                + "<xs:element name=\"p\" type=\"xs:string\"/>"
        );

        await Assert.That(fixture.Element("n").IsNillable).IsTrue();
        await Assert.That(fixture.Element("p").IsNillable).IsFalse();
        await Assert.That(fixture.Element("d").ValueConstraint.Kind).IsEqualTo(ElementValueConstraintKind.Default);
        await Assert.That(fixture.Element("d").ValueConstraint.Value).IsEqualTo("x");
        await Assert.That(fixture.Element("f").ValueConstraint.Kind).IsEqualTo(ElementValueConstraintKind.Fixed);
        await Assert.That(fixture.Element("f").ValueConstraint.Value).IsEqualTo("y");
        await Assert.That(fixture.Element("p").ValueConstraint.Kind).IsEqualTo(ElementValueConstraintKind.None);
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DefaultTogetherWithFixed_IsALocatedSchemaError()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"ok\"/>\n<xs:element name=\"a\" type=\"xs:string\" default=\"x\" fixed=\"y\"/>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-element"));
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.1");
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task AbstractBlockAndFinal_AreExposed()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"a\" abstract=\"true\" block=\"extension substitution\" final=\"restriction\"/>"
                + "<xs:element name=\"b\"/>"
        );

        var a = fixture.Element("a");

        await Assert.That(a.IsAbstract).IsTrue();
        await Assert.That(a.Block).IsEqualTo(XsdDerivationMethod.Extension | XsdDerivationMethod.Substitution);
        await Assert.That(a.Final).IsEqualTo(XsdDerivationMethod.Restriction);
        await Assert.That(fixture.Element("b").IsAbstract).IsFalse();
        await Assert.That(fixture.Element("b").Block).IsEqualTo(XsdDerivationMethod.None);
    }

    [Test]
    public async Task BlockAndFinal_FallBackToTheSchemaDefaults_AndAllExpandsToEveryMember()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"a\"/><xs:element name=\"b\" block=\"\" final=\"\"/><xs:element name=\"c\" block=\"#all\" final=\"#all\"/>",
            "blockDefault=\"#all\" finalDefault=\"extension list\""
        );

        await Assert.That(fixture.Element("a").Block).IsEqualTo(AllBlock);
        await Assert.That(fixture.Element("a").Final).IsEqualTo(XsdDerivationMethod.Extension);
        await Assert.That(fixture.Element("b").Block).IsEqualTo(XsdDerivationMethod.None);
        await Assert.That(fixture.Element("b").Final).IsEqualTo(XsdDerivationMethod.None);
        await Assert.That(fixture.Element("c").Block).IsEqualTo(AllBlock);
        await Assert.That(fixture.Element("c").Final)
            .IsEqualTo(XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction);
    }

    [Test]
    public async Task ElementWithTypeAttributeAndAnonymousType_IsASchemaError()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"a\" type=\"xs:string\"><xs:simpleType><xs:restriction base=\"xs:string\"/></xs:simpleType></xs:element>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.3");
    }

    [Test]
    public async Task ElementWithAnonymousType_KeepsTheSyntaxForTheTypeBuilders()
    {
        var fixture = ElementFixture.Build("<xs:element name=\"a\"><xs:complexType/></xs:element>").Resolve();

        var element = fixture.Element("a");

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(element.InlineTypeSyntax!.Name.LocalName).IsEqualTo("complexType");
        await Assert.That(element.TypeDefinition).IsNull();
    }

    [Test]
    public async Task GlobalElement_WithoutName_IsASchemaError()
    {
        var fixture = ElementFixture.Build("<xs:element type=\"xs:string\"/>");

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.2.1");
    }

    [Test]
    public async Task GlobalElement_WithRef_IsASchemaError()
    {
        var fixture = ElementFixture.Build("<xs:element name=\"a\" ref=\"t:a\"/>");

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.2.1");
    }
}
