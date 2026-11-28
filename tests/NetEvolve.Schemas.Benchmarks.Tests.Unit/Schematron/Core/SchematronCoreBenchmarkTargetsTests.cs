namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.Schematron.Core;

using NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// The Schematron core benchmarks (SCH-70) and their recorded targets. The benchmark project targets only the newest
/// runtimes, so the benchmark sources are checked instead of referenced.
/// </summary>
public class SchematronCoreBenchmarkTargetsTests
{
    private const string BenchmarkFolder = "benchmarks/NetEvolve.Schemas.Benchmarks/Schematron/Core";
    private const string TargetFile = "benchmarks/targets/schematron-core.json";

    private static readonly string[] Sizes = ["Small", "Medium", "Large"];

    [Test]
    public async Task Targets_CoverCompileValidateAndSvrlWrite_AndNothingElse(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string[] expected =
        [
            .. Sizes.Select(s => $"SchemaCompilationBenchmarks.Compile{s}"),
            .. Sizes.SelectMany(s =>
                new[]
                {
                    $"{s}RuleSetValidationBenchmarks.ValidateStream",
                    $"{s}RuleSetValidationBenchmarks.ValidateMemory",
                }
            ),
            "SvrlWriteBenchmarks.WriteSvrl",
            "LetRuleSetBenchmarks.ValidateMemory",
        ];

        var entries = await ReadTargets(cancellationToken);

        await Assert.That(entries.Select(t => t.Name)).IsEquivalentTo(expected);
    }

    [Test]
    public async Task ValidateTargets_AreGatedByRatioToSystemXml_OthersByTheRecordedMean(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = await ReadTargets(cancellationToken);

        foreach (var entry in entries)
        {
            var hasBaseline = entry.Name.Contains("Validation", StringComparison.Ordinal);

            await Assert.That(entry.BaselineRatio is not null).IsEqualTo(hasBaseline).Because(entry.Name);
        }
    }

    [Test]
    public async Task Folder_HasEveryBenchmarkClass_WithTheBaselineMethod()
    {
        var code = string.Concat(
            Directory.GetFiles(Path.Combine(RepositoryRoot(), BenchmarkFolder), "*.cs").Select(File.ReadAllText)
        );

        foreach (
            var type in new[]
            {
                "SchemaCompilationBenchmarks",
                "SvrlWriteBenchmarks",
                "LetRuleSetBenchmarks",
                "SmallRuleSetValidationBenchmarks",
                "MediumRuleSetValidationBenchmarks",
                "LargeRuleSetValidationBenchmarks",
            }
        )
        {
            await Assert.That(code).Contains($"class {type}").Because(type);
        }

        await Assert.That(code).Contains("[Benchmark(Baseline = true)]");
        await Assert.That(code).Contains("ValidateSystemXml");
    }

    private static async Task<IReadOnlyList<BenchmarkTarget>> ReadTargets(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return TargetReader.Read(
            await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), TargetFile), cancellationToken)
        );
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root (Schemas.slnx) not found.");
    }
}
