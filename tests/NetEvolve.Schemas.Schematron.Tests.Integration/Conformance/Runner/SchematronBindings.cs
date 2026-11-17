namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The query bindings of the pinned suite and which of them are selected.</summary>
internal static class SchematronBindings
{
    private const string Xslt3Reason = "Cases of the xslt3 binding are owned by SCH-21.";

    /// <summary>
    /// The ISO Schematron bindings of the suite. The ids are the suite file names without the
    /// <c>iso-schematron-</c> prefix. xslt-1 and xslt-2 are selected; both run on the XPath 2.0 grammar (#137).
    /// </summary>
    public static IReadOnlyList<BindingRule> Iso { get; } =
    [
        new("xslt-1", "iso-schematron-xslt-1.xml"),
        new("xslt-2", "iso-schematron-xslt-2.xml"),
        new("xslt-3", "iso-schematron-xslt-3.xml", Xslt3Reason),
        new("xslt-3-2025", "iso-schematron-xslt-3-2025.xml", Xslt3Reason),
    ];
}
