namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

public class Qt3Xp20RateTests
{
    [Test]
    public async Task Render_ShowsPassedFailedSkippedExcludedApplicableAndTruncatedRate()
    {
        var table = Qt3Xp20Rate.Render(new TestSetCounts(Passed: 9995, Failed: 5, Skipped: 7), excluded: 3);

        await Assert
            .That(table)
            .IsEqualTo(
                "| Suite | Passed | Failed | Skipped | Excluded | Applicable | Rate |\n"
                    + "| --- | ---: | ---: | ---: | ---: | ---: | ---: |\n"
                    + "| XPath 2.0 (QT3) | 9995 | 5 | 7 | 3 | 10000 | 99.9% |\n\n"
            );
    }

    [Test]
    public async Task Render_WithoutApplicableCases_ShowsNotAvailable()
    {
        var table = Qt3Xp20Rate.Render(new TestSetCounts(0, 0, 2), excluded: 0);

        await Assert.That(table).Contains("| 0 | n/a |");
    }

    [Test]
    [Arguments(10, 2, 1, 3, 16, true)]
    [Arguments(10, 2, 1, 3, 17, false)]
    [Arguments(10, 2, 1, 3, 15, false)]
    public async Task IsConsistent_ComparesApplicablePlusSkippedWithTotalMinusExcluded(
        int passed,
        int failed,
        int skipped,
        int excluded,
        int totalCases,
        bool expected
    ) =>
        await Assert
            .That(Qt3Xp20Rate.IsConsistent(new TestSetCounts(passed, failed, skipped), excluded, totalCases))
            .IsEqualTo(expected);

    [Test]
    public async Task Flush_AppendsOneRowAndSkipsEmptyCounts(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".md");
        try
        {
            var counts = new TestSetCounts(4, 1, 2);

            Qt3Xp20Rate.Flush(path, counts, excluded: 4);

            await Assert
                .That(await File.ReadAllTextAsync(path, cancellationToken))
                .Contains("| XPath 2.0 (QT3) | 4 | 1 | 2 | 4 | 5 | 80.0% |");
            Qt3Xp20Rate.Flush(path, default, excluded: 4);
            await Assert
                .That((await File.ReadAllTextAsync(path, cancellationToken)).Split("XPath 2.0 (QT3)").Length)
                .IsEqualTo(2);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
