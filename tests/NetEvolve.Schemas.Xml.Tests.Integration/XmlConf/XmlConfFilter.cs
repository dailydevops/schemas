namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>Selects the cases of XML 1.0 Fifth Edition and Namespaces in XML 1.0.</summary>
internal static class XmlConfFilter
{
    private const string Xml11Reason = "XML 1.1 case; XML 1.1 cases belong to DEF-50 (#62).";
    private const string Namespaces11Reason = "Namespaces in XML 1.1 case; these cases belong to DEF-51 (#63).";
    private const string EditionReason =
        "Applies only to XML 1.0 editions one to four; the library follows the Fifth Edition name rules.";

    /// <summary>Drops cases for XML 1.1, Namespaces in XML 1.1, and for editions one to four only.</summary>
    /// <param name="cases">The catalog.</param>
    /// <returns>The selection; the order of the catalog is kept.</returns>
    public static XmlConfSelection Select(IReadOnlyList<XmlConfCase> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);
        var selected = new List<XmlConfCase>();
        var excluded = new List<ExcludedCase>();
        foreach (var xmlConfCase in cases)
        {
            if (ExclusionReason(xmlConfCase) is { } reason)
            {
                excluded.Add(new ExcludedCase(xmlConfCase, reason));
            }
            else
            {
                selected.Add(xmlConfCase);
            }
        }

        return new XmlConfSelection(selected, excluded);
    }

    private static string? ExclusionReason(XmlConfCase xmlConfCase)
    {
        if (xmlConfCase.Recommendation.StartsWith("NS1.1", StringComparison.Ordinal))
        {
            return Namespaces11Reason;
        }

        if (
            xmlConfCase.Recommendation.StartsWith("XML1.1", StringComparison.Ordinal)
            || (xmlConfCase.Versions.Count > 0 && !xmlConfCase.Versions.Contains("1.0", StringComparer.Ordinal))
        )
        {
            return Xml11Reason;
        }

        return xmlConfCase.Editions.Count > 0 && !xmlConfCase.Editions.Contains("5", StringComparer.Ordinal)
            ? EditionReason
            : null;
    }
}
