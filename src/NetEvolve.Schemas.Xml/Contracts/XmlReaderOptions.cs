namespace NetEvolve.Schemas.Xml.Contracts;

using NetEvolve.Arguments;

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
    /// <remarks>Limitation: only the declarations of the internal subset are parsed (XML-14); the external subset is not read.</remarks>
    public bool DtdProcessing => _dtdProcessing;

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
