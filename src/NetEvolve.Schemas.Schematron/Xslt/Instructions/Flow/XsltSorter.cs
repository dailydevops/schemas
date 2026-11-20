namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Sorts a sequence by a sort key specification (XSLT 2.0 section 13.1): every key is evaluated once per item with the focus of the
/// initial sequence, atomized, and compared by the XPath rules for its type (numbers with the promotion of XPath 2.0, strings by
/// codepoint, booleans), and the order is stable in both directions. An empty key sorts before every value, and NaN before every
/// other number. Instructions that take a sort key specification share this class.
/// </summary>
/// <remarks>
/// Limitation: two values are compared pairwise by the promotion of XPath 2.0, and not all by the most specific type to which every
/// value converts, so a mix of integers beyond 2^53 and doubles can order differently from the specification. The only collation is
/// the codepoint collation (DEF-43, #55).
/// Keys of type xs:date, xs:dateTime, xs:time, or a duration are not sortable until the XPath package compares them (XP-26); a sort of two
/// such values raises XTDE1030.
/// </remarks>
internal static class XsltSorter
{
    /// <summary>Computes the order of the items.</summary>
    /// <param name="frame">The frame; it evaluates the keys with its run and records a problem the instruction found.</param>
    /// <param name="keys">The sort keys, the primary key first.</param>
    /// <param name="items">The initial sequence; it answers <c>last()</c> for the key expressions.</param>
    /// <param name="order">The indexes of the items in sorted order when the status is <see cref="QueryStatus.Ok"/>.</param>
    /// <param name="beforeItem">
    /// Called with the index of an item before the keys of that item are evaluated, so that an instruction can bind what its keys read
    /// (<c>xsl:for-each-group</c> binds the current group and the grouping key of the group); <see langword="null"/> for none.
    /// </param>
    /// <returns><see cref="QueryStatus.Problem"/> when a key failed, has more than one item (XTTE1020), or two values cannot be compared (XTDE1030).</returns>
    internal static QueryStatus TrySort(
        XsltFrame frame,
        XsltSortKey[] keys,
        ItemBuffer items,
        out int[] order,
        Action<int>? beforeItem = null
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(keys);
        Argument.ThrowIfNull(items);

        var count = items.Count;
        var width = keys.Length;
        order = new int[count];
        for (var i = 0; i < count; i++)
        {
            order[i] = i;
        }

        var values = new SortValue[count * width];
        using var key = new ItemBuffer(1);
        for (var i = 0; i < count; i++)
        {
            var item = items.Get(i);
            var focus = QueryFocus.ForItem(in item, i + 1, -1, items);
            beforeItem?.Invoke(i);
            for (var k = 0; k < width; k++)
            {
                var status = TryEvaluateKey(frame, keys[k], in focus, key, out values[(i * width) + k]);
                if (status == QueryStatus.Problem)
                {
                    return QueryStatus.Problem;
                }
            }
        }

        if (count < 2)
        {
            return QueryStatus.Ok;
        }

        var comparer = new KeyComparer(keys, values);
        order = comparer.Sort(order);
        return comparer.Incomparable < 0
            ? QueryStatus.Ok
            : frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    "The values of a sort key cannot be compared with each other, because they are of types that the XPath lt operator does not compare (XTDE1030).",
                    keys[comparer.Incomparable].Location,
                    "XTDE1030"
                )
            );
    }

    private static QueryStatus TryEvaluateKey(
        XsltFrame frame,
        XsltSortKey sort,
        in QueryFocus focus,
        ItemBuffer buffer,
        out SortValue value
    )
    {
        value = SortValue.Empty;
        buffer.Reset();
        if (frame.Run.TryEvaluate(sort.Select, in focus, buffer) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (buffer.Overflow)
        {
            return frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    "A sort key value is a sequence of more than one item (XTTE1020).",
                    sort.Location,
                    "XTTE1020"
                )
            );
        }

        if (buffer.Count == 0)
        {
            return QueryStatus.Ok;
        }

        var item = buffer.Get(0);
        return SortValue.TryCreate(in item, sort.AsText, out value)
            ? QueryStatus.Ok
            : frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    $"A sort key value of kind {item.Kind} has no text form yet (limitation).",
                    sort.Location
                )
            );
    }

    /// <summary>Compares the items by their key values; a pair that cannot be compared counts as equal and is remembered.</summary>
    private sealed class KeyComparer
    {
        private readonly XsltSortKey[] _keys;
        private readonly SortValue[] _values;

        internal KeyComparer(XsltSortKey[] keys, SortValue[] values)
        {
            _keys = keys;
            _values = values;
        }

        /// <summary>Gets the index of the first key with two values that cannot be compared, or -1.</summary>
        internal int Incomparable { get; private set; } = -1;

        // A stable bottom-up merge sort: a right item goes first only when it is strictly less, so equal items keep their order in
        // both directions.
        internal int[] Sort(int[] order)
        {
            var count = order.Length;
            var from = order;
            var to = new int[count];
            for (var run = 1; run < count; run <<= 1)
            {
                for (var low = 0; low < count; low += 2 * run)
                {
                    var middle = Math.Min(low + run, count);
                    var high = Math.Min(low + (2 * run), count);
                    int left = low,
                        right = middle,
                        at = low;
                    while (left < middle && right < high)
                    {
                        to[at++] = Compare(from[right], from[left]) < 0 ? from[right++] : from[left++];
                    }

                    while (left < middle)
                    {
                        to[at++] = from[left++];
                    }

                    while (right < high)
                    {
                        to[at++] = from[right++];
                    }
                }

                (from, to) = (to, from);
            }

            return from;
        }

        private int Compare(int a, int b)
        {
            var width = _keys.Length;
            for (var k = 0; k < width; k++)
            {
                var order = CompareValues(_values[(a * width) + k], _values[(b * width) + k], k);
                if (order != 0)
                {
                    return _keys[k].Descending ? -order : order;
                }
            }

            return 0;
        }

        private int CompareValues(in SortValue x, in SortValue y, int key)
        {
            if (x.Category == SortCategory.Empty || y.Category == SortCategory.Empty)
            {
                return x.Category == y.Category ? 0
                    : x.Category == SortCategory.Empty ? -1
                    : 1;
            }

            if (x.Category != y.Category || x.Category == SortCategory.Other)
            {
                if (Incomparable < 0)
                {
                    Incomparable = key;
                }

                return 0;
            }

            return x.Category switch
            {
                SortCategory.Text => Math.Sign(CodepointComparison.Compare(x.Text!, y.Text!)),
                SortCategory.Number => CompareNumbers(x.Item, y.Item),
                _ => BooleanComparison.Compare(ComparisonRelation.LessThan, x.Item, y.Item) ? -1
                : BooleanComparison.Compare(ComparisonRelation.GreaterThan, x.Item, y.Item) ? 1
                : 0,
            };
        }

        // NaN is equal to itself and less than any other number (XSLT 2.0 section 13.1.2); the XPath comparison orders it against nothing.
        private static int CompareNumbers(in XdmItem x, in XdmItem y)
        {
            var xNaN = IsNaN(in x);
            var yNaN = IsNaN(in y);
            if (xNaN || yNaN)
            {
                return xNaN == yNaN ? 0
                    : xNaN ? -1
                    : 1;
            }

            return NumericComparison.Compare(ComparisonRelation.LessThan, x, y) ? -1
                : NumericComparison.Compare(ComparisonRelation.GreaterThan, x, y) ? 1
                : 0;
        }

        private static bool IsNaN(in XdmItem item) =>
            item.Kind == XdmItemKind.XsDouble
                ? item.TryGetDouble(out var d) && double.IsNaN(d)
                : item.Kind == XdmItemKind.XsFloat && item.TryGetFloat(out var f) && float.IsNaN(f);
    }
}
