namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;

using System;
using System.Buffers;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A pool that counts the arrays it has handed out and not yet got back.</summary>
internal sealed class TrackingPool : ArrayPool<XPathNode>
{
    private readonly ArrayPool<XPathNode> _inner = Create();

    public int Rented { get; private set; }

    public int Returned { get; private set; }

    public int Outstanding => Rented - Returned;

    public override XPathNode[] Rent(int minimumLength)
    {
        Rented++;
        return _inner.Rent(minimumLength);
    }

    public override void Return(XPathNode[] array, bool clearArray = false)
    {
        Returned++;
        _inner.Return(array, clearArray);
    }
}
