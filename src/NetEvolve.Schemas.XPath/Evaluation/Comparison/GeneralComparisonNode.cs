namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a general comparison (XPath 2.0, 3.5.2): the operands are atomized, and the result is true when a pair of
/// an item of the first operand and an item of the second operand has the relation, tested by the value comparison of the relation
/// after the casts of <c>xs:untypedAtomic</c>; otherwise it is false. The pairs are tried in the order of the operands, and the
/// comparison ends at the first pair that holds.
/// </summary>
/// <remarks>
/// <para>
/// Which error a comparison raises, or whether it raises one, depends on the order of the pairs: the comparison ends at the first
/// match, as XPath 2.0, 3.5.2 allows, so <c>(1, "a") = 1</c> is true while <c>("a", 1) = 1</c> is XPTY0004.
/// </para>
/// <para>
/// Limitation: XPath 1.0 compatibility mode is not applied here: the lowering selects <see cref="Compatibility.CompatibilityGeneralComparisonNode"/> for it. Both operands are atomized completely before the first pair is
/// tried. The cast of an <c>xs:untypedAtomic</c> item to <c>xs:double</c>, <c>xs:float</c>, <c>xs:boolean</c>, or <c>xs:integer</c>
/// (<see cref="Functions.Binding.UntypedAtomicCast"/>) allocates nothing, pinned by <c>UntypedAtomicCastAllocationTests</c>; only a
/// decimal, date and time, or duration target is boxed (#661). Pairs of items that need no cast allocate nothing.
/// </para>
/// </remarks>
internal sealed class GeneralComparisonNode : ExecutableNode
{
    private readonly ComparisonRelation _relation;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;
    private readonly ComparisonOperatorTable _table;

    /// <summary>Initializes a new instance of the <see cref="GeneralComparisonNode"/> class.</summary>
    /// <param name="operator">The operator, one of =, !=, &lt;, &lt;=, &gt;, and &gt;=.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="table">The operator table; entries added to it later are seen.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a general comparison operator.</exception>
    public GeneralComparisonNode(
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
    /// <exception cref="XPathException">An <c>xs:untypedAtomic</c> item is not a value of the type it is cast to (FORG0001), or the types of a pair have no operator (XPTY0004).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var left = XPathAtomization.Atomize(_left.Evaluate(context));
        var right = XPathAtomization.Atomize(_right.Evaluate(context));
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

    private bool Holds(XdmItem left, XdmItem right, XPathEvaluationContext context)
    {
        ComparisonOperands.CastUntypedAtomic(ref left, ref right);

        return _table.TryGetContextual(_relation, left.TypeId, right.TypeId, out var implementation)
            ? implementation!(_relation, left, right, context)
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
