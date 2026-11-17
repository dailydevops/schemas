namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Runs the selected cases through a reader and records one result per case.</summary>
internal static class XmlConfRunner
{
    /// <summary>Runs the selection.</summary>
    /// <param name="selection">The selection; excluded cases become skipped results with their reason.</param>
    /// <param name="reader">The reader seam.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per selected and excluded case.</returns>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        XmlConfSelection selection,
        IXmlConfReader reader,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(reader);
        var results = new List<CaseResult>(selection.Selected.Count + selection.Excluded.Count);
        foreach (var xmlConfCase in selection.Selected)
        {
            var observed = await reader.ReadAsync(xmlConfCase, cancellationToken);
            results.Add(
                Judge(xmlConfCase, observed) is { } failure
                    ? new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Fail, failure)
                    : new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Pass)
            );
        }

        foreach (var (xmlConfCase, reason) in selection.Excluded)
        {
            results.Add(new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Skipped, reason));
        }

        return results;
    }

    // Returns the failure reason, or null when the observation satisfies the case.
    // Limitation: a non-validating reader must accept an invalid case (validity errors belong to XML-60), and a
    // reader is not required to report an error case, so any real observation passes it.
    private static string? Judge(XmlConfCase xmlConfCase, XmlConfObservation observed) =>
        (xmlConfCase.Type, observed) switch
        {
            (_, XmlConfObservation.NotImplemented) => "No reader is bound.",
            (XmlConfType.NotWellFormed, XmlConfObservation.Accepted) =>
                "Expected a fatal error but the reader reported none.",
            (XmlConfType.Valid or XmlConfType.Invalid, XmlConfObservation.FatalError) =>
                "Expected no fatal error but the reader reported one.",
            _ => null,
        };
}
