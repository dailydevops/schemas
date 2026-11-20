namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>xsl:document</c>: builds a document node from the content (XSLT 2.0 section 11.1.1). An item sink returns the document node; a
/// tree sink takes its children.
/// </summary>
internal sealed class XsltDocumentExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly XsltBodyExecutable _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltDocumentExecutable(XsltBodyExecutable body, DocumentLocation location, int slotEnd)
    {
        Argument.ThrowIfNull(body);

        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(body).ToArray();
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

        return ElementConstruction.ConstructDocument(frame, sink, _location, _body);
    }
}
