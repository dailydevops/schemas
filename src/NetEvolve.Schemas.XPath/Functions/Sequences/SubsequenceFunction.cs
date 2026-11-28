namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:subsequence</c> (Functions and Operators 1.0, 15.1.9): the items whose position <c>p</c> satisfies
/// <c>round($startingLoc) &lt;= p &lt; round($startingLoc) + round($length)</c>, or <c>round($startingLoc) &lt;= p</c> without a
/// length. The arguments are doubles that are rounded as <c>fn:round</c> rounds, half toward positive infinity, and the comparison is
/// made as written, so NaN selects nothing, an infinite start selects nothing or everything, and an infinite length selects what follows the start.
/// </summary>
internal sealed class SubsequenceFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="SubsequenceFunction"/> class.</summary>
    public SubsequenceFunction()
        : base("subsequence", 2, 3, [Items, OneDouble, OneDouble]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var source = invocation.Arguments[0];
        _ = invocation.Arguments[1][0].TryGetDouble(out var startArgument);
        var start = Round(startArgument);
        var end = double.PositiveInfinity;
        if (invocation.Arguments.Length > 2)
        {
            _ = invocation.Arguments[2][0].TryGetDouble(out var lengthArgument);
            end = start + Round(lengthArgument);
        }

        // NaN selects nothing: every comparison of the specification is false. The window is the positions from first up to, not including, stop.
        if (double.IsNaN(start) || double.IsNaN(end))
        {
            return XPathEvaluationStatus.Ok;
        }

        if (source.IsUnreadLazy)
        {
            invocation.Result = Stream(source, start, end);
            return XPathEvaluationStatus.Ok;
        }

        var count = source.Count;
        var first =
            start <= 1 ? 1
            : start > count ? count + 1
            : (int)start;
        var stop =
            end > count ? count + 1
            : end <= 1 ? 1
            : (int)end;
        if (stop <= first)
        {
            return XPathEvaluationStatus.Ok;
        }

        if (first == 1 && stop == count + 1)
        {
            invocation.Result = source;
            return XPathEvaluationStatus.Ok;
        }

        var selected = default(XdmItemList);
        try
        {
            for (var position = first; position < stop; position++)
            {
                selected.Add(source[position - 1]);
            }

            invocation.Result = selected.ToSequence();
        }
        finally
        {
            selected.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }

    // An unread lazy source is pulled up to the end of the window and no further: a position p is selected when start <= p < end, so the
    // loop stops at the first position at or after end. An infinite start selects nothing and reads nothing.
    private static XdmSequence Stream(in XdmSequence source, double start, double end)
    {
        if (double.IsPositiveInfinity(start) || end <= 1 || end <= start)
        {
            return XdmSequence.Empty;
        }

        var selected = default(XdmItemList);
        try
        {
            using var cursor = source.Open();
            for (long position = 1; position < end && cursor.MoveNext(); position++)
            {
                if (position >= start)
                {
                    selected.Add(cursor.Current);
                }
            }

            return selected.ToSequence();
        }
        finally
        {
            selected.Dispose();
        }
    }

    // fn:round: the closest integer, half toward positive infinity. The fraction is read from the floor, so that no value is
    // pushed over a boundary by an addition (0.49999999999999994 rounds to 0).
    internal static double Round(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return value;
        }

        var floor = Math.Floor(value);
        return value - floor >= 0.5 ? floor + 1 : floor;
    }
}
