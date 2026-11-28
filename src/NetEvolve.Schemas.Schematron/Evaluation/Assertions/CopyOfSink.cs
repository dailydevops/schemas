namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The sink that the <see langword="select"/> of an <c>xsl:copy-of</c> in a property evaluates into: every item adds its string value to the
/// text of the property, as a <c>value-of</c> does, and goes on into the content of the property, where a node is copied and an atomic
/// value is its text (SCH-84, #289).
/// </summary>
/// <remarks>Not thread-safe: one sink per message builder, started again for every <c>xsl:copy-of</c>.</remarks>
internal sealed class CopyOfSink : ISequenceSink
{
    private TextSink? _text;
    private IFindingContentBuilder? _content;

    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Items;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Starts a copy.</summary>
    /// <param name="text">The text of the property.</param>
    /// <param name="content">The content of the property.</param>
    internal void Begin(TextSink text, IFindingContentBuilder content)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(content);

        _text = text;
        _content = content;
    }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        var text = _text!;
        var mark = text.Length;
        text.Item(in item);
        if (item.IsNode)
        {
            _content!.Item(in item);
        }
        else if (text.Length > mark)
        {
            // The text of an atomic value includes the space that separates it from the value before it.
            _content!.Text(text.Written.Slice(mark));
        }
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text)
    {
        _text!.Text(text);
        _content!.Text(text);
    }

    /// <inheritdoc />
    public IXmlSink BeginTree() =>
        throw new InvalidOperationException("A copy into the text of a property never builds a temporary tree.");

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) =>
        throw new InvalidOperationException("A copy into the text of a property never builds a temporary tree.");
}
