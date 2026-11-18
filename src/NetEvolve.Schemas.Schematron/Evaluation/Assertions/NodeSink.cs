namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A push-only sink that counts what an expression returns and keeps its first item when that is a node, for <c>subject</c> and the <c>path</c> of <c>name</c>.</summary>
internal sealed class NodeSink : ISequenceSink
{
    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Items;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Gets the number of items pushed; non-empty text counts as one value.</summary>
    internal int Count { get; private set; }

    /// <summary>Gets a value indicating whether the first item is a node.</summary>
    internal bool FirstIsNode { get; private set; }

    /// <summary>Gets the first item when <see cref="FirstIsNode"/>.</summary>
    internal XPathNode First { get; private set; }

    /// <summary>Forgets what was pushed.</summary>
    internal void Reset()
    {
        Count = 0;
        FirstIsNode = false;
        First = default;
    }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        if (Count++ == 0 && item.TryGetNode(out var node))
        {
            FirstIsNode = true;
            First = node;
        }
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text)
    {
        if (!text.IsEmpty)
        {
            Count++;
        }
    }

    /// <inheritdoc />
    public IXmlSink BeginTree() =>
        throw new InvalidOperationException("A node selection never builds a temporary tree.");

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) =>
        throw new InvalidOperationException("A node selection never builds a temporary tree.");
}
