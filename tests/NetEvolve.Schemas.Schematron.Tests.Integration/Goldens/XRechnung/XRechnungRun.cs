namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

using System.Buffers;
using System.Text;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

/// <summary>
/// Fetches the pinned KoSIT XRechnung rules and test suite and validates every instance once per process; every test reads its
/// results from this run.
/// </summary>
/// <remarks>
/// The test suite holds only instances that are valid for XRechnung, so the expected result of every instance is "valid": no
/// <c>svrl:failed-assert</c> or <c>svrl:successful-report</c> that is not flagged as a warning or information. A failing instance
/// names its document and its rule ids. A rule set that does not build fails every instance with <see cref="BuildBlockedPrefix"/>.
/// </remarks>
internal static class XRechnungRun
{
    /// <summary>The key of the test-suite manifest.</summary>
    public const string SuiteKey = "kosit-xrechnung";

    /// <summary>The key of the rules manifest.</summary>
    public const string RulesKey = "kosit-xrechnung-schematron";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (XRechnung goldens)";

    /// <summary>The syntaxes, which are also the test sets of the results, with the file name suffix of their instances.</summary>
    public static readonly (string Syntax, string Suffix)[] Syntaxes = [("ubl", "_ubl.xml"), ("cii", "_uncefact.xml")];

    /// <summary>The start of the failure reason of an instance whose rule set does not build.</summary>
    public const string BuildBlockedPrefix = "The rule set does not build";

    /// <summary>
    /// The diagnostic codes that keep the rules from building, each with the open issue that tracks it. Empty since the rules build with the
    /// opt-in global visibility of a pattern-level <see langword="let"/> (<c>WithGlobalPatternLets</c>, #615), which <see cref="Build"/> switches on.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> BuildBlockers = new Dictionary<string, int>(
        StringComparer.Ordinal
    )
    { };

    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Dictionary<string, IReadOnlyList<CaseResult>>? s_results;
    private static (string Rules, string Suite)? s_roots;

    /// <summary>Reads the manifest of a suite key.</summary>
    /// <param name="key">The suite key.</param>
    /// <returns>The manifest.</returns>
    public static SuiteManifest LoadManifest(string key) =>
        SuiteManifestLoader
            .LoadDirectory(En16931Run.SuitesDirectory)
            .Single(m => string.Equals(m.Key, key, StringComparison.Ordinal));

    /// <summary>Gets the full path of the rules of a syntax.</summary>
    /// <param name="rulesRoot">The rules root.</param>
    /// <param name="syntax">The syntax.</param>
    /// <returns>The path of the validation schema.</returns>
    public static string SchemaPath(string rulesRoot, string syntax) =>
        Path.Combine(rulesRoot, "schematron", syntax, $"XRechnung-{syntax.ToUpperInvariant()}-validation.sch");

    /// <summary>Builds the validator of a schema; the includes may reach the rules root, which is the parent of the syntax folder.</summary>
    /// <param name="schemaPath">The full path of the schema.</param>
    /// <returns>The validator.</returns>
    public static ISchematronValidator Build(string schemaPath)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(schemaPath);
        return SchemaValidator
            .ForSchematron(File.ReadAllBytes(schemaPath), new Uri(schemaPath).AbsoluteUri)
            .WithGlobalPatternLets(true)
            .WithResolver(
                new SandboxedFileSystemResourceResolver(Path.GetDirectoryName(Path.GetDirectoryName(schemaPath))!)
            )
            .Build();
    }

    /// <summary>Fetches both archives by their pinned checksums, or reuses the cache.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The roots of the rules and of the test suite.</returns>
    public static async Task<(string Rules, string Suite)> GetRootsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (s_roots is null)
            {
                var fetcher = new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot());
                var rules = await fetcher.FetchAsync(LoadManifest(RulesKey), cancellationToken);
                var suite = await fetcher.FetchAsync(LoadManifest(SuiteKey), cancellationToken);
                s_roots = (rules, suite);
            }

            return s_roots.Value;
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Lists the instances of a syntax.</summary>
    /// <param name="suiteRoot">The test-suite root.</param>
    /// <param name="syntax">The syntax.</param>
    /// <returns>The full paths in a stable order.</returns>
    public static IReadOnlyList<string> Instances(string suiteRoot, string syntax)
    {
        var suffix = Syntaxes.Single(s => s.Syntax == syntax).Suffix;
        return Directory
            .EnumerateFiles(Path.Combine(suiteRoot, "instances"), "*" + suffix, SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Gets the results of every syntax.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The results by syntax.</returns>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<CaseResult>>> GetResultsAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (rules, suite) = await GetRootsAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (s_results is null)
            {
                var results = new Dictionary<string, IReadOnlyList<CaseResult>>(StringComparer.Ordinal);
                foreach (var (syntax, _) in Syntaxes)
                {
                    results[syntax] = Run(
                        SchemaPath(rules, syntax),
                        suite,
                        Instances(suite, syntax),
                        syntax,
                        cancellationToken
                    );
                }

                s_results = results;
            }

            return s_results;
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Gets the diagnostic codes with which the rules of a syntax fail to build.</summary>
    /// <param name="schemaPath">The full path of the schema.</param>
    /// <returns>The distinct codes in a stable order, or an empty list when the rules build.</returns>
    public static IReadOnlyList<string> BuildErrorCodes(string schemaPath)
    {
        try
        {
            _ = Build(schemaPath);
            return [];
        }
        catch (SchemaCompilationException exception)
        {
            return exception
                .Issues.Select(i => i.Code.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();
        }
    }

    private static List<CaseResult> Run(
        string schemaPath,
        string suiteRoot,
        IReadOnlyList<string> files,
        string syntax,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = new List<CaseResult>(files.Count);
        ISchematronValidator? validator = null;
        string? blocked = null;
        try
        {
            validator = Build(schemaPath);
        }
        catch (SchemaCompilationException exception)
        {
            var codes = exception.Issues.Select(i => i.Code.ToString()).Distinct(StringComparer.Ordinal);
            blocked = $"{BuildBlockedPrefix}: {string.Join(", ", codes)}.";
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetRelativePath(suiteRoot, file).Replace('\\', '/');
            var reason = blocked ?? Validate(validator!, file, cancellationToken);
            results.Add(
                reason is null
                    ? new CaseResult(id, syntax, CaseOutcome.Pass)
                    : new CaseResult(id, syntax, CaseOutcome.Fail, reason)
            );
        }

        return results;
    }

    internal static string? Validate(
        ISchematronValidator validator,
        string file,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? customLevels = null
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new ArrayBufferWriter<byte>();
        try
        {
            using var sink = new SvrlReportSink(output);
            var result = validator
                .WithReportSink(sink)
                .Validate(File.ReadAllBytes(file), new Uri(file).AbsoluteUri, cancellationToken);
            if (result.IsTruncated)
            {
                return "The validation was truncated.";
            }
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return $"{exception.GetType().Name}: {exception.Message}";
        }

        var svrl = XDocument.Parse(new UTF8Encoding(false).GetString(output.WrittenSpan));
        var errors = svrl.Descendants(Svrl + "error").Select(e => e.ToString(SaveOptions.DisableFormatting)).ToList();
        if (errors.Count > 0)
        {
            return "The SVRL holds an svrl:error: " + string.Join(" ", errors);
        }

        var fatal = svrl.Descendants()
            .Where(e =>
                (e.Name == Svrl + "failed-assert" || e.Name == Svrl + "successful-report")
                && (string?)e.Attribute("flag") is not ("warning" or "information")
                && !IsDowngraded(customLevels, (string?)e.Attribute("id"))
            )
            .Select(e => (string?)e.Attribute("id") ?? "(no id)")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        return fatal.Count == 0 ? null : "Expected valid but observed invalid: " + string.Join(", ", fatal) + ".";
    }

    // A scenario can lower the severity of a rule to a warning or information, which the validator then does not count as invalid.
    private static bool IsDowngraded(IReadOnlyDictionary<string, string>? customLevels, string? id) =>
        id is not null
        && customLevels is not null
        && customLevels.TryGetValue(id, out var level)
        && level is "warning" or "information";
}
