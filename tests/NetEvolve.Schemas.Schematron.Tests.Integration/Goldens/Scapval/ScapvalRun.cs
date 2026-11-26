namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Scapval;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

/// <summary>
/// Fetches the pinned NIST SCAPVal archive and builds every Schematron rule set once per process.
/// </summary>
/// <remarks>
/// The rules are <c>.sch</c> sources of up to about 900 KB each with <c>xsl:function</c> declarations, phases and the <c>xslt</c> and
/// <c>xslt2</c> bindings. A rule set that does not build is a known failure in <see cref="KnownFailures"/>, linked to its issue. The unit
/// tests of the archive are decima templates, not instances, and are not run.
/// </remarks>
internal static class ScapvalRun
{
    /// <summary>The key of the suite manifest.</summary>
    public const string SuiteKey = "scapval";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (SCAPVal goldens)";

    /// <summary>The test set of the build cases.</summary>
    public const string BuildSet = "build";

    /// <summary>The start of the failure reason of a rule set that does not build.</summary>
    public const string BuildBlockedPrefix = "The rule set does not build";

    /// <summary>The number of rule sets in the pinned archive.</summary>
    public const int RuleSetCount = 31;

    /// <summary>Every known failure by test set and case id, with the open issue that tracks it.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> KnownFailures = new Dictionary<
        string,
        IReadOnlyDictionary<string, int>
    >(StringComparer.Ordinal)
    {
        [BuildSet] = new Dictionary<string, int>(StringComparer.Ordinal),
    };

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Dictionary<string, IReadOnlyList<CaseResult>>? s_results;
    private static string? s_root;

    /// <summary>Reads the manifest that pins the archive.</summary>
    /// <returns>The manifest.</returns>
    public static SuiteManifest LoadManifest() =>
        SuiteManifestLoader
            .LoadDirectory(En16931Run.SuitesDirectory)
            .Single(m => string.Equals(m.Key, SuiteKey, StringComparison.Ordinal));

    /// <summary>Lists the rule sets.</summary>
    /// <param name="root">The suite root.</param>
    /// <returns>The full paths in a stable order.</returns>
    public static IReadOnlyList<string> RuleSets(string root) =>
        [
            .. Directory
                .EnumerateFiles(Path.Combine(root, "main", "resources", "rules"), "*.sch", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal),
        ];

    /// <summary>Gets the case id of a file.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="file">The full path.</param>
    /// <returns>The path relative to the root with forward slashes.</returns>
    public static string CaseId(string root, string file) => Path.GetRelativePath(root, file).Replace('\\', '/');

    /// <summary>Builds the validator of a rule set.</summary>
    /// <param name="schemaPath">The full path of the schema.</param>
    /// <returns>The validator.</returns>
    public static ISchematronValidator Build(string schemaPath)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(schemaPath);
        return SchemaValidator.ForSchematron(File.ReadAllBytes(schemaPath), new Uri(schemaPath).AbsoluteUri).Build();
    }

    /// <summary>Fetches the archive by its pinned checksum, or reuses the cache.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite root.</returns>
    public static async Task<string> GetRootAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_root ??= await new SuiteFetcher(
                new HttpSuiteTransport(Client),
                ConformanceCache.ResolveRoot()
            ).FetchAsync(LoadManifest(), cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Gets the results of the test sets.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The results by test set.</returns>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<CaseResult>>> GetResultsAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await GetRootAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_results ??= new Dictionary<string, IReadOnlyList<CaseResult>>(StringComparer.Ordinal)
            {
                [BuildSet] = RunBuilds(root, cancellationToken),
            };
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    private static List<CaseResult> RunBuilds(string root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = new List<CaseResult>();
        foreach (var file in RuleSets(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = CaseId(root, file);
            results.Add(
                TryBuild(file, out _) is { } blocked
                    ? new CaseResult(id, BuildSet, CaseOutcome.Fail, blocked)
                    : new CaseResult(id, BuildSet, CaseOutcome.Pass)
            );
        }

        return results;
    }

    private static string? TryBuild(string schemaPath, out ISchematronValidator? validator)
    {
        try
        {
            validator = Build(schemaPath);
            return null;
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            validator = null;
            var detail = exception is SchemaCompilationException compilation
                ? string.Join(", ", compilation.Issues.Select(i => i.Code.ToString()).Distinct(StringComparer.Ordinal))
                : exception.GetType().Name + ": " + exception.Message;
            return $"{BuildBlockedPrefix}: {detail}.";
        }
    }
}
