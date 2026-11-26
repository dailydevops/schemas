namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using System.Buffers;
using System.Text;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Svrl;

/// <summary>Runs the selected expectations on the Schematron validator of the public entry point.</summary>
/// <remarks>
/// A schema, a phase, and an instance are validated once, however many expectations check the SVRL output; the schema resolves its includes
/// below the root of the XSpec project and nowhere else. A validation that does not finish (a schema that does not compile, an unexpected
/// exception, a truncated run) fails every expectation that depends on it, with its reason.
/// </remarks>
internal static class XspecEngine
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Runs the selection.</summary>
    /// <param name="selection">The selection; excluded parts become skipped results with their reason.</param>
    /// <param name="suiteRoot">The root directory of the XSpec project; schemas resolve their includes below it.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per expectation and per excluded part.</returns>
    public static IReadOnlyList<CaseResult> Run(
        XspecSelection selection,
        string suiteRoot,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        var resolver = new SandboxedFileSystemResourceResolver(suiteRoot);
        var validations = new Dictionary<(string Schema, string? Phase, string Context), Validation>();
        var results = new List<CaseResult>(selection.Selected.Count + selection.Excluded.Count);
        foreach (var xspecCase in selection.Selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = (
                xspecCase.SchemaPath,
                xspecCase.Phase,
                xspecCase.Context.Path ?? "inline:" + xspecCase.Context.Xml
            );
            if (!validations.TryGetValue(key, out var validation))
            {
                validation = Validate(xspecCase, resolver, cancellationToken);
                validations.Add(key, validation);
            }

            var reason = validation.Problem
                ?? XspecJudge.Check(xspecCase.Expectation, validation.Svrl!, validation.Locator!);
            results.Add(
                new CaseResult(
                    xspecCase.Id,
                    xspecCase.TestSet,
                    reason is null ? CaseOutcome.Pass : CaseOutcome.Fail,
                    reason
                )
            );
        }

        foreach (var (id, testSet, reason) in selection.Excluded)
        {
            results.Add(new CaseResult(id, testSet, CaseOutcome.Skipped, reason));
        }

        return results;
    }

    private static Validation Validate(
        XspecCase xspecCase,
        SandboxedFileSystemResourceResolver resolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var instance = xspecCase.Context.Path is { } path
                ? File.ReadAllBytes(path)
                : Utf8.GetBytes(xspecCase.Context.Xml!);
            var instanceUri = new Uri(
                xspecCase.Context.Path ?? Path.Combine(Path.GetDirectoryName(xspecCase.SchemaPath)!, "context.xml")
            ).AbsoluteUri;
            var builder = SchemaValidator
                .ForSchematron(File.ReadAllBytes(xspecCase.SchemaPath), new Uri(xspecCase.SchemaPath).AbsoluteUri)
                .WithResolver(resolver);
            var validator = (xspecCase.Phase is { } phase ? builder.WithPhase(phase) : builder).Build();

            var output = new ArrayBufferWriter<byte>();
            using (var sink = new SvrlReportSink(output))
            {
                if (validator.WithReportSink(sink).Validate(instance, instanceUri, cancellationToken).IsTruncated)
                {
                    return new Validation(null, null, "The validation was truncated.");
                }
            }

            return new Validation(
                XDocument.Parse(Utf8.GetString(output.WrittenSpan)),
                XspecNodeLocator.Load(instance),
                null
            );
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return new Validation(null, null, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private sealed record Validation(XDocument? Svrl, XspecNodeLocator? Locator, string? Problem);
}
