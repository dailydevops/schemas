namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The error codes of the XPath 3.0 and 3.1 function delta: the table of the library follows Functions and Operators 1.0 and holds
/// none of them, and the table is outside the work package, so the codes are defined here, like those of the text functions. A code
/// of the standard namespace that starts with <c>FO</c> is a function error to the library.
/// </summary>
internal static class Delta3Errors
{
    /// <summary>The string passed to <c>fn:parse-xml</c> or <c>fn:parse-xml-fragment</c> is not a well-formed document or fragment, or breaks a limit or the secure reader defaults.</summary>
    internal static readonly XPathErrorCode FODC0006 = new("FODC0006");

    /// <summary>Builds the exception for <see cref="FODC0006"/>.</summary>
    /// <returns>The exception to throw.</returns>
    internal static XPathException InvalidXml() =>
        new(
            new XPathDiagnostic(
                FODC0006,
                XPathErrorKind.Function,
                "The string is not a well-formed XML document or fragment.",
                -1,
                0
            )
        );
}
