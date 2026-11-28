namespace NetEvolve.Schemas.Xml.Contracts;

using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>Immutable reader options; they carry no validation settings (DTD validation is XML-60).</summary>
public sealed class XmlReaderOptions
{
    private bool _dtdProcessing;
    private XmlNameTable? _nameTable;
    private XmlReaderLimits _limits = XmlReaderLimits.Default;
    private DtdValidationOptions? _externalAccess;
    private string? _baseUri;
    private bool _allowXml11Declaration;

    private XmlReaderOptions() { }

    /// <summary>Gets the default options: <see cref="XmlReaderLimits.Default"/> and a library-owned name table per document.</summary>
    public static XmlReaderOptions Default { get; } = new();

    /// <summary>Gets the limits.</summary>
    public XmlReaderLimits Limits => _limits;

    /// <summary>Gets the caller-supplied name table shared across documents, <see langword="null"/> for a library-owned table per document.</summary>
    public XmlNameTable? NameTable => _nameTable;

    /// <summary>Gets a value indicating whether a document type declaration is processed; <see langword="false"/> by default, which rejects it with <see cref="XmlErrorCodes.XML2001"/>.</summary>
    /// <remarks>
    /// With the option on, references to the internal general entities of the document type declaration are expanded in
    /// content and in attribute values, bounded by <see cref="XmlReaderLimits.MaxEntityExpansionCharacters"/> and
    /// <see cref="XmlReaderLimits.MaxEntityDepth"/>. Limitations: only the declarations of the internal subset are parsed
    /// (XML-14) unless the internal option <see cref="ExternalAccess"/> is set, which also reads the external subset and the
    /// external parameter entities (XML-20) and the external general entities that content references (XML-62); without
    /// it a reference to an external parsed entity in content is <see cref="XmlErrorCodes.XML2002"/>, and in an attribute value it is
    /// always <see cref="XmlErrorCodes.XML0022"/>; an undeclared entity is skipped, not an
    /// error, when an external subset or a parameter-entity reference could have declared it (section 4.1). The attribute
    /// definitions of the declaration apply while reading (XML-16): a defaulted or fixed attribute is added to the elements that
    /// omit it, the value of an attribute with a declared type other than CDATA is trimmed and its runs of spaces are collapsed
    /// (section 3.3.3), and a defaulted namespace declaration binds its prefix; the value length limit is checked before that
    /// collapse, and a default is expanded where it is applied. The nodes
    /// that come from an external entity report the document URI as their base URI, not the URI of the entity (#730).
    /// </remarks>
    public bool DtdProcessing => _dtdProcessing;

    /// <summary>Gets the options of external access to the external subset and the external parameter entities; <see langword="null"/> when they are not read.</summary>
    /// <remarks>
    /// Internal until the composition into validation (XML-68) approves a public shape. It takes effect only with
    /// <see cref="DtdProcessing"/>. With options (even <see cref="DtdValidationOptions.Default"/>) a document type
    /// declaration with a system identifier requests the external subset through the resolver gate: a kind whose flag is off
    /// is refused with <see cref="XmlErrorCodes.XML2002"/> without a resolver call. Without options the external subset and
    /// the external parameter entities are skipped, as before.
    /// </remarks>
    internal DtdValidationOptions? ExternalAccess => _externalAccess;

    /// <summary>Gets the absolute base URI of the document, against which a relative system identifier of the document type declaration resolves; <see langword="null"/> when unknown.</summary>
    internal string? BaseUri => _baseUri;

    /// <summary>Gets a value indicating whether the version <c>1.1</c> in the XML declaration is accepted; <see langword="false"/> by default, which rejects it with <see cref="XmlErrorCodes.XML0010"/>.</summary>
    /// <remarks>
    /// Internal, for schema documents (#861): the document is read by the XML 1.0 rules, so a document that uses XML 1.1
    /// only through its declaration reads. XML 1.1 itself (#62) is not supported. Honored by <see cref="Reader.Pipeline.XmlPipelineReader"/>.
    /// </remarks>
    internal bool AllowXml11Declaration => _allowXml11Declaration;

    /// <summary>Returns a copy with another <see cref="AllowXml11Declaration"/>.</summary>
    /// <param name="value"><see langword="true"/> to accept the version <c>1.1</c>.</param>
    /// <returns>The new options.</returns>
    internal XmlReaderOptions WithAllowXml11Declaration(bool value)
    {
        var copy = (XmlReaderOptions)MemberwiseClone();
        copy._allowXml11Declaration = value;

        return copy;
    }

    /// <summary>Returns a copy with other <see cref="ExternalAccess"/>.</summary>
    /// <param name="value">The options, <see langword="null"/> to leave the external subset unread.</param>
    /// <returns>The new options.</returns>
    internal XmlReaderOptions WithExternalAccess(DtdValidationOptions? value)
    {
        var copy = (XmlReaderOptions)MemberwiseClone();
        copy._externalAccess = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="BaseUri"/>.</summary>
    /// <param name="value">The base URI, <see langword="null"/> when unknown.</param>
    /// <returns>The new options.</returns>
    internal XmlReaderOptions WithBaseUri(string? value)
    {
        var copy = (XmlReaderOptions)MemberwiseClone();
        copy._baseUri = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="DtdProcessing"/>.</summary>
    /// <param name="value"><see langword="true"/> to parse the internal subset of a document type declaration.</param>
    /// <returns>The new options.</returns>
    public XmlReaderOptions WithDtdProcessing(bool value)
    {
        var copy = (XmlReaderOptions)MemberwiseClone();
        copy._dtdProcessing = value;

        return copy;
    }

    /// <summary>Returns a copy with other <see cref="Limits"/>.</summary>
    /// <param name="value">The new limits.</param>
    /// <returns>The new options.</returns>
    public XmlReaderOptions WithLimits(XmlReaderLimits value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var copy = (XmlReaderOptions)MemberwiseClone();
        copy._limits = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="NameTable"/>.</summary>
    /// <param name="value">The new table, <see langword="null"/> for a library-owned table per document.</param>
    /// <returns>The new options.</returns>
    public XmlReaderOptions WithNameTable(XmlNameTable? value)
    {
        var copy = (XmlReaderOptions)MemberwiseClone();
        copy._nameTable = value;

        return copy;
    }
}
