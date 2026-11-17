#pragma warning disable CA1065, MA0025 // Stub per #141: throwing bodies stay until XML-08 (limits, options) and XML-18 (interop) replace them; the FND-50 stub convention of #117 and #103.

namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>Immutable reader limits, one per resource that hostile input can inflate, each with one default and one error code. A limit of zero never disables it.</summary>
/// <remarks>Limitation: properties and <c>With*</c> methods throw until XML-08 lands; the defaults and codes are final.</remarks>
public sealed class XmlReaderLimits
{
    private XmlReaderLimits() { }

    /// <summary>Gets the default limits.</summary>
    public static XmlReaderLimits Default => throw Stub.For("XML-08");

    /// <summary>Gets the maximum number of decoded characters of a document; the default is 134,217,728 and the error is <see cref="XmlErrorCodes.XML1101"/>.</summary>
    public long MaxDocumentCharacters => throw Stub.For("XML-08");

    /// <summary>Gets the maximum element nesting depth; the default is 256 and the error is <see cref="XmlErrorCodes.XML1102"/>.</summary>
    public int MaxElementDepth => throw Stub.For("XML-08");

    /// <summary>Gets the maximum number of attributes of one element; the default is 256 and the error is <see cref="XmlErrorCodes.XML1103"/>.</summary>
    public int MaxAttributesPerElement => throw Stub.For("XML-08");

    /// <summary>Gets the maximum number of namespace declarations of one element; the default is 256 and the error is <see cref="XmlErrorCodes.XML1104"/>.</summary>
    public int MaxNamespaceDeclarationsPerElement => throw Stub.For("XML-08");

    /// <summary>Gets the maximum length of a name and of the body of a reference; the default is 1,024 and the error is <see cref="XmlErrorCodes.XML1105"/>.</summary>
    public int MaxNameLength => throw Stub.For("XML-08");

    /// <summary>Gets the maximum length of a comment or processing instruction; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1106"/>.</summary>
    public int MaxTokenLength => throw Stub.For("XML-08");

    /// <summary>Gets the maximum length of a coalesced text node; the default is 16,777,216 and the error is <see cref="XmlErrorCodes.XML1107"/>.</summary>
    public int MaxTextNodeLength => throw Stub.For("XML-08");

    /// <summary>Gets the maximum number of nodes of a document; the default is 10,000,000 and the error is <see cref="XmlErrorCodes.XML1108"/>.</summary>
    public int MaxNodeCount => throw Stub.For("XML-08");

    /// <summary>Gets the maximum number of characters produced by entity expansion (reserved for XML-59); the default is 10,485,760 and the error is <see cref="XmlErrorCodes.XML1109"/>.</summary>
    public long MaxEntityExpansionCharacters => throw Stub.For("XML-08");

    /// <summary>Gets the maximum nesting depth of entity references (reserved for XML-59); the default is 16 and the error is <see cref="XmlErrorCodes.XML1110"/>.</summary>
    public int MaxEntityDepth => throw Stub.For("XML-08");

    /// <summary>Gets the maximum number of distinct names in a name table; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1111"/>.</summary>
    public int MaxDistinctNames => throw Stub.For("XML-08");

    /// <summary>Gets the maximum length of a normalized attribute value; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1112"/>.</summary>
    public int MaxAttributeValueLength => throw Stub.For("XML-08");

    /// <summary>Gets the maximum number of characters of a buffered start tag; the default is 4,194,304 and the error is <see cref="XmlErrorCodes.XML1113"/>.</summary>
    public int MaxStartTagCharacters => throw Stub.For("XML-08");

    /// <summary>Gets the maximum length of the XML declaration; the default is 1,024 and the error is <see cref="XmlErrorCodes.XML1114"/>.</summary>
    public int MaxXmlDeclarationLength => throw Stub.For("XML-08");

    /// <summary>Gets the maximum length of the document type declaration, applied only with a doctype handler; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1115"/>.</summary>
    public int MaxDoctypeLength => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxDocumentCharacters"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxDocumentCharacters(long value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxElementDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxElementDepth(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxAttributesPerElement"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxAttributesPerElement(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxNamespaceDeclarationsPerElement"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxNamespaceDeclarationsPerElement(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxNameLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxNameLength(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxTokenLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxTokenLength(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxTextNodeLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxTextNodeLength(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxNodeCount"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxNodeCount(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxEntityExpansionCharacters"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxEntityExpansionCharacters(long value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxEntityDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxEntityDepth(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxDistinctNames"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxDistinctNames(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxAttributeValueLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxAttributeValueLength(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxStartTagCharacters"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxStartTagCharacters(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxXmlDeclarationLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxXmlDeclarationLength(int value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="MaxDoctypeLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxDoctypeLength(int value) => throw Stub.For("XML-08");
}
