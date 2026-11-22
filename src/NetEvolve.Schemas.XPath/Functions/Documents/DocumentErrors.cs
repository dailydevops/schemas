namespace NetEvolve.Schemas.XPath.Functions.Documents;

using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The error codes of the text functions (Functions and Operators 3.1, 14.9): the table of the library follows Functions and Operators
/// 1.0 and holds none of them, and the table is outside the work package, so the codes are defined here. A code of the standard
/// namespace that starts with <c>FO</c> is a function error to the library, so a host sees them as errors of the specifications.
/// </summary>
/// <remarks>
/// Limitation: <see cref="FOUT1200"/> is raised for a character that is no XML character only. A resource that cannot be decoded with the
/// encoding, whether named or inferred, is <see cref="FOUT1190"/>, which every case of the test sets that names <c>FOUT1200</c> accepts too.
/// </remarks>
internal static class DocumentErrors
{
    /// <summary>The resource cannot be retrieved: the reference is no URI reference or has a fragment identifier, it is relative against no base, no resolver gives it, or it is too large.</summary>
    internal static readonly XPathErrorCode FOUT1170 = new("FOUT1170");

    /// <summary>The encoding is invalid or not supported, or the resource cannot be decoded with it.</summary>
    internal static readonly XPathErrorCode FOUT1190 = new("FOUT1190");

    /// <summary>The decoded resource contains a character that is no XML character.</summary>
    internal static readonly XPathErrorCode FOUT1200 = new("FOUT1200");

    /// <summary>Builds the exception for a code of the text functions.</summary>
    /// <param name="code">The code: <see cref="FOUT1170"/>, <see cref="FOUT1190"/>, or <see cref="FOUT1200"/>.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException Exception(XPathErrorCode code) =>
        new(new XPathDiagnostic(code, XPathErrorKind.Function, MessageOf(code), -1, 0));

    private static string MessageOf(XPathErrorCode code) =>
        code == FOUT1170 ? "The resource cannot be retrieved as text."
        : code == FOUT1190 ? "The encoding is invalid or not supported, or the resource cannot be decoded with it."
        : "The resource contains a character that is not an XML character.";
}
