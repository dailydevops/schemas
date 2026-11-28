namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage4Globals;

using System.Buffers;
using System.Text;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>Validates the committed documents of the own stage 4 schemas and compares the fired rules with the expected rules.</summary>
/// <remarks>
/// <para>
/// The golden harness fetches documents through pinned suite manifests only (decision 3 of #215), so the committed own documents cannot
/// pass through it. This runner keeps its observation model (<see cref="GoldenObservation"/>) and its comparison: the rules are compared as
/// a set of ids, because a rule fires once per matching context node, and the validity must match too. One <see cref="CaseResult"/> is
/// recorded per document, with the schema name as test set, so <c>ConformanceReport.Compare</c> yields the pass rate per own schema.
/// </para>
/// <para>
/// A rule fires when the SVRL output holds an <c>svrl:fired-rule</c> with its id; a document is invalid when the output holds a failed
/// assertion or a successful report. A schema that does not build or an <c>svrl:error</c> in the output fails the case with its reason.
/// </para>
/// </remarks>
internal static class Stage4OwnSchemaRunner
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Runs every document of every own schema.</summary>
    /// <param name="manifest">The stage record.</param>
    /// <param name="stageDirectory">The directory that holds the schemas and the <c>documents</c> folder.</param>
    /// <param name="repositoryRoot">The repository root the schema paths are relative to.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per document in manifest order; the case id is <c>&lt;schema name&gt;/&lt;document&gt;</c>.</returns>
    public static IReadOnlyList<CaseResult> Run(
        Stage4Manifest manifest,
        string stageDirectory,
        string repositoryRoot,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(stageDirectory);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var results = new List<CaseResult>();
        foreach (var schema in manifest.OwnSchemas)
        {
            var schemaPath = Path.Combine(repositoryRoot, schema.Path);
            var name = Path.GetFileNameWithoutExtension(schema.Path);
            var schemaBytes = File.ReadAllBytes(schemaPath);
            foreach (var document in schema.Documents)
            {
                var id = $"{name}/{Path.GetFileName(document.Path)}";
                var testSet = "stage4-" + name;
                var instancePath = Path.Combine(stageDirectory, "documents", document.Path);
                var instance = File.ReadAllBytes(instancePath);
                var (observation, problem) = Validate(
                    schemaBytes,
                    schemaPath,
                    instance,
                    instancePath,
                    document,
                    cancellationToken
                );
                var reason = problem ?? Compare(document, observation!);
                results.Add(
                    reason is null
                        ? new CaseResult(id, testSet, CaseOutcome.Pass)
                        : new CaseResult(id, testSet, CaseOutcome.Fail, reason)
                );
            }
        }

        return results;
    }

    private static (GoldenObservation? Observation, string? Problem) Validate(
        byte[] schema,
        string schemaPath,
        byte[] instance,
        string instancePath,
        Stage4Document document,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new ArrayBufferWriter<byte>();
        try
        {
            var builder = SchemaValidator.ForSchematron(schema, new Uri(schemaPath).AbsoluteUri);
            foreach (var (name, value) in document.Parameters)
            {
                _ = builder.WithParameter(name, value);
            }

            var validator = builder.Build();
            using var sink = new SvrlReportSink(output);
            var result = validator
                .WithReportSink(sink)
                .Validate(instance, new Uri(instancePath).AbsoluteUri, cancellationToken);
            if (result.IsTruncated)
            {
                return (null, "The validation was truncated.");
            }
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return (null, $"{exception.GetType().Name}: {exception.Message}");
        }

        var svrl = XDocument.Parse(Utf8.GetString(output.WrittenSpan));
        var errors = svrl.Descendants(Svrl + "error").Select(e => e.ToString(SaveOptions.DisableFormatting)).ToList();
        if (errors.Count > 0)
        {
            return (null, "The SVRL holds an svrl:error: " + string.Join(" ", errors));
        }

        var fired = svrl.Descendants(Svrl + "fired-rule")
            .Select(e => (string?)e.Attribute("id") ?? string.Empty)
            .ToList();
        var isValid = !svrl.Descendants()
            .Any(e => e.Name == Svrl + "failed-assert" || e.Name == Svrl + "successful-report");
        return (new GoldenObservation(isValid, fired), null);
    }

    private static string? Compare(Stage4Document document, GoldenObservation observed)
    {
        var expected = new HashSet<string>(document.ExpectedRuleIds, StringComparer.Ordinal);
        var fired = new HashSet<string>(
            observed.FiredRuleIds.Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.Ordinal
        );
        var parts = new List<string>();
        if (observed.IsValid != document.ExpectedValid)
        {
            parts.Add(
                $"Expected {(document.ExpectedValid ? "valid" : "invalid")} but observed {(observed.IsValid ? "valid" : "invalid")}."
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
