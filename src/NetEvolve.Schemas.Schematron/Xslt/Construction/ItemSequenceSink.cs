namespace NetEvolve.Schemas.Schematron.Xslt.Construction;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The <see cref="SequenceSinkKind.Items"/> sink: items are kept as pushed (nodes by reference, no copy), adjacent text
/// coalesces to one text node item, and empty text creates none.
/// </summary>
internal sealed class ItemSequenceSink : TemporaryTreeSink
{
    private readonly List<XdmItem> _items = [];
    private char[] _pending = new char[64];
    private int _pendingLength;

    internal ItemSequenceSink(XmlNameTable names)
        : base(names) { }

    public override SequenceSinkKind Kind => SequenceSinkKind.Items;

    public override IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Gets the sequence pushed so far; pending text becomes a text node item.</summary>
    internal XdmSequence Result
    {
        get
        {
            FlushText();
            return XdmSequence.Create(_items.ToArray());
        }
    }

    public override void Item(in XdmItem item)
    {
        Debug.Assert(item.Kind != XdmItemKind.None, "An item must have a value.");

        FlushText();
        _items.Add(item);
    }

    public override void Text(ReadOnlySpan<char> text)
    {
        if (_pendingLength + text.Length > _pending.Length)
        {
            Array.Resize(ref _pending, Math.Max(_pending.Length * 2, _pendingLength + text.Length));
        }

        text.CopyTo(_pending.AsSpan(_pendingLength));
        _pendingLength += text.Length;
    }

    private void FlushText()
    {
        if (_pendingLength == 0)
        {
            return;
        }

        using var builder = new XmlTreeBuilder(Names, null, false);
        builder.Text(_pending.AsSpan(0, _pendingLength), XmlTextKind.Text);
        _pendingLength = 0;
        _items.Add(XdmItem.FromNode(new XPathNode(new XmlNode(builder.Complete(), 0))));
    }
}
