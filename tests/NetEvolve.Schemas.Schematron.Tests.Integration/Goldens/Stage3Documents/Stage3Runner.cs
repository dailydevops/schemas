namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

using System.Buffers;
using System.Text;
using System.Xml.Linq;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Svrl;

/// <summary>Validates the sample invoices of the stage 3 rule sets and compares the failed assertions with the expected rules.</summary>
/// <remarks>
/// An assertion fires when the SVRL output holds a <c>svrl:failed-assert</c> or a <c>svrl:successful-report</c> with its id. An invoice
/// is invalid when such an assertion does not carry the flag <c>warning</c>. A schema that does not build, or an <c>svrl:error</c> in the
/// output, fails the case with its reason.
/// </remarks>
internal static class Stage3Runner
{
    /// <summary>The test set of the default resolver cases.</summary>
    public const string DefaultResolverTestSet = "stage3-default-resolver";

    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Validates every sample invoice of every rule set.</summary>
    /// <param name="manifest">The stage record.</param>
    /// <param name="suiteRoot">The root of the fetched suite.</param>
    /// <param name="resolverFactory">Creates the resolver of a rule set from the directory of its schema; <see langword="null"/> keeps the default resolver.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per invoice in manifest order; the case id is <c>&lt;rule set name&gt;/&lt;file name&gt;</c>.</returns>
    public static IReadOnlyList<CaseResult> Run(
        Stage3Manifest manifest,
        string suiteRoot,
        Func<string, IResourceResolver>? resolverFactory,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(suiteRoot);
        var results = new List<CaseResult>();
        foreach (var ruleSet in manifest.RuleSets)
        {
            var schemaPath = Path.Combine(suiteRoot, ruleSet.Schema);
            var testSet = "stage3-" + ruleSet.Name;
            foreach (var document in ruleSet.Documents)
            {
                var id = $"{ruleSet.Name}/{Path.GetFileName(document.Document)}";
                var (rules, valid, problem) = Validate(
                    schemaPath,
                    Path.Combine(suiteRoot, document.Document),
                    resolverFactory,
                    cancellationToken
                );
                var reason = problem ?? Compare(document, rules!, valid);
                results.Add(
                    reason is null
                        ? new CaseResult(id, testSet, CaseOutcome.Pass)
                        : new CaseResult(id, testSet, CaseOutcome.Fail, reason)
                );
            }
        }

        return results;
    }

    /// <summary>Validates the first invoice of every rule set with the default resolver, which must deny the code-database lookup.</summary>
    /// <param name="manifest">The stage record.</param>
    /// <param name="suiteRoot">The root of the fetched suite.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per rule set; the case id is <c>default-resolver/&lt;rule set name&gt;</c>.</returns>
    /// <remarks>A case passes when the validation fails and the failure carries the code of a denied resource.</remarks>
    public static IReadOnlyList<CaseResult> RunDefaultResolver(
        Stage3Manifest manifest,
        string suiteRoot,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(suiteRoot);
        var code = CoreDiagnosticCodes.ResourceDenied.ToString();
        var results = new List<CaseResult>();
        foreach (var ruleSet in manifest.RuleSets)
        {
            var (_, failure) = ValidateRaw(
                Path.Combine(suiteRoot, ruleSet.Schema),
                Path.Combine(suiteRoot, ruleSet.Documents[0].Document),
                resolver: null,
                cancellationToken
            );
            var message = failure is null
                ? "The validation succeeded."
                : $"{failure.GetType().Name}: {failure.Message}";
            var id = DefaultResolverTestSet + "/" + ruleSet.Name;
            results.Add(
                failure is not null && message.Contains(code, StringComparison.Ordinal)
                    ? new CaseResult(id, DefaultResolverTestSet, CaseOutcome.Pass)
                    : new CaseResult(
                        id,
                        DefaultResolverTestSet,
                        CaseOutcome.Fail,
                        $"No denial with code {code}: {message}"
                    )
            );
        }

        return results;
    }

    /// <summary>Validates one invoice and returns the raw SVRL text, or the exception that the validation threw.</summary>
    /// <param name="schemaPath">The full path of the schema.</param>
    /// <param name="documentPath">The full path of the invoice.</param>
    /// <param name="resolver">The resolver, or <see langword="null"/> for the default resolver.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The SVRL text or the exception.</returns>
    public static (string? Svrl, Exception? Failure) ValidateRaw(
        string schemaPath,
        string documentPath,
        IResourceResolver? resolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNullOrWhiteSpace(schemaPath);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(documentPath);
        var output = new ArrayBufferWriter<byte>();
        try
        {
            var builder = SchemaValidator.ForSchematron(File.ReadAllBytes(schemaPath), new Uri(schemaPath).AbsoluteUri);
            if (resolver is not null)
            {
                _ = builder.WithResolver(resolver);
            }

            using var sink = new SvrlReportSink(output);
            _ = builder
                .Build()
                .WithReportSink(sink)
                .Validate(File.ReadAllBytes(documentPath), new Uri(documentPath).AbsoluteUri, cancellationToken);
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return (null, exception);
        }

        return (Utf8.GetString(output.WrittenSpan), null);
    }

    private static (HashSet<string>? Rules, bool Valid, string? Problem) Validate(
        string schemaPath,
        string documentPath,
        Func<string, IResourceResolver>? resolverFactory,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = resolverFactory?.Invoke(Path.GetDirectoryName(schemaPath)!);
        var (text, failure) = ValidateRaw(schemaPath, documentPath, resolver, cancellationToken);
        if (failure is not null)
        {
            return (null, false, $"{failure.GetType().Name}: {failure.Message}");
        }

        var svrl = XDocument.Parse(text!);
        var errors = svrl.Descendants(Svrl + "error").Select(e => e.ToString(SaveOptions.DisableFormatting)).ToList();
        if (errors.Count > 0)
        {
            return (null, false, "The SVRL holds an svrl:error: " + string.Join(" ", errors));
        }

        var failed = svrl.Descendants()
            .Where(e => e.Name == Svrl + "failed-assert" || e.Name == Svrl + "successful-report")
            .ToList();
        var rules = new HashSet<string>(
            failed.Select(e => (string?)e.Attribute("id") ?? "(no id)"),
            StringComparer.Ordinal
        );
        var valid = failed.All(e => string.Equals((string?)e.Attribute("flag"), "warning", StringComparison.Ordinal));
        return (rules, valid, null);
    }

    private static string? Compare(Stage3Document document, HashSet<string> fired, bool valid)
    {
        var expected = new HashSet<string>(document.Rules, StringComparer.Ordinal);
        var parts = new List<string>();
        if (valid != document.Valid)
        {
            parts.Add(
                $"Expected {(document.Valid ? "valid" : "invalid")} but observed {(valid ? "valid" : "invalid")}."
            );
        }

        parts.AddRange(
            expected
                .Except(fired, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .Select(id => $"Missing expected rule '{id}'.")
        );
        parts.AddRange(
            fired
                .Except(expected, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .Select(id => $"Unexpected fired rule '{id}'.")
        );
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }
}
