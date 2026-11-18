namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The comparison of <c>xs:boolean</c> (XQuery 1.0 and XPath 2.0 Functions and Operators, 9.2: <c>op:boolean-equal</c>,
/// <c>op:boolean-less-than</c>, and <c>op:boolean-greater-than</c>): <see langword="false"/> is less than <see langword="true"/>.
/// </summary>
/// <remarks>Allocates nothing.</remarks>
internal static class BooleanComparison
{
    /// <summary>Compares two booleans by a relation.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The left operand: an <c>xs:boolean</c>.</param>
    /// <param name="right">The right operand: an <c>xs:boolean</c>.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <exception cref="XPathException">An operand is not a boolean (XPTY0004); the operator table maps this type only, so this is a registration error.</exception>
    public static bool Compare(ComparisonRelation relation, in XdmItem left, in XdmItem right)
    {
        ComparisonRelations.Check(relation);

        return left.TryGetBoolean(out var a) & right.TryGetBoolean(out var b)
            ? ComparisonRelations.Holds(relation, a == b ? 0 : (a ? 1 : -1))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
