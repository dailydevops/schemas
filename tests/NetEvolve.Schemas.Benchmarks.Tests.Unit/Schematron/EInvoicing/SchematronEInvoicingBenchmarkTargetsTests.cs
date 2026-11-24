namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.Schematron.EInvoicing;

using NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// The EN 16931 e-invoicing benchmarks (SCH-73) and their recorded targets. The benchmark project targets only the newest
/// runtimes, so the benchmark sources are checked instead of referenced.
/// </summary>
public class SchematronEInvoicingBenchmarkTargetsTests
{
    private const string BenchmarkFolder = "benchmarks/NetEvolve.Schemas.Benchmarks/Schematron/EInvoicing";
    private const string TargetFile = "benchmarks/targets/schematron-einvoicing.json";

    private static readonly string[] Syntaxes = ["Ubl", "Cii"];

    [Test]
    public async Task Targets_CoverStreamAndMemoryOfBothSyntaxes_AndNothingElse(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expected = Syntaxes.SelectMany(s =>
            new[] { $"{s}InvoiceValidationBenchmarks.ValidateStream", $"{s}InvoiceValidationBenchmarks.ValidateMemory" }
        );

        var entries = await ReadTargets(cancellationToken);

        await Assert.That(entries.Select(t => t.Name)).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Targets_AreGatedByRatioToSystemXml(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = await ReadTargets(cancellationToken);

        foreach (var entry in entries)
        {
            await Assert.That(entry.BaselineRatio is not null).IsTrue().Because(entry.Name);
        }
    }

    [Test]
    public async Task Folder_HasBothBenchmarkClasses_WithTheBaselineMethod()
    {
        var code = string.Concat(
            Directory.GetFiles(Path.Combine(RepositoryRoot(), BenchmarkFolder), "*.cs").Select(File.ReadAllText)
        );

        foreach (var syntax in Syntaxes)
        {
            await Assert.That(code).Contains($"class {syntax}InvoiceValidationBenchmarks").Because(syntax);
        }

        await Assert.That(code).Contains("namespace NetEvolve.Schemas.Benchmarks.Schematron.EInvoicing;");
        await Assert.That(code).Contains("[Benchmark(Baseline = true)]");
        await Assert.That(code).Contains("ValidateSystemXml");
    }

    [Test]
    public async Task Folder_HoldsNoCenArtifact(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = Directory.GetFiles(Path.Combine(RepositoryRoot(), BenchmarkFolder));

        await Assert.That(files.Where(f => !f.EndsWith(".cs", StringComparison.Ordinal))).IsEmpty();
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
