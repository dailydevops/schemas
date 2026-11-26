namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NetEvolve.Arguments;

/// <summary>The findings of one SVRL document, read in document order.</summary>
/// <remarks>
/// A finding belongs to the stage of the <c>svrl:active-pattern</c> that precedes it; the eForms patterns name their stage, for example
/// <c>EFORMS-validation-stage-2a-1</c>. An <c>svrl:error</c> is kept apart, because it is no finding of the rule set.
/// </remarks>
internal sealed partial class EformsReport
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    private EformsReport(
        string? phase,
        IReadOnlyList<string> activePatterns,
        IReadOnlyList<EformsFinding> findings,
        IReadOnlyList<string> errors
    )
    {
        Phase = phase;
        ActivePatterns = activePatterns;
        Findings = findings;
        Errors = errors;
    }

    /// <summary>Gets the phase of the report, or <see langword="null"/> when it names none.</summary>
    public string? Phase { get; }

    /// <summary>Gets the ids of the active patterns in document order.</summary>
    public IReadOnlyList<string> ActivePatterns { get; }

    /// <summary>Gets the failed assertions and successful reports in document order.</summary>
    public IReadOnlyList<EformsFinding> Findings { get; }

    /// <summary>Gets the serialized <c>svrl:error</c> elements.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Reads an SVRL document.</summary>
    /// <param name="svrl">The SVRL text.</param>
    /// <returns>The report.</returns>
    public static EformsReport Parse(string svrl)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(svrl);
        var root = XDocument.Parse(svrl).Root!;
        var findings = new List<EformsFinding>();
        var errors = new List<string>();
        var stage = 0;
        var pattern = string.Empty;
        var patterns = new List<string>();
        var prefixes = root.Elements(Svrl + "ns-prefix-in-attribute-values")
            .GroupBy(e => (string?)e.Attribute("uri") ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (string?)g.First().Attribute("prefix") ?? string.Empty,
                StringComparer.Ordinal
            );
        foreach (var element in root.Elements())
        {
            if (element.Name == Svrl + "active-pattern")
            {
                pattern = (string?)element.Attribute("id") ?? string.Empty;
                patterns.Add(pattern);
                stage = StageOf(pattern);
            }
            else if (element.Name == Svrl + "error")
            {
                errors.Add(element.ToString(SaveOptions.DisableFormatting));
            }
            else if (element.Name == Svrl + "failed-assert" || element.Name == Svrl + "successful-report")
            {
                findings.Add(
                    new EformsFinding(
                        pattern,
                        stage,
                        (string?)element.Attribute("id") ?? string.Empty,
                        (string?)element.Attribute("role") ?? string.Empty,
                        EformsLocation.Normalize((string?)element.Attribute("location") ?? string.Empty, prefixes),
                        Collapse(element.Element(Svrl + "text")?.Value ?? string.Empty)
                    )
                );
            }
        }

        return new EformsReport((string?)root.Attribute("phase"), patterns, findings, errors);
    }

    /// <summary>Gets a copy that keeps only the findings of the given patterns.</summary>
    /// <param name="patterns">The ids of the patterns to keep.</param>
    /// <returns>The restricted report.</returns>
    public EformsReport RestrictTo(IReadOnlyCollection<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        return new EformsReport(
            Phase,
            [.. ActivePatterns.Where(patterns.Contains)],
            [.. Findings.Where(f => patterns.Contains(f.Pattern))],
            Errors
        );
    }

    private static int StageOf(string? patternId)
    {
        var match = patternId is null ? null : StagePattern().Match(patternId);
        return match is { Success: true } ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
    }

    private static string Collapse(string text) => WhiteSpace().Replace(text, " ").Trim();

    [GeneratedRegex(@"stage-(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex StagePattern();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhiteSpace();
}
