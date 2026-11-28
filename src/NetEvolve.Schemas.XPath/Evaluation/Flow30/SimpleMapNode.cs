namespace NetEvolve.Schemas.XPath.Evaluation.Flow30;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of the simple map operator, <c>left ! right</c> (XPath 3.0, 3.9): the right side is evaluated once per item of the
/// left side, with that item as the context item and its position and the size of the left side as the focus, and the results are
/// concatenated. Unlike a path step, nothing is sorted and no duplicate is removed, and the items need not be nodes.
/// </summary>
/// <remarks>The results are collected eagerly, bounded by the sequence size limit of the evaluation; the outer focus is restored afterwards.</remarks>
internal sealed class SimpleMapNode : ExecutableNode
{
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="SimpleMapNode"/> class.</summary>
    /// <param name="left">The expression whose items are the focus of the right side.</param>
    /// <param name="right">The expression evaluated once per item.</param>
    /// <param name="location">The location of the expression, reported when the result exceeds the sequence size limit.</param>
    /// <exception cref="System.ArgumentNullException">An expression is <see langword="null"/>.</exception>
    public SimpleMapNode(ExecutableNode left, ExecutableNode right, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        _left = left;
        _right = right;
        _location = location;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The result has more items than the sequence size limit, or an expression raises an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var items = _left.Evaluate(context);
        var count = items.Count;
        if (count == 0)
        {
            return XdmSequence.Empty;
        }

        var results = new XdmItemList(context.Limits.MaxSequenceSize, _location);
        var outer = context.Focus;
        try
        {
            for (var i = 0; i < count; i++)
            {
                _ = context.EnterFocus(items[i], i + 1, count);
                results.AddRange(_right.Evaluate(context));
            }

            return results.ToSequence();
        }
        finally
        {
            context.RestoreFocus(outer);
            results.Dispose();
        }
    }
}
