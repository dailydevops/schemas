namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>xsl:copy-of</c>: evaluates <see langword="select"/> into the sink. A tree sink copies the nodes through the Xml pump; an item sink gets a
/// copy of each node in a temporary tree (XSLT 2.0 section 11.9.2).
/// </summary>
internal sealed class XsltCopyOfExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly QueryExpression _select;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltCopyOfExecutable(QueryExpression select, DocumentLocation location, int slotEnd)
    {
        Argument.ThrowIfNull(select);

        _select = select;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(select).ToArray();
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        if (sink.Kind != SequenceSinkKind.Items)
        {
            return frame.Run.TryEvaluate(_select, frame.Focus, sink) == QueryStatus.Problem
                ? QueryStatus.Problem
                : ConstructionOutput.Surface(frame, sink, _location);
        }

        var copies = new CopySink(frame, sink, _location);
        return frame.Run.TryEvaluate(_select, frame.Focus, copies) == QueryStatus.Problem || copies.Failed
            ? QueryStatus.Problem
            : QueryStatus.Ok;
    }
}
