namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a filter expression (XPath 2.0, 3.3.2): the primary expression, then each predicate keeps the items it
/// selects. The items keep the order of the primary expression; nothing is sorted.
/// </summary>
internal sealed class FilterNode : ExecutableNode, IOrderedNodeSequence
{
    private readonly ExecutableNode _primary;
    private readonly ExecutableNode[] _predicates;
    private readonly PositionalCutoff? _cutoff;

    /// <summary>Initializes a new instance of the <see cref="FilterNode"/> class.</summary>
    /// <param name="primary">The primary expression.</param>
    /// <param name="predicates">The predicates.</param>
    /// <param name="cutoff">The positional predicate that was split off the front of the predicates, <see langword="null"/> for none.</param>
    public FilterNode(ExecutableNode primary, ExecutableNode[] predicates, PositionalCutoff? cutoff = null)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(predicates);

        _primary = primary;
        _predicates = predicates;
        _cutoff = cutoff;
    }

    /// <summary>Gets the primary expression.</summary>
    public ExecutableNode Primary => _primary;

    /// <summary>Gets the predicates that are evaluated per item, after the cutoff.</summary>
    public IReadOnlyList<ExecutableNode> Predicates => _predicates;

    /// <summary>Gets the positional predicate that was split off the front of the predicates, <see langword="null"/> for none.</summary>
    public PositionalCutoff? Cutoff => _cutoff;

    /// <inheritdoc />
    /// <remarks>A filter keeps a subsequence of its primary expression in the same order, so it is ordered when the primary expression is.</remarks>
    public bool IsDocumentOrdered => _primary is IOrderedNodeSequence { IsDocumentOrdered: true };

    /// <inheritdoc />
    /// <exception cref="XPathException">A predicate value has no effective boolean value (FORG0006), or an operand raises an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var value = _primary.Evaluate(context);
        if (value.IsEmpty)
        {
            return value;
        }

        var items = default(XdmItemList);
        try
        {
            items.AddRange(value);
            _cutoff?.Apply(ref items);
            PredicateFilter.Apply(context, _predicates, ref items);
            return items.ToSequence();
        }
        finally
        {
            items.Dispose();
        }
    }
}
