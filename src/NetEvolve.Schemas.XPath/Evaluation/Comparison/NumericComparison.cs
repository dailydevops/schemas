namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The comparison of the numeric types (XQuery 1.0 and XPath 2.0 Functions and Operators, 6.2.4: <c>op:numeric-equal</c>,
/// <c>op:numeric-less-than</c>, and <c>op:numeric-greater-than</c>) with the type promotion of XPath 2.0, B.1: an operand pair is
/// compared in the first of integer and decimal, float, and double that holds both.
/// </summary>
/// <remarks>
/// <para>
/// Integers and decimals are exact at any width. Float and double follow IEEE 754: NaN is equal to nothing, itself included, and
/// ordered against nothing, so every relation but <c>ne</c> is false for it, and a positive and a negative zero are equal. A
/// promotion rounds once, as the arithmetic does (<see cref="NumericPromotion"/>), so an <c>xs:integer</c> beyond 2^53 can equal an
/// <c>xs:double</c> that it is not exactly equal to; the specification defines it so.
/// </para>
/// <para>
/// Steady state allocates nothing for the 64-bit integer, float, and double paths; a decimal and an integer beyond 64 bits allocate,
/// as the data model documents.
/// </para>
/// </remarks>
internal static class NumericComparison
{
    /// <summary>Compares two numbers by a relation.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The left operand: an integer, decimal, float, or double.</param>
    /// <param name="right">The right operand: an integer, decimal, float, or double.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <exception cref="XPathException">An operand is not numeric (XPTY0004); the operator table maps numeric types only, so this is a registration error.</exception>
    public static bool Compare(ComparisonRelation relation, in XdmItem left, in XdmItem right)
    {
        ComparisonRelations.Check(relation);

        if (left.Kind == XdmItemKind.XsDouble || right.Kind == XdmItemKind.XsDouble)
        {
            return NumericPromotion.TryToDouble(left, out var a) & NumericPromotion.TryToDouble(right, out var b)
                ? Holds(relation, a, b)
                : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        if (left.Kind == XdmItemKind.XsFloat || right.Kind == XdmItemKind.XsFloat)
        {
            // A float widens to a double exactly, so the double comparison gives the result of the float comparison.
            return NumericPromotion.TryToSingle(left, out var a) & NumericPromotion.TryToSingle(right, out var b)
                ? Holds(relation, a, b)
                : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        if (left.TryGetInteger(out var x) && right.TryGetInteger(out var y))
        {
            return ComparisonRelations.Holds(relation, x.CompareTo(y));
        }

        return ComparisonRelations.Holds(relation, ToDecimal(left).CompareTo(ToDecimal(right)));
    }

    // The IEEE operators, not CompareTo: the latter orders NaN below every number and equal to itself.
    private static bool Holds(ComparisonRelation relation, double a, double b) =>
        relation switch
        {
            ComparisonRelation.Equal => a == b,
            ComparisonRelation.NotEqual => a != b,
            ComparisonRelation.LessThan => a < b,
            ComparisonRelation.LessThanOrEqual => a <= b,
            ComparisonRelation.GreaterThan => a > b,
            _ => a >= b,
        };

    // The decimal accessor reads an integer beyond 64 bits as well.
    private static XsdDecimal ToDecimal(in XdmItem item) =>
        item.TryGetDecimal(out var value) ? value
        : item.TryGetInteger(out var integer) ? new XsdDecimal(integer, 0)
        : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
}
