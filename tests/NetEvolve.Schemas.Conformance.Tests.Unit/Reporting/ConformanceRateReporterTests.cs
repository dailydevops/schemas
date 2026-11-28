namespace NetEvolve.Schemas.Conformance.Tests.Unit.Reporting;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

public class ConformanceRateReporterTests
{
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

    private static ConformanceReport Failing(string set, string id, string? reason) =>
        ConformanceReport.Compare(
            [new CaseResult(id, set, CaseOutcome.Fail, reason)],
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );

    [Test]
    public async Task Render_RunWithoutFailure_ProducesOneLine()
    {
        var text = ConformanceRateReporter.Render(
            "NetEvolve.Schemas.Schematron",
            "net10.0",
            Report("b-set", 9, 0, 2, 1)
        );

        await Assert
            .That(text)
            .IsEqualTo(
                "- :white_check_mark: **NetEvolve.Schemas.Schematron** (net10.0): 9 of 10 passed (90.0%), 1 pending, 2 skipped\n"
            );
    }

    [Test]
    public async Task Render_OnlyPassedCases_LeavesOutTheCountsOfZero()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 3, 0, 0));

        await Assert.That(text).IsEqualTo("- :white_check_mark: **P** (net10.0): 3 of 3 passed (100.0%)\n");
    }

    [Test]
    public async Task Render_ManyTestSets_ProduceOneLineWithTheSums()
    {
        var sets = new (string, int, int, int, int)[50];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = ($"set{i:D2}", 1, 0, 0, 1);
        }

        var text = ConformanceRateReporter.Render("P", "net10.0", Report(sets));

        await Assert
            .That(text)
            .IsEqualTo("- :white_check_mark: **P** (net10.0): 50 of 100 passed (50.0%), 50 pending\n");
    }

    [Test]
    public async Task Render_Regression_IsListedWithItsTestSetAndReason()
    {
        var text = ConformanceRateReporter.Render(
            "P",
            "net10.0",
            Report(("a", 6, 0, 1, 2), ("b", 1, 1, 0, 0), ("c", 0, 0, 5, 0))
        );

        // applicable = (6 + 2) + (1 + 1) + 0, passed = 7, pending = 2, failed = 1, skipped = 6, rate = 7 / 10
        await Assert
            .That(text)
            .IsEqualTo(
                "- :x: **P** (net10.0): 7 of 10 passed (70.0%), 1 failed, 2 pending, 6 skipped\n"
                    + "  - `b` `r0`: x\n"
            );
    }

    [Test]
    public async Task Render_UnexpectedPass_IsListedAndMarksTheLineAsFailed()
    {
        var report = ConformanceReport.Compare(
            [new CaseResult("b0", "s", CaseOutcome.Pass)],
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                ["s"] = KnownFailureBaseline.Parse("b0\n"),
            }
        );

        var text = ConformanceRateReporter.Render("P", "net10.0", report);

        await Assert
            .That(text)
            .IsEqualTo(
                "- :x: **P** (net10.0): 1 of 1 passed (100.0%), 1 unexpectedly passed\n"
                    + "  - `s` `b0`: passes, but the baseline lists it as a known failure\n"
            );
    }

    [Test]
    public async Task Render_WithoutListedCases_ShowsTheCountsOnly()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 9, 1, 0), listCases: false);

        await Assert.That(text).IsEqualTo("- :x: **P** (net10.0): 9 of 10 passed (90.0%), 1 failed\n");
    }

    [Test]
    public async Task Render_MoreFailedCasesThanTheLimit_ListsFiftyAndCountsTheRest()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 0, 60, 0));

        await Assert.That(CountOf(text, "  - `s` `r")).IsEqualTo(50);
        await Assert.That(text).EndsWith("  - and 10 more\n");
    }

    [Test]
    public async Task Render_CaseIdThatStartsWithItsTestSet_DoesNotRepeatTheTestSet()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Failing("set", "set/case", "boom"));

        await Assert.That(text).EndsWith("\n  - `set/case`: boom\n");
    }

    [Test]
    public async Task Render_ReasonWithSeveralLinesAndMarkup_ShowsTheEscapedFirstLine()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Failing("s", "c", " first <b> *x*\r\nsecond"));

        await Assert.That(text).EndsWith("\n  - `s` `c`: first &lt;b> \\*x\\*\n");
    }

    [Test]
    public async Task Render_LongReason_IsCut()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Failing("s", "c", new string('a', 400)));

        await Assert.That(text).EndsWith("`: " + new string('a', 300) + "...\n");
    }

    [Test]
    public async Task Render_RegressionWithoutReason_SaysThatTheCaseFails()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Failing("s", "c", null));

        await Assert.That(text).EndsWith("\n  - `s` `c`: fails and is not a known failure\n");
    }

    [Test]
    public async Task Render_NearlyAllPassed_TruncatesInsteadOfRounding()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 9999, 0, 0, 1));

        await Assert.That(text).Contains("(99.9%)");
        await Assert.That(text).DoesNotContain("100.0%");
    }

    [Test]
    public async Task Render_SkippedCases_AreExcludedFromDenominator()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 1, 0, 100, 1));

        await Assert
            .That(text)
            .IsEqualTo("- :white_check_mark: **P** (net10.0): 1 of 2 passed (50.0%), 1 pending, 100 skipped\n");
    }

    [Test]
    public async Task Render_NoApplicableCases_ShowsNoRate()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Report("s", 0, 0, 4));

        await Assert.That(text).IsEqualTo("- :white_check_mark: **P** (net10.0): no applicable cases, 4 skipped\n");
    }

    [Test]
    public async Task Render_EmptyReport_ProducesOneLine()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Empty());

        await Assert.That(text).IsEqualTo("- :white_check_mark: **P** (net10.0): no applicable cases\n");
    }

    [Test]
    public async Task Render_MarkupInPackageName_IsEscaped()
    {
        var text = ConformanceRateReporter.Render("A<B&*", "net10.0", Report("s", 1, 0, 0));

        await Assert.That(text).StartsWith("- :white_check_mark: **A&lt;B&amp;\\*** (net10.0): ");
    }

    [Test]
    public async Task Render_BacktickInCaseId_DoesNotEndTheCodeSpan()
    {
        var text = ConformanceRateReporter.Render("P", "net10.0", Failing("s", "a`b", "boom"));

        await Assert.That(text).EndsWith("\n  - `s` `a'b`: boom\n");
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
    public async Task Flush_CollectedTestSets_WriteOneLinePerPackageAndFrameworkInSortedOrder(
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

            await Assert
                .That(await File.ReadAllTextAsync(path, cancellationToken: cancellationToken))
                .IsEqualTo(
                    "- :x: **P** (net10.0): 1 of 2 passed (50.0%), 1 failed\n"
                        + "  - `s2` `r0`: x\n"
                        + "- :white_check_mark: **P** (net11.0): 4 of 5 passed (80.0%), 1 pending\n"
                );
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
