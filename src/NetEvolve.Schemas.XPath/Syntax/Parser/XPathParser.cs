namespace NetEvolve.Schemas.XPath.Syntax.Parser;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Parses an XPath expression into the syntax tree; XPath 2.0 is the default, XPath 3.0 is selected by the version.
/// </summary>
/// <remarks>
/// <para>
/// The grammar is XPath 2.0 (Second Edition) appendix A, extended by XPath 3.0 when that version is selected: the
/// productions of A.1, the precedence order of A.4, and the extra-grammatical constraints (leading lone slash,
/// reserved function names, occurrence indicators). Every syntax error, including a lexical one, is reported as
/// XPST0003 with the offset and length of the offending token.
/// </para>
/// <para>
/// Limitation: expressions nested deeper than <see cref="MaxNestingDepth"/> (parentheses, predicates, function
/// arguments, conditionals, unary signs, and, in XPath 3.0, parenthesized and function item types together) are
/// rejected with XPST0003 at the token that exceeds the bound, so hostile input ends in a diagnostic and not in a
/// stack overflow. The bound is fixed (#640 makes it follow the
/// evaluation limits); a left-deep chain of binary operators does not nest and is bounded by the recursion depth
/// limit of the lowering (XP-75, #229).
/// Static checks beyond the grammar (names, functions, prefixes, the namespace axis) are not part of parsing.
/// </para>
/// </remarks>
internal static class XPathParser
{
    /// <summary>The deepest nesting of expressions the parser accepts.</summary>
    internal const int MaxNestingDepth = 100;

    /// <summary>Parses an expression without throwing.</summary>
    /// <param name="text">The expression.</param>
    /// <param name="expression">The tree when the expression is valid, otherwise <see langword="null"/>.</param>
    /// <param name="diagnostic">The XPST0003 diagnostic when the expression is not valid.</param>
    /// <returns><see langword="true"/> when the expression is valid.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out Expression? expression, out XPathDiagnostic diagnostic) =>
        TryParse(text, XPathVersion.V20, out expression, out diagnostic);

    /// <summary>Parses an expression of the given language version without throwing.</summary>
    /// <param name="text">The expression.</param>
    /// <param name="version">The language version that decides which grammar applies.</param>
    /// <param name="expression">The tree when the expression is valid, otherwise <see langword="null"/>.</param>
    /// <param name="diagnostic">The XPST0003 diagnostic when the expression is not valid.</param>
    /// <returns><see langword="true"/> when the expression is valid.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> text,
        XPathVersion version,
        out Expression? expression,
        out XPathDiagnostic diagnostic
    )
    {
        try
        {
            expression = new ExpressionParser(text, version).ParseExpression();
            diagnostic = default;
            return true;
        }
        catch (XPathException exception)
        {
            expression = null;
            diagnostic = exception.Diagnostic;
            return false;
        }
    }
}
