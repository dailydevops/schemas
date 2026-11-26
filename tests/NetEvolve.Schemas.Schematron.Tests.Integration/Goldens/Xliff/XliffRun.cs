namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Xliff;

using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd;

/// <summary>
/// Fetches the pinned OASIS XLIFF 2.1 archive and runs every instance of the test suite through the XSD step and the Schematron step once
/// per process; every test reads its results from this run.
/// </summary>
/// <remarks>
/// The expected verdict of an instance is its folder: <c>valid</c> or <c>invalid</c>. The verdict observed is the XSD step and the Schematron
/// step together: an instance is invalid when the XSD validation reports an issue or a rule set reports a <c>svrl:failed-assert</c> or a
/// <c>svrl:successful-report</c> that is not flagged as a warning or information. The upstream NVDL dispatch attaches every module rule set
/// to every document, so the Schematron step runs all rule sets on every instance.
/// </remarks>
internal static class XliffRun
{
    /// <summary>The key of the suite manifest.</summary>
    public const string SuiteKey = "xliff";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (XLIFF 2.1 goldens)";

    /// <summary>The start of the failure reason of an instance whose validators do not build.</summary>
    public const string BuildBlockedPrefix = "The validators do not build";

    /// <summary>The test sets of the results, which are the folders of the test suite.</summary>
    public static readonly string[] Sets = ["core", "modules"];

    /// <summary>The Schematron rule sets of the schema folder, which the upstream NVDL dispatch attaches to every document.</summary>
    public static readonly string[] RuleSets =
    [
        "xliff_core_2.1.sch",
        "matches.sch",
        "glossary.sch",
        "fs.sch",
        "resource_data.sch",
        "size_restriction.sch",
        "metadata.sch",
        "validation.sch",
        "its.sch",
    ];

    /// <summary>The XSD files of the schema folder: the core schema and the module schemas.</summary>
    public static readonly string[] SchemaFiles =
    [
        "xliff_core_2.0.xsd",
        "matches.xsd",
        "glossary.xsd",
        "metadata.xsd",
        "fs.xsd",
        "resource_data.xsd",
        "size_restriction.xsd",
        "validation.xsd",
        "itsm.xsd",
        "its.xsd",
    ];

    /// <summary>
    /// The instances that do not have the verdict of their folder, each with the open issue that tracks it: #1363 for the cases that the
    /// pinned upstream rule sets do not decide as stated, #1362 for the case that needs the NVDL attribute sections.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> KnownFailures = new Dictionary<string, int>(
        StringComparer.Ordinal
    )
    {
        ["test-suite/core/invalid/bad_DifferentXmlSpace.xlf"] = 1363,
        ["test-suite/core/invalid/bad_DuplicateExtElemIdsInFile.xlf"] = 1363,
        ["test-suite/core/invalid/bad_DuplicateExtElemIdsInGroup.xlf"] = 1363,
        ["test-suite/core/invalid/bad_DuplicateExtElemIdsInUnit.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidFSAttribute.xlf"] = 1362,
        ["test-suite/core/invalid/bad_InvalidFragIdBadOrder.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidFragIdDuplicatedPrefix.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidFragIdMissplacedLeaf.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidFragIdNoSingleLeaf.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidFragIdPrefixNotNmtoken.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidFragIdPrefixTooShort.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidFragIdUnknownPrefix.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidValidation.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidXmlLangInheritedFromFile.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidXmlLangInheritedFromGroup.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidXmlLangInheritedFromUnit.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidXmlLangOnFile.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidXmlLangOnGroup.xlf"] = 1363,
        ["test-suite/core/invalid/bad_InvalidXmlLangOnUnit.xlf"] = 1363,
        ["test-suite/core/invalid/bad_SrcLangNotWellFormed.xlf"] = 1363,
        ["test-suite/core/invalid/bad_WrongLangOnTarget.xlf"] = 1363,
        ["test-suite/core/valid/allExtensions.xlf"] = 1363,
        ["test-suite/core/valid/withGlossary.xlf"] = 1363,
        ["test-suite/core/valid/withMatches.xlf"] = 1363,
        ["test-suite/core/valid/withModulesAttributesInEc.xlf"] = 1363,
        ["test-suite/core/valid/withReferences.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-ctr-property-not-legit-category.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-ctr_appliesTo-not-using-ref-to-resolvableID.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-ctr_property-not-content-or-valid-attribute-ref.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-ctr_ref-not-pointed-to-resolvableID.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-ctr_revisions-not-using-ref-to-resolvableID.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-mtc_match-has-xml_lang.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-mtc_wrong-ref-syntax.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-slr_equivStorage-not-integer.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-slr_sizeInfo-ec-not-isolated.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-slr_sizeInfo-not-integer.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-slr_sizeInfoRef-ec-not-isolated.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-slr_sizeInfoRef-has-no-data-sib.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-slr_sizeRestriction-patterns.xlf"] = 1363,
        ["test-suite/modules/invalid/Bad-slr_storageRestriction-patterns.xlf"] = 1363,
        ["test-suite/modules/valid/Good-res_source-has-no-content-and-href.xlf"] = 1363,
    };

    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
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

    /// <summary>Lists the instances of a test set and a verdict folder.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="set">The test set, <c>core</c> or <c>modules</c>.</param>
    /// <param name="folder">The verdict folder, <c>valid</c> or <c>invalid</c>.</param>
    /// <returns>The full paths of the <c>.xlf</c> files in a stable order.</returns>
    public static IReadOnlyList<string> Instances(string root, string set, string folder) =>
        [
            .. Directory
                .EnumerateFiles(Path.Combine(root, "test-suite", set, folder), "*.xlf")
                .OrderBy(f => f, StringComparer.Ordinal),
        ];

    /// <summary>Gets the results of every test set.</summary>
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
            if (s_results is null)
            {
                var results = new Dictionary<string, IReadOnlyList<CaseResult>>(StringComparer.Ordinal);
                foreach (var set in Sets)
                {
                    results[set] = await RunAsync(root, set, cancellationToken);
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

    /// <summary>Builds the validators of the XSD step and of every rule set.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="cancellationToken">A token to cancel the build.</param>
    /// <returns>The validators.</returns>
    public static async Task<Validators> BuildAsync(string root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schemas = Path.Combine(root, "schemas");
        var builder = SchemaValidator
            .ForXsd()
            .WithVersion(XsdVersion.V10)
            .WithSchemaLocationHints(true)
            .WithResolver(new SandboxedFileSystemResourceResolver(schemas));
        foreach (var file in SchemaFiles)
        {
            var path = Path.Combine(schemas, file);
            _ = builder.AddSchema(await File.ReadAllBytesAsync(path, cancellationToken), new Uri(path).AbsoluteUri);
        }

        var ruleSets = new List<KeyValuePair<string, ISchematronValidator>>(RuleSets.Length);
        foreach (var file in RuleSets)
        {
            var path = Path.Combine(schemas, file);
            var validator = SchemaValidator
                .ForSchematron(await File.ReadAllBytesAsync(path, cancellationToken), new Uri(path).AbsoluteUri)
                .WithGlobalPatternLets(true)
                .WithDtdProcessing(true)
                .WithResolver(new SandboxedFileSystemResourceResolver(schemas))
                .Build();
            ruleSets.Add(new KeyValuePair<string, ISchematronValidator>(file, validator));
        }

        return new Validators(await builder.BuildAsync(cancellationToken), ruleSets);
    }

    /// <summary>Gets the observed verdict of one instance: the problems of the XSD step and of every rule set.</summary>
    /// <param name="validators">The validators.</param>
    /// <param name="file">The full path of the instance.</param>
    /// <param name="cancellationToken">A token to cancel the validation.</param>
    /// <returns>The problems; empty when the instance is valid.</returns>
    public static async Task<IReadOnlyList<string>> ObserveAsync(
        Validators validators,
        string file,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(validators);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
        var uri = new Uri(file).AbsoluteUri;
        var problems = new List<string>();
        try
        {
            if (!(await validators.Xsd.ValidateAsync(bytes, uri, cancellationToken)).IsValid)
            {
                problems.Add("xsd");
            }
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            problems.Add($"xsd {exception.GetType().Name}: {exception.Message}");
        }

        foreach (var ruleSet in validators.RuleSets)
        {
            problems.AddRange(ObserveRuleSet(ruleSet.Key, ruleSet.Value, bytes, uri, cancellationToken));
        }

        return problems;
    }

    private static string Describe(XElement fired)
    {
        var text = string.Concat(fired.Element(Svrl + "text")?.Value.Trim() ?? (string?)fired.Attribute("test"));
        return text.Length <= 90 ? text : text.Substring(0, 90);
    }

    private static async Task<List<CaseResult>> RunAsync(string root, string set, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validators? validators = null;
        string? blocked = null;
        try
        {
            validators = await BuildAsync(root, cancellationToken);
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            blocked = $"{BuildBlockedPrefix}: {exception.GetType().Name}: {exception.Message}";
        }

        var results = new List<CaseResult>();
        foreach (var expectValid in new[] { true, false })
        {
            foreach (var file in Instances(root, set, expectValid ? "valid" : "invalid"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = Path.GetRelativePath(root, file).Replace('\\', '/');
                var reason = blocked ?? Judge(await ObserveAsync(validators!, file, cancellationToken), expectValid);
                results.Add(
                    reason is null
                        ? new CaseResult(id, set, CaseOutcome.Pass)
                        : new CaseResult(id, set, CaseOutcome.Fail, reason)
                );
            }
        }

        return results;
    }

    private static string? Judge(IReadOnlyList<string> problems, bool expectValid) =>
        (expectValid, problems.Count) switch
        {
            (true, 0) or (false, > 0) => null,
            (true, _) => "Expected valid but observed invalid: " + string.Join(", ", problems) + ".",
            _ => "Expected invalid but observed valid.",
        };

    private static List<string> ObserveRuleSet(
        string name,
        ISchematronValidator validator,
        byte[] bytes,
        string uri,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new ArrayBufferWriter<byte>();
        try
        {
            using var sink = new SvrlReportSink(output);
            var result = validator.WithReportSink(sink).Validate(bytes, uri, cancellationToken);
            if (result.IsTruncated)
            {
                return [$"{name} truncated"];
            }
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return [$"{name} {exception.GetType().Name}: {exception.Message}"];
        }

        var svrl = XDocument.Parse(new UTF8Encoding(false).GetString(output.WrittenSpan));
        var problems = svrl.Descendants(Svrl + "error").Select(_ => $"{name} svrl:error").ToList();
        problems.AddRange(
            svrl.Descendants()
                .Where(e =>
                    (e.Name == Svrl + "failed-assert" || e.Name == Svrl + "successful-report")
                    && (string?)e.Attribute("flag") is not ("warning" or "information")
                )
                .Select(e => $"{name} {(string?)e.Attribute("id") ?? Describe(e)}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
        );
        return problems;
    }

    /// <summary>The validators of the XSD step and of the rule sets.</summary>
    /// <param name="Xsd">The XSD validator over the core and the module schemas.</param>
    /// <param name="RuleSets">The Schematron validators by file name.</param>
    internal sealed record Validators(
        IXsdValidator Xsd,
        IReadOnlyList<KeyValuePair<string, ISchematronValidator>> RuleSets
    );
}
