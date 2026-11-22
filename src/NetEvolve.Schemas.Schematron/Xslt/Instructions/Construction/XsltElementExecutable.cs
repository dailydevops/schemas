namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary><c>xsl:element</c>: expands the computed name, then builds the element with the content (XSLT 2.0 section 11.2).</summary>
internal sealed class XsltElementExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly NameTemplate _name;
    private readonly XsltBodyExecutable _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltElementExecutable(NameTemplate name, XsltBodyExecutable body, DocumentLocation location, int slotEnd)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(body);

        _name = name;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(name.ReferencedSlots).Add(body).ToArray();
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

        return _name.TryResolve(frame, _location, out var name) == QueryStatus.Problem
            ? QueryStatus.Problem
            : ElementConstruction.Construct(frame, sink, _location, in name, [], [], _body);
    }
}
