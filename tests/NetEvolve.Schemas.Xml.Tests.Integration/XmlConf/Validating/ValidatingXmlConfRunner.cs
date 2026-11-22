namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Validating;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

/// <summary>
/// Runs the valid and invalid cases of the suite through the validating reader mode (XML-68): a valid case must read
/// without a validity issue, an invalid case must read without a fatal status and report at least one validity issue.
/// </summary>
/// <remarks>
/// <para>
/// A validity issue is an issue of severity error (design #122, D3); a warning such as <c>XML5911</c> is an optional report
/// of the processor and does not make a valid document invalid. The reader runs with all three external flags on and a
/// file resolver that is confined to the suite directory, so the external subset and the external entities load for every
/// value of the <c>ENTITIES</c> tag. The base URI of the document is its file URI.
/// </para>
/// <para>
/// Limitations: the cases of <see cref="ValidatingXmlConfGaps.Excluded"/> are not run (the reader itself cannot read them:
/// namespace processing switched off, a name longer than the limit, a version other than 1.0, a document missing from the
/// pinned archive); each is a skipped result with its reason and stays in the reader baseline. The cases of
/// <see cref="ValidatingXmlConfGaps.Known"/> run and fail; they are the known gaps of the validation. The cases that are not
/// valid or invalid (not well-formed, error) belong to the reader runs of <see cref="XmlConfRunner"/>.
/// </para>
/// </remarks>
internal static class ValidatingXmlConfRunner
{
    /// <summary>Runs the valid and invalid cases of a selection.</summary>
    /// <param name="selection">The selection; excluded cases are not part of the run.</param>
    /// <param name="suiteRoot">The suite root directory.</param>
    /// <param name="excluded">The cases that are not run, each with its reason; they become skipped results.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per valid or invalid case, in catalog order.</returns>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        XmlConfSelection selection,
        string suiteRoot,
        IReadOnlyDictionary<string, string> excluded,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(suiteRoot);
        ArgumentNullException.ThrowIfNull(excluded);

        var options = DtdValidationOptions
            .Default.WithResolver(new SandboxedFileSystemResourceResolver(suiteRoot))
            .WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(true)
            .WithAllowExternalParameterEntities(true);
        var results = new List<CaseResult>();
        foreach (var xmlConfCase in selection.Selected)
        {
            if (xmlConfCase.Type is not (XmlConfType.Valid or XmlConfType.Invalid))
            {
                continue;
            }

            if (excluded.TryGetValue(xmlConfCase.Id, out var reason))
            {
                results.Add(new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Skipped, reason));
                continue;
            }

            var failure = await JudgeAsync(xmlConfCase, suiteRoot, options, cancellationToken);
            results.Add(
                failure is null
                    ? new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Pass)
                    : new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Fail, failure)
            );
        }

        return results;
    }

    /// <summary>Reads one document through the validating reader mode.</summary>
    /// <param name="bytes">The document.</param>
    /// <param name="baseUri">The absolute URI of the document.</param>
    /// <param name="options">The options of validation.</param>
    /// <returns>The outcome of the run.</returns>
    public static DtdPipelineResult Observe(byte[] bytes, string baseUri, DtdValidationOptions options) =>
        DtdValidatingPipeline.Validate(bytes.AsSpan(), options, documentUri: baseUri);

    /// <summary>Judges an outcome against the type of the case.</summary>
    /// <param name="type">The type of the case.</param>
    /// <param name="outcome">The outcome of the run.</param>
    /// <returns>The reason the outcome fails the case, or <see langword="null"/> when it satisfies it.</returns>
    public static string? Judge(XmlConfType type, DtdPipelineResult outcome)
    {
        if (!outcome.IsWellFormed)
        {
            return "The reader reported a fatal status " + outcome.Diagnostic?.Code.Alias + ".";
        }

        var errors = ErrorCodes(outcome);
        return (type, errors.Count) switch
        {
            (XmlConfType.Valid, > 0) => "The document is valid but the run reported " + string.Join(", ", errors) + ".",
            (XmlConfType.Invalid, 0) => "The document is invalid but the run reported no validity issue.",
            _ => null,
        };
    }

    private static List<string> ErrorCodes(DtdPipelineResult outcome)
    {
        var codes = new List<string>();
        foreach (var issue in outcome.Issues.Issues)
        {
            if (issue.Severity == IssueSeverity.Error)
            {
                codes.Add(Alias(issue.Code));
            }
        }

        return codes;
    }

    // The alias of the code (XML5004), which the reasons and the gap list use instead of the numeric code.
    private static string Alias(DiagnosticCode code)
    {
        foreach (var known in DtdValidityCodes.All)
        {
            if (known.Code == code)
            {
                return known.Alias;
            }
        }

        return code.ToString();
    }

    private static async Task<string?> JudgeAsync(
        XmlConfCase xmlConfCase,
        string suiteRoot,
        DtdValidationOptions options,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(suiteRoot, xmlConfCase.Uri);
        if (!File.Exists(path))
        {
            return "The document is missing from the pinned archive.";
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);

        return Judge(xmlConfCase.Type, Observe(bytes, new Uri(path).AbsoluteUri, options));
    }
}
