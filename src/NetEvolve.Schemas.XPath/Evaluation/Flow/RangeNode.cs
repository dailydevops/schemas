namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using System.Numerics;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a range expression (XPath 2.0, 3.3.1): the integers from the first operand to the second, in order. Each operand
/// follows the function conversion rules for <c>xs:integer?</c>; an empty operand, or a start above the end, gives the empty sequence.
/// </summary>
/// <remarks>
/// <para>
/// The size of the range is checked against the range size limit before anything is built. A range of two or more items is a lazy
/// sequence that holds its two bounds; <see cref="ForNode"/> and <see cref="QuantifiedNode"/> read the bounds with
/// <see cref="TryGetBounds"/> and count through them without building a sequence, so a loop that stops early costs only the items it
/// visits. Limitation: the range size limit applies to such a loop as well, even when it would stop early (XP-75, #229).
/// </para>
/// <para>
/// An operand beyond 64 bits is rare: the range is exact, built from <see cref="BigInteger"/> values, and bounded by the range size
/// limit and the sequence size limit, because a lazy source needs 64-bit bounds.
/// </para>
/// </remarks>
internal sealed class RangeNode : ExecutableNode
{
    private static readonly XPathParameterType OptionalInteger = new(XPathItemType.Integer, Occurrence.ZeroOrOne);

    private readonly ExecutableNode _from;
    private readonly ExecutableNode _to;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="RangeNode"/> class.</summary>
    /// <param name="from">The first operand.</param>
    /// <param name="to">The second operand.</param>
    /// <param name="location">The location of the expression, reported when the range exceeds a limit.</param>
    /// <exception cref="System.ArgumentNullException">An operand is <see langword="null"/>.</exception>
    public RangeNode(ExecutableNode from, ExecutableNode to, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        _from = from;
        _to = to;
        _location = location;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An operand is not an integer (XPTY0004, FORG0001), or the range is larger than the range size limit.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!TryGetBounds(context, out var first, out var last, out var value))
        {
            return value;
        }

        return first == last
            ? XdmSequence.Create(XdmItem.FromInteger(first))
            : XdmSequence.FromLazy(new RangeItems(first, last));
    }

    /// <summary>Evaluates the operands and checks the size of the range.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="first">The first integer, when the result is <see langword="true"/>.</param>
    /// <param name="last">The last integer, not below <paramref name="first"/>, when the result is <see langword="true"/>.</param>
    /// <param name="value">The value of the range expression, when the result is <see langword="false"/>: empty, or the exact items of a range beyond 64 bits.</param>
    /// <returns><see langword="true"/> when the range has items and both bounds fit 64 bits.</returns>
    /// <exception cref="XPathException">An operand is not an integer (XPTY0004, FORG0001), or the range is larger than the range size limit.</exception>
    public bool TryGetBounds(XPathEvaluationContext context, out long first, out long last, out XdmSequence value)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Both operands are evaluated, so a type error in the second one is raised even when the first one is empty.
        var hasFirst = TryGetOperand(_from, context, out var from);
        var hasLast = TryGetOperand(_to, context, out var to);
        first = 0;
        last = 0;
        value = default;
        if (!hasFirst || !hasLast)
        {
            return false;
        }

        if (!from.TryGetInteger(out first) || !to.TryGetInteger(out last))
        {
            value = Wide(context, from, to);
            return false;
        }

        if (first > last)
        {
            return false;
        }

        context.Limits.ThrowIfRangeTooLarge(first, last, _location);
        return true;
    }

    private static bool TryGetOperand(ExecutableNode node, XPathEvaluationContext context, out XdmItem item)
    {
        var atomic = XPathAtomization.Atomize(node.Evaluate(context), context.Annotations);

        // An integer beyond 64 bits is an item of the decimal family that the function conversion does not take for an integer.
        if (atomic.Count == 1 && atomic[0].TryGetBigInteger(out _))
        {
            item = atomic[0];
            return true;
        }

        var converted = FunctionConversion.Convert(atomic, OptionalInteger, context.Annotations);
        item = converted.IsEmpty ? default : converted[0];
        return !converted.IsEmpty;
    }

    private XdmSequence Wide(XPathEvaluationContext context, in XdmItem from, in XdmItem to)
    {
        _ = from.TryGetBigInteger(out var first);
        _ = to.TryGetBigInteger(out var last);
        if (first > last)
        {
            return XdmSequence.Empty;
        }

        var limits = context.Limits;
        if (last - first + 1 > limits.MaxRangeSize)
        {
            throw XPathLimitErrors.RangeSize(limits.MaxRangeSize, _location);
        }

        var items = new XdmItemList(limits.MaxSequenceSize, _location);
        try
        {
            for (var next = first; next <= last; next++)
            {
                items.Add(XdmItem.FromBigInteger(next));
            }

            return items.ToSequence();
        }
        finally
        {
            items.Dispose();
        }
    }
}
