namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>Validity constraint Enumeration (XML 1.0, section 3.3.1): the value is one of the declared tokens.</summary>
public class EnumerationTests
{
    private static DtdDeclarations Choices() =>
        new AttributeStepModel().Element("r").Attlist("r", Enumeration("e", "yes", "no", "Maybe")).Build();

    [Test]
    public async Task Enumeration_ReportsXml5019_AtTheAttribute_WithCodeSeverityConstraintAndLocation()
    {
        var run = Run("<r ~e=\"perhaps\"/>", Choices());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5019.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Enumeration");
        await Assert.That(issue.Location).IsEqualTo(run.Expected);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 4));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5019.Message);
        await Assert.That(issue.Message).Contains("'perhaps'");
        await Assert.That(issue.Message).Contains("'e'");
    }

    [Test]
    [Arguments("yes")]
    [Arguments("no")]
    [Arguments("Maybe")]
    public async Task Enumeration_WithADeclaredValue_ReportsNothing(string value)
    {
        var run = Run("<r e=\"" + value + "\"/>", Choices());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("")]
    [Arguments("YES")]
    [Arguments("maybe")]
    [Arguments("yes no")]
    [Arguments(" yes")]
    [Arguments("ye")]
    public async Task Enumeration_WithAValueThatIsNotDeclared_IsReported(string value)
    {
        var run = Run("<r ~e=\"" + value + "\"/>", Choices());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5019.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task Enumeration_ReportsEachElementThatCarriesAWrongValue()
    {
        var run = Run("<r ~e=\"x\"><r e=\"yes\"/><r e=\"y\"/></r>", Choices());

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[1].Location.Column).IsGreaterThan(run.Issues[0].Location.Column);
    }
}
