namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A step that is not an axis step: a filter expression, a function call, a parenthesized expression, or the context item
/// expression (XPath 2.0, 3.2). It is evaluated once per context item.
/// </summary>
internal sealed class ExpressionPathStep : PathStep
{
    private readonly ExecutableNode _expression;
    private readonly bool _isContextItem;
    private readonly bool _isOrdered;

    /// <summary>Initializes a new instance of the <see cref="ExpressionPathStep"/> class.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="isContextItem">Whether the expression is <c>.</c>, which hands every item on and keeps what is known about the sequence.</param>
    public ExpressionPathStep(ExecutableNode expression, bool isContextItem)
    {
        ArgumentNullException.ThrowIfNull(expression);

        _expression = expression;
        _isContextItem = isContextItem;
        _isOrdered = expression is IOrderedNodeSequence { IsDocumentOrdered: true };
    }

    /// <inheritdoc />
    public override void EvaluateFromContext(XPathEvaluationContext context, ref XdmItemList results)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The first step runs in the focus of the whole path, unchanged.
        results.AddRange(_expression.Evaluate(context));
    }

    /// <inheritdoc />
    public override void EvaluateOver(
        XPathEvaluationContext context,
        ReadOnlySpan<XdmItem> inputs,
        ref XdmItemList results
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_isContextItem)
        {
            results.AddRange(inputs);
            return;
        }

        var outer = context.Focus;
        try
        {
            for (var i = 0; i < inputs.Length; i++)
            {
                _ = context.EnterFocus(inputs[i], i + 1, inputs.Length);
                results.AddRange(_expression.Evaluate(context));
            }
        }
        finally
        {
            context.RestoreFocus(outer);
        }
    }

    /// <inheritdoc />
    public override XPathOrderProperties Compose(XPathOrderProperties input)
    {
        if (_isContextItem)
        {
            return input;
        }

        // One evaluation of an expression that is ordered itself stays ordered; the results of several evaluations are concatenated.
        return _isOrdered && input.HasFlag(XPathOrderProperties.Single)
            ? XPathOrderProperties.Ordered | XPathOrderProperties.DuplicateFree
            : XPathOrderProperties.None;
    }
}
