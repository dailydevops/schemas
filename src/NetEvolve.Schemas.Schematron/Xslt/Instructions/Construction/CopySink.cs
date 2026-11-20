namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The sink that <c>xsl:copy-of</c> evaluates its <see langword="select"/> into when the destination is an item sequence: an item sink keeps
/// nodes by reference, so every node is copied on its way (XSLT 2.0 section 11.9.2), while atomic values and text pass unchanged. A
/// tree sink copies the nodes it receives itself and needs no such step.
/// </summary>
internal sealed class CopySink : ISequenceSink
{
    private readonly XsltFrame _frame;
    private readonly ISequenceSink _inner;
    private readonly DocumentLocation _location;

    internal CopySink(XsltFrame frame, ISequenceSink inner, DocumentLocation location)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(inner);

        _frame = frame;
        _inner = inner;
        _location = location;
    }

    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Items;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Gets a value indicating whether a node could not be copied; the frame holds the problem.</summary>
    internal bool Failed { get; private set; }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        if (Failed)
        {
            return;
        }

        if (!item.IsNode)
        {
            _inner.Item(in item);
            return;
        }

        Failed = ConstructionOutput.PushCopy(_frame, _inner, in item, _location) == QueryStatus.Problem;
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text) => _inner.Text(text);

    /// <inheritdoc />
    public IXmlSink BeginTree() => _inner.BeginTree();

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) => _inner.EndTree(tree);
}
