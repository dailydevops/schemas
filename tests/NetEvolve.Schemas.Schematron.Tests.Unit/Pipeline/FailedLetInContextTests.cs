namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// A <see langword="let"/> whose value fails is an error only where an expression reads it (#1446): a rule context that names the variable in the
/// operand of a short-circuited <see langword="and"/> runs without an error, as the variables of the compiled stylesheet of the reference processors do.
/// </summary>
public class FailedLetInContextTests
{
    private const string Schema =
        "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" queryBinding=\"xslt2\">"
        + "<let name=\"off\" value=\"false()\"/>"
        + "<let name=\"broken\" value=\"normalize-space(/r/c)\"/>"
        + "<pattern><rule context=\"/r/a[$off and $broken = 'x']\"><assert test=\"false()\">Never.</assert></rule>"
        + "<rule context=\"/r/b[$broken = 'x']\"><assert test=\"false()\">Reads it.</assert></rule></pattern></schema>";

    [Test]
    public async Task ContextWithAFailingLetInAShortCircuitedOperand_RunsWithoutAnError()
    {
        var result = Validate(Build(Schema), "<r><a/><c>1</c><c>2</c></r>");

        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task ContextThatReadsTheFailingLet_ReportsTheErrorOnce()
    {
        var result = Validate(Build(Schema), "<r><b/><b/><c>1</c><c>2</c></r>");

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task UnreadFailingLetOfAnEarlierRule_IsNotReportedWithTheFailureOfAnUnrelatedLet()
    {
        const string schema =
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" queryBinding=\"xslt2\">"
            + "<let name=\"off\" value=\"false()\"/>"
            + "<let name=\"unread\" value=\"xs:integer('x')\"/>"
            + "<let name=\"read\" value=\"normalize-space(/r/d)\"/>"
            + "<pattern><rule context=\"/r/a[$off and $unread = 'x']\"><assert test=\"false()\">Never.</assert></rule>"
            + "<rule context=\"/r/b\"><assert test=\"$read = 'x'\">Reads it.</assert></rule></pattern></schema>";

        var result = Validate(Build(schema), "<r><a/><b/><d>1</d><d>2</d></r>");

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Message).IsEqualTo("The type of a value does not match the required type.");
    }

    [Test]
    public async Task UnreadFailingLetOfAnEarlierPattern_IsNotReportedWhenALaterPatternReadsAnotherLet()
    {
        const string schema =
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" queryBinding=\"xslt2\">"
            + "<pattern><let name=\"unread\" value=\"xs:integer('x')\"/><let name=\"off\" value=\"false()\"/>"
            + "<rule context=\"/r/a[$off and $unread = 'x']\"><assert test=\"false()\">Never.</assert></rule></pattern>"
            + "<pattern><let name=\"read\" value=\"normalize-space(/r/d)\"/>"
            + "<rule context=\"/r/b\"><assert test=\"$read = 'x'\">Reads it.</assert></rule></pattern></schema>";

        var result = Validate(Build(schema), "<r><a/><b/><d>1</d><d>2</d></r>");

        await Assert.That(result.Issues.Count).IsEqualTo(1);
    }
}
