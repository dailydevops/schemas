namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>xsl:copy</c>: copies the context item (XSLT 2.0 section 11.9.1). The context item of the frame is the item to copy; without one
/// it is the dynamic error XPDY0002.
/// </summary>
internal sealed class XsltCopyExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly XsltBodyExecutable _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltCopyExecutable(XsltBodyExecutable body, DocumentLocation location, int slotEnd)
    {
        ArgumentNullException.ThrowIfNull(body);

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
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        var item = frame.Focus.Item;
        if (item.Kind == XdmItemKind.None)
        {
            return ConstructionOutput.Error(frame, _location, "XPDY0002", "The context item of xsl:copy is absent.");
        }

        if (!item.TryGetNode(out var node))
        {
            sink.Item(in item);
            return ConstructionOutput.Surface(frame, sink, _location);
        }

        return node.Node.Kind switch
        {
            XmlNodeKind.Element => CopyElement(frame, sink, node),
            XmlNodeKind.Document => ElementConstruction.ConstructDocument(frame, sink, _location, _body),
            _ => ConstructionOutput.PushCopy(frame, sink, in item, _location),
        };
    }

    // The new element has the name and the namespace nodes of the source; its attributes and children come from the content.
    private QueryStatus CopyElement(XsltFrame frame, ISequenceSink sink, XPathNode source)
    {
        var xml = source.Node;
        var names = source.Tree!.Names;
        var name = xml.Name;
        var namespaces = new List<NamespaceBinding>();
        for (var ns = xml.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            namespaces.Add(new NamespaceBinding(names.GetString(ns.Name.Local), ns.StringValue));
        }

        return ElementConstruction.Construct(
            frame,
            sink,
            _location,
            new ExpandedName(
                names.GetString(name.Prefix),
                names.GetString(name.Local),
                names.GetString(name.NamespaceUri)
            ),
            namespaces.ToArray(),
            [],
            _body
        );
    }
}
