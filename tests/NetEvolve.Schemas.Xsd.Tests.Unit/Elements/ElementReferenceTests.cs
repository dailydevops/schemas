namespace NetEvolve.Schemas.Xsd.Tests.Unit.Elements;

public class ElementReferenceTests
{
    [Test]
    [Arguments("ref=\"t:e\" type=\"xs:string\"")]
    [Arguments("ref=\"t:e\" nillable=\"true\"")]
    [Arguments("ref=\"t:e\" default=\"x\"")]
    [Arguments("ref=\"t:e\" fixed=\"x\"")]
    [Arguments("ref=\"t:e\" form=\"qualified\"")]
    [Arguments("ref=\"t:e\" block=\"extension\"")]
    public async Task ElementReference_WithADeclarationAttribute_IsASchemaError(string attributes)
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"e\"/><xs:group name=\"g\"><xs:sequence>\n<xs:element "
                + attributes
                + "/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-element"));
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.2.2");
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task ElementReference_WithAnAnonymousType_IsASchemaError()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"e\"/><xs:group name=\"g\"><xs:sequence><xs:element ref=\"t:e\"><xs:complexType/></xs:element></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.2.2");
    }

    [Test]
    public async Task LocalElement_WithNeitherNameNorRef_IsASchemaError()
    {
        var fixture = ElementFixture.Build(
            "<xs:group name=\"g\"><xs:sequence><xs:element type=\"xs:string\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.2.1");
    }

    [Test]
    public async Task LocalElement_WithNameAndRef_IsASchemaError()
    {
        var fixture = ElementFixture.Build(
            "<xs:element name=\"e\"/><xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" ref=\"t:e\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Constraint).IsEqualTo("src-element.2.1");
    }

    [Test]
    public async Task ElementReference_ToAnUndeclaredElement_IsAResolveError()
    {
        var fixture = ElementFixture
            .Build("<xs:group name=\"g\"><xs:sequence><xs:element ref=\"t:missing\"/></xs:sequence></xs:group>")
            .Resolve();

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-resolve"));
    }
}
