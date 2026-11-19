namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The focus of an evaluation: context item, position, and size. The item is a node or an atomic value; <see cref="Node"/> is
/// the node of a node item and the default node otherwise. The default focus has no item.
/// </summary>
internal readonly struct QueryFocus
{
    private readonly IFocusSizeSource? _sizeSource;

    internal QueryFocus(XPathNode node, int position, int size)
        : this(node.Tree is null ? default : XdmItem.FromNode(node), node, position, size, null) { }

    private QueryFocus(XdmItem item, XPathNode node, int position, int size, IFocusSizeSource? sizeSource)
    {
        Item = item;
        Node = node;
        Position = position;
        Size = size;
        _sizeSource = sizeSource;
    }

    /// <summary>Gets the context item, or an item of kind <see cref="XdmItemKind.None"/> when the focus is absent.</summary>
    internal XdmItem Item { get; }

    internal XPathNode Node { get; }

    internal int Position { get; }

    /// <summary>The size of the context, -1 until <c>last()</c> needs it, so no count is taken per node; read it with <see cref="ResolveSize"/>.</summary>
    internal int Size { get; }

    /// <summary>Creates the focus of an item, which can be atomic.</summary>
    /// <param name="item">The context item.</param>
    /// <param name="position">The context position.</param>
    /// <param name="size">The context size, or -1 when <paramref name="sizeSource"/> knows it or it is unknown.</param>
    /// <param name="sizeSource">Answers the size when <c>last()</c> asks for it; <see langword="null"/> for none.</param>
    /// <returns>The focus.</returns>
    internal static QueryFocus ForItem(in XdmItem item, int position, int size, IFocusSizeSource? sizeSource = null) =>
        new(item, item.TryGetNode(out var node) ? node : default, position, size, sizeSource);

    /// <summary>Gets the size of the context: <see cref="Size"/> when it is known, otherwise the answer of the size source, otherwise -1.</summary>
    /// <returns>The size, or -1 when it is not known.</returns>
    internal int ResolveSize() => Size >= 0 ? Size : _sizeSource?.GetSize() ?? -1;
}
