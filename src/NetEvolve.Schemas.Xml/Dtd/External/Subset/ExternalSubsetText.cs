namespace NetEvolve.Schemas.Xml.Dtd.External.Subset;

using System;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;

/// <summary>The decoded text of an external subset or an external parameter entity; it keeps the entity open for the recursion check until it is disposed.</summary>
internal sealed class ExternalSubsetText : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetText"/> class.</summary>
    /// <param name="frame">The open frame the text was read from; this instance closes it.</param>
    /// <param name="text">The decoded text.</param>
    internal ExternalSubsetText(ExternalEntityFrame frame, string text) => throw new NotSupportedException();

    /// <summary>Gets the decoded text without the text declaration; the line ends are normalized.</summary>
    internal string Text => throw new NotSupportedException();

    /// <summary>Gets the base URI of the entity: the final URI the resolver reported, otherwise the resolved URI.</summary>
    internal string? BaseUri => throw new NotSupportedException();

    /// <summary>Closes the entity; repeated calls do nothing.</summary>
    public void Dispose() => throw new NotSupportedException();
}
