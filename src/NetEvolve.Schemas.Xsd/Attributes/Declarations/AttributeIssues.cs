namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Looks the rule of a code up in the catalog and reports a violation of it.</summary>
internal static class AttributeIssues
{
    internal static void Report(
        XsdIssueReporter reporter,
        DiagnosticCode code,
        DocumentLocation location,
        string message
    )
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == code)
            {
                reporter.Report(rule, location, message);
                return;
            }
        }
    }
}
