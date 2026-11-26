namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

using System.Buffers;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;
using LibraryValidator = NetEvolve.Schemas.ISchematronValidator;

/// <summary>
/// Fetches the pinned eForms SDK and validates its example notices with the static rule set, once per process.
/// </summary>
/// <remarks>
/// The expected report of a notice is the SVRL file of the same name in <c>examples/reports</c>; its phase names the phase to run,
/// because the SDK selects the rules by the notice subtype. The static rule set is used; the dynamic rule set needs an HTTP lookup of
/// the changed notice and is not run.
/// </remarks>
internal static class EformsSdkRun
{
    /// <summary>The suite key of the pinned archive.</summary>
    public const string SuiteKey = "eforms-sdk";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (eForms SDK)";

    // The rule set includes about 260 files; the default of 16 external resources is for untrusted input.
    private static readonly ValidationLimits Limits = ValidationLimits.Default.WithMaxExternalResources(1_000);

    private const string RuleSet = "schematrons/static/complete-validation.sch";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IReadOnlyList<CaseResult>? s_results;

    /// <summary>Gets the directory of the suite manifests.</summary>
    public static string SuitesDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "suites");

    /// <summary>Gets the baseline directory of the suite.</summary>
    public static string BaselineDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "baselines", SuiteKey);

    /// <summary>Gets the results of every notice.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The verdicts of all notices; the test set is in <see cref="CaseResult.TestSet"/>.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_results ??= await RunAsync(cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Gets the file names of the example notices that have an expected report.</summary>
    /// <param name="root">The root of the SDK.</param>
    /// <returns>The notice file names in a stable order.</returns>
    public static IReadOnlyList<string> NoticeNames(string root)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(root);
        return
        [
            .. Directory
                .GetFiles(Path.Combine(root, "examples", "notices"), "*.xml")
                .Select(Path.GetFileName)
                .OfType<string>()
                .OrderBy(n => n, StringComparer.Ordinal),
        ];
    }

    private static async Task<IReadOnlyList<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = SuiteManifestLoader
            .LoadDirectory(SuitesDirectory)
            .Single(m => string.Equals(m.Key, SuiteKey, StringComparison.Ordinal));
        var root = await new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            manifest,
            cancellationToken
        );
        return Run(root, cancellationToken);
    }

    private static List<CaseResult> Run(string root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schemaPath = Path.Combine(root, RuleSet);
        var schema = File.ReadAllBytes(schemaPath);
        var resolver = new SandboxedFileSystemResourceResolver(Path.GetDirectoryName(schemaPath)!);
        var validators = new Dictionary<string, LibraryValidator>(StringComparer.Ordinal);
        var results = new List<CaseResult>();
        foreach (var notice in NoticeNames(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var expected = EformsReport.Parse(
                    File.ReadAllText(Path.Combine(root, "examples", "reports", Path.ChangeExtension(notice, ".svrl")))
                );
                var phase = expected.Phase!;
                if (!validators.TryGetValue(phase, out var validator))
                {
                    validators[phase] = validator = SchemaValidator
                        .ForSchematron(schema, new Uri(schemaPath).AbsoluteUri)
                        .WithResolver(resolver)
                        .WithLimits(Limits)
                        .WithPhase(phase)
                        .Build();
                }

                var actual = Validate(
                    validator,
                    File.ReadAllBytes(Path.Combine(root, "examples", "notices", notice)),
                    cancellationToken
                );
                results.AddRange(EformsComparer.Compare(notice, expected, actual.RestrictTo(expected.ActivePatterns)));
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
            {
                results.Add(
                    new CaseResult(
                        $"{EformsComparer.ValidTestSet}/{notice}",
                        EformsComparer.ValidTestSet,
                        CaseOutcome.Fail,
                        $"{exception.GetType().Name}: {exception.Message}"
                    )
                );
            }
        }

        return results;
    }

    private static EformsReport Validate(LibraryValidator validator, byte[] notice, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(output))
        {
            var result = validator
                .WithReportSink(sink)
                .Validate(notice, "https://eforms.invalid/notice.xml", cancellationToken);
            if (result.IsTruncated)
            {
                throw new InvalidOperationException("The validation was truncated.");
            }
        }

        return EformsReport.Parse(Utf8.GetString(output.WrittenSpan));
    }
}
