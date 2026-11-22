namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>xsl:attribute</c>: expands the computed name, evaluates the value, and adds the attribute to the content of the element
/// (XSLT 2.0 section 11.3); after a child node it is the error XTDE0410, in the content of a document XTDE0420.
/// </summary>
internal sealed class XsltAttributeExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly NameTemplate _name;
    private readonly AttributeValueTemplate? _separator;
    private readonly QueryExpression? _select;
    private readonly XsltBodyExecutable? _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltAttributeExecutable(
        NameTemplate name,
        AttributeValueTemplate? separator,
        QueryExpression? select,
        XsltBodyExecutable? body,
        DocumentLocation location,
        int slotEnd
    )
    {
        ArgumentNullException.ThrowIfNull(name);

        _name = name;
        _separator = separator;
        _select = select;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet()
            .Add(name.ReferencedSlots)
            .Add(separator is null ? default : separator.ReferencedSlots)
            .Add(select)
            .Add(body)
            .ToArray();
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

        if (_name.TryResolve(frame, _location, out var name) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        // The default separator is one space for select and none for a sequence constructor (XSLT 2.0 section 11.3).
        var separator = _select is not null ? " " : string.Empty;
        if (_separator is not null && _separator.TryEvaluate(frame, _location, out separator) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        return
            SimpleContent.TryEvaluate(frame, _select, _body, separator, _location, out var value) == QueryStatus.Problem
            ? QueryStatus.Problem
            : ConstructionOutput.PushAttribute(
                frame,
                sink,
                new PendingAttribute(name.Prefix, name.Local, name.Uri, value),
                _location
            );
    }
}
