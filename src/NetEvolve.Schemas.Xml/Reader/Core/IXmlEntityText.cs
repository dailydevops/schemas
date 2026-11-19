namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The text of an external parsed entity as a pull source of chunks (design #122, section 8, rule 5): the parser reads it as
/// a content frame over the outer input and refills the frame chunk by chunk, so the entity is never buffered completely.
/// </summary>
/// <remarks>Dispose it to close the entity; the parser does that when the frame ends, on every error, and when it is disposed.</remarks>
internal interface IXmlEntityText : IDisposable
{
    /// <summary>Gets the base URI of the entity, <see langword="null"/> when it is not known.</summary>
    string? BaseUri { get; }

    /// <summary>Gets the error that ended the text, <see langword="null"/> while it is read and after a complete read.</summary>
    XmlErrorCode? Error { get; }

    /// <summary>Reads the next chunk of the text; its line ends are normalized and its text declaration is consumed.</summary>
    /// <param name="chunk">The characters, valid until the next call.</param>
    /// <returns><see langword="false"/> at the end of the text or after an error; look at <see cref="Error"/>.</returns>
    bool TryRead(out ReadOnlyMemory<char> chunk);
}
