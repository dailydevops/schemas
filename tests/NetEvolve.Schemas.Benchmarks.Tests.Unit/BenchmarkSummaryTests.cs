namespace NetEvolve.Schemas.Benchmarks.Tests.Unit;

using NetEvolve.Schemas.Benchmarks.Gate;

public class BenchmarkSummaryTests
{
    private const string Targets = """
        [
          { "name": "Parse.Fast", "meanNs": 100.0, "allocatedBytes": 0, "baselineRatio": 0.5 },
          { "name": "Solo.Alone", "meanNs": 200.0, "allocatedBytes": 24 }
        ]
        """;

    private static string Results(double fast, long fastBytes, string parameters = "") =>
        FormattableString.Invariant(
            $$"""
            {
              "Benchmarks": [
                { "Type": "Parse", "Method": "FastSystemXml", "Parameters": "", "Statistics": { "Mean": 200 }, "Memory": { "BytesAllocatedPerOperation": 100 } },
                { "Type": "Parse", "Method": "Fast", "Parameters": "", "Statistics": { "Mean": {{fast}} }, "Memory": { "BytesAllocatedPerOperation": {{fastBytes}} } },
                { "Type": "Solo", "Method": "Alone", "Parameters": "{{parameters}}", "Statistics": { "Mean": 200 }, "Memory": { "BytesAllocatedPerOperation": 24 } }
              ]
            }
            """
        );

    private static string Render(string results, string targets = Targets) =>
        BenchmarkSummary.Render(ResultReader.Read(results, allowMissingMeasurement: true), TargetReader.Read(targets));

    [Test]
    public async Task Render_Always_WritesHeaderAndAllRows()
    {
        var lines = Render(Results(100, 0)).TrimEnd().Split('\n');

        await Assert.That(lines.Length).IsEqualTo(5);
        await Assert
            .That(lines[0].TrimEnd())
            .IsEqualTo(
                "| Benchmark | Mean (ns) | Target mean | delta % | Allocated (B) | Target allocated | delta % | Ratio to System.Xml | Target ratio | Status |"
            );
    }

    [Test]
    public async Task Render_RowWithBaselineRatio_ShowsRatioAndTarget() =>
        await Assert
            .That(Render(Results(100, 0)))
            .Contains("| Parse.Fast | 100 | 100 | +0.0% | 0 | 0 | +0.0% | 0.50 | 0.50 | ok |");

    [Test]
    public async Task Render_RowWithoutBaseline_ShowsDashForRatio() =>
        await Assert
            .That(Render(Results(100, 0)))
            .Contains("| Solo.Alone | 200 | 200 | +0.0% | 24 | 24 | +0.0% | - | - | ok |");

    [Test]
    public async Task Render_RowWithoutTarget_ShowsNoTarget() =>
        await Assert
            .That(Render(Results(100, 0), "[]"))
            .Contains("| Solo.Alone | 200 | - | - | 24 | - | - | - | - | no target |");

    [Test]
    public async Task Render_BaselineRow_IsMarkedBaseline() =>
        await Assert
            .That(Render(Results(100, 0)))
            .Contains("| Parse.FastSystemXml | 200 | - | - | 100 | - | - | 1.00 | - | baseline |");

    [Test]
    public async Task Render_AllocationAboveTarget_IsViolation() =>
        await Assert.That(Render(Results(100, 8))).Contains("| 8 | 0 | - | 0.50 | 0.50 | violation |");

    [Test]
    public async Task Render_RatioWithinTolerance_IsWithinTolerance() =>
        // 114 / 200 = 0.57 <= 0.5 * 1.15 = 0.575.
        await Assert
            .That(Render(Results(114, 0)))
            .Contains("| Parse.Fast | 114 | 100 | +14.0% | 0 | 0 | +0.0% | 0.57 | 0.50 | within tolerance |");

    [Test]
    public async Task Render_RatioAboveTolerance_IsViolation() =>
        await Assert.That(Render(Results(120, 0))).Contains("| 0.60 | 0.50 | violation |");

    [Test]
    public async Task Render_PipeInName_IsEscaped() =>
        await Assert.That(Render(Results(100, 0, "a|b"))).Contains(@"| Solo.Alone(a\|b) |");

    [Test]
    public async Task Render_BenchmarkWithoutStatistics_ShowsNotAvailable()
    {
        var json = Results(100, 0).Replace("{ \"Mean\": 100 }", "null", StringComparison.Ordinal);

        await Assert.That(Render(json)).Contains("| Parse.Fast | n/a | 100 | - | n/a | 0 | - | - | 0.50 | n/a |");
    }

    [Test]
    public async Task Render_BaselineWithoutMeasurementAfterMeasuredOne_KeepsRatio()
    {
        var failed = Results(100, 0).Replace("{ \"Mean\": 200 }", "null", StringComparison.Ordinal);

        await Assert
            .That(
                BenchmarkSummary.Render(
                    [
                        .. ResultReader.Read(Results(100, 0)),
                        .. ResultReader.Read(failed, allowMissingMeasurement: true),
                    ],
                    TargetReader.Read(Targets)
                )
            )
            .Contains("| Parse.Fast | 100 | 100 | +0.0% | 0 | 0 | +0.0% | 0.50 | 0.50 | ok |");
    }

    [Test]
    public async Task Run_ReportDirectory_WritesRowsOfAllReports(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Directory
            .CreateDirectory(Path.Combine(Path.GetTempPath(), "summary-" + Guid.NewGuid().ToString("N")))
            .FullName;
        var targets = Directory.CreateDirectory(Path.Combine(dir, "targets")).FullName;
        await File.WriteAllTextAsync(Path.Combine(dir, "A-report-full.json"), Results(100, 0), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(targets, "parse.json"), Targets, cancellationToken);
        try
        {
            using var output = new StringWriter();

            await Assert.That(BenchmarkSummary.Run(dir, targets, output)).IsEqualTo(0);
            await Assert.That(output.ToString()).Contains("| Parse.Fast |");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
