namespace NetEvolve.Schemas.Schematron.Evaluation.Annotations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Forwards the first item of a sequence to the text sink, which converts it to its string value, and counts every item, so the caller
/// can tell the empty sequence and a sequence of several items from the one value a dynamic attribute needs.
/// </summary>
internal sealed class SingleValueSink : ISequenceSink
{
    private TextSink? _into;

    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Text;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Gets the number of items and text calls received since <see cref="Begin"/>.</summary>
    internal int Count { get; private set; }

    /// <summary>Starts one value: the first item goes to <paramref name="into"/>.</summary>
    /// <param name="into">The text sink that receives the first item.</param>
    internal void Begin(TextSink into)
    {
        Argument.ThrowIfNull(into);

        _into = into;
        Count = 0;
    }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        if (Count++ == 0)
        {
            _into!.Item(in item);
        }
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text)
    {
        if (Count++ == 0)
        {
            _into!.Text(text);
        }
    }

    /// <inheritdoc />
    public IXmlSink BeginTree() =>
        throw new InvalidOperationException(
            "An attribute value is built as text only; a temporary tree is a programming error."
        );

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) =>
        throw new InvalidOperationException(
            "An attribute value is built as text only; a temporary tree is a programming error."
        );
}
