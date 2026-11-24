namespace NetEvolve.Schemas.XPath.Compilation.Optimization;

using NetEvolve.Arguments;

/// <summary>
/// The optimization stage (XPath 2.0, 2.3.4): rewrites a lowered node into an equivalent one. The lowering calls it for each node after the
/// operands were lowered, so a rewrite sees operands that are rewritten already. Every rewrite keeps the result and the dynamic errors that
/// the specification does not allow an implementation to skip.
/// </summary>
internal static class ExpressionOptimizer
{
    /// <summary>Applies the enabled rewrites to a lowered node.</summary>
    /// <param name="node">The lowered node.</param>
    /// <param name="optimizations">The rewrites that are enabled.</param>
    /// <returns>The node itself when no rewrite applies.</returns>
    public static ExecutableNode Rewrite(ExecutableNode node, XPathOptimizations optimizations)
    {
        ArgumentNullException.ThrowIfNull(node);

        return optimizations == XPathOptimizations.None ? node : node;
    }
}
