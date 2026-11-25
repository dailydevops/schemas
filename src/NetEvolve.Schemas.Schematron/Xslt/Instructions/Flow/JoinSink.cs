namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The sink of the value of one <c>xsl:value-of</c> (design #150 EP-8, RD-5): every item that arrives is atomized to its string
/// and pushed as text into the destination, with the separator between two items. The destination adds no separator of its own,
/// because a text push resets the adjacency of atomic values. Adjacent text that arrives from a sequence constructor is one item.
/// </summary>
/// <remarks>
/// Limitation: only the atomic kinds that <see cref="AtomicText"/> converts have a text form (the XPath casting rules for double,
/// float, and the remaining kinds are not available to this package yet, #624); another kind is recorded in <see cref="Failure"/>
/// and the item is dropped.
/// </remarks>
internal sealed class JoinSink : ISequenceSink, IDisposable
{
    private readonly ISequenceSink _into;
    private readonly string _separator;
    private char[] _pending = [];
    private int _pendingLength;
    private int _count;

    /// <summary>Initializes a new instance of the <see cref="JoinSink"/> class.</summary>
    /// <param name="into">The destination of the text.</param>
    /// <param name="separator">The text between two items.</param>
    internal JoinSink(ISequenceSink into, string separator)
    {
        ArgumentNullException.ThrowIfNull(into);
        ArgumentNullException.ThrowIfNull(separator);

        _into = into;
        _separator = separator;
    }

    /// <summary>Gets <see cref="SequenceSinkKind.Text"/> over a text sink, which has no tree to begin; otherwise <see cref="SequenceSinkKind.Items"/>.</summary>
    public SequenceSinkKind Kind =>
        _into.Kind == SequenceSinkKind.Text ? SequenceSinkKind.Text : SequenceSinkKind.Items;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Gets the reason the first item without a text form could not be converted, or <see langword="null"/>.</summary>
    internal string? Failure { get; private set; }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        FlushText();
        if (item.TryGetNode(out var node))
        {
            Begin();
            if (node.TryGetStringValueSpan(out var span))
            {
                _into.Text(span);
            }
            else
            {
                _into.Text(node.StringValue().AsSpan());
            }

            return;
        }

        if (!AtomicText.TryGet(in item, out var text))
        {
            Failure ??= $"A value of kind {item.Kind} has no text form yet (limitation).";
            return;
        }

        Begin();
        _into.Text(text.AsSpan());
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return;
        }

        if (_pendingLength + text.Length > _pending.Length)
        {
            var grown = ArrayPool<char>.Shared.Rent(Math.Max(64, _pendingLength + text.Length));
            _pending.AsSpan(0, _pendingLength).CopyTo(grown);
            Return();
            _pending = grown;
        }

        text.CopyTo(_pending.AsSpan(_pendingLength));
        _pendingLength += text.Length;
    }

    /// <inheritdoc />
    public IXmlSink BeginTree() => _into.BeginTree();

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) => _into.EndTree(tree);

    /// <summary>Pushes the text that has not been pushed yet; call it when the last item arrived.</summary>
    internal void Complete() => FlushText();

    /// <inheritdoc />
    public void Dispose()
    {
        Return();
        _pending = [];
        _pendingLength = 0;
    }

    private void Begin()
    {
        if (_count++ > 0 && _separator.Length > 0)
        {
            _into.Text(_separator.AsSpan());
        }
    }

    private void FlushText()
    {
        if (_pendingLength == 0)
        {
            return;
        }

        Begin();
        _into.Text(_pending.AsSpan(0, _pendingLength));
        _pendingLength = 0;
    }

    private void Return()
    {
        if (_pending.Length > 0)
        {
            ArrayPool<char>.Shared.Return(_pending);
        }
    }
}
