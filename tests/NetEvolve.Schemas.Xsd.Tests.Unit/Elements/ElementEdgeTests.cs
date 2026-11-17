namespace NetEvolve.Schemas.Xsd.Tests.Unit.Elements;

using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

public class ElementEdgeTests
{
    private static readonly SchemaNamespaceScope Scope = new("urn:test", false);

    [Test]
    public async Task Builders_RejectSyntaxThatIsNotASchemaSyntaxNode()
    {
        var fixture = ElementFixture.Build(string.Empty);
        var syntax = new FakeSyntaxElement("element", 1);

        await Assert
            .That(() => new ElementDeclarationBuilder(Scope).Build(syntax, fixture.Registry, fixture.Reporter))
            .Throws<NotSupportedException>();
        await Assert
            .That(() => new ModelGroupDefinitionBuilder(Scope).Build(syntax, fixture.Registry, fixture.Reporter))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task Builders_ReportTheirKind()
    {
        await Assert.That(new ElementDeclarationBuilder(Scope).Kind).IsEqualTo(XsdComponentKind.ElementDeclaration);
        await Assert.That(new ModelGroupDefinitionBuilder(Scope).Kind).IsEqualTo(XsdComponentKind.ModelGroupDefinition);
    }

    [Test]
    public async Task Builders_RejectNullArguments()
    {
        var fixture = ElementFixture.Build(string.Empty);
        var elements = new ElementDeclarationBuilder(Scope);
        var groups = new ModelGroupDefinitionBuilder(Scope);

        await Assert
            .That(() => elements.Build(null!, fixture.Registry, fixture.Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => groups.Build(null!, fixture.Registry, fixture.Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ParticleBuilder(Scope).BuildGroup(null!, fixture.Reporter))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ComponentsAndTheirKinds_AreExposed()
    {
        var fixture = ElementFixture
            .Build("<xs:group name=\"g\"><xs:sequence><xs:any/><xs:element name=\"a\"/></xs:sequence></xs:group>")
            .Resolve();

        var group = fixture.Group("g");

        await Assert.That(group.Kind).IsEqualTo(XsdComponentKind.ModelGroupDefinition);
        await Assert.That(group.ModelGroup.Kind).IsEqualTo(XsdComponentKind.ModelGroup);
        await Assert.That(group.ModelGroup.Name == default).IsTrue();
        await Assert.That(group.ModelGroup.Particles[0].Kind).IsEqualTo(XsdComponentKind.Particle);
        await Assert.That(group.ModelGroup.Particles[0].Name == default).IsTrue();
        await Assert.That(group.ModelGroup.Particles[0].Term).IsTypeOf<Wildcard>();
        await Assert.That(group.ModelGroup.Particles[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task ReferencesInNestedCompositors_AreBoundThroughTheOwningGroup()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:element name=\"e\" type=\"xs:string\"/>"
                    + "<xs:group name=\"inner\"><xs:choice/></xs:group>"
                    + "<xs:group name=\"g\"><xs:sequence><xs:choice><xs:element ref=\"t:e\"/><xs:group ref=\"t:inner\"/>"
                    + "<xs:element name=\"l\" type=\"xs:int\"/></xs:choice></xs:sequence></xs:group>"
            )
            .Resolve();

        var choice = (ModelGroup)fixture.Group("g").ModelGroup.Particles[0].Term!;

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(fixture.Group("g").ReferenceCount).IsEqualTo(3);
        await Assert.That(choice.Particles[0].Term).IsSameReferenceAs(fixture.Element("e"));
        await Assert.That(choice.Particles[1].Term).IsSameReferenceAs(fixture.Group("inner"));
        await Assert
            .That(((ElementDeclaration)choice.Particles[2].Term!).TypeDefinition!.Name.LocalName)
            .IsEqualTo("int");
    }

    [Test]
    public async Task OccurrenceAboveLongMaxValue_IsReadAsUnbounded()
    {
        var fixture = ElementFixture.Build(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" minOccurs=\"0\" maxOccurs=\"99999999999999999999999\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(fixture.Group("g").ModelGroup.Particles[0].MaxOccurs).IsEqualTo(Particle.Unbounded);
    }

    [Test]
    public async Task GroupDefinition_WithTwoCompositors_IsASchemaError()
    {
        var fixture = ElementFixture.Build("<xs:group name=\"g\"><xs:sequence/><xs:choice/></xs:group>");

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-model_group_defn"));
        await Assert.That(fixture.Group("g").ModelGroup.Compositor.ToString()).IsEqualTo("Sequence");
    }

    [Test]
    public async Task GroupDefinition_WithoutName_IsASchemaError_AndIsNotRegistered()
    {
        var fixture = ElementFixture.Build("<xs:group><xs:sequence/></xs:group>");

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-model_group_defn"));
        await Assert.That(fixture.Registry.Count).IsEqualTo(ElementFixture.Build(string.Empty).Registry.Count);
    }

    [Test]
    public async Task GroupReference_WithoutRef_IsASchemaError()
    {
        var fixture = ElementFixture.Build("<xs:group name=\"g\"><xs:sequence><xs:group/></xs:sequence></xs:group>");

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-model_group_defn"));
        await Assert.That(fixture.Group("g").ModelGroup.Particles.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WildcardWithAnInvalidNamespace_IsASchemaError()
    {
        var fixture = ElementFixture.Build(
            "<xs:group name=\"g\"><xs:sequence><xs:any namespace=\"##any ##other\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-wildcard"));
        await Assert.That(fixture.Group("g").ModelGroup.Particles.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("<xs:element name=\"a\" type=\"p:x\"/>")]
    [Arguments("<xs:group name=\"g\"><xs:sequence><xs:element ref=\"p:x\"/></xs:sequence></xs:group>")]
    [Arguments("<xs:group name=\"g\"><xs:sequence><xs:group ref=\"p:x\"/></xs:sequence></xs:group>")]
    public async Task UndeclaredPrefix_IsAResolveError(string body)
    {
        var fixture = ElementFixture.Build(body);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-resolve"));
    }

    [Test]
    public async Task InlineType_CanBeBoundByTheTypeBuilders()
    {
        var fixture = ElementFixture.Build("<xs:element name=\"a\"><xs:simpleType/></xs:element>").Resolve();
        var type = new FakeComponent(XsdComponentKind.SimpleType, string.Empty);
        var element = fixture.Element("a");

        element.BindInlineType(type);

        await Assert.That(element.TypeDefinition).IsSameReferenceAs(type);
        await Assert.That(element.TypeIdentity).IsSameReferenceAs(type);
        await Assert.That(() => element.BindInlineType(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task EntryPoints_RejectNullArguments()
    {
        var fixture = ElementFixture.Build("<xs:element name=\"a\"/><xs:group name=\"g\"><xs:sequence/></xs:group>");
        var element = fixture.Element("a");
        var group = fixture.Group("g").ModelGroup;
        var location = element.Location;

        await Assert.That(() => element.Bind(0, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => group.Bind(0, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => group.CollectReferences(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => ModelGroup.Create(ModelGroupCompositor.Sequence, location, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ModelGroupDefinition.Create(element.Name, location, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Particle.Create(location, 1, 1, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => ElementDeclarationsConsistent.Check(null!, fixture.Reporter))
            .Throws<ArgumentNullException>();
        await Assert.That(() => ElementDeclarationsConsistent.Check(group, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ElementDeclaration_ReportsItsKind_AndOwnReferenceCount()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"a\"/><xs:element name=\"b\"><xs:complexType/></xs:element>"
        );

        await Assert.That(fixture.Element("a").Kind).IsEqualTo(XsdComponentKind.ElementDeclaration);
        await Assert.That(fixture.Element("a").ReferenceCount).IsEqualTo(1);
        await Assert.That(fixture.Element("b").ReferenceCount).IsEqualTo(0);
        await Assert.That(fixture.Element("a").GetReference(0).Space).IsEqualTo(XsdSymbolSpace.Types);
    }
}
