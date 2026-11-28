namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of the comma operator (XPath 2.0, 3.3.1): the operands are evaluated in order and their items are concatenated into
/// one flat sequence.
/// </summary>
/// <remarks>The result is bounded by the sequence size limit of the evaluation; a lazy operand is read to the end.</remarks>
internal sealed class SequenceNode : ExecutableNode
{
    private readonly ExecutableNode[] _items;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="SequenceNode"/> class.</summary>
    /// <param name="items">The operands.</param>
    /// <param name="location">The location of the expression, reported when the result exceeds the sequence size limit.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="items"/> or one of its elements is <see langword="null"/>.</exception>
    public SequenceNode(ExecutableNode[] items, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (var i = 0; i < items.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(items[i], nameof(items));
        }

        _items = items;
        _location = location;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The result has more items than the sequence size limit, or an operand raises an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var list = new XdmItemList(context.Limits.MaxSequenceSize, _location);
        try
        {
            for (var i = 0; i < _items.Length; i++)
            {
                list.AddRange(_items[i].Evaluate(context));
            }

            return list.ToSequence();
        }
        finally
        {
            list.Dispose();
        }
    }
}
