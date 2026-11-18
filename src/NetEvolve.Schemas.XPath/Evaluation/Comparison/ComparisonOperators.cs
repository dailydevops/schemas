namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the entries of the numeric, string, and boolean types in the comparison operator table,
/// every relation for every pair of integer, decimal, float, and double, for every pair of string and URI, and for the pair of
/// booleans (XPath 2.0, B.2).
/// </summary>
/// <remarks>
/// <para>
/// The implementations promote the pair themselves, so one implementation serves every pair of a family. A folder for another type
/// family registers its own entries through its own registration file in the same table.
/// </para>
/// <para>
/// Limitation: the types that compare by <c>eq</c> and <c>ne</c> only (<c>xs:QName</c>, <c>xs:NOTATION</c>, the binary and Gregorian
/// types) have no value type yet, and the dates, times, and durations are left to XP-26; a comparison of them is XPTY0004 until
/// their folders register entries.
/// </para>
/// </remarks>
internal static class ComparisonOperators
{
    private static readonly XdmTypeId[] NumericTypes =
    [
        XdmTypeId.XsInteger,
        XdmTypeId.XsDecimal,
        XdmTypeId.XsFloat,
        XdmTypeId.XsDouble,
    ];

    // xs:anyURI is promoted to xs:string (XPath 2.0, B.1), so every pair of the two compares as strings.
    private static readonly XdmTypeId[] TextTypes = [XdmTypeId.XsString, XdmTypeId.AnyUri];

    /// <summary>Registers the entries of the numeric, string, and boolean types.</summary>
    /// <param name="table">The table.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">An entry is already registered.</exception>
    public static void Register(ComparisonOperatorTable table)
    {
        Argument.ThrowIfNull(table);

        for (var relation = ComparisonRelation.Equal; relation <= ComparisonRelation.GreaterThanOrEqual; relation++)
        {
            RegisterPairs(table, relation, NumericTypes, NumericComparison.Compare);
            RegisterPairs(table, relation, TextTypes, TextComparison.Compare);
            table.Register(relation, XdmTypeId.Boolean, XdmTypeId.Boolean, BooleanComparison.Compare);
        }
    }

    private static void RegisterPairs(
        ComparisonOperatorTable table,
        ComparisonRelation relation,
        XdmTypeId[] types,
        ComparisonImplementation implementation
    )
    {
        for (var left = 0; left < types.Length; left++)
        {
            for (var right = 0; right < types.Length; right++)
            {
                table.Register(relation, types[left], types[right], implementation);
            }
        }
    }
}
