namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The error codes of <c>format-number</c>: FODF1280 and FODF1310 of Functions and Operators 3.1 (4.7.2 and 4.7.3), and XTDE1280 and
/// XTDE1310 of XSLT 2.0 (16.4 and 16.4.2) for the XSLT 2.0 host mode. They are defined here because the table of the error catalog covers
/// the codes of XPath 2.0 and of Functions and Operators 1.0 only.
/// </summary>
internal static class FormatNumberErrors
{
    internal static readonly XPathErrorCode FODF1280 = new("FODF1280");
    internal static readonly XPathErrorCode FODF1310 = new("FODF1310");
    internal static readonly XPathErrorCode XTDE1280 = new("XTDE1280");
    internal static readonly XPathErrorCode XTDE1310 = new("XTDE1310");

    /// <summary>Creates the error for a decimal format name that is invalid or names no decimal format.</summary>
    /// <param name="xslt20">Whether the XSLT 2.0 code applies.</param>
    /// <param name="name">The name as written.</param>
    /// <returns>The exception to throw.</returns>
    public static XPathException UnknownFormat(bool xslt20, string name) =>
        Create(
            xslt20 ? XTDE1280 : FODF1280,
            "The decimal format name '" + name + "' is not a valid name or names no decimal format."
        );

    /// <summary>Creates the error for a picture string, or a decimal format, that breaks a rule.</summary>
    /// <param name="xslt20">Whether the XSLT 2.0 code applies.</param>
    /// <param name="problem">The rule that is broken.</param>
    /// <returns>The exception to throw.</returns>
    public static XPathException InvalidPicture(bool xslt20, string? problem) =>
        Create(xslt20 ? XTDE1310 : FODF1310, problem ?? "The picture string is not valid.");

    private static XPathException Create(XPathErrorCode code, string message) =>
        new(new XPathDiagnostic(code, XPathErrorCodes.KindOf(code), message, -1, 0));
}
