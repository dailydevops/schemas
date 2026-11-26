namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.Xsd.V11;

using NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// The XSD 1.1 benchmarks (XSD-80) and their recorded targets. System.Xml.Schema has no XSD 1.1 support, so no benchmark of this
/// folder has a baseline and every target gates the recorded mean and the allocation. The benchmark sources are scanned, as the
/// benchmark project targets only the newest runtimes: a benchmark is a method with a <c>[Benchmark]</c> attribute and its
/// name is <c>Type.Method</c> as in the report.
/// </summary>
public class XsdV11BenchmarkTargetsTests
{
    private const string BenchmarkFolder = "benchmarks/NetEvolve.Schemas.Benchmarks/Xsd/V11";
    private const string TargetFile = "benchmarks/targets/xsd-v11.json";

    [Test]
    public async Task Folder_ContainsBenchmarks()
    {
        var names = Scan();

        await Assert.That(names).IsNotEmpty();
    }

    [Test]
    public async Task Targets_HaveOneEntryPerBenchmark_AndNoOther(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = await ReadTargets(cancellationToken);

        await Assert
            .That(entries.Select(t => t.Name).OrderBy(name => name, StringComparer.Ordinal))
            .IsEquivalentTo(Scan().OrderBy(name => name, StringComparer.Ordinal));
    }

    [Test]
    public async Task Targets_HaveNoBaselineRatio_BecauseSystemXmlHasNoXsd11(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = await ReadTargets(cancellationToken);

        await Assert.That(entries).IsNotEmpty();
        await Assert.That(entries.Where(t => t.BaselineRatio is not null).Select(t => t.Name)).IsEmpty();
    }

    [Test]
    public async Task Scenarios_CoverAssertionsAndTheVersionComparison()
    {
        var names = Scan();

        foreach (var kind in new[] { "Stream", "Span" })
        {
            await Assert.That(names).Contains($"AssertionDocument1000Benchmarks.Validate{kind}");
            await Assert.That(names).Contains($"AssertionDocument10000Benchmarks.Validate{kind}");
        }

        await Assert.That(names).Contains("AssertionSubtree100Benchmarks.ValidateStream");
        await Assert.That(names).Contains("AssertionSubtree1000Benchmarks.ValidateStream");
        foreach (var corpus in new[] { "Ubl", "Cii" })
        {
            await Assert.That(names).Contains($"{corpus}ModeBenchmarks.ValidateStreamV10");
            await Assert.That(names).Contains($"{corpus}ModeBenchmarks.ValidateStreamV11");
        }
    }

    [Test]
    public async Task Targets_RecordAssertionAllocation_IndependentOfTheDocumentSize(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = await ReadTargets(cancellationToken);
        foreach (var kind in new[] { "Stream", "Span" })
        {
            var small = entries.Single(t => t.Name == $"AssertionDocument1000Benchmarks.Validate{kind}");
            var large = entries.Single(t => t.Name == $"AssertionDocument10000Benchmarks.Validate{kind}");

            // The same subtree size at ten times the document size: at most 10 % (plus 1 KiB) above the small document.
            await Assert.That((double)large.AllocatedBytes).IsLessThanOrEqualTo(small.AllocatedBytes * 1.1 + 1024);
        }
    }

    [Test]
    public async Task Targets_RecordAssertionAllocation_GrowingWithTheSubtreeSize(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = await ReadTargets(cancellationToken);
        var small = entries.Single(t => t.Name == "AssertionSubtree100Benchmarks.ValidateStream");
        var large = entries.Single(t => t.Name == "AssertionSubtree1000Benchmarks.ValidateStream");

        await Assert.That(large.AllocatedBytes).IsGreaterThan(small.AllocatedBytes);
    }

    [Test]
    public async Task Benchmarks_UseInlineData_WithoutNetworkAccess(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The version comparison reuses the pinned corpora of XSD-66 (XsdBenchmarkInput); this folder fetches nothing itself.
        foreach (var file in SourceFiles())
        {
            var code = await File.ReadAllTextAsync(file, cancellationToken);

            await Assert.That(code).DoesNotContain("HttpClient");
        }

        var others = Directory
            .GetFiles(Path.Combine(RepositoryRoot(), BenchmarkFolder), "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".cs", StringComparison.Ordinal));
        await Assert.That(others).IsEmpty();
    }

    private static async Task<IReadOnlyList<BenchmarkTarget>> ReadTargets(CancellationToken cancellationToken) =>
        TargetReader.Read(await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), TargetFile), cancellationToken));

    private static List<string> Scan()
    {
        var names = new List<string>();
        foreach (var file in SourceFiles())
        {
            string? type = null;
            var benchmark = false;
            foreach (var line in File.ReadLines(file).Select(l => l.Trim()))
            {
                if (
                    line.StartsWith("[Params", StringComparison.Ordinal)
                    || line.StartsWith("[Arguments", StringComparison.Ordinal)
                )
                {
                    throw new InvalidOperationException(
                        $"{Path.GetFileName(file)}: parameterized benchmarks need the parameter text in the target name; extend the scan first."
                    );
                }

                if (line.StartsWith("[Benchmark", StringComparison.Ordinal))
                {
                    benchmark = true;
                }
                else if (ClassName(line) is { } name)
                {
                    type = name;
                }
                else if (benchmark && MethodName(line) is { } method)
                {
                    names.Add($"{type}.{method}");
                    benchmark = false;
                }
            }
        }

        return names;
    }

    private static string? ClassName(string line)
    {
        const string Keyword = " class ";
        var at = line.IndexOf(Keyword, StringComparison.Ordinal);
        if (
            at < 0
            || !(
                line.StartsWith("public ", StringComparison.Ordinal)
                || line.StartsWith("internal ", StringComparison.Ordinal)
            )
        )
        {
            return null;
        }

        return line[(at + Keyword.Length)..].Split([' ', ':', '{', '('])[0];
    }

    private static string? MethodName(string line)
    {
        var paren = line.IndexOf('(', StringComparison.Ordinal);
        if (paren < 0 || !line.StartsWith("public ", StringComparison.Ordinal))
        {
            return null;
        }

        return line[..paren].TrimEnd().Split(' ')[^1];
    }

    private static string[] SourceFiles()
    {
        var folder = Path.Combine(RepositoryRoot(), BenchmarkFolder);

        return Directory.Exists(folder) ? Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories) : [];
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
