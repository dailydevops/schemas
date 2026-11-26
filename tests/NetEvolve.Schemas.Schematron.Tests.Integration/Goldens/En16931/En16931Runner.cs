namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

using System.Buffers;
using System.Text;
using System.Xml.Linq;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Svrl;

/// <summary>Validates the cases of one syntax with the CEN EN 16931 rules and compares the fired rules with the expected rules.</summary>
/// <remarks>
/// A rule fires when the SVRL output holds a <c>svrl:failed-assert</c> or a <c>svrl:successful-report</c> with its id; the CEN rules
/// carry their ids on the assertions. A document is invalid when such an assertion has the flag <c>fatal</c>. A schema that does not
/// build, or an <c>svrl:error</c> in the output, fails the case with its reason.
/// </remarks>
internal static class En16931Runner
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Validates every case with the schema.</summary>
    /// <param name="schemaPath">The full path of the validation schema; its includes are resolved relative to it.</param>
    /// <param name="cases">The cases of one syntax.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <param name="globalPatternLets"><see langword="true"/> to make the pattern-level variables visible from every pattern.</param>
    /// <param name="testSet">The test set of the results; the syntax of the case when <see langword="null"/>.</param>
    /// <returns>One result per case in case order.</returns>
    public static IReadOnlyList<CaseResult> Run(
        string schemaPath,
        IReadOnlyList<En16931Case> cases,
        CancellationToken cancellationToken,
        bool globalPatternLets = false,
        string? testSet = null
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNullOrWhiteSpace(schemaPath);
        ArgumentNullException.ThrowIfNull(cases);
        var validator = Build(schemaPath, globalPatternLets);
        var results = new List<CaseResult>(cases.Count);
        foreach (var testCase in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (fired, fatal, problem) = Validate(validator, schemaPath, testCase, cancellationToken);
            var reason = problem ?? Compare(testCase, fired!, fatal);
            results.Add(
                reason is null
                    ? new CaseResult(testCase.Id, testSet ?? testCase.Syntax, CaseOutcome.Pass)
                    : new CaseResult(testCase.Id, testSet ?? testCase.Syntax, CaseOutcome.Fail, reason)
            );
        }

        return results;
    }

    /// <summary>Builds the validator of a schema, which expands its abstract patterns and reads its includes.</summary>
    /// <param name="schemaPath">The full path of the schema.</param>
    /// <param name="globalPatternLets"><see langword="true"/> to make the pattern-level variables visible from every pattern.</param>
    /// <returns>The validator.</returns>
    public static ISchematronValidator Build(string schemaPath, bool globalPatternLets = false)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(schemaPath);
        return SchemaValidator
            .ForSchematron(File.ReadAllBytes(schemaPath), new Uri(schemaPath).AbsoluteUri)
            .WithResolver(new SandboxedFileSystemResourceResolver(Path.GetDirectoryName(schemaPath)!))
            .WithGlobalPatternLets(globalPatternLets)
            .Build();
    }

    private static (HashSet<string>? Fired, HashSet<string> Fatal, string? Problem) Validate(
        ISchematronValidator validator,
        string schemaPath,
        En16931Case testCase,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new ArrayBufferWriter<byte>();
        try
        {
            using var sink = new SvrlReportSink(output);
            var result = validator
                .WithReportSink(sink)
                .Validate(
                    testCase.Document,
                    new Uri(Path.Combine(Path.GetDirectoryName(schemaPath)!, testCase.Id)).AbsoluteUri,
                    cancellationToken
                );
            if (result.IsTruncated)
            {
                return (null, [], "The validation was truncated.");
            }
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return (null, [], $"{exception.GetType().Name}: {exception.Message}");
        }

        var svrl = XDocument.Parse(Utf8.GetString(output.WrittenSpan));
        var errors = svrl.Descendants(Svrl + "error").Select(e => e.ToString(SaveOptions.DisableFormatting)).ToList();
        if (errors.Count > 0)
        {
            return (null, [], "The SVRL holds an svrl:error: " + string.Join(" ", errors));
        }

        var failed = svrl.Descendants()
            .Where(e => e.Name == Svrl + "failed-assert" || e.Name == Svrl + "successful-report")
            .ToList();
        var fired = new HashSet<string>(
            failed.Select(e => (string?)e.Attribute("id") ?? string.Empty).Where(id => id.Length > 0),
            StringComparer.Ordinal
        );
        var fatal = new HashSet<string>(
            failed
                .Where(e => !string.Equals((string?)e.Attribute("flag"), "warning", StringComparison.Ordinal))
                .Select(e => (string?)e.Attribute("id") ?? "(no id)"),
            StringComparer.Ordinal
        );
        return (fired, fatal, null);
    }

    private static string? Compare(En16931Case testCase, HashSet<string> fired, HashSet<string> fatal)
    {
        if (testCase.IsExample)
        {
            return fatal.Count == 0
                ? null
                : "Expected valid but observed invalid: "
                    + string.Join(", ", fatal.OrderBy(id => id, StringComparer.Ordinal))
                    + ".";
        }

        var relevant = new HashSet<string>(fired.Where(testCase.Scope.Contains), StringComparer.Ordinal);
        var expected = new HashSet<string>(testCase.ExpectedRuleIds, StringComparer.Ordinal);
        var parts = new List<string>();
        parts.AddRange(
            expected
                .Except(relevant)
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => $"Missing expected rule '{id}'.")
        );
        parts.AddRange(
            relevant
                .Except(expected)
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => $"Unexpected fired rule '{id}'.")
        );
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }
}
