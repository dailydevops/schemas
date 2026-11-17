namespace NetEvolve.Schemas.Xml.Input.Decoding;

/// <summary>An encoding family detected from the first bytes (XML 1.0 Appendix F) or confirmed by the declared name.</summary>
/// <remarks>
/// Limitation: UCS-4 octet orders 2143 and 3412 (DEF-64) and EBCDIC (DEF-62) are detected but not decoded.
/// The ASCII-compatible row of Appendix F is reported as <see cref="Utf8"/>; the declared name later selects
/// <see cref="Iso88591"/> or <see cref="UsAscii"/>.
/// </remarks>
internal enum XmlInputEncoding
{
    /// <summary>UTF-8, also the default when nothing else is detected.</summary>
    Utf8,

    /// <summary>UTF-16, little endian.</summary>
    Utf16LittleEndian,

    /// <summary>UTF-16, big endian.</summary>
    Utf16BigEndian,

    /// <summary>UCS-4, octet order 1234 (big endian).</summary>
    Ucs4Order1234,

    /// <summary>UCS-4, octet order 4321 (little endian).</summary>
    Ucs4Order4321,

    /// <summary>UCS-4, octet order 2143. Detected only.</summary>
    Ucs4Order2143,

    /// <summary>UCS-4, octet order 3412. Detected only.</summary>
    Ucs4Order3412,

    /// <summary>EBCDIC. Detected only.</summary>
    Ebcdic,

    /// <summary>ISO-8859-1, selected by the declared name only.</summary>
    Iso88591,

    /// <summary>US-ASCII, selected by the declared name only.</summary>
    UsAscii,
}
