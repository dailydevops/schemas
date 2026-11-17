namespace NetEvolve.Schemas.Xml.Writer;

/// <summary>Immutable options of an <see cref="XmlWriter"/>.</summary>
public sealed class XmlWriterOptions
{
    /// <summary>Initializes a new instance of the <see cref="XmlWriterOptions"/> class.</summary>
    /// <param name="canonical">Whether to produce the documented subset of the canonical form of the W3C XML Conformance Test Suite.</param>
    /// <param name="encoding">The encoding of byte output; ignored for character output and, in canonical mode, always UTF-8.</param>
    /// <param name="omitXmlDeclaration">Whether to leave out the XML declaration; canonical mode always does.</param>
    public XmlWriterOptions(
        bool canonical = false,
        XmlWriterEncoding encoding = XmlWriterEncoding.Utf8,
        bool omitXmlDeclaration = false
    )
    {
        Canonical = canonical;
        Encoding = encoding;
        OmitXmlDeclaration = omitXmlDeclaration;
    }

    /// <summary>Gets the default options: plain output, UTF-8, with the XML declaration when the document has one.</summary>
    public static XmlWriterOptions Default { get; } = new();

    /// <summary>Gets a value indicating whether canonical output is produced.</summary>
    public bool Canonical { get; }

    /// <summary>Gets the encoding of byte output.</summary>
    public XmlWriterEncoding Encoding { get; }

    /// <summary>Gets a value indicating whether the XML declaration is left out.</summary>
    public bool OmitXmlDeclaration { get; }
}
