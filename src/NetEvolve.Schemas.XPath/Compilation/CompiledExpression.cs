namespace NetEvolve.Schemas.XPath.Compilation;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>An immutable compiled expression: the source text and the executable tree. Safe for concurrent evaluation.</summary>
/// <remarks>Limitation: the public <see cref="XPathExpression"/> wraps this type once XP-19 lands.</remarks>
internal sealed class CompiledExpression
{
    /// <summary>Initializes a new instance of the <see cref="CompiledExpression"/> class.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="version">The language version.</param>
    /// <param name="root">The root of the executable tree.</param>
    public CompiledExpression(string source, XPathVersion version, ExecutableNode root)
    {
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(root);

        Source = source;
        Version = version;
        Root = root;
    }

    /// <summary>Gets the source text.</summary>
    public string Source { get; }

    /// <summary>Gets the language version.</summary>
    public XPathVersion Version { get; }

    /// <summary>Gets the root of the executable tree.</summary>
    public ExecutableNode Root { get; }

    /// <summary>Evaluates the expression.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The value.</returns>
    public XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        return Root.Evaluate(context);
    }
}
