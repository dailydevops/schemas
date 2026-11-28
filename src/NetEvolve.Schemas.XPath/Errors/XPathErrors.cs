namespace NetEvolve.Schemas.XPath.Errors;

using NetEvolve.Schemas.XPath.Syntax.Tree;

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

    /// <summary>Builds the XPST0017 exception for a call that no function matches; the message names the function and its arity.</summary>
    /// <param name="name">The function name as written.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="location">The location of the call.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException UnknownFunction(QualifiedName name, int arity, SourceSpan location)
    {
        var diagnostic = XPathDiagnostic.Create(XPathErrorCodes.XPST0017, location.Offset, location.Length);
        var written = name.Prefix is null ? name.LocalName : name.Prefix + ":" + name.LocalName;
        return new XPathException(
            new XPathDiagnostic(
                diagnostic.QName,
                diagnostic.Kind,
                $"No function matches the name and number of arguments: {written}#{arity}.",
                diagnostic.Offset,
                diagnostic.Length
            )
        );
    }
}
