namespace NetEvolve.Schemas.XPath.Errors;

/// <summary>Throw helpers: hot paths write <c>throw XPathErrors.Exception(...)</c> and carry no construction code inline.</summary>
internal static class XPathErrors
{
    /// <summary>Builds the exception for a code.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="offset">The offset in the expression, or -1 when unknown.</param>
    /// <param name="length">The length in the expression.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException Exception(XPathErrorCode code, int offset = -1, int length = 0) =>
        new(XPathDiagnostic.Create(code, offset, length));
}
