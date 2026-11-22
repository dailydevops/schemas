namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The <see cref="SequenceSinkKind.Text"/> sink (design #150 EP-8): a pooled character buffer that holds the text of one message,
/// so building it needs no intermediate string. Adjacent atomic values are separated by one space and a <see cref="Text"/> call or a
/// node resets that adjacency, as in a tree sink.
/// </summary>
/// <remarks>
/// Limitation: only string, boolean, integer, and decimal values have a text form here. Untyped atomic and URI values would
/// convert as their string, but the XPath package keeps both kinds internal, so no test of this package can build one and an
/// arm for them would stay uncovered; they wait with double, float, and the remaining kinds for the XPath casting rules, which
/// the XPath package does not offer yet (#624). All of them record a <see cref="Failure"/> instead of a guessed lexical form. The date, time, and duration kinds
/// already convert in the XSLT <c>AtomicText</c> converter (#562) and wait for a neutral home of that converter (#632); this sink has its own conversion only because the Assertions namespace must not depend
/// on <c>Xslt.Construction</c> (design #137 AP-14, #150 EP-8), so those kinds record a <see cref="Failure"/> here until the
/// converter moves to a neutral location. A cut at the maximum length keeps a surrogate pair whole only when both halves arrive in
/// one append; a pair split across two appends at the cap can still leave a lone high surrogate at the end. Not thread-safe.
/// </remarks>
internal sealed class TextSink : ISequenceSink, IDisposable
{
    private readonly int _maxLength;
    private char[] _buffer = [];
    private int _length;
    private bool _lastAtomic;

    /// <summary>Initializes a new instance of the <see cref="TextSink"/> class.</summary>
    /// <param name="maxLength">The most characters the sink holds; the rest is cut and flagged.</param>
    internal TextSink(int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 0);

        _maxLength = maxLength;
    }

    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Text;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Gets the number of characters written so far; a later <see cref="Rewind"/> returns to it.</summary>
    internal int Length => _length;

    /// <summary>Gets the most characters the sink holds.</summary>
    internal int MaxLength => _maxLength;

    /// <summary>Gets the text written so far; valid until the next write or <see cref="Clear"/>.</summary>
    internal ReadOnlySpan<char> Written => new(_buffer, 0, _length);

    /// <summary>Gets a value indicating whether text was cut at the maximum length.</summary>
    internal bool IsTruncated { get; private set; }

    /// <summary>Gets the reason the last atomic value could not be converted, or <see langword="null"/>.</summary>
    internal string? Failure { get; private set; }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        if (item.TryGetNode(out var node))
        {
            _lastAtomic = false;
            AppendNode(in node);
            return;
        }

        if (!HasTextForm(in item))
        {
            Failure ??= $"A value of kind {item.Kind} has no text form yet (limitation).";
            return;
        }

        if (_lastAtomic)
        {
            Append(" ".AsSpan());
        }

        _lastAtomic = true;
        AppendAtomic(in item);
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text)
    {
        _lastAtomic = false;
        Append(text);
    }

    /// <inheritdoc />
    public IXmlSink BeginTree() =>
        throw new InvalidOperationException(
            "Message text is built as text only; a temporary tree is a programming error."
        );

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) =>
        throw new InvalidOperationException(
            "Message text is built as text only; a temporary tree is a programming error."
        );

    /// <summary>Appends a number without allocating; resets the adjacency like <see cref="Text"/>.</summary>
    /// <param name="value">The number.</param>
    internal void Integer(long value)
    {
        _lastAtomic = false;
        AppendInteger(value);
    }

    /// <summary>Drops what was written after <paramref name="length"/> characters, so a part that failed leaves nothing of itself behind.</summary>
    /// <param name="length">A value that <see cref="Length"/> returned earlier.</param>
    internal void Rewind(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, _length);

        _length = length;
        _lastAtomic = false;
    }

    /// <summary>Empties the sink and keeps its buffer for the next message.</summary>
    internal void Clear()
    {
        _length = 0;
        _lastAtomic = false;
        IsTruncated = false;
        Failure = null;
    }

    /// <inheritdoc />
    public override string ToString() => _length == 0 ? string.Empty : new string(_buffer, 0, _length);

    /// <inheritdoc />
    public void Dispose()
    {
        var buffer = _buffer;
        _buffer = [];
        _length = 0;
        if (buffer.Length > 0)
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static bool HasTextForm(in XdmItem item) =>
        item.Kind is XdmItemKind.XsString or XdmItemKind.Boolean or XdmItemKind.XsInteger or XdmItemKind.XsDecimal;

    private void AppendAtomic(in XdmItem item)
    {
        switch (item.Kind)
        {
            case XdmItemKind.XsString:
                _ = item.TryGetString(out var text);
                Append(text.AsSpan());
                break;
            case XdmItemKind.Boolean:
                _ = item.TryGetBoolean(out var flag);
                Append((flag ? "true" : "false").AsSpan());
                break;
            case XdmItemKind.XsInteger:
                _ = item.TryGetInteger(out var integer);
                AppendInteger(integer);
                break;
            default:
                _ = item.TryGetDecimal(out var number);
                Append(number.ToString().AsSpan());
                break;
        }
    }

    private void AppendNode(in XPathNode node)
    {
        if (node.TryGetStringValueSpan(out var span))
        {
            Append(span);
        }
        else if (node.Kind is XdmNodeKind.Element or XdmNodeKind.Document && node.Tree is XmlDocumentTree)
        {
            AppendDescendantText(node.Node);
        }
        else
        {
            Append(node.StringValue().AsSpan());
        }
    }

    // The string value of an element or a document is its descendant text in document order; walking it spares the string.
    private void AppendDescendantText(XmlNode root)
    {
        var node = root.FirstChild;
        while (!node.IsEmpty)
        {
            if (node.Kind == XmlNodeKind.Text && new XPathNode(node).TryGetStringValueSpan(out var span))
            {
                Append(span);
            }

            if (node.Kind == XmlNodeKind.Element && !node.FirstChild.IsEmpty)
            {
                node = node.FirstChild;
                continue;
            }

            while (node.NextSibling.IsEmpty)
            {
                node = node.Parent;
                if (node.IsEmpty || node == root)
                {
                    return;
                }
            }

            node = node.NextSibling;
        }
    }

    private void AppendInteger(long value)
    {
        Span<char> digits = stackalloc char[20];
        var at = digits.Length;
        var negative = value < 0;
        var rest = negative ? unchecked((ulong)(-(value + 1)) + 1UL) : (ulong)value;
        do
        {
            digits[--at] = (char)('0' + (int)(rest % 10UL));
            rest /= 10UL;
        } while (rest != 0UL);

        if (negative)
        {
            digits[--at] = '-';
        }

        Append(digits.Slice(at));
    }

    private void Append(ReadOnlySpan<char> text)
    {
        var room = _maxLength - _length;
        if (text.Length > room)
        {
            // A cut between a high and a low surrogate would leave a lone high surrogate, so the pair goes whole.
            if (room > 0 && char.IsHighSurrogate(text[room - 1]) && char.IsLowSurrogate(text[room]))
            {
                room--;
            }

            text = text.Slice(0, room);
            IsTruncated = true;
        }

        if (text.IsEmpty)
        {
            return;
        }

        if (_buffer.Length - _length < text.Length)
        {
            var grown = ArrayPool<char>.Shared.Rent(Math.Max(Math.Max(256, _buffer.Length * 2), _length + text.Length));
            _buffer.AsSpan(0, _length).CopyTo(grown);
            if (_buffer.Length > 0)
            {
                ArrayPool<char>.Shared.Return(_buffer);
            }

            _buffer = grown;
        }

        text.CopyTo(_buffer.AsSpan(_length));
        _length += text.Length;
    }
}
