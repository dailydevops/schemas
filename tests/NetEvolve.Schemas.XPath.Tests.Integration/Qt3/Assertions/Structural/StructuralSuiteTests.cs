namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

[Explicit]
[Category(ConformanceCategory.Name)]
public class StructuralSuiteTests
{
    private static readonly string[] Kinds = ["assert-deep-eq", "assert-permutation", "assert-type", "assert-xml"];

    /// <summary>Runs every structural case of the selection: none stays skipped, and the report goes to the test output and the job summary.</summary>
    [Test]
    public async Task Suite_EveryStructuralCaseMovedOutOfTheSkippedBucket(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var engine = await Qt3Engine.GetSharedAsync(cancellationToken);
        var cases = new List<Qt3Case>();
        foreach (var testCase in selection.Selected)
        {
            if (Array.IndexOf(Kinds, testCase.Assertion.Kind) >= 0)
            {
                cases.Add(testCase);
            }
        }

        var movement = StructuralMovement.Count(cases, Qt3Runner.Run(cases, engine));
        var report = StructuralMovement.Render(movement);
        await TestContext.Current!.OutputWriter.WriteLineAsync(report.AsMemory(), cancellationToken);
        var summary = Environment.GetEnvironmentVariable(ConformanceRateReporter.SummaryVariable);
        if (!string.IsNullOrEmpty(summary))
        {
            await File.AppendAllTextAsync(summary, "### Structural QT3 assertions\n\n" + report, cancellationToken);
        }

        var skipped = 0;
        var moved = 0;
        foreach (var set in movement)
        {
            skipped += set.Skipped;
            moved += set.Moved;
        }

        await Assert.That(skipped).IsEqualTo(0);
        await Assert.That(moved).IsEqualTo(cases.Count);
        await Assert.That(moved).IsGreaterThan(0);
    }
}
