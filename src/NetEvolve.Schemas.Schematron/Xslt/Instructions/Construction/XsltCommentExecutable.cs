namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary><c>xsl:comment</c>: evaluates the simple content and pushes a comment node (XSLT 2.0 section 11.8).</summary>
internal sealed class XsltCommentExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly QueryExpression? _select;
    private readonly XsltBodyExecutable? _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltCommentExecutable(
        QueryExpression? select,
        XsltBodyExecutable? body,
        DocumentLocation location,
        int slotEnd
    )
    {
        _select = select;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(select).Add(body).ToArray();
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

        return SimpleContent.TryEvaluate(frame, _select, _body, " ", _location, out var value) == QueryStatus.Problem
            ? QueryStatus.Problem
            : ConstructionOutput.PushComment(frame, sink, SimpleContent.ToCommentValue(value), _location);
    }
}
