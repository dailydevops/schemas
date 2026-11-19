namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>Immutable reader options; they carry no validation settings (DTD validation is XML-60).</summary>
public sealed class XmlReaderOptions
{
    private bool _dtdProcessing;
    private XmlNameTable? _nameTable;
    private XmlReaderLimits _limits = XmlReaderLimits.Default;

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
    /// (XML-14); the external subset and external entities are not read (XML-20, XML-61, XML-62), so a reference to an
    /// external parsed entity in content is <see cref="XmlErrorCodes.XML2002"/>; an undeclared entity is skipped, not an
    /// error, when an external subset or a parameter-entity reference could have declared it (section 4.1).
    /// </remarks>
    public bool DtdProcessing => _dtdProcessing;

    /// <summary>Gets the options of external access to the external subset and the external parameter entities; <see langword="null"/> when they are not read.</summary>
    internal DtdValidationOptions? ExternalAccess => throw new NotSupportedException();

    /// <summary>Gets the absolute base URI of the document, against which a relative system identifier of the document type declaration resolves; <see langword="null"/> when unknown.</summary>
    internal string? BaseUri => throw new NotSupportedException();

    /// <summary>Returns a copy with other <see cref="ExternalAccess"/>.</summary>
    /// <param name="value">The options, <see langword="null"/> to leave the external subset unread.</param>
    /// <returns>The new options.</returns>
    internal XmlReaderOptions WithExternalAccess(DtdValidationOptions? value) => throw new NotSupportedException();

    /// <summary>Returns a copy with another <see cref="BaseUri"/>.</summary>
    /// <param name="value">The base URI, <see langword="null"/> when unknown.</param>
    /// <returns>The new options.</returns>
    internal XmlReaderOptions WithBaseUri(string? value) => throw new NotSupportedException();

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
        Argument.ThrowIfNull(value);

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
