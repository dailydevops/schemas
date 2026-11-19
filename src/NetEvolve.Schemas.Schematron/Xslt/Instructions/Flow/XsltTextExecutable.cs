namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary><c>xsl:text</c>: pushes its text, unchanged, into the sink (XSLT 2.0 section 11.4.2).</summary>
internal sealed class XsltTextExecutable : XsltExecutable
{
    private readonly string _text;

    internal XsltTextExecutable(string text)
    {
        Argument.ThrowIfNull(text);

        _text = text;
    }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        sink.Text(_text.AsSpan());
        return QueryStatus.Ok;
    }
}
