namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>Publishes the rate of the schema tests of XSD 1.0 as a row of its own in the job summary; the row shows the counts only.</summary>
internal static class SchemaRateReport
{
    /// <summary>The package label of the row.</summary>
    public const string Package = XstsRateReport.Package + " XSTS 1.0 schema tests";

    /// <summary>Gets the key that identifies a case across test sets.</summary>
    /// <param name="testSet">The test set.</param>
    /// <param name="caseId">The case id.</param>
    /// <returns>The key.</returns>
    public static string KeyOf(string testSet, string caseId) => testSet + "|" + caseId;

    /// <summary>Publishes the rate of the schema tests among the results.</summary>
    /// <param name="results">The case results of the run.</param>
    /// <param name="schemaCases">The keys (see <see cref="KeyOf"/>) of the schema tests of XSD 1.0; other results are not counted.</param>
    /// <param name="summaryPath">The summary file path; nothing is written when it is <see langword="null"/> or empty.</param>
    /// <param name="package">The label of the row; the instance tests publish their own row through the same code.</param>
    public static void Publish(
        IReadOnlyCollection<CaseResult> results,
        IReadOnlySet<string> schemaCases,
        string? summaryPath,
        string package = Package
    )
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(schemaCases);

        if (string.IsNullOrEmpty(summaryPath))
        {
            return;
        }

        var selected = results.Where(r => schemaCases.Contains(KeyOf(r.TestSet, r.CaseId))).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        foreach (var result in selected)
        {
            if (!baselines.ContainsKey(result.TestSet))
            {
                baselines.Add(result.TestSet, BaselineFiles.Load(XstsSuite.BaselineDirectory, result.TestSet));
            }
        }

        ConformanceRateReporter.Append(
            package,
            TargetFrameworkName.Of(typeof(SchemaRateReport).Assembly),
            ConformanceReport.Compare(selected, baselines),
            summaryPath,
            // The row of the whole suite lists the failed cases; this row repeats a part of them.
            listCases: false
        );
    }
}
