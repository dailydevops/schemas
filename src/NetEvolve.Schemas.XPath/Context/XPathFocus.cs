namespace NetEvolve.Schemas.XPath.Context;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The focus of an evaluation (XPath 2.0, 2.1.2): context item, position, and size. A value, so a child focus costs no allocation.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XPathFocus
{
    internal XPathFocus(XdmItem? item, int position, int size)
    {
        Item = item;
        Position = position;
        Size = size;
    }

    /// <summary>Gets the context item; <see langword="null"/> when the focus is absent.</summary>
    internal XdmItem? Item { get; }

    /// <summary>Gets the one-based position; zero when the focus is absent.</summary>
    internal int Position { get; }

    /// <summary>Gets the size of the sequence the item came from; zero when the focus is absent.</summary>
    internal int Size { get; }
}
