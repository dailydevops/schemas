namespace NetEvolve.Schemas.Conformance.Tests.Unit.Reporting;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

public class ConformanceRateReporterTests
{
    private const string Header =
        "| Package | Target framework | Test set | Applicable | Passed | Failed | Skipped | Rate |\n"
        + "| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |\n";

    private const string Footnote =
        "\n_Rate = passed / (passed + failed). Skipped cases are excluded from the denominator. The rate is truncated, never rounded up, to one decimal._\n\n";

    private static ConformanceReport Report(string set, int passed, int failed, int skipped)
    {
        var results = new List<CaseResult>();
        for (var i = 0; i < passed; i++)
        {
            results.Add(new CaseResult($"p{i}", set, CaseOutcome.Pass, null));
        }

        for (var i = 0; i < failed; i++)
        {
            results.Add(new CaseResult($"f{i}", set, CaseOutcome.Fail, "x"));
        }

        for (var i = 0; i < skipped; i++)
        {
            results.Add(new CaseResult($"s{i}", set, CaseOutcome.Skipped, "x"));
        }

        return ConformanceReport.Compare(
            results,
            new Dictionary<string, Baseline.KnownFailureBaseline>(StringComparer.Ordinal)
        );
    }

    [Test]
    public async Task Render_FixedCounts_ProducesExpectedTable()
    {
        var text = ConformanceRateReporter.Render("NetEvolve.Schemas.Schematron", "net10.0", Report("b-set", 9, 1, 2));

        await Assert
            .That(text)
            .IsEqualTo(
                Header + "| NetEvolve.Schemas.Schematron | net10.0 | b-set | 10 | 9 | 1 | 2 | 90.0% |\n" + Footnote
            );
    }

    [Test]
    public async Task Render_NearlyAllPassed_TruncatesInsteadOfRounding()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 9999, 1, 0));

        await Assert.That(text).Contains("| 99.9% |");
        await Assert.That(text).DoesNotContain("100.0%");
    }

    [Test]
    public async Task Render_SkippedCases_AreExcludedFromDenominator()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 1, 1, 100));

        await Assert.That(text).Contains("| 2 | 1 | 1 | 100 | 50.0% |");
    }

    [Test]
    public async Task Render_NoSuites_ProducesNoSuitesRow()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Empty());

        await Assert.That(text).IsEqualTo(Header + "| P | net10.0 | _no suites_ | 0 | 0 | 0 | 0 | n/a |\n" + Footnote);
    }

    private static ConformanceReport Empty() =>
        ConformanceReport.Compare([], new Dictionary<string, Baseline.KnownFailureBaseline>(StringComparer.Ordinal));

    [Test]
    public async Task Append_WithoutSummaryPath_IsNoOp()
    {
        var report = Report("s", 1, 0, 0);

        await Assert.That(() => ConformanceRateReporter.Append("P", "net10.0", report, null)).ThrowsNothing();
        await Assert.That(() => ConformanceRateReporter.Append("P", "net10.0", report, "")).ThrowsNothing();
    }

    [Test]
    [NotInParallel]
    public async Task Append_WithoutEnvironmentVariable_IsNoOp()
    {
        var previous = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
        try
        {
            var report = Report("s", 1, 0, 0);

            await Assert.That(() => ConformanceRateReporter.Append("P", "net10.0", report)).ThrowsNothing();
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", previous);
        }
    }

    [Test]
    [NotInParallel]
    public async Task Append_WithEnvironmentVariable_AppendsToFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var previous = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", path);
        try
        {
            ConformanceRateReporter.Append("P", "net10.0", Report("s", 1, 0, 0));

            await Assert
                .That(await File.ReadAllTextAsync(path, cancellationToken: cancellationToken))
                .IsEqualTo(ConformanceRateReporter.Render("P", "net10.0", Report("s", 1, 0, 0)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", previous);
            File.Delete(path);
        }
    }

    [Test]
    public async Task Append_TwoFrameworks_ProduceSeparateRows(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            ConformanceRateReporter.Append("P", "net10.0", Report("s", 1, 0, 0), path);
            ConformanceRateReporter.Append("P", "net11.0", Report("s", 1, 1, 0), path);

            var text = await File.ReadAllTextAsync(path, cancellationToken: cancellationToken);
            await Assert.That(text).Contains("| P | net10.0 | s | 1 | 1 | 0 | 0 | 100.0% |");
            await Assert.That(text).Contains("| P | net11.0 | s | 2 | 1 | 1 | 0 | 50.0% |");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Render_PipeInPackageName_IsEscaped()
    {
        var text = ConformanceRateReporter.Render("A|B", "net10.0", Report("s", 1, 0, 0));

        await Assert.That(text).Contains("| A&#124;B | net10.0 |");
    }
}
