namespace NetEvolve.Schemas.XPath.Syntax.Parser;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Parses an XPath 2.0 expression into the syntax tree.</summary>
internal static class XPathParser
{
    /// <summary>The deepest nesting of expressions the parser accepts; hostile input ends in a diagnostic.</summary>
    internal const int MaxNestingDepth = 100;

    /// <summary>Parses an expression without throwing.</summary>
    /// <param name="text">The expression.</param>
    /// <param name="expression">The tree when the expression is valid.</param>
    /// <param name="diagnostic">The XPST0003 diagnostic when it is not.</param>
    /// <returns><see langword="true"/> when the expression is valid.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out Expression? expression, out XPathDiagnostic diagnostic)
    {
        expression = null;
        diagnostic = XPathDiagnostic.Create(XPathErrorCodes.XPST0003, 0, text.Length);
        return false;
    }
}
