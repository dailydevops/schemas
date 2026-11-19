namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Sequences;

/// <summary>
/// <c>fn:substring</c> (Functions and Operators 1.0, 7.4.3): the characters whose position <c>p</c> satisfies
/// <c>round($startingLoc) &lt;= p &lt; round($startingLoc) + round($length)</c>, or <c>round($startingLoc) &lt;= p</c> without a length.
/// Positions count code points, starting at 1. The arguments are doubles that are rounded as <c>fn:round</c> rounds (half toward
/// positive infinity) and compared as written, so NaN selects nothing, an infinite start selects nothing or everything, and an infinite
/// length selects what follows the start.
/// </summary>
internal sealed class SubstringFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="SubstringFunction"/> class.</summary>
    public SubstringFunction()
        : base("substring", 2, 3, [OptionalString, OneDouble, OneDouble]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var source = Text(invocation.Arguments[0]);
        _ = invocation.Arguments[1][0].TryGetDouble(out var startArgument);
        var start = SubsequenceFunction.Round(startArgument);
        var end = double.PositiveInfinity;
        if (invocation.Arguments.Length > 2)
        {
            _ = invocation.Arguments[2][0].TryGetDouble(out var lengthArgument);
            end = start + SubsequenceFunction.Round(lengthArgument);
        }

        Return(ref invocation, Window(source, start, end));
        return XPathEvaluationStatus.Ok;
    }

    // The positions from start up to, not including, end: NaN selects nothing, the window is clamped to 1 to count + 1.
    private static string Window(string source, double start, double end)
    {
        if (double.IsNaN(start) || double.IsNaN(end))
        {
            return string.Empty;
        }

        var count = CodePoints.Count(source.AsSpan());
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
            return string.Empty;
        }

        if (first == 1 && stop == count + 1)
        {
            return source;
        }

        // Without a surrogate pair a position is a code unit; with one, the offsets are found by reading code points.
        if (count == source.Length)
        {
            return source.Substring(first - 1, stop - first);
        }

        var span = source.AsSpan();
        var offset = 0;
        for (var position = 1; position < first; position++)
        {
            _ = CodePoints.Next(span, ref offset);
        }

        var from = offset;
        for (var position = first; position < stop; position++)
        {
            _ = CodePoints.Next(span, ref offset);
        }

        return source.Substring(from, offset - from);
    }
}
