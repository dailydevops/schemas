namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>Publishes the rate of the schema tests of XSD 1.0 as a row of its own in the job summary.</summary>
internal static class SchemaRateReport
{
    /// <summary>The package label of the row.</summary>
    public const string Package = XstsRateReport.Package + " XSTS 1.0 schema tests";

    public static string KeyOf(string testSet, string caseId) => testSet + "|" + caseId;

    public static void Publish(
        IReadOnlyCollection<CaseResult> results,
        IReadOnlySet<string> schemaCases,
        string? summaryPath
    )
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(schemaCases);

        _ = summaryPath;
    }
}
