namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;

using System;
using System.Buffers;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A node source over an array that can throw after a number of nodes.</summary>
internal struct ArraySource : IXPathNodeSource
{
    private readonly XPathNode[] _nodes;
    private readonly int _throwAt;
    private int _next;

    public ArraySource(XPathNode[] nodes, int throwAt = -1)
    {
        _nodes = nodes;
        _throwAt = throwAt;
        _next = 0;
    }

    public bool TryGetNext(out XPathNode node)
    {
        if (_next == _throwAt)
        {
            throw new InvalidOperationException("The source failed.");
        }

        if (_next < _nodes.Length)
        {
            node = _nodes[_next++];
            return true;
        }

        node = default;
        return false;
    }
}
