namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.Regex;

using System.Text.RegularExpressions;
using NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// The regex benchmarks (DT-59) and their recorded targets. The benchmark project targets only the newest runtimes, so
/// the benchmark sources are scanned instead of referenced: a benchmark is a method with a <c>[Benchmark]</c>
/// attribute, its name is <c>Type.Method</c> as in the report, and the baseline ends with
/// <see cref="SystemXmlBaseline.MethodSuffix"/>.
/// </summary>
public class RegexBenchmarkTargetsTests
{
    private const string BenchmarkFolder = "benchmarks/NetEvolve.Schemas.Benchmarks/Regex";
    private const string TargetFile = "benchmarks/targets/regex.json";

    [Test]
    public async Task Folder_CoversCompileMatchAndStepLimit()
    {
        var names = Scan().Select(m => m.Name).ToArray();

        await Assert.That(names.Any(n => n.Contains("Compile", StringComparison.Ordinal))).IsTrue();
        await Assert.That(names.Any(n => n.Contains("Match", StringComparison.Ordinal))).IsTrue();
        await Assert.That(names.Any(n => n.Contains("StepLimit", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Folder_BenchmarkMethods_AreBaselineExactlyWhenNamedLikeOne()
    {
        var misnamed = Scan()
            .Where(m => m.IsBaseline != m.Method.EndsWith(SystemXmlBaseline.MethodSuffix, StringComparison.Ordinal))
            .Select(m => m.Name);

        await Assert.That(misnamed).IsEmpty();
    }

    [Test]
    public async Task Targets_HaveOneEntryPerBenchmark_AndNoOther(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expected = Scan().Where(m => !m.IsBaseline).Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal);

        await Assert.That(await ReadTargets(cancellationToken)).IsNotEmpty();
        await Assert
            .That((await ReadTargets(cancellationToken)).Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal))
            .IsEquivalentTo(expected);
    }

    [Test]
    public async Task Targets_MatchingACompiledPattern_AllocatesNothing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var match = (await ReadTargets(cancellationToken)).Where(t =>
            t.Name.Contains("MatchCompiled", StringComparison.Ordinal)
        );

        await Assert.That(match).IsNotEmpty();
        await Assert.That(match.Where(t => t.AllocatedBytes != 0).Select(t => t.Name)).IsEmpty();
    }

    [Test]
    public async Task Targets_WithABaseline_AreGatedByRatio_AndTheRestByMean(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scanned = Scan().Where(m => !m.IsBaseline).ToDictionary(m => m.Name, m => m.Type, StringComparer.Ordinal);
        var withBaseline = Scan().Where(m => m.IsBaseline).Select(m => m.Type).ToHashSet(StringComparer.Ordinal);
        var wrong = (await ReadTargets(cancellationToken))
            .Where(t => withBaseline.Contains(scanned[t.Name]) != t.BaselineRatio.HasValue)
            .Select(t => t.Name);

        await Assert.That(wrong).IsEmpty();
    }

    private sealed record BenchmarkMethod(string Type, string Method, bool IsBaseline)
    {
        public string Name => $"{Type}.{Method}";
    }

    private static async Task<IReadOnlyList<BenchmarkTarget>> ReadTargets(CancellationToken cancellationToken) =>
        TargetReader.Read(await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), TargetFile), cancellationToken));

    private static List<BenchmarkMethod> Scan()
    {
        var methods = new List<BenchmarkMethod>();
        var folder = Path.Combine(RepositoryRoot(), BenchmarkFolder);
        var files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories) : [];
        foreach (var file in files)
        {
            string? type = null;
            bool? baseline = null;
            foreach (var line in File.ReadLines(file).Select(l => l.Trim()))
            {
                if (line.StartsWith("[Params", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"{Path.GetFileName(file)}: parameterized benchmarks need the parameter text in the target name; extend the scan first."
                    );
                }

                if (line.StartsWith("[Benchmark", StringComparison.Ordinal))
                {
                    baseline = line.Contains("Baseline = true", StringComparison.Ordinal);
                }
                else if (Regex.Match(line, @"^(?:public|internal) (?:sealed )?class (\w+)") is { Success: true } c)
                {
                    type = c.Groups[1].Value;
                }
                else if (
                    baseline is { } isBaseline
                    && Regex.Match(line, @"^public [\w<>\[\]]+ (\w+)\(") is { Success: true } m
                )
                {
                    methods.Add(new BenchmarkMethod(type!, m.Groups[1].Value, isBaseline));
                    baseline = null;
                }
            }
        }

        return methods;
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
