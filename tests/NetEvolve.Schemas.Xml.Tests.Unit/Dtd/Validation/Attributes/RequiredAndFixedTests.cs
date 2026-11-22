namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>Validity constraints Required Attribute and Fixed Attribute Default (XML 1.0, section 3.3.2).</summary>
public class RequiredAndFixedTests
{
    private static DtdDeclarations Required1() =>
        new AttributeStepModel().Element("r").Element("c").Attlist("r", Required("a"), Cdata("o")).Build();

    [Test]
    public async Task RequiredAttribute_Absent_ReportsXml5020_AtTheStartTag_WithCodeSeverityConstraintAndLocation()
    {
        var run = Run("~<r o=\"1\"/>", Required1());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5020.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Required Attribute");
        await Assert.That(issue.Location).IsEqualTo(run.Expected);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 1));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5020.Message);
        await Assert.That(issue.Message).Contains("'a'");
        await Assert.That(issue.Message).Contains("'r'");
    }

    [Test]
    public async Task RequiredAttribute_Present_ReportsNothing()
    {
        var run = Run("<r o=\"1\" a=\"\"/>", Required1());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task RequiredAttribute_IsCheckedAtEveryElementOfTheType_AtItsOwnLocation()
    {
        var run = Run("<r a=\"1\">\n  ~<r/>\n</r>", Required1());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Location).IsEqualTo(new DocumentLocation(SystemId, 2, 3));
    }

    [Test]
    public async Task RequiredAttributes_Absent_AreReportedInDeclarationOrder()
    {
        var model = new AttributeStepModel()
            .Element("r")
            .Attlist("r", Required("b"), Required("a"), Required("c"))
            .Build();

        var run = Run("<r a=\"1\"/>", model);

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Message).Contains("'b'");
        await Assert.That(run.Issues[1].Message).Contains("'c'");
    }

    [Test]
    public async Task RequiredAttribute_IsMatchedByTheExactName()
    {
        var run = Run("<r ~A=\"1\"/>", Required1());

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5020.Code);
        await Assert.That(run.Issues[1].Code).IsEqualTo(DtdValidityCodes.XML5005.Code);
    }

    [Test]
    public async Task ElementWithoutRequiredAttributes_ReportsNothingWhenItHasNoAttributes()
    {
        var run = Run("<r a=\"1\"><c/></r>", Required1());

        await Assert.That(run.Issues).IsEmpty();
    }

    private static DtdDeclarations Fixed() =>
        new AttributeStepModel()
            .Element("r")
            .Entity("amp2", "&amp;")
            .Attlist(
                "r",
                Cdata("f", DtdDefaultKind.Fixed, "v"),
                NmToken("n", DtdDefaultKind.Fixed, "  x  "),
                Cdata("e", DtdDefaultKind.Fixed, "a&amp;b&#x41;&amp2;"),
                Cdata("l", DtdDefaultKind.Fixed, "a\r\nb"),
                Cdata("d", DtdDefaultKind.Default, "v"),
                Cdata("u", DtdDefaultKind.Fixed, "&ghost;")
            )
            .Build();

    [Test]
    public async Task FixedAttribute_WithAnotherValue_ReportsXml5022_AtTheAttribute_WithCodeSeverityConstraintAndLocation()
    {
        var run = Run("<r ~f=\"w\"/>", Fixed());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5022.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Fixed Attribute Default");
        await Assert.That(issue.Location).IsEqualTo(run.Expected);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 4));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5022.Message);
        await Assert.That(issue.Message).Contains("'w'");
        await Assert.That(issue.Message).Contains("'v'");
    }

    [Test]
    public async Task FixedAttribute_WithTheDeclaredValue_ReportsNothing()
    {
        var run = Run("<r f=\"v\"/>", Fixed());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task FixedAttribute_IsComparedWithTheNormalizedDefault_OfItsType()
    {
        var run = Run("<r n=\"x\"/>", Fixed());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task FixedAttribute_IsComparedWithTheExpandedDefault()
    {
        var accepted = Run("<r e=\"a&bA&\"/>", Fixed());
        var rejected = Run("<r ~e=\"a&b\"/>", Fixed());

        await Assert.That(accepted.Issues).IsEmpty();
        await Assert.That(rejected.Issues.Count).IsEqualTo(1);
        await Assert.That(rejected.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5022.Code);
    }

    [Test]
    public async Task FixedAttribute_TreatsALineEndInTheDefaultAsOneSpace()
    {
        var accepted = Run("<r l=\"a b\"/>", Fixed());
        var rejected = Run("<r ~l=\"a  b\"/>", Fixed());

        await Assert.That(accepted.Issues).IsEmpty();
        await Assert.That(rejected.Issues.Count).IsEqualTo(1);
        await Assert.That(rejected.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5022.Code);
    }

    [Test]
    public async Task FixedAttribute_WithADefaultThatCannotBeExpanded_IsNotCompared()
    {
        var run = Run("<r u=\"anything\"/>", Fixed());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task AttributeWithAPlainDefault_MayCarryAnotherValue()
    {
        var run = Run("<r d=\"w\"/>", Fixed());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task DefaultedFixedAttribute_IsNotCompared()
    {
        var model = Fixed();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdAttributeStep();
        step.BeginDocument(Context(issues, model));

        step.StartElement(Element("r", "f", "w", DtdAttributeType.Cdata, isSpecified: false));

        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
