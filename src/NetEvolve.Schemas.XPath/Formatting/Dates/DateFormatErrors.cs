namespace NetEvolve.Schemas.XPath.Formatting.Dates;

using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The error codes of <c>format-date</c>, <c>format-dateTime</c>, and <c>format-time</c>: FOFD1340 and FOFD1350 of Functions and
/// Operators 3.1 (9.8.4), and XTDE1340 and XTDE1350 of XSLT 2.0 (16.5) for the XSLT 2.0 host mode. They are defined here because the
/// table of the error catalog covers the codes of XPath 2.0 and of Functions and Operators 1.0 only.
/// </summary>
internal static class DateFormatErrors
{
    internal static readonly XPathErrorCode FOFD1340 = new("FOFD1340");
    internal static readonly XPathErrorCode FOFD1350 = new("FOFD1350");
    internal static readonly XPathErrorCode XTDE1340 = new("XTDE1340");
    internal static readonly XPathErrorCode XTDE1350 = new("XTDE1350");

    /// <summary>Creates the error for a picture string, or a calendar, that breaks a rule.</summary>
    /// <param name="xslt20">Whether the XSLT 2.0 code applies.</param>
    /// <param name="problem">The rule that is broken.</param>
    /// <returns>The exception to throw.</returns>
    public static XPathException InvalidPicture(bool xslt20, string problem) =>
        Create(xslt20 ? XTDE1340 : FOFD1340, problem);

    /// <summary>Creates the error for a component that the value does not have.</summary>
    /// <param name="xslt20">Whether the XSLT 2.0 code applies.</param>
    /// <param name="component">The component specifier.</param>
    /// <param name="type">The name of the type of the value.</param>
    /// <returns>The exception to throw.</returns>
    public static XPathException ComponentNotAvailable(bool xslt20, char component, string type) =>
        Create(
            xslt20 ? XTDE1350 : FOFD1350,
            "The component '" + component + "' is not available for a value of type " + type + "."
        );

    private static XPathException Create(XPathErrorCode code, string message) =>
        new(new XPathDiagnostic(code, XPathErrorCodes.KindOf(code), message, -1, 0));
}
