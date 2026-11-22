namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>Runs one document through the validating reader mode and names what it reports, so a constraint test reads as one line.</summary>
internal static class ConstraintHarness
{
    internal const string DocumentUri = LoaderHarness.DocumentUri;

    /// <summary>A document with an empty root element whose declarations live in the external subset <c>ext.dtd</c>.</summary>
    internal const string ExternalSubsetDocument = "<!DOCTYPE r SYSTEM \"ext.dtd\"><r/>";

    private const string BaseUri = "http://example.org/doc/";

    /// <summary>A document whose internal subset holds the declarations, followed by the content.</summary>
    internal static string Doc(string declarations, string content) => "<!DOCTYPE r [" + declarations + "]>" + content;

    /// <summary>Runs a document whose external subset <c>ext.dtd</c> and external entities come from the resources.</summary>
    /// <param name="xml">The document.</param>
    /// <param name="resources">The served resources as pairs of a file name next to the document and its text.</param>
    internal static Outcome Run(string xml, params (string Name, string Text)[] resources)
    {
        if (resources.Length == 0)
        {
            return Run(xml, DtdValidationOptions.Default);
        }

        var resolver = new RecordingResolver();
        foreach (var (name, text) in resources)
        {
            _ = resolver.Add(BaseUri + name, LoaderHarness.Utf8(text));
        }

        return Run(xml, LoaderHarness.Open(resolver));
    }

    /// <summary>Asserts that the document is not well formed and that the fatal status carries the code.</summary>
    internal static async Task ReportsFatal(string xml, string alias, params (string Name, string Text)[] resources)
    {
        var outcome = Run(xml, resources);

        await Assert.That(outcome.Fatal).IsEqualTo(alias);
    }

    /// <summary>Asserts that the document is well formed; validity issues are not looked at.</summary>
    internal static async Task IsWellFormed(string xml, params (string Name, string Text)[] resources)
    {
        var outcome = Run(xml, resources);

        await Assert.That(outcome.Fatal).IsNull();
    }

    /// <summary>Asserts that the document is well formed and that its validity errors are exactly the one code.</summary>
    internal static async Task ReportsValidity(string xml, string alias, params (string Name, string Text)[] resources)
    {
        var outcome = Run(xml, resources);

        await Assert.That(outcome.Fatal).IsNull();
        await Assert.That(outcome.Codes).IsEqualTo(alias);
    }

    /// <summary>Asserts that the document is well formed and valid: no fatal status and no validity error.</summary>
    internal static async Task IsValid(string xml, params (string Name, string Text)[] resources)
    {
        var outcome = Run(xml, resources);

        await Assert.That(outcome.Fatal).IsNull();
        await Assert.That(outcome.Codes).IsEqualTo(string.Empty);
    }

    /// <summary>Asserts that the document is well formed and that the run does not report the code, for a gap.</summary>
    internal static async Task DoesNotReport(string xml, string alias, params (string Name, string Text)[] resources)
    {
        var outcome = Run(xml, resources);

        await Assert.That(outcome.Fatal).IsNull();
        await Assert.That(outcome.Validity).DoesNotContain(alias);
    }

    private static Outcome Run(string xml, DtdValidationOptions options)
    {
        var result = DtdValidatingPipeline.Validate(xml.AsSpan(), options, documentUri: DocumentUri);
        var fatal = result.IsWellFormed ? null : result.Diagnostic?.Code.Alias ?? result.Status.ToString();
        var validity = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var issue in result.Issues.Issues)
        {
            if (issue.Severity == IssueSeverity.Error && !IsFatalIssue(issue, result))
            {
                _ = validity.Add(Alias(issue.Code));
            }
        }

        return new Outcome(fatal, [.. validity]);
    }

    // A fatal status is also the last issue of the result (decision 6 of #327); it is not a validity error.
    private static bool IsFatalIssue(ValidationIssue issue, DtdPipelineResult result) =>
        !result.IsWellFormed && result.Diagnostic is { } diagnostic && issue.Code == diagnostic.Code.Code;

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

    /// <summary>What a run reported: the fatal well-formedness code, if any, and the distinct validity error codes in order.</summary>
    /// <param name="Fatal">The alias of the fatal status, or <see langword="null"/> when the document is well formed.</param>
    /// <param name="Validity">The distinct aliases of the validity errors, ordered by code.</param>
    internal readonly record struct Outcome(string? Fatal, string[] Validity)
    {
        /// <summary>Gets the validity errors as one string, empty when there are none.</summary>
        internal string Codes => string.Join(",", Validity);

        /// <inheritdoc />
        public override string ToString() => Fatal is null ? "well-formed[" + Codes + "]" : "fatal " + Fatal;
    }
}
