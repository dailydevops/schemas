namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The focus of an evaluation: context item, position, and size.</summary>
internal readonly struct QueryFocus
{
    internal QueryFocus(XPathNode node, int position, int size)
    {
        Node = node;
        Position = position;
        Size = size;
    }

    internal XPathNode Node { get; }

    internal int Position { get; }

    /// <summary>The size of the context, -1 until <c>last()</c> needs it, so no count is taken per node.</summary>
    internal int Size { get; }
}
