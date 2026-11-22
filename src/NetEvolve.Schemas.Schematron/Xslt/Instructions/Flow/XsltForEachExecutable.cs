namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// <c>xsl:for-each</c>: evaluates <see langword="select"/> with the focus of the frame into a bounded buffer, sorts it when sort keys
/// are present, and runs the body once per item with the focus (item, position in the sorted sequence, size). The size is left at -1
/// and answered by the buffer when <c>last()</c> asks for it (design #150 RD-7). Every iteration is a cancellation point
/// (design #150 EP-16), and the focus of the caller is restored when the loop ends, also when it fails.
/// </summary>
internal sealed class XsltForEachExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly QueryExpression _select;
    private readonly XsltSortKey[] _keys;
    private readonly XsltBodyExecutable _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltForEachExecutable(
        QueryExpression select,
        XsltSortKey[] keys,
        XsltBodyExecutable body,
        int slotEnd,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(body);

        _select = select;
        _keys = keys;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        var slots = new SlotSet().Add(select).Add(body);
        foreach (var key in keys)
        {
            _ = slots.Add(key);
        }

        _slots = slots.ToArray();
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        using var items = new ItemBuffer(frame.MaxSequenceLength);
        if (frame.Run.TryEvaluate(_select, frame.Focus, items) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (items.Overflow)
        {
            return frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationLimitStop,
                    $"The select of xsl:for-each has more than the limit of {frame.MaxSequenceLength} items.",
                    _location
                )
            );
        }

        var order = Array.Empty<int>();
        if (_keys.Length > 0 && XsltSorter.TrySort(frame, _keys, items, out order) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        var outer = frame.Focus;
        try
        {
            for (var i = 0; i < items.Count; i++)
            {
                frame.CancellationToken.ThrowIfCancellationRequested();
                var item = items.Get(_keys.Length == 0 ? i : order[i]);
                frame.Focus = QueryFocus.ForItem(in item, i + 1, -1, items);
                if (_body.TryExecute(frame, sink) == QueryStatus.Problem)
                {
                    return QueryStatus.Problem;
                }
            }

            return QueryStatus.Ok;
        }
        finally
        {
            frame.Focus = outer;
        }
    }
}
