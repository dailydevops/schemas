namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.Xsd.ContentModels;

using System.Text.RegularExpressions;
using NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// The content model benchmarks (XSD-65) and their recorded targets. The benchmark project targets only the newest
/// runtimes, so the benchmark sources are scanned instead of referenced.
/// </summary>
public class ContentModelBenchmarkTargetsTests
{
    private const string BenchmarkFolder = "benchmarks/NetEvolve.Schemas.Benchmarks/Xsd/ContentModels";
    private const string TargetFile = "benchmarks/targets/xsd-content-models.json";

    private static readonly string[] Expected =
    [
        "ContentModelCompileBenchmarks.CompileMaxOccurs10",
        "ContentModelCompileBenchmarks.CompileMaxOccurs10000",
        "ContentModelCompileBenchmarks.CompileMaxOccursOneBillion",
        "ContentModelMatchBenchmarks.MatchChoice",
        "ContentModelMatchBenchmarks.MatchNestedCountedGroups",
        "ContentModelMatchBenchmarks.MatchSequence",
    ];

    [Test]
    public async Task Folder_ContainsTheListedScenarios() =>
        await Assert.That(Scan().OrderBy(n => n, StringComparer.Ordinal)).IsEquivalentTo(Expected);

    [Test]
    public async Task Targets_HaveOneEntryPerBenchmark_AndNoOther(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var targets = await ReadTargets(cancellationToken);

        await Assert.That(targets.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal)).IsEquivalentTo(Expected);
    }

    [Test]
    public async Task Targets_MatchSteps_AllocateZeroBytes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var targets = await ReadTargets(cancellationToken);

        var match = targets.Where(t => t.Name.StartsWith("ContentModelMatchBenchmarks.", StringComparison.Ordinal));

        await Assert.That(match.Count()).IsEqualTo(3);
        await Assert.That(match.Where(t => t.AllocatedBytes != 0).Select(t => t.Name)).IsEmpty();
    }

    [Test]
    [Arguments(1.06, false)]
    [Arguments(1.04, true)]
    public async Task Gate_AppliesTheFivePercentLimit_ToTheRecordedTargets(
        double factor,
        bool passes,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var targets = await ReadTargets(cancellationToken);
        var results = string.Join(
            ",",
            targets.Select(t =>
            {
                var dot = t.Name.IndexOf('.', StringComparison.Ordinal);
                return FormattableString.Invariant(
                    $$"""{ "Type": "{{t.Name[..dot]}}", "Method": "{{t.Name[(dot + 1)..]}}", "Parameters": "", "Statistics": { "Mean": {{t.MeanNs * factor}} }, "Memory": { "BytesAllocatedPerOperation": {{t.AllocatedBytes}} } }"""
                );
            })
        );

        var violations = BenchmarkGate.Evaluate(ResultReader.Read($$"""{ "Benchmarks": [ {{results}} ] }"""), targets);

        await Assert.That(violations.Count == 0).IsEqualTo(passes);
    }

    private static async Task<IReadOnlyList<BenchmarkTarget>> ReadTargets(CancellationToken cancellationToken) =>
        TargetReader.Read(await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), TargetFile), cancellationToken));

    // A benchmark is a method with a [Benchmark] attribute, named Type.Method as in the report.
    private static List<string> Scan()
    {
        var names = new List<string>();
        var folder = Path.Combine(RepositoryRoot(), BenchmarkFolder);
        foreach (var file in Directory.Exists(folder) ? Directory.GetFiles(folder, "*.cs") : [])
        {
            var code = File.ReadAllText(file);
            var type = ClassRegex.Match(code).Groups[1].Value;
            names.AddRange(MethodRegex.Matches(code).Select(m => $"{type}.{m.Groups[1].Value}"));
        }

        return names;
    }

    private static readonly Regex ClassRegex = new(@"public class (\w+)", RegexOptions.None, TimeSpan.FromSeconds(5));
    private static readonly Regex MethodRegex = new(
        @"\[Benchmark[^\]]*\]\s*public \S+ (\w+)\(",
        RegexOptions.None,
        TimeSpan.FromSeconds(5)
    );

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
