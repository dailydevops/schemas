namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>Validity constraint Name Token (XML 1.0, section 3.3.1): NMTOKEN and NMTOKENS values are name tokens.</summary>
public class NameTokenTests
{
    private static DtdDeclarations Tokens() =>
        new AttributeStepModel().Element("r").Attlist("r", NmToken("n"), NmTokens("ns"), Cdata("c")).Build();

    [Test]
    public async Task NameToken_ReportsXml5014_AtTheAttribute_WithCodeSeverityConstraintAndLocation()
    {
        var run = Run("<r ~n=\"a b\"/>", Tokens());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5014.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Name Token");
        await Assert.That(issue.Location).IsEqualTo(run.Expected);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 4));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5014.Message);
        await Assert.That(issue.Message).Contains("'a b'");
        await Assert.That(issue.Message).Contains("'n'");
    }

    [Test]
    [Arguments("abc")]
    [Arguments("1")]
    [Arguments("-a")]
    [Arguments(".5")]
    [Arguments("a:b")]
    [Arguments("_")]
    [Arguments("·x")]
    [Arguments("x̀")]
    [Arguments("‿")]
    [Arguments("\U00010000")]
    [Arguments("a\U000EFFFF")]
    public async Task NmTokenAttribute_WithANameToken_ReportsNothing(string value)
    {
        var run = Run("<r n=\"" + value + "\"/>", Tokens());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("")]
    [Arguments("a b")]
    [Arguments(" a")]
    [Arguments("a ")]
    [Arguments("a!")]
    [Arguments("a,b")]
    [Arguments("×")]
    [Arguments("\uD800")]
    [Arguments("a\uDC00")]
    [Arguments("󰀀")]
    [Arguments("a\uD800")]
    public async Task NmTokenAttribute_WithAValueThatIsNotANameToken_IsReported(string value)
    {
        var run = Run("<r ~n=\"" + value + "\"/>", Tokens());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5014.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task NmTokensAttribute_WithNameTokens_ReportsNothing()
    {
        var run = Run("<r ns=\"a 1 -b\"/>", Tokens());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task NmTokensAttribute_WithASingleToken_ReportsNothing()
    {
        var run = Run("<r ns=\"a\"/>", Tokens());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task NmTokensAttribute_ReportsEachOffendingToken_AndOnlyThat()
    {
        var run = Run("<r ~ns=\"a! b c?\"/>", Tokens());

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5014.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).Contains("'a!'");
        await Assert.That(run.Issues[0].Message).DoesNotContain("'b'");
        await Assert.That(run.Issues[1].Message).Contains("'c?'");
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("a  b")]
    [Arguments(" a")]
    [Arguments("a ")]
    public async Task NmTokensAttribute_WithAnEmptyToken_IsReportedOnce(string value)
    {
        var run = Run("<r ~ns=\"" + value + "\"/>", Tokens());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5014.Code);
        await Assert.That(run.Issues[0].Message).Contains("''");
    }

    [Test]
    public async Task CdataAttribute_AcceptsAnyValue()
    {
        var run = Run("<r c=\"a b!\"/>", Tokens());

        await Assert.That(run.Issues).IsEmpty();
    }
}
