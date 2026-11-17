namespace NetEvolve.Schemas.Xsd.Tests.Unit.Elements;

using NetEvolve.Schemas.Xsd.Elements;

public class ElementDeclarationsConsistentTests
{
    private static async Task<ElementFixture> Check(string body)
    {
        var fixture = ElementFixture.Build(body).Resolve();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
        ElementDeclarationsConsistent.Check(fixture.Group("g").ModelGroup, fixture.Reporter);
        return fixture;
    }

    [Test]
    public async Task TwoLocalElementsWithTheSameNameAndDifferentTypes_Violate()
    {
        var fixture = await Check(
            "<xs:group name=\"g\"><xs:sequence>\n<xs:element name=\"a\" type=\"xs:string\"/>\n<xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("cos-element-consistent"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(4);
    }

    [Test]
    public async Task TwoLocalElementsWithTheSameNameAndTheSameType_AreConsistent()
    {
        var fixture = await Check(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:string\"/><xs:element name=\"a\" type=\"xs:string\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DifferentNames_AreConsistent()
    {
        var fixture = await Check(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:string\"/><xs:element name=\"b\" type=\"xs:int\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SameLocalNameInDifferentNamespaces_IsConsistent()
    {
        var fixture = await Check(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:string\" form=\"qualified\"/><xs:element name=\"a\" type=\"xs:int\" form=\"unqualified\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TwoAnonymousTypes_AreAlwaysDifferentTypes()
    {
        var fixture = await Check(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\"><xs:complexType/></xs:element><xs:element name=\"a\"><xs:complexType/></xs:element></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("cos-element-consistent"));
    }

    [Test]
    public async Task ConflictAcrossNestedCompositors_IsFound()
    {
        var fixture = await Check(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:string\"/><xs:choice><xs:element name=\"a\" type=\"xs:int\"/></xs:choice></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ConflictThroughAGroupReference_IsFound()
    {
        var fixture = await Check(
            "<xs:group name=\"inner\"><xs:sequence><xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:group>"
                + "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:string\"/><xs:group ref=\"t:inner\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ConflictBetweenAnElementReferenceAndALocalElement_IsFound()
    {
        var fixture = await Check(
            "<xs:element name=\"a\" type=\"xs:int\"/>"
                + "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:string\" form=\"qualified\"/><xs:element ref=\"t:a\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ElementContentIsNotPartOfTheContentModel()
    {
        var fixture = await Check(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\"><xs:complexType><xs:sequence><xs:element name=\"b\" type=\"xs:int\"/></xs:sequence></xs:complexType></xs:element><xs:element name=\"b\" type=\"xs:string\"/></xs:sequence></xs:group>"
        );

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task UnresolvedTypes_AreSkippedWithoutFollowUpErrors()
    {
        var fixture = ElementFixture
            .Build(
                "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"t:missing\"/><xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:group>"
            )
            .Resolve();

        ElementDeclarationsConsistent.Check(fixture.Group("g").ModelGroup, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("src-resolve"));
    }

    [Test]
    public async Task CircularGroupReferences_DoNotLoopForever()
    {
        var fixture = ElementFixture
            .Build("<xs:group name=\"g\"><xs:sequence><xs:group ref=\"t:g\"/></xs:sequence></xs:group>")
            .Resolve();

        ElementDeclarationsConsistent.Check(fixture.Group("g").ModelGroup, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("mg-props-correct"));
    }
}
