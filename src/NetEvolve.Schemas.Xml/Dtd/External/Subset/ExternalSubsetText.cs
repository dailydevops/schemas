namespace NetEvolve.Schemas.Xml.Dtd.External.Subset;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;

/// <summary>The decoded text of an external subset or an external parameter entity; it keeps the entity open for the recursion check until it is disposed.</summary>
internal sealed class ExternalSubsetText : IDisposable
{
    private readonly ExternalEntityFrame _frame;

    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetText"/> class.</summary>
    /// <param name="frame">The open frame the text was read from; this instance closes it.</param>
    /// <param name="text">The decoded text.</param>
    internal ExternalSubsetText(ExternalEntityFrame frame, string text)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(text);

        _frame = frame;
        Text = text;
    }

    /// <summary>Gets the decoded text without the text declaration; the line ends are normalized.</summary>
    internal string Text { get; }

    /// <summary>Gets the base URI of the entity: the final URI the resolver reported, otherwise the resolved URI.</summary>
    internal string? BaseUri => _frame.BaseUri;

    /// <summary>Closes the entity; repeated calls do nothing.</summary>
    public void Dispose() => _frame.Dispose();
}
