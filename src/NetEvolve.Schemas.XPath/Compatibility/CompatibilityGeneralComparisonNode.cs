namespace NetEvolve.Schemas.XPath.Compatibility;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a general comparison in XPath 1.0 compatibility mode (XPath 2.0, 3.5.2), applied in this order: when one operand
/// is a single atomic <c>xs:boolean</c>, the other operand becomes its effective boolean value; both operands are atomized; for
/// <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, and <c>&gt;=</c> every item becomes an <c>xs:double</c> by <c>fn:number</c>; the result is true when a
/// pair has the relation, where a pair with a numeric item is compared as two doubles by <c>fn:number</c>, two untyped values as strings,
/// and one untyped value is cast to the type of the other item. The lowering selects this node when
/// <see cref="XPathStaticContext.XPath10Compatibility"/> is on; the node of the mode without it is <see cref="GeneralComparisonNode"/>.
/// </summary>
/// <remarks>
/// Limitation: <c>fn:number</c> runs once per pair and not once per item, so a relational comparison of two long sequences repeats the
/// conversion; converting each item once is the upgrade path. Both operands are evaluated and atomized completely before the first pair is
/// tried. The comparison ends at the first pair that holds.
/// </remarks>
internal sealed class CompatibilityGeneralComparisonNode : ExecutableNode
{
    private readonly ComparisonRelation _relation;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;
    private readonly ComparisonOperatorTable _table;

    /// <summary>Initializes a new instance of the <see cref="CompatibilityGeneralComparisonNode"/> class.</summary>
    /// <param name="operator">The operator, one of =, !=, &lt;, &lt;=, &gt;, and &gt;=.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="table">The operator table; entries added to it later are seen.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a general comparison operator.</exception>
    public CompatibilityGeneralComparisonNode(
        ComparisonOperator @operator,
        ExecutableNode left,
        ExecutableNode right,
        ComparisonOperatorTable table
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            (int)@operator,
            (int)ComparisonOperator.GeneralEqual,
            nameof(@operator)
        );
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            (int)@operator,
            (int)ComparisonOperator.GeneralGreaterThanOrEqual,
            nameof(@operator)
        );
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(table);

        _relation = ComparisonRelations.Of(@operator);
        _left = left;
        _right = right;
        _table = table;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The other operand of a boolean has no effective boolean value (FORG0006), an untyped value is not a value of the type it is cast to (FORG0001), or the types of a pair have no operator (XPTY0004).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var leftValue = _left.Evaluate(context);
        var rightValue = _right.Evaluate(context);
        if (IsSingleBoolean(leftValue))
        {
            rightValue = XdmSequence.Create(XdmItem.FromBoolean(XPathAtomization.EffectiveBooleanValue(rightValue)));
        }
        else if (IsSingleBoolean(rightValue))
        {
            leftValue = XdmSequence.Create(XdmItem.FromBoolean(XPathAtomization.EffectiveBooleanValue(leftValue)));
        }

        var left = XPathAtomization.Atomize(leftValue, context.Annotations);
        var right = XPathAtomization.Atomize(rightValue, context.Annotations);
        var leftCount = left.Count;
        var rightCount = right.Count;
        for (var i = 0; i < leftCount; i++)
        {
            for (var j = 0; j < rightCount; j++)
            {
                if (Holds(left[i], right[j], context))
                {
                    return XdmSequence.Create(XdmItem.FromBoolean(true));
                }
            }
        }

        return XdmSequence.Create(XdmItem.FromBoolean(false));
    }

    private static bool IsSingleBoolean(in XdmSequence value) =>
        value.Count == 1 && value[0].Kind == XdmItemKind.Boolean;

    private bool Holds(XdmItem left, XdmItem right, XPathEvaluationContext context)
    {
        if (
            _relation > ComparisonRelation.NotEqual
            || CompatibilityOperands.IsNumeric(left)
            || CompatibilityOperands.IsNumeric(right)
        )
        {
            left = CompatibilityOperands.ToNumber(left);
            right = CompatibilityOperands.ToNumber(right);
        }
        else
        {
            ComparisonOperands.CastUntypedAtomic(ref left, ref right);
        }

        return _table.TryGetContextual(_relation, left.TypeId, right.TypeId, out var implementation)
            ? implementation!(_relation, left, right, context)
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
