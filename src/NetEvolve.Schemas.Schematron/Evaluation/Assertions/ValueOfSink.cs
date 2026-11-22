namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Joins the items of one <c>value-of</c> with one space and hands them to the text sink; the sink adds no second separator.</summary>
internal sealed class ValueOfSink : ISequenceSink
{
    private TextSink? _into;
    private int _items;

    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Text;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Starts one <c>value-of</c>: its items go to <paramref name="into"/> and none is pending.</summary>
    /// <param name="into">The text sink of the message.</param>
    internal void Begin(TextSink into)
    {
        ArgumentNullException.ThrowIfNull(into);

        _into = into;
        _items = 0;

        // Each value-of joins its own items; the previous one must not leave an atomic value pending.
        into.Text(ReadOnlySpan<char>.Empty);
    }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        if (_items++ > 0)
        {
            _into!.Text(" ".AsSpan());
        }

        _into!.Item(in item);
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text) => _into!.Text(text);

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
}
