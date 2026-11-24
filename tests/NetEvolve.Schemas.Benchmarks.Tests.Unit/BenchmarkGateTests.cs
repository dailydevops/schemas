namespace NetEvolve.Schemas.Benchmarks.Tests.Unit;

using NetEvolve.Schemas.Benchmarks.Gate;

public class BenchmarkGateTests
{
    private const string Targets = """
        [
          { "name": "Parse.Fast", "meanNs": 100.0, "allocatedBytes": 0, "baselineRatio": 0.5 },
          { "name": "Parse.Alone", "meanNs": 200.0, "allocatedBytes": 24 }
        ]
        """;

    private static string Results(double fast, long fastBytes, double baseline, double alone, long aloneBytes) =>
        FormattableString.Invariant(
            $$"""
            {
              "Title": "fixture",
              "Benchmarks": [
                { "Type": "Parse", "Method": "FastSystemXml", "Parameters": "", "Statistics": { "Mean": {{baseline}} }, "Memory": { "BytesAllocatedPerOperation": 100 } },
                { "Type": "Parse", "Method": "Fast", "Parameters": "", "Statistics": { "Mean": {{fast}} }, "Memory": { "BytesAllocatedPerOperation": {{fastBytes}} } },
                { "Type": "Parse", "Method": "Alone", "Parameters": "", "Statistics": { "Mean": {{alone}} }, "Memory": { "BytesAllocatedPerOperation": {{aloneBytes}} } }
              ]
            }
            """
        );

    private static IReadOnlyList<GateViolation> Evaluate(string results) =>
        BenchmarkGate.Evaluate(ResultReader.Read(results), TargetReader.Read(Targets));

    [Test]
    public async Task Evaluate_EqualValues_Passes() =>
        await Assert.That(Evaluate(Results(100, 0, 200, 200, 24))).IsEmpty();

    [Test]
    public async Task Evaluate_HigherAllocation_Fails()
    {
        var violations = Evaluate(Results(100, 8, 200, 200, 24));

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(GateViolationKind.AllocationGrowth);
        await Assert.That(violations[0].Name).IsEqualTo("Parse.Fast");
    }

    [Test]
    public async Task Evaluate_RatioWithinTolerance_Passes() =>
        // 0.57 / 1 = 0.57 <= 0.5 * 1.15 = 0.575.
        await Assert.That(Evaluate(Results(114, 0, 200, 200, 24))).IsEmpty();

    [Test]
    public async Task Evaluate_RatioAboveTolerance_FailsWithRatioTargetAndLimit()
    {
        // 120 / 200 = 0.6 > 0.575 (0.5 * 1.15).
        var violations = Evaluate(Results(120, 0, 200, 200, 24));

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(GateViolationKind.WorseBaselineRatio);
        await Assert.That(violations[0].Message).IsEqualTo("ratio 0.600 > target 0.500 (limit 0.575)");
    }

    [Test]
    public async Task Evaluate_HigherAllocationWithRatioWithinTolerance_StillFails()
    {
        var violations = Evaluate(Results(114, 1, 200, 200, 24));

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(GateViolationKind.AllocationGrowth);
    }

    [Test]
    public async Task Evaluate_MoreThanFivePercentSlowerWithoutBaseline_Fails()
    {
        var violations = Evaluate(Results(100, 0, 200, 210.5, 24));

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(GateViolationKind.Regression);
        await Assert.That(violations[0].Name).IsEqualTo("Parse.Alone");
    }

    [Test]
    public async Task Evaluate_ExactlyFivePercentSlowerWithoutBaseline_Passes() =>
        await Assert.That(Evaluate(Results(100, 0, 200, 210, 24))).IsEmpty();

    [Test]
    public async Task Evaluate_BenchmarkWithoutTarget_IsReportedAsError()
    {
        var results = ResultReader.Read(Results(100, 0, 200, 200, 24));

        var violations = BenchmarkGate.Evaluate(results, []);

        await Assert.That(violations.Count).IsEqualTo(2);
        await Assert.That(violations[0].Kind).IsEqualTo(GateViolationKind.MissingTarget);
        await Assert.That(violations[1].Kind).IsEqualTo(GateViolationKind.MissingTarget);
    }

    [Test]
    public async Task Evaluate_RatioTargetWithoutBaselineResult_Fails()
    {
        var results = ResultReader.Read(
            """{ "Benchmarks": [ { "Type": "Parse", "Method": "Fast", "Parameters": "", "Statistics": { "Mean": 100 }, "Memory": { "BytesAllocatedPerOperation": 0 } } ] }"""
        );

        var violations = BenchmarkGate.Evaluate(results, TargetReader.Read(Targets));

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(GateViolationKind.MissingBaseline);
    }

    [Test]
    public async Task Run_EmptyTargetsDirectory_ReturnsErrorExitCode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Directory
            .CreateDirectory(Path.Combine(Path.GetTempPath(), "gate-" + Guid.NewGuid().ToString("N")))
            .FullName;
        var file = Path.Combine(dir, "results.json");
        await File.WriteAllTextAsync(file, Results(100, 0, 200, 200, 24), cancellationToken);
        try
        {
            using var output = new StringWriter();

            var exitCode = BenchmarkGate.Run(file, dir, output);

            await Assert.That(exitCode).IsEqualTo(1);
            await Assert.That(output.ToString()).Contains("MissingTarget");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Run_MatchingTargetFile_ReturnsZero(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Directory
            .CreateDirectory(Path.Combine(Path.GetTempPath(), "gate-" + Guid.NewGuid().ToString("N")))
            .FullName;
        var file = Path.Combine(dir, "results.json");
        await File.WriteAllTextAsync(file, Results(100, 0, 200, 200, 24), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(dir, "parse.json"), Targets, cancellationToken);
        try
        {
            using var output = new StringWriter();

            await Assert.That(BenchmarkGate.Run(file, dir, output)).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
