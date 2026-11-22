namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.Xml.Dtd;

using NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// The DTD validation benchmarks (XML-72) and their recorded targets. The benchmark project targets only the newest
/// runtimes, so the benchmark sources are scanned instead of referenced: a benchmark is a method with a
/// <c>[Benchmark]</c> attribute, its name is <c>Type.Method</c> as in the report, and the baseline is the method
/// that ends with <see cref="SystemXmlBaseline.MethodSuffix"/>.
/// </summary>
public class DtdBenchmarkTargetsTests
{
    private const string BenchmarkFolder = "benchmarks/NetEvolve.Schemas.Benchmarks/Xml/Dtd";
    private const string TargetFile = "benchmarks/targets/xml-dtd.json";

    [Test]
    public async Task Folder_ContainsBenchmarks_WithOneBaselinePerClass()
    {
        var methods = Scan();

        await Assert.That(methods.Where(m => !m.IsBaseline)).IsNotEmpty();
        foreach (var type in methods.Select(m => m.Type).Distinct(StringComparer.Ordinal))
        {
            var baselines = methods
                .Where(m => string.Equals(m.Type, type, StringComparison.Ordinal) && m.IsBaseline)
                .ToArray();

            await Assert.That(baselines.Length).IsEqualTo(1);
            await Assert.That(baselines[0].Method).EndsWith(SystemXmlBaseline.MethodSuffix);
        }
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

        var expected = Scan().Where(m => !m.IsBaseline).Select(m => m.Name).Order(StringComparer.Ordinal).ToArray();
        var entries = TargetReader.Read(
            await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), TargetFile), cancellationToken)
        );

        await Assert.That(entries.Select(t => t.Name).Order(StringComparer.Ordinal)).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Targets_AreGatedByRatioToTheBaseline_NotByTheAbsoluteMean(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The recorded mean comes from one machine; the gate runs on another, so only the ratio to System.Xml is portable.
        var entries = TargetReader.Read(
            await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), TargetFile), cancellationToken)
        );

        await Assert.That(entries).IsNotEmpty();
        await Assert.That(entries.Where(t => t.BaselineRatio is null).Select(t => t.Name)).IsEmpty();
    }

    [Test]
    public async Task Folder_Sources_UseNoNetworkAndNoFileAccess(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string[] forbidden =
        [
            "XmlUrlResolver",
            "HttpClient",
            "WebRequest",
            "WebClient",
            "File.",
            "FileStream",
            "FileInfo",
            "Directory.",
            "FileSystemResourceResolver",
        ];
        var found = new List<string>();
        var files = SourceFiles();
        foreach (var file in files)
        {
            var code = await File.ReadAllTextAsync(file, cancellationToken);
            foreach (var word in forbidden)
            {
                if (code.Contains(word, StringComparison.Ordinal))
                {
                    found.Add($"{Path.GetFileName(file)}: {word}");
                }
            }
        }

        await Assert.That(files).IsNotEmpty();
        await Assert.That(found).IsEmpty();
    }

    private sealed record BenchmarkMethod(string Type, string Method, bool IsBaseline)
    {
        public string Name => $"{Type}.{Method}";
    }

    private static List<BenchmarkMethod> Scan()
    {
        var methods = new List<BenchmarkMethod>();
        foreach (var file in SourceFiles())
        {
            string? type = null;
            bool? baseline = null;
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
                    baseline = line.Contains("Baseline = true", StringComparison.Ordinal);
                }
                else if (ClassName(line) is { } name)
                {
                    type = name;
                }
                else if (baseline is { } isBaseline && MethodName(line) is { } method)
                {
                    methods.Add(new BenchmarkMethod(type!, method, isBaseline));
                    baseline = null;
                }
            }
        }

        return methods;
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
        // A field or property initializer has its "=" before the parenthesis; the "=>" of a method body comes after it.
        if (
            paren < 0
            || !line.StartsWith("public ", StringComparison.Ordinal)
            || line.IndexOf('=', StringComparison.Ordinal) is >= 0 and var assignment && assignment < paren
        )
        {
            return null;
        }

        var head = line[..paren].TrimEnd();

        return head[(head.LastIndexOf(' ') + 1)..];
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
