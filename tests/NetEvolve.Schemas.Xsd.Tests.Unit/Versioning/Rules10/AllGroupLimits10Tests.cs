namespace NetEvolve.Schemas.Xsd.Tests.Unit.Versioning.Rules10;

using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Elements;
using NetEvolve.Schemas.Xsd.Versioning.Rules10;

/// <summary>
/// All Group Limited (Structures 1.0, section 3.8.6): in strict 1.0 mode an <c>all</c> group whose member occurs more than once,
/// is nested in another group, or contains a wildcard is a located schema error; the check reports nothing in 1.1 mode.
/// </summary>
public class AllGroupLimits10Tests
{
    private static readonly DocumentLocation Where = new("file:///f.xsd", 3, 5);

    private const string MemberOccursTwice =
        "<xs:group name=\"g\">\n<xs:all>\n<xs:element name=\"a\" type=\"xs:int\" maxOccurs=\"2\"/>\n</xs:all></xs:group>";

    private const string WithWildcard = "<xs:group name=\"g\">\n<xs:all>\n<xs:any/>\n</xs:all></xs:group>";

    private const string Inner =
        "<xs:group name=\"inner\"><xs:all><xs:element name=\"a\" type=\"xs:int\"/></xs:all></xs:group>";

    // An inline all group inside a sequence is a syntax error of the schema document, so a nested group arrives by reference.
    private const string NestedInSequence =
        Inner + "<xs:group name=\"g\">\n<xs:sequence>\n<xs:group ref=\"t:inner\"/></xs:sequence></xs:group>";

    private static ElementFixture Build(string body)
    {
        var fixture = ElementFixture.Build(body).Resolve();
        return fixture;
    }

    private static ModelGroupDefinition Definition(ModelGroupCompositor compositor, params Particle[] particles) =>
        ModelGroupDefinition.Create(
            new XPathQName("urn:test", "d"),
            Where,
            ModelGroup.Create(compositor, Where, particles)
        );

    private static int CheckDefinition(ElementFixture fixture, XsdVersion version, string group = "g")
    {
        var before = fixture.Issues.Count;
        AllGroupLimits10.CheckDefinition(fixture.Group(group), version, fixture.Reporter);
        return fixture.Issues.Count - before;
    }

    // The three schemas of the acceptance criteria: a member with maxOccurs above 1, a nested all group, and a wildcard.
    [Test]
    [Arguments(MemberOccursTwice, 4)]
    [Arguments(NestedInSequence, 4)]
    [Arguments(WithWildcard, 4)]
    public async Task AllGroupOutsideTheLimits_IsALocatedSchemaErrorInStrict10Mode(string body, int line)
    {
        var fixture = Build(body);
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);

        await Assert.That(CheckDefinition(fixture, XsdVersion.V10)).IsEqualTo(1);

        var issue = fixture.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(fixture.CodeOf("cos-all-limited"));
        await Assert.That(issue.Location.Line).IsEqualTo(line);
    }

    [Test]
    [Arguments(MemberOccursTwice)]
    [Arguments(NestedInSequence)]
    [Arguments(WithWildcard)]
    public async Task AllGroupOutsideTheLimits_RaisesNoErrorInVersion11Mode(string body)
    {
        var fixture = Build(body);

        await Assert.That(CheckDefinition(fixture, XsdVersion.V11)).IsEqualTo(0);
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EachViolationOfOneGroup_IsReportedOnItsOwnParticle()
    {
        var fixture = Build(
            "<xs:group name=\"g\">\n<xs:all>\n<xs:element name=\"a\" type=\"xs:int\" maxOccurs=\"unbounded\"/>\n<xs:any maxOccurs=\"2\"/>\n</xs:all></xs:group>"
        );

        await Assert.That(CheckDefinition(fixture, XsdVersion.V10)).IsEqualTo(3);

        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(4);
        await Assert.That(fixture.Issues[1].Location.Line).IsEqualTo(5);
        await Assert.That(fixture.Issues[2].Location.Line).IsEqualTo(5);
    }

    [Test]
    public async Task GroupReferenceInsideAnAllGroup_IsNotAnElementMember()
    {
        var fixture = Build(Inner + "<xs:group name=\"g\"><xs:all>\n<xs:group ref=\"t:inner\"/></xs:all></xs:group>");

        await Assert.That(CheckDefinition(fixture, XsdVersion.V10)).IsEqualTo(1);
        await Assert.That(CheckDefinition(fixture, XsdVersion.V11)).IsEqualTo(0);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task InlineAllGroupInsideACompositor_IsNested(bool isChoice)
    {
        var fixture = Build(Inner);
        var definition = Definition(
            isChoice ? ModelGroupCompositor.Choice : ModelGroupCompositor.Sequence,
            Particle.Create(Where, 1, 1, fixture.Group("inner").ModelGroup)
        );

        AllGroupLimits10.CheckDefinition(definition, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("cos-all-limited"));
        await Assert.That(fixture.Issues[0].Location).IsEqualTo(Where);

        AllGroupLimits10.CheckDefinition(definition, XsdVersion.V11, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task InlineAllGroupInsideAnAllGroup_IsNotAnElementMember()
    {
        var fixture = Build(Inner);
        var definition = Definition(
            ModelGroupCompositor.All,
            Particle.Create(Where, 1, 1, fixture.Group("inner").ModelGroup)
        );

        AllGroupLimits10.CheckDefinition(definition, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GroupReferenceToASequenceInsideASequence_IsNotAnAllGroup()
    {
        var fixture = Build(
            "<xs:group name=\"inner\"><xs:sequence><xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:group>"
                + "<xs:group name=\"g\"><xs:sequence><xs:group ref=\"t:inner\"/></xs:sequence></xs:group>"
        );

        await Assert.That(CheckDefinition(fixture, XsdVersion.V10)).IsEqualTo(0);
        await Assert.That(CheckDefinition(fixture, XsdVersion.V10, "inner")).IsEqualTo(0);
    }

    [Test]
    public async Task ReferenceToADefinitionThatHoldsANestedAllGroup_IsReportedOnTheDefinitionOnly()
    {
        var fixture = Build(Inner);
        var holder = Definition(
            ModelGroupCompositor.Sequence,
            Particle.Create(Where, 1, 1, fixture.Group("inner").ModelGroup)
        );
        var content = Particle.Create(Where, 1, 1, holder);

        AllGroupLimits10.CheckContent(content, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);

        AllGroupLimits10.CheckDefinition(holder, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AllGroupWithinTheLimits_IsValid()
    {
        var fixture = Build(
            "<xs:element name=\"e\" type=\"xs:int\"/>"
                + "<xs:group name=\"g\"><xs:all minOccurs=\"0\">"
                + "<xs:element name=\"a\" type=\"xs:int\"/>"
                + "<xs:element name=\"b\" type=\"xs:int\" minOccurs=\"0\" maxOccurs=\"1\"/>"
                + "<xs:element name=\"c\" type=\"xs:int\" minOccurs=\"0\" maxOccurs=\"0\"/>"
                + "<xs:element ref=\"t:e\"/>"
                + "</xs:all></xs:group>"
        );
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);

        await Assert.That(CheckDefinition(fixture, XsdVersion.V10)).IsEqualTo(0);
    }

    [Test]
    public async Task UnresolvedElementReferenceInAnAllGroup_CausesNoSecondError()
    {
        var fixture = Build("<xs:group name=\"g\"><xs:all><xs:element ref=\"t:missing\"/></xs:all></xs:group>");
        var resolverIssues = fixture.Issues.Count;

        await Assert.That(CheckDefinition(fixture, XsdVersion.V10)).IsEqualTo(0);
        await Assert.That(fixture.Issues.Count).IsEqualTo(resolverIssues);
    }

    // The content type of a complex type: the all group must be the only content and occur once.
    [Test]
    public async Task ContentTypeWithAnAllGroupThatOccursOnce_IsValid()
    {
        var fixture = Build(
            "<xs:group name=\"g\"><xs:all><xs:element name=\"a\" type=\"xs:int\"/></xs:all></xs:group>"
        );
        var content = Particle.Create(Where, 0, 1, fixture.Group("g").ModelGroup);
        var before = fixture.Issues.Count;

        AllGroupLimits10.CheckContent(content, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(before);
    }

    [Test]
    public async Task ContentTypeWithAnAllGroupThatOccursTwice_IsALocatedSchemaError()
    {
        var fixture = Build(
            "<xs:group name=\"g\"><xs:all><xs:element name=\"a\" type=\"xs:int\"/></xs:all></xs:group>"
        );
        var content = Particle.Create(Where, 1, 2, fixture.Group("g").ModelGroup);

        AllGroupLimits10.CheckContent(content, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(fixture.CodeOf("cos-all-limited"));
        await Assert.That(fixture.Issues[0].Location).IsEqualTo(Where);

        AllGroupLimits10.CheckContent(content, XsdVersion.V11, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ContentTypeWithAnInlineAllGroup_ChecksItsMembers()
    {
        var fixture = Build(MemberOccursTwice);
        var content = Particle.Create(Where, 1, 1, fixture.Group("g").ModelGroup);

        AllGroupLimits10.CheckContent(content, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(4);
    }

    [Test]
    public async Task ContentTypeThatReferencesAnAllGroupDefinition_LeavesTheMembersToTheDefinitionCheck()
    {
        var fixture = Build(
            "<xs:group name=\"g\"><xs:all><xs:element name=\"a\" type=\"xs:int\" maxOccurs=\"2\"/></xs:all></xs:group>"
        );
        var once = Particle.Create(Where, 1, 1, fixture.Group("g"));
        var twice = Particle.Create(Where, 1, 2, fixture.Group("g"));

        AllGroupLimits10.CheckContent(once, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);

        AllGroupLimits10.CheckContent(twice, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ContentTypeThatReferencesASequenceDefinition_IsNotChecked()
    {
        var fixture = Build(
            "<xs:group name=\"g\"><xs:sequence><xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:group>"
        );
        var content = Particle.Create(Where, 1, 5, fixture.Group("g"));

        AllGroupLimits10.CheckContent(content, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ContentTypeWithASequenceThatHoldsAnAllGroup_IsNested()
    {
        var fixture = Build(Inner);
        var sequence = ModelGroup.Create(
            ModelGroupCompositor.Sequence,
            Where,
            [Particle.Create(Where, 1, 1, fixture.Group("inner").ModelGroup)]
        );

        AllGroupLimits10.CheckContent(Particle.Create(Where, 1, 1, sequence), XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ContentTypeWithAnElementTerm_HasNothingToCheck()
    {
        var fixture = Build("<xs:element name=\"e\" type=\"xs:int\"/>");
        var content = Particle.Create(Where, 1, 3, fixture.Element("e"));

        AllGroupLimits10.CheckContent(content, XsdVersion.V10, fixture.Reporter);

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Guards_RejectNullsAndAnUnknownVersion()
    {
        var fixture = Build(MemberOccursTwice);
        var definition = fixture.Group("g");
        var content = Particle.Create(Where, 1, 1, definition.ModelGroup);
        var reporter = fixture.Reporter;

        await Assert
            .That(() => AllGroupLimits10.CheckContent(null!, XsdVersion.V10, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AllGroupLimits10.CheckContent(content, XsdVersion.V10, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AllGroupLimits10.CheckContent(content, (XsdVersion)2, reporter))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => AllGroupLimits10.CheckContent(content, (XsdVersion)(-1), reporter))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => AllGroupLimits10.CheckDefinition(null!, XsdVersion.V10, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AllGroupLimits10.CheckDefinition(definition, XsdVersion.V10, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AllGroupLimits10.CheckDefinition(definition, (XsdVersion)2, reporter))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => AllGroupLimits10.CheckDefinition(definition, (XsdVersion)(-1), reporter))
            .Throws<ArgumentOutOfRangeException>();
    }
}
