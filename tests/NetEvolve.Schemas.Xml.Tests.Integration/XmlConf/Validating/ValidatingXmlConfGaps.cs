namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Validating;

/// <summary>
/// The valid and invalid cases of the suite that the validating reader mode does not pass or does not run, each with its
/// reason. The tables are the single source of the known gaps; the suite builds its baseline from <see cref="Known"/>.
/// </summary>
internal static class ValidatingXmlConfGaps
{
    private const string NamespacesOff =
        "Needs namespace processing switched off; the reader has no such mode. Also in the reader baseline of the suite.";

    private const string NameLength =
        "A name that exceeds the default MaxNameLength (XML1105); the reader cannot read the document. Also in the reader baseline of the suite.";

    private const string Version =
        "A version other than 1.0 (XML0010), legal in the Fifth Edition; the reader supports 1.0 only. Also in the reader baseline of the suite.";

    private const string Missing =
        "The document is missing from the pinned archive. Also in the reader baseline of the suite.";

    private static string Anomalies(string constraint) =>
        "The declaration parser records no parse-time anomaly (#608), so the constraint "
        + constraint
        + " is not reported.";

    private static string EntityReferences(string constraint) =>
        "The reader has no event for an entity reference (#647), so the constraint " + constraint + " is not reported.";

    /// <summary>
    /// The cases that are not run because the reader cannot read them; each is a skipped result. The rate counts them
    /// neither as passed nor as failed.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Excluded { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["eduni-errata-4e/x-ibm-1-0.5-valid-P04-ibm04v01.xml"] = NamespacesOff,
            ["eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v01.xml"] = NamespacesOff,
            ["eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v02.xml"] = NamespacesOff,
            ["eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v03.xml"] = NamespacesOff,
            ["eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v05.xml"] = NamespacesOff,
            ["eduni-errata-4e/x-rmt-008b"] = Version,
            ["eduni-namespaces-misc/hst-bh-005"] = Missing,
            ["eduni-namespaces-misc/hst-bh-006"] = Missing,
            ["ibm/ibm-valid-P85-ibm85v01.xml"] = NameLength,
            ["ibm/ibm-valid-P87-ibm87v01.xml"] = NameLength,
            ["oasis/o-p04pass1"] = NamespacesOff,
            ["oasis/o-p05pass1"] = NamespacesOff,
            ["oasis/o-p08pass1"] = NamespacesOff,
            ["xmltest/valid-sa-012"] = NamespacesOff,
        };

    /// <summary>The cases that run and fail: invalid documents for which the run reports no validity issue.</summary>
    public static IReadOnlyDictionary<string, string> Known { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["eduni-errata-2e/rmt-e2e-14"] = Anomalies("Proper Declaration/PE Nesting"),
            ["eduni-errata-2e/rmt-e2e-15a"] = EntityReferences("Element Valid for EMPTY content"),
            ["eduni-errata-3e/rmt-e3e-13"] = EntityReferences("Entity Declared"),
            ["eduni-namespaces-1.0/rmt-ns10-045"] =
                "ID values are checked against Name only (#925): a colon in an ID value is accepted.",
            ["eduni-namespaces-1.0/rmt-ns10-046"] =
                "ID values are checked against Name only (#925): a colon in an ID value is accepted.",
            ["ibm/ibm-invalid-P45-ibm45i01.xml"] = Anomalies("Unique Element Type Declaration"),
            ["ibm/ibm-invalid-P49-ibm49i01.xml"] = Anomalies("Proper Group/PE Nesting"),
            ["ibm/ibm-invalid-P50-ibm50i01.xml"] = Anomalies("Proper Group/PE Nesting"),
            ["ibm/ibm-invalid-P51-ibm51i01.xml"] = Anomalies("Proper Group/PE Nesting"),
            ["sun/el04"] = Anomalies("Unique Element Type Declaration"),
            ["sun/inv-not-sa05"] =
                "The standalone normalization clause skips enumerated attributes (#924), so Standalone Document Declaration is not reported.",
            ["sun/inv-not-sa06"] =
                "The standalone normalization clause skips notation attributes (#924), so Standalone Document Declaration is not reported.",
            ["xmltest/invalid--002"] = Anomalies("Proper Group/PE Nesting"),
            ["xmltest/invalid--005"] = Anomalies("Proper Declaration/PE Nesting"),
            ["xmltest/invalid--006"] = Anomalies("Proper Declaration/PE Nesting"),
            ["xmltest/invalid-not-sa-022"] = Anomalies("Proper Conditional Section/PE Nesting"),
        };
}
