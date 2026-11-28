namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>group-adjacent</c>: the expression is evaluated for every item with the item as the focus and must give exactly one atomic value
/// (XTTE1100); an item starts a new group when its key differs from the key of the item before it, and joins that group otherwise.
/// </summary>
internal sealed class XsltGroupAdjacentGrouping : XsltGrouping
{
    private readonly QueryExpression _key;

    internal XsltGroupAdjacentGrouping(QueryExpression key, DocumentLocation location)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(key);

        _key = key;
    }

    /// <inheritdoc />
    internal override ReadOnlySpan<int> ReferencedSlots => _key.ReferencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryGroup(XsltFrame frame, ItemBuffer items, List<XsltGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(groups);

        // Two values are enough to tell that the key is not one value: a second one is dropped and flagged, and counts as two as well.
        using var keys = new ItemBuffer(2);
        string? previous = null;
        for (var i = 0; i < items.Count; i++)
        {
            frame.CancellationToken.ThrowIfCancellationRequested();
            keys.Reset();
            var item = items.Get(i);
            if (frame.Run.TryEvaluate(_key, QueryFocus.ForItem(in item, i + 1, -1, items), keys) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            if (keys.Count != 1)
            {
                return Fail(
                    frame,
                    SchematronDiagnosticCodes.EvaluationFailed,
                    "The group-adjacent expression gave no value or more than one value for an item (XTTE1100).",
                    "XTTE1100"
                );
            }

            if (TryKey(frame, keys.Get(0), out var atom, out var text) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            if (previous is null || !string.Equals(previous, text, StringComparison.Ordinal))
            {
                groups.Add(new XsltGroup(in atom));
            }

            previous = text;
            groups[groups.Count - 1].Members.Add(i);
        }

        return QueryStatus.Ok;
    }
}
