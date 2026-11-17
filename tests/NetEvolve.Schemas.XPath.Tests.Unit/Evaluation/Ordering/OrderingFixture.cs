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

/// <summary>A flat tree of a document and a number of elements that counts its document order comparisons.</summary>
internal class CountingTree : XmlTree
{
    private readonly int _count;
    private readonly SpanInterningNameTable _names = new(1000);

    public CountingTree(int elements) => _count = elements + 1;

    public int Comparisons { get; private set; }

    public override XmlNameTable Names => _names;

    public override IReadOnlyList<XmlUnparsedEntity> UnparsedEntities => [];

    public override IReadOnlyList<XmlNotation> Notations => [];

    public XPathNode At(int index) => new(new XmlNode(this, index));

    protected internal override XmlNodeKind GetKind(int index) =>
        index == 0 ? XmlNodeKind.Document : XmlNodeKind.Element;

    protected internal override XmlName GetName(int index) => default;

    protected internal override int GetParent(int index) => index == 0 ? NoNode : 0;

    protected internal override int GetFirstChild(int index) => index == 0 && _count > 1 ? 1 : NoNode;

    protected internal override int GetNextSibling(int index) => index > 0 && index + 1 < _count ? index + 1 : NoNode;

    protected internal override int GetFirstAttribute(int index) => NoNode;

    protected internal override int GetNextAttribute(int index) => NoNode;

    protected internal override int GetFirstNamespace(int index) => NoNode;

    protected internal override int GetNextNamespace(int index) => NoNode;

    protected internal override string GetStringValue(int index) => string.Empty;

    protected internal override Uri? GetBaseUri(int index) => null;

    protected internal override Uri? GetDocumentUri(int index) => null;

    protected internal override int CompareDocumentOrder(int first, int second)
    {
        Comparisons++;
        return OnCompare(first, second);
    }

    protected virtual int OnCompare(int first, int second) => first.CompareTo(second);
}

/// <summary>A tree whose comparison fails.</summary>
internal sealed class ThrowingCompareTree(int elements) : CountingTree(elements)
{
    protected override int OnCompare(int first, int second) =>
        throw new InvalidOperationException("The comparison failed.");
}
