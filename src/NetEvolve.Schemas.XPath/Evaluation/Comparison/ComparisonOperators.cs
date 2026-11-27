namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the entries of the numeric, string, and boolean types in the comparison operator table,
/// every relation for every pair of integer, decimal, float, and double, for every pair of string and URI, and for the pair of
/// booleans, and <c>eq</c> and <c>ne</c> for the pairs of <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, and <c>xs:QName</c>
/// (XPath 2.0, B.2).
/// </summary>
/// <remarks>
/// <para>
/// The implementations promote the pair themselves, so one implementation serves every pair of a family. A folder for another type
/// family registers its own entries through its own registration file in the same table.
/// </para>
/// <para>
/// Limitation: <c>xs:NOTATION</c> has no entries here; a comparison of it is XPTY0004. The dates, times, durations, and Gregorian types
/// register their entries through the registration file of the temporal folder (<c>TemporalOperators</c>), not through this one.
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

    private static readonly XdmTypeId HexBinary = XdmTypes.Get("hexBinary");
    private static readonly XdmTypeId Base64Binary = XdmTypes.Get("base64Binary");
    private static readonly XdmTypeId QName = XdmTypes.Get("QName");

    /// <summary>Registers the entries of the numeric, string, and boolean types.</summary>
    /// <param name="table">The table.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">An entry is already registered.</exception>
    public static void Register(ComparisonOperatorTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        for (var relation = ComparisonRelation.Equal; relation <= ComparisonRelation.GreaterThanOrEqual; relation++)
        {
            RegisterPairs(table, relation, NumericTypes, NumericComparison.Compare);
            RegisterPairs(table, relation, TextTypes, TextComparison.Compare);
            table.Register(relation, XdmTypeId.Boolean, XdmTypeId.Boolean, BooleanComparison.Compare);
        }

        // The binary types and xs:QName have equality only (F&O, 10.4 and 11.2); the ordering operators stay without an entry.
        foreach (var relation in new[] { ComparisonRelation.Equal, ComparisonRelation.NotEqual })
        {
            table.Register(relation, HexBinary, HexBinary, EqualityComparison.CompareBinary);
            table.Register(relation, Base64Binary, Base64Binary, EqualityComparison.CompareBinary);
            table.Register(relation, QName, QName, EqualityComparison.CompareQName);
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
