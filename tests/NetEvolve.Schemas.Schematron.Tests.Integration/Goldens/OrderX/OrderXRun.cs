namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.OrderX;

using System.Buffers;
using System.Text;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

/// <summary>
/// Fetches the pinned Order-X archive and validates every official example once per process; every test reads its results from this
/// run.
/// </summary>
/// <remarks>
/// The examples are positive only, so the expected result of every example is "valid": no <c>svrl:failed-assert</c> or
/// <c>svrl:successful-report</c> that is not flagged as a warning or information. A failing example names its document and its rule
/// ids. A rule set that does not build fails every example of its profile with <see cref="BuildBlockedPrefix"/>.
/// </remarks>
internal static class OrderXRun
{
    /// <summary>The key of the archive manifest.</summary>
    public const string SuiteKey = "order-x";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (Order-X goldens)";

    /// <summary>The start of the failure reason of an example whose rule set does not build.</summary>
    public const string BuildBlockedPrefix = "The rule set does not build";

    /// <summary>The profiles, which are also the test sets of the results, with their folder and file name part.</summary>
    public static readonly (string Profile, string Name)[] Profiles = [("basic", "Basic"), ("comfort", "Comfort")];

    /// <summary>The diagnostic codes that keep the rules from building, each with the open issue that tracks it.</summary>
    public static readonly IReadOnlyDictionary<string, int> BuildBlockers = new Dictionary<string, int>(
        StringComparer.Ordinal
    )
    { };

    private const string DocsDirectory = "docs/order-x_100_EN";
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Dictionary<string, IReadOnlyList<CaseResult>>? s_results;
    private static string? s_root;

    /// <summary>Reads the manifest of the archive.</summary>
    /// <returns>The manifest.</returns>
    public static SuiteManifest LoadManifest() =>
        SuiteManifestLoader
            .LoadDirectory(En16931Run.SuitesDirectory)
            .Single(m => string.Equals(m.Key, SuiteKey, StringComparison.Ordinal));

    /// <summary>Gets the full path of the rules of a profile.</summary>
    /// <param name="root">The archive root.</param>
    /// <param name="profile">The profile.</param>
    /// <returns>The path of the schema.</returns>
    public static string SchemaPath(string root, string profile)
    {
        var name = Profiles.Single(p => p.Profile == profile).Name;
        return Path.Combine(
            root,
            DocsDirectory,
            "04-XSD-UML-SCHEMATRON",
            name,
            "Schematron",
            $"SCRDMCCBDACIOMessageStructure_100pD20B_{name.ToUpperInvariant()}.sch"
        );
    }

    /// <summary>Builds the validator of a schema.</summary>
    /// <param name="schemaPath">The full path of the schema.</param>
    /// <returns>The validator.</returns>
    public static ISchematronValidator Build(string schemaPath)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(schemaPath);
        return SchemaValidator.ForSchematron(File.ReadAllBytes(schemaPath), new Uri(schemaPath).AbsoluteUri).Build();
    }

    /// <summary>Fetches the archive by its pinned checksum, or reuses the cache.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The archive root.</returns>
    public static async Task<string> GetRootAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            s_root ??= await new SuiteFetcher(
                new HttpSuiteTransport(Client),
                ConformanceCache.ResolveRoot()
            ).FetchAsync(LoadManifest(), cancellationToken);
            return s_root;
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Lists the examples of a profile.</summary>
    /// <param name="root">The archive root.</param>
    /// <param name="profile">The profile.</param>
    /// <returns>The full paths in a stable order.</returns>
    public static IReadOnlyList<string> Instances(string root, string profile)
    {
        var name = Profiles.Single(p => p.Profile == profile).Name.ToUpperInvariant();
        return Directory
            .EnumerateFiles(
                Path.Combine(
                    root,
                    DocsDirectory,
                    "05-ORDER-X_EXAMPLES",
                    $"_ORDER-x_EXAMPLES_{name}",
                    "_XML for direct access"
                ),
                "*.xml"
            )
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Gets the results of every profile.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The results by profile.</returns>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<CaseResult>>> GetResultsAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await GetRootAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (s_results is null)
            {
                var results = new Dictionary<string, IReadOnlyList<CaseResult>>(StringComparer.Ordinal);
                foreach (var (profile, _) in Profiles)
                {
                    results[profile] = Run(
                        SchemaPath(root, profile),
                        root,
                        Instances(root, profile),
                        profile,
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

    /// <summary>Gets the diagnostic codes with which the rules of a profile fail to build.</summary>
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
        string root,
        IReadOnlyList<string> files,
        string profile,
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
            var id = Path.GetRelativePath(root, file).Replace('\\', '/');
            var reason = blocked ?? Validate(validator!, file, cancellationToken);
            results.Add(
                reason is null
                    ? new CaseResult(id, profile, CaseOutcome.Pass)
                    : new CaseResult(id, profile, CaseOutcome.Fail, reason)
            );
        }

        return results;
    }

    private static string? Validate(ISchematronValidator validator, string file, CancellationToken cancellationToken)
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
            )
            .Select(e => (string?)e.Attribute("id") ?? "(no id)")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        return fatal.Count == 0 ? null : "Expected valid but observed invalid: " + string.Join(", ", fatal) + ".";
    }
}
