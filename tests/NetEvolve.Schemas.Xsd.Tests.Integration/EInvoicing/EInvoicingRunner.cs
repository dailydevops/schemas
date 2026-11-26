namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

/// <summary>
/// Builds every schema set once through the public entry point (strict XSD 1.0) and validates the shipped documents with it. A document
/// with the expectation <c>valid</c> must be accepted; any other document must get the verdict of System.Xml.Schema.
/// </summary>
internal static class EInvoicingRunner
{
    /// <summary>The expectation of an official example that the schema set must accept.</summary>
    public const string Valid = "valid";

    /// <summary>The expectation of a document that must get the verdict of System.Xml.Schema.</summary>
    public const string Differential = "differential";

    /// <summary>The case id suffix of the build of the schema set.</summary>
    public const string SchemasCase = "schemas";

    // The coupled code list modules of UN/CEFACT CII reference more schema documents than the default limit of 16 allows.
    private static readonly ValidationLimits Limits = ValidationLimits.Default.WithMaxExternalResources(256);

    /// <summary>Lists the documents of a group, in ordinal order, as paths relative to the suite root with forward slashes.</summary>
    /// <param name="suiteRoot">The root of the fetched suite.</param>
    /// <param name="group">The group.</param>
    /// <returns>The relative paths.</returns>
    public static IReadOnlyList<string> ListDocuments(string suiteRoot, EInvoicingDocumentGroup group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        ArgumentNullException.ThrowIfNull(group);

        // The archives mix the cases of the extension (BIS3_Invoice_negativ.XML), and the CI runs on a case-sensitive file system.
        var files = Directory.EnumerateFiles(
            Path.Combine(suiteRoot, group.Directory),
            "*.xml",
            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }
        );
        return
        [
            .. files
                .Select(f => Path.GetRelativePath(suiteRoot, f).Replace(Path.DirectorySeparatorChar, '/'))
                .OrderBy(f => f, StringComparer.Ordinal),
        ];
    }

    /// <summary>Runs every set of the record.</summary>
    /// <param name="manifest">The record.</param>
    /// <param name="rootOf">Gives the root of a fetched suite by its key.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result for the build of every set and one per document; the case id is <c>&lt;set name&gt;/&lt;relative path&gt;</c>.</returns>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        EInvoicingManifest manifest,
        Func<string, string> rootOf,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(rootOf);

        var excluded = new HashSet<string>(manifest.Excluded.Select(e => e.Id), StringComparer.Ordinal);
        var deviations = new HashSet<string>(manifest.Deviations.Select(d => d.Id), StringComparer.Ordinal);
        var baseline = new SystemXmlSchemaEngine();
        var results = new List<CaseResult>();
        foreach (var set in manifest.Sets)
        {
            var schemaRoot = rootOf(set.SchemaSuite);
            var schemas = set.Schemas.Select(s => Path.GetFullPath(Path.Combine(schemaRoot, s))).ToArray();
            var ours = new OursEngine(schemaRoot, Limits);

            var build = await ours.ObserveAsync(
                new DifferentialCase(set.Name, schemas, null, Verdict.Valid, Verdict.NotRun),
                cancellationToken
            );
            results.Add(Result(set, SchemasCase, build is null ? "the schema set cannot be observed" : Reject(build)));

            foreach (var group in set.Documents)
            {
                var documentRoot = rootOf(group.Suite);
                foreach (var relative in ListDocuments(documentRoot, group))
                {
                    var id = set.Name + "/" + relative;
                    if (excluded.Contains(id))
                    {
                        continue;
                    }

                    var differentialCase = new DifferentialCase(
                        id,
                        schemas,
                        Path.GetFullPath(Path.Combine(documentRoot, relative)),
                        Verdict.Valid,
                        Verdict.Valid
                    );
                    results.Add(
                        Result(
                            set,
                            relative,
                            await Compare(
                                ours,
                                baseline,
                                group,
                                differentialCase,
                                deviations.Contains(id),
                                cancellationToken
                            )
                        )
                    );
                }
            }
        }

        return results;
    }

    private static CaseResult Result(EInvoicingSet set, string name, string? reason) =>
        reason is null
            ? new CaseResult(set.Name + "/" + name, set.Name, CaseOutcome.Pass)
            : new CaseResult(set.Name + "/" + name, set.Name, CaseOutcome.Fail, reason);

    private static string? Reject(EngineResult build) =>
        build.Schema == Verdict.Valid
            ? null
            : "the schema set is rejected"
                + (build.Issues.Count > 0 ? " at " + Position(build.Issues[0]) : string.Empty);

    private static async Task<string?> Compare(
        OursEngine ours,
        SystemXmlSchemaEngine baseline,
        EInvoicingDocumentGroup group,
        DifferentialCase differentialCase,
        bool deviates,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observed = await ours.ObserveAsync(differentialCase, cancellationToken);
        if (observed is null)
        {
            return "the document cannot be observed";
        }

        if (observed.Schema != Verdict.Valid)
        {
            return "the schema set is rejected";
        }

        if (string.Equals(group.Expectation, Valid, StringComparison.Ordinal))
        {
            return observed.Instance == Verdict.Valid ? null : "an official example is rejected" + At(observed);
        }

        var expected = await baseline.RunAsync(differentialCase, cancellationToken);
        var equal = observed.Instance == expected.Instance;
        return (equal, deviates) switch
        {
            (true, false) or (false, true) => null,
            (true, true) => "the documented deviation from System.Xml.Schema no longer applies",
            _ => $"the verdict is {observed.Instance}, System.Xml.Schema gives {expected.Instance}" + At(observed),
        };
    }

    private static string At(EngineResult result) =>
        result.Issues.Count > 0 ? " at " + Position(result.Issues[0]) : string.Empty;

    private static string Position(IssuePosition position) => $"{position.Line}:{position.Column}";
}
