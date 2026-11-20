namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>group-by</c>: the expression is evaluated for every item with the item as the focus (position and size of the population), each
/// atomic value of the result is a grouping key of the item, and the item joins the group of every distinct key, in the order of the
/// keys. An item with no key joins no group, and an item joins a group once however often its keys repeat.
/// </summary>
/// <remarks>The memberships of all items are bounded by the sequence limit of the frame, as the items of the population are (design #150 EP-16).</remarks>
internal sealed class XsltGroupByGrouping : XsltGrouping
{
    private readonly QueryExpression _key;

    internal XsltGroupByGrouping(QueryExpression key, DocumentLocation location)
        : base(location)
    {
        Argument.ThrowIfNull(key);

        _key = key;
    }

    /// <inheritdoc />
    internal override ReadOnlySpan<int> ReferencedSlots => _key.ReferencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryGroup(XsltFrame frame, ItemBuffer items, List<XsltGroup> groups)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(items);
        Argument.ThrowIfNull(groups);

        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        using var keys = new ItemBuffer(frame.MaxSequenceLength);
        var memberships = 0;
        for (var i = 0; i < items.Count; i++)
        {
            frame.CancellationToken.ThrowIfCancellationRequested();
            keys.Reset();
            var item = items.Get(i);
            if (frame.Run.TryEvaluate(_key, QueryFocus.ForItem(in item, i + 1, -1, items), keys) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            if (keys.Overflow)
            {
                return LimitReached(
                    frame,
                    $"The group-by expression has more than the limit of {frame.MaxSequenceLength} keys for an item."
                );
            }

            for (var k = 0; k < keys.Count; k++)
            {
                if (TryKey(frame, keys.Get(k), out var atom, out var text) == QueryStatus.Problem)
                {
                    return QueryStatus.Problem;
                }

                if (!index.TryGetValue(text, out var group))
                {
                    group = groups.Count;
                    index.Add(text, group);
                    groups.Add(new XsltGroup(in atom));
                }

                var members = groups[group].Members;
                if (members.Count > 0 && members[members.Count - 1] == i)
                {
                    continue;
                }

                if (++memberships > frame.MaxSequenceLength)
                {
                    return LimitReached(
                        frame,
                        $"The groups of xsl:for-each-group have more than the limit of {frame.MaxSequenceLength} members."
                    );
                }

                members.Add(i);
            }
        }

        return QueryStatus.Ok;
    }

    private QueryStatus LimitReached(XsltFrame frame, string message) =>
        Fail(frame, SchematronDiagnosticCodes.EvaluationLimitStop, message);
}
