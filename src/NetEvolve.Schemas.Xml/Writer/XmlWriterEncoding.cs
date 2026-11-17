namespace NetEvolve.Schemas.Xml.Writer;

/// <summary>The character encoding of the byte output of an <see cref="XmlWriter"/>.</summary>
/// <remarks>Limitation: output encodings are limited to UTF-8 and UTF-16.</remarks>
public enum XmlWriterEncoding
{
    /// <summary>UTF-8 without a byte order mark.</summary>
    Utf8 = 0,

    /// <summary>UTF-16 little endian with a byte order mark, written by <c>StartDocument</c>.</summary>
    Utf16 = 1,
}
