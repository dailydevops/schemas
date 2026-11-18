namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The comparison of <c>xs:string</c> and <c>xs:anyURI</c> (XQuery 1.0 and XPath 2.0 Functions and Operators, 7.3.2: the
/// <c>fn:compare</c> of the two values with the default collation): an <c>xs:anyURI</c> is promoted to <c>xs:string</c>
/// (XPath 2.0, B.1), and the strings are compared by Unicode codepoint.
/// </summary>
/// <remarks>Limitation: only the Unicode codepoint collation exists (<see cref="CodepointComparison"/>). Allocates nothing.</remarks>
internal static class TextComparison
{
    /// <summary>Compares two strings by a relation.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The left operand: an <c>xs:string</c> or <c>xs:anyURI</c>.</param>
    /// <param name="right">The right operand: an <c>xs:string</c> or <c>xs:anyURI</c>.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <exception cref="XPathException">An operand is not a string or URI (XPTY0004); the operator table maps these types only, so this is a registration error.</exception>
    public static bool Compare(ComparisonRelation relation, in XdmItem left, in XdmItem right)
    {
        ComparisonRelations.Check(relation);

        var a = Text(left);
        var b = Text(right);

        // The codepoint order and the ordinal order agree on equality, and the ordinal test is the faster one.
        return relation switch
        {
            ComparisonRelation.Equal => string.Equals(a, b, StringComparison.Ordinal),
            ComparisonRelation.NotEqual => !string.Equals(a, b, StringComparison.Ordinal),
            _ => ComparisonRelations.Holds(relation, CodepointComparison.Compare(a, b)),
        };
    }

    private static string Text(in XdmItem item) =>
        item.TryGetString(out var text) ? text!
        : item.TryGetAnyUri(out var uri) ? uri!
        : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
}
