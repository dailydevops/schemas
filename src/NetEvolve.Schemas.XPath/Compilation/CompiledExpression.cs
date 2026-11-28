namespace NetEvolve.Schemas.XPath.Compilation;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>An immutable compiled expression: the source text and the executable tree. Safe for concurrent evaluation.</summary>
/// <remarks>Limitation: the public <see cref="XPathExpression"/> wraps this type once XP-19 lands.</remarks>
internal sealed class CompiledExpression
{
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="CompiledExpression"/> class.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="version">The language version.</param>
    /// <param name="root">The root of the executable tree.</param>
    /// <param name="depth">The depth of the executable tree, the root being one.</param>
    /// <param name="location">The location of the whole expression, reported when the tree is deeper than a limit.</param>
    public CompiledExpression(string source, XPathVersion version, ExecutableNode root, int depth, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);

        Source = source;
        Version = version;
        Root = root;
        Depth = depth;
        _location = location;
    }

    /// <summary>Gets the source text.</summary>
    public string Source { get; }

    /// <summary>Gets the language version.</summary>
    public XPathVersion Version { get; }

    /// <summary>Gets the root of the executable tree.</summary>
    public ExecutableNode Root { get; }

    /// <summary>Gets the depth of the executable tree, the root being one; the evaluation recurses once per level.</summary>
    public int Depth { get; }

    /// <summary>Evaluates the expression.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The value.</returns>
    /// <exception cref="XPathException">The tree is deeper than the recursion depth limit of the context (<see cref="XPathLimitErrors.RecursionDepthExceeded"/>), or an operator raises an error.</exception>
    public XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // One comparison per evaluation: the tree is as deep as the recursion of the operators below it.
        context.Limits.ThrowIfTooDeep(Depth, _location);
        return Root.Evaluate(context);
    }

    /// <summary>Evaluates the expression to its effective boolean value.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The effective boolean value.</returns>
    /// <exception cref="XPathException">The tree is deeper than the recursion depth limit of the context, an operator raises an error, or the value has no effective boolean value (FORG0006).</exception>
    public bool EvaluateBoolean(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Limits.ThrowIfTooDeep(Depth, _location);
        return Root.EvaluateBoolean(context);
    }
}
