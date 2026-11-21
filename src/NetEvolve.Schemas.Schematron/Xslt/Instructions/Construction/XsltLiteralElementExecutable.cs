namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// A literal result element: builds the element with the namespace nodes of the stylesheet that are not excluded, the attributes
/// with their effective values, and the content (XSLT 2.0 section 11.1).
/// </summary>
internal sealed class XsltLiteralElementExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly ExpandedName _name;
    private readonly NamespaceBinding[] _namespaces;
    private readonly LiteralAttribute[] _attributes;
    private readonly XsltBodyExecutable _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltLiteralElementExecutable(
        ExpandedName name,
        NamespaceBinding[] namespaces,
        LiteralAttribute[] attributes,
        XsltBodyExecutable body,
        DocumentLocation location,
        int slotEnd
    )
    {
        Argument.ThrowIfNull(namespaces);
        Argument.ThrowIfNull(attributes);
        Argument.ThrowIfNull(body);

        _name = name;
        _namespaces = namespaces;
        _attributes = attributes;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        var slots = new SlotSet().Add(body);
        foreach (var attribute in attributes)
        {
            _ = slots.Add(attribute.Template.ReferencedSlots);
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
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        var values = new PendingAttribute[_attributes.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var attribute = _attributes[i];
            if (attribute.Template.TryEvaluate(frame, _location, out var value) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            values[i] = new PendingAttribute(attribute.Prefix, attribute.Local, attribute.Uri, value);
        }

        return ElementConstruction.Construct(frame, sink, _location, in _name, _namespaces, values, _body);
    }
}
