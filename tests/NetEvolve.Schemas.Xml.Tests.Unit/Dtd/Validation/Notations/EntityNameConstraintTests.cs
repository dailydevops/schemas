namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Notations;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationModel;

/// <summary>Validity constraint Entity Name (XML 1.0, section 3.3.1): ENTITY and ENTITIES values name declared unparsed entities.</summary>
public class EntityNameConstraintTests
{
    private static DtdDeclarations Entities() =>
        new NotationModel()
            .Element("r")
            .Attlist("r", EntityAttribute("e"), EntitiesAttribute("es"), CdataAttribute("c"))
            .Notation("gif")
            .Entity(Unparsed("logo", "gif"))
            .Entity(Unparsed("photo", "gif"))
            .Entity(Parsed("copy"))
            .Entity(Parameter("pe"))
            .Build();

    [Test]
    public async Task EntityName_ReportsXml5013_AtTheAttribute_WithCodeSeverityConstraintAndLocation()
    {
        var run = Run("<r ~e=\"ghost\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5013.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Entity Name");
        await Assert.That(issue.Location).IsEqualTo(run.Expected);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 4));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5013.Message);
        await Assert.That(issue.Message).Contains("'ghost'");
    }

    [Test]
    public async Task EntityAttribute_NamingADeclaredUnparsedEntity_ReportsNothing()
    {
        var run = Run("<r e=\"logo\"/>", Entities());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task EntityAttribute_NamingAParsedEntity_IsReported()
    {
        var run = Run("<r ~e=\"copy\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5013.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).Contains("'copy'");
        await Assert.That(run.Issues[0].Message).Contains("parsed");
    }

    [Test]
    public async Task EntityAttribute_NamingAParameterEntity_IsReported()
    {
        var run = Run("<r ~e=\"pe\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5013.Code);
    }

    [Test]
    [Arguments("")]
    [Arguments("1logo")]
    [Arguments("LOGO")]
    [Arguments("logo ")]
    public async Task EntityAttribute_WithAValueThatIsNotTheNameOfAnUnparsedEntity_IsReported(string value)
    {
        var run = Run("<r ~e=\"" + value + "\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5013.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task EntitiesAttribute_WithEveryTokenDeclared_ReportsNothing()
    {
        var run = Run("<r es=\"logo photo logo\"/>", Entities());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task EntitiesAttribute_WithOneUndeclaredToken_ReportsExactlyThatToken()
    {
        var run = Run("<r ~es=\"logo ghost photo\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5013.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).Contains("'ghost'");
        await Assert.That(run.Issues[0].Message).DoesNotContain("'logo'");
        await Assert.That(run.Issues[0].Message).DoesNotContain("'photo'");
    }

    [Test]
    public async Task EntitiesAttribute_WithSeveralOffendingTokens_ReportsEachOnce_InTokenOrder()
    {
        var run = Run("<r ~es=\"ghost logo copy\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Message).Contains("'ghost'");
        await Assert.That(run.Issues[1].Message).Contains("'copy'");
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected);
    }

    [Test]
    [Arguments(" logo")]
    [Arguments("logo ")]
    [Arguments("logo  photo")]
    [Arguments("")]
    public async Task EntitiesAttribute_WithAnEmptyToken_IsReported(string value)
    {
        var run = Run("<r ~es=\"" + value + "\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5013.Code);
    }

    [Test]
    public async Task UnparsedEntity_WhoseNotationIsUndeclared_IsStillAnUnparsedEntityForTheAttribute()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", EntityAttribute("e"))
            .Entity(Unparsed("logo", "missing"))
            .Build();

        var run = Run("<r e=\"logo\"/>", model);

        // Only the declaration is reported (Notation Declared); the attribute names a declared unparsed entity.
        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5025.Code);
    }

    [Test]
    public async Task EveryElementIsChecked_EachOccurrenceIsReported()
    {
        var run = Run("<r ~e=\"ghost\"><r e=\"ghost\"/></r>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[1].Location).IsEqualTo(new DocumentLocation(SystemId, 1, 17));
    }

    [Test]
    public async Task AttributesOfOtherTypes_AreNotEntityValues()
    {
        var run = Run("<r c=\"ghost\" undeclared=\"ghost\"/>", Entities());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task DefaultedEntityValue_IsCheckedLikeASpecifiedOne()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdNotationStep();
        step.BeginDocument(Context(issues, Entities()));

        step.StartElement(Element("r", "e", "ghost", DtdAttributeType.Entity, isSpecified: false));

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task EntityNames_AreCaseSensitive()
    {
        var run = Run("<r ~e=\"Logo\"/>", Entities());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
    }
}
