namespace NetEvolve.Schemas.XPath.Compilation;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Lowers a syntax tree into an immutable executable tree (XPath 2.0, 2.2.3, static analysis phase) and reports the
/// static errors found on the way. The syntax tree is not retained.
/// </summary>
internal static class ExpressionCompiler
{
    /// <summary>Compiles a syntax tree without throwing for static errors.</summary>
    /// <param name="syntax">The syntax tree.</param>
    /// <param name="source">The source text, kept for diagnostics.</param>
    /// <param name="context">The static context.</param>
    /// <param name="registry">The lowerings.</param>
    /// <param name="compiled">The compiled expression, <see langword="null"/> on failure.</param>
    /// <param name="diagnostic">The diagnostic, <see langword="default"/> on success.</param>
    /// <param name="limits">The limits the lowering recurses under; <see langword="null"/> for <see cref="XPathLimits.Default"/>.</param>
    /// <returns><see langword="true"/> when the tree compiled; a tree deeper than <see cref="XPathLimits.MaxRecursionDepth"/> does not.</returns>
    public static bool TryCompile(
        Expression syntax,
        string source,
        XPathStaticContext context,
        LoweringRegistry registry,
        out CompiledExpression? compiled,
        out XPathDiagnostic diagnostic,
        XPathLimits? limits = null
    )
    {
        Argument.ThrowIfNull(syntax);
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(registry);

        try
        {
            var lowering = new LoweringContext(context, registry, limits);
            var root = lowering.Lower(syntax);
            compiled = new CompiledExpression(source, context.Version, root, lowering.MaxDepth, syntax.Location);
            diagnostic = default;
            return true;
        }
        catch (XPathException exception)
        {
            compiled = null;
            diagnostic = exception.Diagnostic;
            return false;
        }
    }
}
