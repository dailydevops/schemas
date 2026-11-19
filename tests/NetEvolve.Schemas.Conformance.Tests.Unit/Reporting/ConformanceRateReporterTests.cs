namespace NetEvolve.Schemas.Conformance.Tests.Unit.Reporting;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

public class ConformanceRateReporterTests
{
    private const string Header =
        "| Package | Target framework | Test sets | Applicable | Passed | Baselined | Regressions | Skipped | Rate |\n"
        + "| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |\n";

    private const string DetailHeader =
        "| Test set | Applicable | Passed | Baselined | Regressions | Skipped | Rate |\n"
        + "| --- | ---: | ---: | ---: | ---: | ---: | ---: |\n";

    private const string Footnote =
        "\n_Rate = passed / (passed + failed). Skipped cases are excluded from the denominator. The rate is truncated, never rounded up, to one decimal."
        + " Baselined = failed cases recorded in the committed known-failure baseline (known gaps, not regressions)."
        + " Regressions = failed cases outside the baseline; they fail the run._\n\n";

    private static string Details(string package, string targetFramework, int sets, string rows) =>
        $"\n<details>\n<summary>{package} {targetFramework}: {sets} test sets</summary>\n\n"
        + DetailHeader
        + rows
        + "\n</details>\n";

    // Failed cases b0..b(n-1) are baselined, r0..r(n-1) are regressions; ids stay below ten so the baseline is sorted.
    private static ConformanceReport Report(string set, int passed, int regressions, int skipped, int baselined = 0) =>
        Report((set, passed, regressions, skipped, baselined));

    private static ConformanceReport Report(
        params (string Set, int Passed, int Regressions, int Skipped, int Baselined)[] sets
    )
    {
        var results = new List<CaseResult>();
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        foreach (var (set, passed, regressions, skipped, baselined) in sets)
        {
            for (var i = 0; i < passed; i++)
            {
                results.Add(new CaseResult($"p{i}", set, CaseOutcome.Pass, null));
            }

            for (var i = 0; i < baselined; i++)
            {
                results.Add(new CaseResult($"b{i}", set, CaseOutcome.Fail, "x"));
            }

            for (var i = 0; i < regressions; i++)
            {
                results.Add(new CaseResult($"r{i}", set, CaseOutcome.Fail, "x"));
            }

            for (var i = 0; i < skipped; i++)
            {
                results.Add(new CaseResult($"s{i}", set, CaseOutcome.Skipped, "x"));
            }

            baselines[set] = KnownFailureBaseline.Parse(
                string.Concat(Enumerable.Range(0, baselined).Select(i => $"b{i}\n"))
            );
        }

        return ConformanceReport.Compare(results, baselines);
    }

    private static ConformanceReport Empty() =>
        ConformanceReport.Compare([], new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal));

    private static int CountOf(string text, string value)
    {
        var count = 0;
        for (
            var i = text.IndexOf(value, StringComparison.Ordinal);
            i >= 0;
            i = text.IndexOf(value, i + 1, StringComparison.Ordinal)
        )
        {
            count++;
        }

        return count;
    }

    [Test]
    public async Task Render_FixedCounts_ProducesOneTableAndOneCollapsedDetailBlock()
    {
        var text = ConformanceRateReporter.Render("NetEvolve.Schemas.Schematron", "net10.0", Report("b-set", 9, 1, 2));

        await Assert
            .That(text)
            .IsEqualTo(
                Header
                    + "| NetEvolve.Schemas.Schematron | net10.0 | 1 | 10 | 9 | 0 | 1 | 2 | 90.0% |\n"
                    + Details("NetEvolve.Schemas.Schematron", "net10.0", 1, "| b-set | 10 | 9 | 0 | 1 | 2 | 90.0% |\n")
                    + Footnote
            );
    }

    [Test]
    public async Task Render_ManyTestSets_ProduceOneHeaderAndOneFootnote()
    {
        var sets = new (string, int, int, int, int)[50];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = ($"set{i:D2}", 1, 0, 0, 1);
        }

        var text = ConformanceRateReporter.Render("P", "net10.0", Report(sets));

        await Assert.That(CountOf(text, "| Package |")).IsEqualTo(1);
        await Assert.That(CountOf(text, "| Test set |")).IsEqualTo(1);
        await Assert.That(CountOf(text, "<details>")).IsEqualTo(1);
        await Assert.That(CountOf(text, "</details>")).IsEqualTo(1);
        await Assert.That(CountOf(text, "_Rate = passed")).IsEqualTo(1);
        await Assert.That(text).Contains("| P | net10.0 | 50 | 100 | 50 | 50 | 0 | 0 | 50.0% |\n");
    }

    [Test]
    public async Task Render_Aggregation_SumsAllTestSetsIntoTheTopRow()
    {
        var text = ConformanceRateReporter.Render(
            "P",
            "net10.0",
            Report(("a", 6, 0, 1, 2), ("b", 1, 1, 0, 0), ("c", 0, 0, 5, 0))
        );

        // applicable = (6 + 2) + (1 + 1) + 0, passed = 7, baselined = 2, regressions = 1, skipped = 6, rate = 7 / 10
        await Assert.That(text).Contains("| P | net10.0 | 3 | 10 | 7 | 2 | 1 | 6 | 70.0% |\n");
    }

    [Test]
    public async Task Render_Detail_SortsLowestRateFirstThenMostBaselinedThenName()
    {
        var text = ConformanceRateReporter.Render(
            "P",
            "net10.0",
            Report(
                ("e-full", 4, 0, 0, 0),
                ("d-lowest", 1, 0, 0, 3),
                ("c-na", 0, 0, 2, 0),
                ("b-half-few", 1, 0, 0, 1),
                ("a-half-many", 2, 0, 0, 2)
            )
        );

        var rows = text[(text.IndexOf(DetailHeader, StringComparison.Ordinal) + DetailHeader.Length)..];
        await Assert
            .That(rows)
            .StartsWith(
                "| d-lowest | 4 | 1 | 3 | 0 | 0 | 25.0% |\n"
                    + "| a-half-many | 4 | 2 | 2 | 0 | 0 | 50.0% |\n"
                    + "| b-half-few | 2 | 1 | 1 | 0 | 0 | 50.0% |\n"
                    + "| e-full | 4 | 4 | 0 | 0 | 0 | 100.0% |\n"
                    + "| c-na | 0 | 0 | 0 | 0 | 2 | n/a |\n"
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

        await Assert.That(text).Contains("| P | net10.0 | 1 | 2 | 1 | 0 | 1 | 100 | 50.0% |");
    }

    [Test]
    public async Task Render_BaselinedFailures_AreNotCountedAsRegressions()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 0, 0, 0, 3));

        await Assert.That(text).Contains("| P | net10.0 | 1 | 3 | 0 | 3 | 0 | 0 | 0.0% |");
    }

    [Test]
    public async Task Render_NoApplicableCases_ProducesNotApplicableRate()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 0, 0, 4));

        await Assert.That(text).Contains("| P | net10.0 | 1 | 0 | 0 | 0 | 0 | 4 | n/a |");
        await Assert.That(text).Contains("| s | 0 | 0 | 0 | 0 | 4 | n/a |");
    }

    [Test]
    public async Task Render_EmptyReport_ProducesOneRowWithoutDetailBlock()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Empty());

        await Assert.That(text).IsEqualTo(Header + "| P | net10.0 | 0 | 0 | 0 | 0 | 0 | 0 | n/a |\n" + Footnote);
    }

    [Test]
    public async Task Render_PipeInPackageName_IsEscaped()
    {
        var text = ConformanceRateReporter.Render("A|B", "net10.0", Report("s", 1, 0, 0));

        await Assert.That(text).Contains("| A&#124;B | net10.0 |");
    }

    [Test]
    public async Task Render_MarkupInTestSetName_IsEscaped()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("<b>&|", 1, 0, 0));

        await Assert.That(text).Contains("| &lt;b>&amp;&#124; | 1 | 1 | 0 | 0 | 0 | 100.0% |");
    }

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
    [NotInParallel]
    public async Task Flush_CollectedTestSets_WriteOneTablePerRunSortedByPackageAndFramework(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            ConformanceRateReporter.Collect("P", "net11.0", Report("s1", 1, 0, 0));
            ConformanceRateReporter.Collect("P", "net10.0", Report("s2", 1, 1, 0));
            ConformanceRateReporter.Collect("P", "net11.0", Report("s3", 3, 0, 0, 1));

            ConformanceRateReporter.Flush(path);

            var text = await File.ReadAllTextAsync(path, cancellationToken: cancellationToken);
            var net10 = text.IndexOf("| P | net10.0 | 1 | 2 | 1 | 0 | 1 | 0 | 50.0% |\n", StringComparison.Ordinal);
            var net11 = text.IndexOf("| P | net11.0 | 2 | 5 | 4 | 1 | 0 | 0 | 80.0% |\n", StringComparison.Ordinal);
            await Assert.That(net10).IsGreaterThan(-1);
            await Assert.That(net11).IsGreaterThan(net10);
            await Assert.That(CountOf(text, "| Package |")).IsEqualTo(1);
            await Assert.That(CountOf(text, "<details>")).IsEqualTo(2);
            await Assert.That(CountOf(text, "_Rate = passed")).IsEqualTo(1);
        }
        finally
        {
            ConformanceRateReporter.Flush(null);
            File.Delete(path);
        }
    }

    [Test]
    [NotInParallel]
    public async Task Flush_SecondCall_WritesNothingBecauseTheBufferIsEmpty(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            ConformanceRateReporter.Collect("P", "net10.0", Report("s", 1, 0, 0));
            ConformanceRateReporter.Flush(path);
            var first = await File.ReadAllTextAsync(path, cancellationToken: cancellationToken);

            ConformanceRateReporter.Flush(path);

            await Assert.That(first).IsNotEmpty();
            await Assert.That(await File.ReadAllTextAsync(path, cancellationToken: cancellationToken)).IsEqualTo(first);
        }
        finally
        {
            ConformanceRateReporter.Flush(null);
            File.Delete(path);
        }
    }

    [Test]
    [NotInParallel]
    public async Task Flush_WithoutSummaryPath_IsNoOpAndDropsTheBuffer(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            ConformanceRateReporter.Collect("P", "net10.0", Report("s", 1, 0, 0));
            ConformanceRateReporter.Flush(null);
            ConformanceRateReporter.Flush("");

            ConformanceRateReporter.Flush(path);

            await Assert.That(File.Exists(path)).IsFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    [NotInParallel]
    public async Task Flush_WithoutEnvironmentVariable_IsNoOp()
    {
        var previous = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
        try
        {
            ConformanceRateReporter.Collect("P", "net10.0", Report("s", 1, 0, 0));

            await Assert.That(() => ConformanceRateReporter.Flush()).ThrowsNothing();
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", previous);
        }
    }
}
