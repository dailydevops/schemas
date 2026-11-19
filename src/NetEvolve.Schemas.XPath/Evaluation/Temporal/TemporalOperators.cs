namespace NetEvolve.Schemas.XPath.Evaluation.Temporal;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the entries of the date, time, and duration types in the arithmetic and the comparison
/// operator tables (XPath 2.0, B.2), added to the tables next to the numeric entries, without a change to the numeric code.
/// </summary>
/// <remarks>
/// <para>
/// Arithmetic: <c>+</c> and <c>-</c> for two durations of one subtype, for a date or a dateTime with either subtype, and for a time with
/// <c>xs:dayTimeDuration</c> (the moment first for <c>-</c>); <c>-</c> for two moments of one type; <c>*</c> and <c>div</c> for a
/// duration of either subtype and a number, and <c>div</c> for two durations of one subtype. Every other pair has no entry, so the
/// evaluation raises XPTY0004: the sum of two dates, any operation on <c>xs:duration</c>, a year-month with a day-time duration, and
/// <c>idiv</c> and <c>mod</c>.
/// </para>
/// <para>
/// Comparison: the six relations for <c>xs:dateTime</c>, <c>xs:date</c>, <c>xs:time</c>, <c>xs:yearMonthDuration</c>, and
/// <c>xs:dayTimeDuration</c>; <c>eq</c> and <c>ne</c> for <c>xs:duration</c> and the five Gregorian types. A year-month and a day-time
/// duration are durations, so their equality finds the row of <c>xs:duration</c> by subtype substitution, and an ordering of them has no
/// row.
/// </para>
/// <para>
/// Only the rows that read the implicit timezone are registered with the dynamic context; the others are registered without it. The
/// implementations are those of <see cref="TemporalArithmetic"/> and <see cref="TemporalComparison"/>.
/// </para>
/// </remarks>
internal static class TemporalOperators
{
    private static readonly XdmTypeId[] Durations = [XdmTypeId.YearMonthDuration, XdmTypeId.DayTimeDuration];

    private static readonly XdmTypeId[] Numbers =
    [
        XdmTypeId.XsInteger,
        XdmTypeId.XsDecimal,
        XdmTypeId.XsFloat,
        XdmTypeId.XsDouble,
    ];

    private static readonly XdmTypeId[] Moments = [XdmTypeId.DateTime, XdmTypeId.Date, XdmTypeId.Time];

    private static readonly XdmTypeId[] Gregorian =
    [
        XdmTypes.Get("gYearMonth"),
        XdmTypes.Get("gYear"),
        XdmTypes.Get("gMonthDay"),
        XdmTypes.Get("gDay"),
        XdmTypes.Get("gMonth"),
    ];

    /// <summary>Registers the arithmetic entries of the date, time, and duration types.</summary>
    /// <param name="table">The table.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">An entry is already registered.</exception>
    public static void Register(ArithmeticOperatorTable table)
    {
        Argument.ThrowIfNull(table);

        foreach (var duration in Durations)
        {
            table.Register(ArithmeticOperator.Add, duration, duration, TemporalArithmetic.AddDurations);
            table.Register(ArithmeticOperator.Subtract, duration, duration, TemporalArithmetic.SubtractDurations);
            table.Register(ArithmeticOperator.Divide, duration, duration, TemporalArithmetic.DivideDurations);
            foreach (var number in Numbers)
            {
                table.Register(ArithmeticOperator.Multiply, duration, number, TemporalArithmetic.MultiplyDuration);
                table.Register(ArithmeticOperator.Multiply, number, duration, TemporalArithmetic.MultiplyDuration);
                table.Register(ArithmeticOperator.Divide, duration, number, TemporalArithmetic.DivideDuration);
            }
        }

        foreach (var moment in Moments)
        {
            // A time takes a day-time duration only, a date and a dateTime take both subtypes.
            foreach (var duration in Durations)
            {
                if (moment == XdmTypeId.Time && duration == XdmTypeId.YearMonthDuration)
                {
                    continue;
                }

                table.Register(ArithmeticOperator.Add, moment, duration, TemporalArithmetic.AddToMoment);
                table.Register(ArithmeticOperator.Add, duration, moment, TemporalArithmetic.AddToMoment);
                table.Register(ArithmeticOperator.Subtract, moment, duration, TemporalArithmetic.SubtractFromMoment);
            }

            table.RegisterContextual(ArithmeticOperator.Subtract, moment, moment, TemporalArithmetic.SubtractMoments);
        }
    }

    /// <summary>Registers the comparison entries of the date, time, and duration types.</summary>
    /// <param name="table">The table.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">An entry is already registered.</exception>
    public static void Register(ComparisonOperatorTable table)
    {
        Argument.ThrowIfNull(table);

        for (var relation = ComparisonRelation.Equal; relation <= ComparisonRelation.GreaterThanOrEqual; relation++)
        {
            foreach (var moment in Moments)
            {
                table.RegisterContextual(relation, moment, moment, TemporalComparison.CompareMoments);
            }

            foreach (var duration in Durations)
            {
                table.Register(relation, duration, duration, TemporalComparison.CompareDurations);
            }
        }

        // The types without an order: xs:duration is partially ordered and the Gregorian types are ordered by XML Schema only, so XPath 2.0 maps eq and ne.
        foreach (var relation in new[] { ComparisonRelation.Equal, ComparisonRelation.NotEqual })
        {
            table.Register(relation, XdmTypeId.Duration, XdmTypeId.Duration, TemporalComparison.CompareDurations);
            foreach (var type in Gregorian)
            {
                table.RegisterContextual(relation, type, type, TemporalComparison.CompareMoments);
            }
        }
    }
}
