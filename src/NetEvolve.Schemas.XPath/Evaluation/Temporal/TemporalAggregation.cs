namespace NetEvolve.Schemas.XPath.Evaluation.Temporal;

using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The access of <c>fn:min</c>, <c>fn:max</c>, <c>fn:sum</c>, and <c>fn:avg</c> to the date, time, and duration operators (Functions and
/// Operators 1.0, 15.4): the functions order and add the values through the entries of the operator tables that
/// <see cref="TemporalOperators"/> fills, the same entries that the operators <c>lt</c>, <c>gt</c>, <c>+</c>, and <c>div</c> use, and
/// never through a second implementation.
/// </summary>
/// <remarks>
/// The functions have no handle on the operator tables of a composition root (XP-19), so this class holds one read-only table of each kind
/// with the temporal entries only; the entries are stateless, so the tables are shared by every evaluation.
/// </remarks>
internal static class TemporalAggregation
{
    private static readonly ArithmeticOperatorTable Arithmetic = CreateArithmetic();

    private static readonly ComparisonOperatorTable Comparison = CreateComparison();

    /// <summary>Determines whether an item is of a type that <c>fn:min</c> and <c>fn:max</c> order: a date, a time, a dateTime, or a duration of one of the two subtypes.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> for an orderable date, time, or duration.</returns>
    public static bool IsOrdered(in XdmItem item) =>
        item.Kind
            is XdmItemKind.DateTime
                or XdmItemKind.Date
                or XdmItemKind.Time
                or XdmItemKind.YearMonthDuration
                or XdmItemKind.DayTimeDuration;

    /// <summary>Determines whether an item is of a type that <c>fn:sum</c> and <c>fn:avg</c> add: a duration of one of the two subtypes.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> for a year-month or a day-time duration.</returns>
    public static bool IsSummable(in XdmItem item) =>
        item.Kind is XdmItemKind.YearMonthDuration or XdmItemKind.DayTimeDuration;

    /// <summary>Tests a relation between two values through the comparison table.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <param name="context">The dynamic context, which gives the implicit timezone.</param>
    /// <param name="holds">Whether the relation holds; <see langword="false"/> when the pair has no entry.</param>
    /// <returns><see langword="true"/> when the table has an entry for the pair.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static bool TryCompare(
        ComparisonRelation relation,
        in XdmItem left,
        in XdmItem right,
        XPathEvaluationContext context,
        out bool holds
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Comparison.TryGetContextual(relation, left.TypeId, right.TypeId, out var implementation))
        {
            holds = implementation!(relation, left, right, context);
            return true;
        }

        holds = false;
        return false;
    }

    /// <summary>Applies an arithmetic operator to two values through the operator table.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <param name="context">The dynamic context.</param>
    /// <param name="result">The result; <see langword="default"/> when the pair has no entry.</param>
    /// <returns><see langword="true"/> when the table has an entry for the pair.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="Errors.XPathException">The operator raises a dynamic error (FODT0002, FOAR0001, FOCA0005).</exception>
    public static bool TryApply(
        ArithmeticOperator @operator,
        in XdmItem left,
        in XdmItem right,
        XPathEvaluationContext context,
        out XdmItem result
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Arithmetic.TryGetContextual(@operator, left.TypeId, right.TypeId, out var implementation))
        {
            result = implementation!(left, right, context);
            return true;
        }

        result = default;
        return false;
    }

    private static ArithmeticOperatorTable CreateArithmetic()
    {
        var table = new ArithmeticOperatorTable();
        TemporalOperators.Register(table);
        return table;
    }

    private static ComparisonOperatorTable CreateComparison()
    {
        var table = new ComparisonOperatorTable();
        TemporalOperators.Register(table);
        return table;
    }
}
