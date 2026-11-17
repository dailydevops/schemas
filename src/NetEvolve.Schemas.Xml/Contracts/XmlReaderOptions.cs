#pragma warning disable CA1065, MA0025 // Stub per #141: throwing bodies stay until XML-08 (limits, options) and XML-18 (interop) replace them; the FND-50 stub convention of #117 and #103.

namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>Immutable reader options; they carry no validation settings (DTD validation is XML-60).</summary>
/// <remarks>Limitation: members throw until XML-08 lands.</remarks>
public sealed class XmlReaderOptions
{
    private XmlReaderOptions() { }

    /// <summary>Gets the default options: <see cref="XmlReaderLimits.Default"/> and a library-owned name table per document.</summary>
    public static XmlReaderOptions Default => throw Stub.For("XML-08");

    /// <summary>Gets the limits.</summary>
    public XmlReaderLimits Limits => throw Stub.For("XML-08");

    /// <summary>Gets the caller-supplied name table shared across documents, <see langword="null"/> for a library-owned table per document.</summary>
    public XmlNameTable? NameTable => throw Stub.For("XML-08");

    /// <summary>Returns a copy with other <see cref="Limits"/>.</summary>
    /// <param name="value">The new limits.</param>
    /// <returns>The new options.</returns>
    public XmlReaderOptions WithLimits(XmlReaderLimits value) => throw Stub.For("XML-08");

    /// <summary>Returns a copy with another <see cref="NameTable"/>.</summary>
    /// <param name="value">The new table, <see langword="null"/> for a library-owned table per document.</param>
    /// <returns>The new options.</returns>
    public XmlReaderOptions WithNameTable(XmlNameTable? value) => throw Stub.For("XML-08");
}
