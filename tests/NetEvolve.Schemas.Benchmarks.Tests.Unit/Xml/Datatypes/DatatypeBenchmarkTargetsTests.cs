namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.Xml.Datatypes;

using NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// The datatype parsing and decimal benchmarks (DT-58) and their recorded targets. The benchmark project targets only the
/// newest runtimes, so the benchmark sources are scanned instead of referenced. Every benchmark class has the three input
/// sets <c>Short</c>, <c>Typical</c>, and <c>Long</c> as a <c>Size</c> parameter, so the report names are
/// <c>Type.Method(Size=...)</c>.
/// </summary>
public class DatatypeBenchmarkTargetsTests
{
    private const string BenchmarkFolder = "benchmarks/NetEvolve.Schemas.Benchmarks/Xml/Datatypes";
    private const string TargetFile = "benchmarks/targets/datatypes.json";

    private static readonly string[] Sizes = ["Short", "Typical", "Long"];

    private static readonly string[] ParsedTypes =
    [
        "DecimalParseBenchmarks",
        "IntegerParseBenchmarks",
        "DoubleParseBenchmarks",
        "DateTimeParseBenchmarks",
        "DurationParseBenchmarks",
        "Base64ParseBenchmarks",
    ];

    [Test]
    public async Task Folder_CoversEveryListedDatatype_WithOneBaselinePerClass()
    {
        var methods = Scan();

        foreach (var type in ParsedTypes)
        {
            var ofType = methods.Where(m => string.Equals(m.Type, type, StringComparison.Ordinal)).ToArray();

            await Assert.That(ofType.Count(m => m.IsBaseline)).IsEqualTo(1);
            await Assert.That(ofType.Count(m => !m.IsBaseline)).IsEqualTo(1);
        }

        await Assert
            .That(methods.Where(m => !m.IsBaseline).Select(m => m.Method))
            .Contains("Add")
            .And.Contains("Multiply")
            .And.Contains("Divide")
            .And.Contains("Compare");
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
    public async Task Targets_HaveOneEntryPerBenchmarkAndInputSet_AndNoOther(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expected = Scan()
            .Where(m => !m.IsBaseline)
            .SelectMany(m => Sizes.Select(size => $"{m.Name}(Size={size})"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var entries = await ReadTargets(cancellationToken);

        await Assert
            .That(entries.Select(t => t.Name).OrderBy(name => name, StringComparer.Ordinal))
            .IsEquivalentTo(expected);
    }

    [Test]
    public async Task Targets_AreGatedByRatio_ExactlyWhereASystemXmlBaselineExists(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var baselined = Scan().Where(m => m.IsBaseline).Select(m => m.Type).ToHashSet(StringComparer.Ordinal);
        var entries = await ReadTargets(cancellationToken);

        await Assert.That(baselined).IsNotEmpty();
        foreach (var entry in entries)
        {
            var type = entry.Name[..entry.Name.IndexOf('.', StringComparison.Ordinal)];

            await Assert.That(entry.BaselineRatio is not null).IsEqualTo(baselined.Contains(type));
        }
    }

    [Test]
    public async Task Targets_RequireNoAllocationForSpanParsersWithoutOctets(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Bounded integers, double, dateTime, and duration parse the Short and Typical forms from a span without allocating.
        // The Long forms allocate today (integer, double, duration); their recorded targets gate that value, and the
        // misses are tracked as bugs (limitation).
        string[] zeroAllocation =
        [
            "IntegerParseBenchmarks",
            "DoubleParseBenchmarks",
            "DateTimeParseBenchmarks",
            "DurationParseBenchmarks",
        ];
        var entries = await ReadTargets(cancellationToken);
        var selected = entries
            .Where(t => zeroAllocation.Any(type => t.Name.StartsWith(type + ".", StringComparison.Ordinal)))
            .Where(t => !t.Name.EndsWith("(Size=Long)", StringComparison.Ordinal))
            .ToArray();

        await Assert.That(selected.Length).IsEqualTo(zeroAllocation.Length * 2);
        await Assert.That(selected.Where(t => t.AllocatedBytes != 0).Select(t => t.Name)).IsEmpty();
    }

    [Test]
    public async Task Folder_Sources_UseNoNetworkAndNoFileAccess(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string[] forbidden = ["HttpClient", "WebRequest", "WebClient", "File.", "FileStream", "FileInfo", "Directory."];
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

    private static async Task<IReadOnlyList<BenchmarkTarget>> ReadTargets(CancellationToken cancellationToken) =>
        TargetReader.Read(await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), TargetFile), cancellationToken));

    private static List<BenchmarkMethod> Scan()
    {
        var methods = new List<BenchmarkMethod>();
        foreach (var file in SourceFiles())
        {
            string? type = null;
            bool? baseline = null;
            foreach (var line in File.ReadLines(file).Select(l => l.Trim()))
            {
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
        if (
            paren < 0
            || !line.StartsWith("public ", StringComparison.Ordinal)
            || line.IndexOf('=', StringComparison.Ordinal) is >= 0 and var assignment && assignment < paren
        )
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
