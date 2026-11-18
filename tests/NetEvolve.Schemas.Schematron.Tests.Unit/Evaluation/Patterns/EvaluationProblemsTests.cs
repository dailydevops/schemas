namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

/// <summary>The sink that turns the problems of the evaluate stage into error issues and error events.</summary>
public class EvaluationProblemsTests
{
    private static SchematronProblem Problem(string message = "boom") =>
        new(SchematronDiagnosticCodes.EvaluationFailed, message, FakeModel.Origin, "XPDY0002");

    private static (EvaluationProblems Sink, ValidationIssueCollector Collector, EventLog Log) Create(
        int maxIssues = 10
    )
    {
        var log = new EventLog();
        var collector = new ValidationIssueCollector(ValidationLimits.Default.WithMaxIssues(maxIssues));
        return (new EvaluationProblems(collector, new IssueMapper(collector, log), "file:///i.xml"), collector, log);
    }

    [Test]
    public async Task Add_MapsAProblemToAnErrorIssue_AndAnErrorEvent()
    {
        var (sink, collector, log) = Create();

        sink.Add(Problem());

        var issue = collector.ToResult().Issues[0];
        await Assert.That(issue.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Message).IsEqualTo("boom");
        await Assert.That(issue.Constraint).IsEqualTo("XPDY0002");
        await Assert.That(log.Errors[0]).IsEqualTo(("NES3301", "boom", "/", true));
        await Assert.That(sink.HasDroppedError).IsFalse();
    }

    [Test]
    public async Task Add_WhenTheCollectorIsFull_RecordsTheDrop_AndStillDeliversTheEvent()
    {
        var (sink, collector, log) = Create(maxIssues: 1);

        sink.Add(Problem("first"));
        sink.Add(Problem("second"));

        await Assert.That(collector.Count).IsEqualTo(1);
        await Assert.That(sink.HasDroppedError).IsTrue();
        await Assert.That(log.Errors[1]).IsEqualTo(("NES3301", "second", "/", false));
    }

    [Test]
    public async Task Constructor_NullArguments_Throw()
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var mapper = new IssueMapper(collector, null);

        await Assert.That(() => new EvaluationProblems(null!, mapper, null)).Throws<ArgumentNullException>();
        await Assert.That(() => new EvaluationProblems(collector, null!, null)).Throws<ArgumentNullException>();
    }
}
