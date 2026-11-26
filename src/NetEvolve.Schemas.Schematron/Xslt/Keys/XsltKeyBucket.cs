namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The nodes of one key value of an index, in document order without duplicates. The sequence of the whole bucket is built once, on
/// the first lookup, so a lookup of one value without a subtree returns it without a copy. Not thread-safe: one per run.
/// </summary>
internal sealed class XsltKeyBucket
{
    private readonly List<XPathNode> _nodes = [];
    private XdmSequence _sequence;

    /// <summary>Gets the nodes.</summary>
    internal IReadOnlyList<XPathNode> Nodes => _nodes;

    /// <summary>Gets the nodes as a sequence.</summary>
    internal XdmSequence Sequence
    {
        get
        {
            if (_sequence.IsEmpty)
            {
                var items = new XdmItem[_nodes.Count];
                for (var i = 0; i < items.Length; i++)
                {
                    items[i] = XdmItem.FromNode(_nodes[i]);
                }

                _sequence = XdmSequence.Create(items);
            }

            return _sequence;
        }
    }

    // The nodes arrive in document order, so a node that was added last is the only duplicate that can occur.
    internal void Add(XPathNode node)
    {
        if (_nodes.Count == 0 || _nodes[_nodes.Count - 1] != node)
        {
            _nodes.Add(node);
        }
    }
}
