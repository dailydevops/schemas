namespace NetEvolve.Schemas.Xml.Contracts;

using NetEvolve.Arguments;

/// <summary>Immutable reader limits, one per resource that hostile input can inflate, each with one default and one error code. A limit of zero never disables it.</summary>
/// <remarks>Every value must be at least one; <c>With*</c> methods reject zero and negative values.</remarks>
public sealed class XmlReaderLimits
{
    private long _maxDocumentCharacters = 134_217_728;
    private int _maxElementDepth = 256;
    private int _maxAttributesPerElement = 256;
    private int _maxNamespaceDeclarationsPerElement = 256;
    private int _maxNameLength = 1_024;
    private int _maxTokenLength = 1_048_576;
    private int _maxTextNodeLength = 16_777_216;
    private int _maxNodeCount = 10_000_000;
    private long _maxEntityExpansionCharacters = 10_485_760;
    private int _maxEntityDepth = 16;
    private int _maxDistinctNames = 1_048_576;
    private int _maxAttributeValueLength = 1_048_576;
    private int _maxStartTagCharacters = 4_194_304;
    private int _maxXmlDeclarationLength = 1_024;
    private int _maxDoctypeLength = 1_048_576;

    private XmlReaderLimits() { }

    /// <summary>Gets the default limits.</summary>
    public static XmlReaderLimits Default { get; } = new();

    /// <summary>Gets the maximum number of decoded characters of a document; the default is 134,217,728 and the error is <see cref="XmlErrorCodes.XML1101"/>.</summary>
    public long MaxDocumentCharacters => _maxDocumentCharacters;

    /// <summary>Gets the maximum element nesting depth; the default is 256 and the error is <see cref="XmlErrorCodes.XML1102"/>.</summary>
    public int MaxElementDepth => _maxElementDepth;

    /// <summary>Gets the maximum number of attributes of one element; the default is 256 and the error is <see cref="XmlErrorCodes.XML1103"/>.</summary>
    public int MaxAttributesPerElement => _maxAttributesPerElement;

    /// <summary>Gets the maximum number of namespace declarations of one element; the default is 256 and the error is <see cref="XmlErrorCodes.XML1104"/>.</summary>
    public int MaxNamespaceDeclarationsPerElement => _maxNamespaceDeclarationsPerElement;

    /// <summary>Gets the maximum length of a name and of the body of a reference; the default is 1,024 and the error is <see cref="XmlErrorCodes.XML1105"/>.</summary>
    public int MaxNameLength => _maxNameLength;

    /// <summary>Gets the maximum length of a comment or processing instruction; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1106"/>.</summary>
    public int MaxTokenLength => _maxTokenLength;

    /// <summary>Gets the maximum length of a coalesced text node; the default is 16,777,216 and the error is <see cref="XmlErrorCodes.XML1107"/>.</summary>
    public int MaxTextNodeLength => _maxTextNodeLength;

    /// <summary>Gets the maximum number of nodes of a document; the default is 10,000,000 and the error is <see cref="XmlErrorCodes.XML1108"/>.</summary>
    public int MaxNodeCount => _maxNodeCount;

    /// <summary>Gets the maximum number of characters produced by entity expansion, charged for the replacement text of every entity reference; the default is 10,485,760 and the error is <see cref="XmlErrorCodes.XML1109"/>.</summary>
    public long MaxEntityExpansionCharacters => _maxEntityExpansionCharacters;

    /// <summary>Gets the maximum nesting depth of entity references; the default is 16 and the error is <see cref="XmlErrorCodes.XML1110"/>.</summary>
    public int MaxEntityDepth => _maxEntityDepth;

    /// <summary>Gets the maximum number of distinct names in a name table; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1111"/>.</summary>
    public int MaxDistinctNames => _maxDistinctNames;

    /// <summary>Gets the maximum length of a normalized attribute value; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1112"/>.</summary>
    public int MaxAttributeValueLength => _maxAttributeValueLength;

    /// <summary>Gets the maximum number of characters of a buffered start tag; the default is 4,194,304 and the error is <see cref="XmlErrorCodes.XML1113"/>.</summary>
    public int MaxStartTagCharacters => _maxStartTagCharacters;

    /// <summary>Gets the maximum length of the XML declaration; the default is 1,024 and the error is <see cref="XmlErrorCodes.XML1114"/>.</summary>
    public int MaxXmlDeclarationLength => _maxXmlDeclarationLength;

    /// <summary>Gets the maximum length of the document type declaration, applied only with a doctype handler; the default is 1,048,576 and the error is <see cref="XmlErrorCodes.XML1115"/>.</summary>
    public int MaxDoctypeLength => _maxDoctypeLength;

    /// <summary>Returns a copy with another <see cref="MaxDocumentCharacters"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxDocumentCharacters(long value)
    {
        Argument.ThrowIfLessThan(value, 1L);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxDocumentCharacters = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxElementDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxElementDepth(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxElementDepth = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxAttributesPerElement"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxAttributesPerElement(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxAttributesPerElement = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxNamespaceDeclarationsPerElement"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxNamespaceDeclarationsPerElement(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxNamespaceDeclarationsPerElement = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxNameLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxNameLength(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxNameLength = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxTokenLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxTokenLength(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxTokenLength = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxTextNodeLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxTextNodeLength(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxTextNodeLength = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxNodeCount"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxNodeCount(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxNodeCount = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxEntityExpansionCharacters"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxEntityExpansionCharacters(long value)
    {
        Argument.ThrowIfLessThan(value, 1L);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxEntityExpansionCharacters = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxEntityDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxEntityDepth(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxEntityDepth = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxDistinctNames"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxDistinctNames(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxDistinctNames = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxAttributeValueLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxAttributeValueLength(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxAttributeValueLength = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxStartTagCharacters"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxStartTagCharacters(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxStartTagCharacters = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxXmlDeclarationLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxXmlDeclarationLength(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxXmlDeclarationLength = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxDoctypeLength"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public XmlReaderLimits WithMaxDoctypeLength(int value)
    {
        Argument.ThrowIfLessThan(value, 1);

        var copy = (XmlReaderLimits)MemberwiseClone();
        copy._maxDoctypeLength = value;

        return copy;
    }
}
