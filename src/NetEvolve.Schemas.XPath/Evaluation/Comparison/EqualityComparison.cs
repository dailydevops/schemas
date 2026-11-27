namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The equality of <c>xs:hexBinary</c> and <c>xs:base64Binary</c> (XQuery 1.0 and XPath 2.0 Functions and Operators, 10.4:
/// <c>op:hexBinary-equal</c> and <c>op:base64Binary-equal</c>: the octets are equal) and of <c>xs:QName</c> (11.2:
/// <c>op:QName-equal</c>: the namespace URI and the local name are equal, the prefix is irrelevant). These types have no order, so
/// only <c>eq</c> and <c>ne</c> are registered.
/// </summary>
/// <remarks>Allocates nothing.</remarks>
internal static class EqualityComparison
{
    /// <summary>Compares two binary values by <c>eq</c> or <c>ne</c>.</summary>
    /// <param name="relation">The relation: <c>eq</c> or <c>ne</c>.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="XPathException">An operand is not binary (XPTY0004); the operator table maps these types only, so this is a registration error.</exception>
    public static bool CompareBinary(ComparisonRelation relation, in XdmItem left, in XdmItem right) =>
        left.TryGetBinary(out var a) & right.TryGetBinary(out var b)
            ? Result(relation, a.Equals(b))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);

    /// <summary>Compares two QNames by <c>eq</c> or <c>ne</c>.</summary>
    /// <param name="relation">The relation: <c>eq</c> or <c>ne</c>.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="XPathException">An operand is not a QName (XPTY0004); the operator table maps this type only, so this is a registration error.</exception>
    public static bool CompareQName(ComparisonRelation relation, in XdmItem left, in XdmItem right) =>
        left.TryGetQName(out var a) & right.TryGetQName(out var b)
            ? Result(relation, a!.Equals(b))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);

    // Two equal values have the order 0 and two different ones a nonzero order, which is all that eq and ne read.
    private static bool Result(ComparisonRelation relation, bool equal) =>
        ComparisonRelations.Holds(relation, equal ? 0 : 1);
}
