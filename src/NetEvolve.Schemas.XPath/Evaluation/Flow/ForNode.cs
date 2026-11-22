namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a <see langword="for"/> expression with one range variable (XPath 2.0, 3.7): the body is evaluated once for each item of the
/// source, in order, and the results are concatenated. A body that is another <see cref="ForNode"/> collects into the same list, so the
/// bindings of one expression with several variables build no intermediate sequence.
/// </summary>
/// <remarks>
/// The source is read item by item, a range without building it. The results are collected eagerly, bounded by the sequence size
/// limit of the evaluation: a deferred result would read the variables and the focus when it is consumed, after the bindings are gone.
/// </remarks>
internal sealed class ForNode : BindingNode
{
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="ForNode"/> class.</summary>
    /// <param name="name">The expanded name of the range variable.</param>
    /// <param name="source">The expression whose items the variable takes.</param>
    /// <param name="body">The return expression.</param>
    /// <param name="location">The location of the expression, reported when the result exceeds the sequence size limit.</param>
    /// <exception cref="System.ArgumentNullException">An expression is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException">The local name of <paramref name="name"/> is empty.</exception>
    public ForNode(XPathQName name, ExecutableNode source, ExecutableNode body, SourceSpan location)
        : base(name, source, body) => _location = location;

    /// <inheritdoc />
    /// <exception cref="XPathException">The result has more items than the sequence size limit, or an expression raises an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var state = new BindingState(new XdmItemList(context.Limits.MaxSequenceSize, _location), false);
        try
        {
            _ = Iterate(context, ref state);
            return state.Items.ToSequence();
        }
        finally
        {
            state.Items.Dispose();
        }
    }

    /// <inheritdoc />
    protected override bool Visit(XPathEvaluationContext context, ref BindingState state)
    {
        if (Body is ForNode inner)
        {
            return inner.Iterate(context, ref state);
        }

        state.Items.AddRange(Body.Evaluate(context));
        return true;
    }
}
