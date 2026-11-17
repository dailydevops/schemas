namespace NetEvolve.Schemas.Xsd.Tests.Unit.Elements;

using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

public class ModelGroupDefinitionTests
{
    [Test]
    public async Task GroupDefinition_IsRegistered_AndExposesCompositorAndParticles()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:group name=\"g\"><xs:choice>"
                    + "<xs:element name=\"a\" type=\"xs:string\" minOccurs=\"0\" maxOccurs=\"unbounded\"/>"
                    + "<xs:element name=\"b\" type=\"xs:int\" maxOccurs=\"3\"/>"
                    + "<xs:any namespace=\"##other\" processContents=\"lax\"/>"
                    + "</xs:choice></xs:group>"
            )
            .Resolve();

        var group = fixture.Group("g");
        var particles = group.ModelGroup.Particles;

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(group.Name.LocalName).IsEqualTo("g");
        await Assert.That(group.ModelGroup.Compositor).IsEqualTo(ModelGroupCompositor.Choice);
        await Assert.That(particles.Count).IsEqualTo(3);
        await Assert.That(particles[0].MinOccurs).IsEqualTo(0L);
        await Assert.That(particles[0].MaxOccurs).IsEqualTo(Particle.Unbounded);
        await Assert.That(particles[1].MinOccurs).IsEqualTo(1L);
        await Assert.That(particles[1].MaxOccurs).IsEqualTo(3L);
        await Assert.That(particles[2].Term).IsTypeOf<Wildcard>();
    }

    [Test]
    [Arguments("sequence", "Sequence")]
    [Arguments("choice", "Choice")]
    [Arguments("all", "All")]
    public async Task GroupDefinition_ReadsEveryCompositor(string tag, string expected)
    {
        var fixture = ElementFixture.Build("<xs:group name=\"g\"><xs:" + tag + "/></xs:group>");

        await Assert.That(fixture.Group("g").ModelGroup.Compositor.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task GroupDefinition_WithoutCompositor_IsASchemaError()
    {
        var fixture = ElementFixture.Build("<xs:group name=\"g\"/>");

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-model_group_defn"));
    }

    [Test]
    public async Task MinOccursAboveMaxOccurs_IsASchemaError()
    {
        var fixture = ElementFixture.Build(
            "<xs:group name=\"g\"><xs:sequence>\n<xs:element name=\"a\" minOccurs=\"3\" maxOccurs=\"2\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("p-props-correct"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task GroupReference_ResolvesToTheDefinition()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:group name=\"inner\"><xs:sequence/></xs:group>"
                    + "<xs:group name=\"outer\"><xs:sequence><xs:group ref=\"t:inner\" maxOccurs=\"2\"/></xs:sequence></xs:group>"
            )
            .Resolve();

        var particle = fixture.Group("outer").ModelGroup.Particles[0];

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(particle.Term).IsSameReferenceAs(fixture.Group("inner"));
        await Assert.That(particle.MaxOccurs).IsEqualTo(2L);
    }

    [Test]
    public async Task GroupReference_ToAnUndeclaredGroup_IsAResolveError()
    {
        var fixture = ElementFixture
            .Build("<xs:group name=\"g\"><xs:sequence><xs:group ref=\"t:missing\"/></xs:sequence></xs:group>")
            .Resolve();

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-resolve"));
    }

    [Test]
    public async Task GroupReference_Forward_Resolves()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:group name=\"first\"><xs:sequence><xs:group ref=\"t:later\"/></xs:sequence></xs:group>"
                    + "<xs:group name=\"later\"><xs:choice/></xs:group>"
            )
            .Resolve();

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert
            .That(fixture.Group("first").ModelGroup.Particles[0].Term)
            .IsSameReferenceAs(fixture.Group("later"));
    }

    [Test]
    public async Task ElementReference_ResolvesToTheGlobalDeclaration()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:element name=\"e\" type=\"xs:string\"/>"
                    + "<xs:group name=\"g\"><xs:sequence><xs:element ref=\"t:e\" minOccurs=\"0\"/></xs:sequence></xs:group>"
            )
            .Resolve();

        var particle = fixture.Group("g").ModelGroup.Particles[0];

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        await Assert.That(particle.Term).IsSameReferenceAs(fixture.Element("e"));
        await Assert.That(particle.MinOccurs).IsEqualTo(0L);
    }

    [Test]
    public async Task CircularGroupReference_IsALocatedSchemaError()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:group name=\"a\"><xs:sequence><xs:group ref=\"t:b\"/></xs:sequence></xs:group>\n"
                    + "<xs:group name=\"b\"><xs:sequence><xs:group ref=\"t:a\"/></xs:sequence></xs:group>"
            )
            .Resolve();

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("mg-props-correct"));
        await Assert.That(fixture.Issues[0].Location.Line).IsGreaterThan(1);
    }

    [Test]
    public async Task GroupReferencingItselfThroughANestedCompositor_IsASchemaError()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:group name=\"a\"><xs:sequence><xs:choice><xs:group ref=\"t:a\"/></xs:choice></xs:sequence></xs:group>"
            )
            .Resolve();

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("mg-props-correct"));
    }

    [Test]
    public async Task ElementReference_InsideAGroup_IsNotACircularDefinition()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:element name=\"e\" type=\"xs:string\"/>"
                    + "<xs:group name=\"a\"><xs:sequence><xs:element ref=\"t:e\"/><xs:element name=\"l\" type=\"t:missingNot\"/></xs:sequence></xs:group>"
            )
            .Resolve();

        // Only the undeclared type is reported; element references never form a circular definition.
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-resolve"));
    }
}
