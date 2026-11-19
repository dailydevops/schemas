namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a quantified expression with one range variable (XPath 2.0, 3.9): <c>some</c> is true when the effective boolean
/// value of the body is true for at least one item of the source, <c>every</c> when it is true for each one. The loop stops at the first
/// item that decides, so the remaining items are not read and the body is not evaluated for them; <c>some</c> over the empty sequence is
/// false and <c>every</c> over it is true. An expression with several variables is a chain of these nodes of the same quantifier.
/// </summary>
internal sealed class QuantifiedNode : BindingNode
{
    private readonly Quantifier _quantifier;

    /// <summary>Initializes a new instance of the <see cref="QuantifiedNode"/> class.</summary>
    /// <param name="quantifier">The quantifier.</param>
    /// <param name="name">The expanded name of the range variable.</param>
    /// <param name="source">The expression whose items the variable takes.</param>
    /// <param name="body">The <c>satisfies</c> expression.</param>
    /// <exception cref="System.ArgumentNullException">An expression is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException">The local name of <paramref name="name"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="quantifier"/> is not a defined quantifier.</exception>
    public QuantifiedNode(Quantifier quantifier, XPathQName name, ExecutableNode source, ExecutableNode body)
        : base(name, source, body)
    {
        Argument.ThrowIfLessThan((int)quantifier, (int)Quantifier.Some, nameof(quantifier));
        Argument.ThrowIfGreaterThan((int)quantifier, (int)Quantifier.Every, nameof(quantifier));

        _quantifier = quantifier;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The body has no effective boolean value (FORG0006), or an expression raises an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        // Some starts false and every starts true: the value of the empty sequence, which a decision replaces.
        var state = new BindingState(default, _quantifier == Quantifier.Every);
        _ = Iterate(context, ref state);
        return XdmSequence.Create(XdmItem.FromBoolean(state.Result));
    }

    /// <inheritdoc />
    protected override bool Visit(XPathEvaluationContext context, ref BindingState state)
    {
        var truth = XPathAtomization.EffectiveBooleanValue(Body.Evaluate(context));
        if (truth == (_quantifier == Quantifier.Some))
        {
            state.Result = truth;
            return false;
        }

        return true;
    }
}
