#pragma warning disable MA0025 // Stub per #141: throwing bodies stay until XML-18 replaces them; the FND-50 stub convention of #117 and #103.

namespace NetEvolve.Schemas.Xml.Interop;

using System.Xml;
using System.Xml.Linq;
using NetEvolve.Schemas.Xml.Contracts;
using XmlNode = NetEvolve.Schemas.Xml.Contracts.XmlNode;

/// <summary>
/// Copies between the library's document model and the base class library types <see cref="XmlReader"/>,
/// <see cref="XDocument"/>, and <see cref="XmlDocument"/>. This is the only place of the package that references System.Xml.
/// </summary>
/// <remarks>
/// Limitation: every member throws until XML-18 lands, and the package ships these stubs until then. Adapters copy; they are
/// not views over the foreign tree. The library has no type named <c>XmlDocument</c>: the document is an <see cref="XmlTree"/>.
/// </remarks>
public static class XmlInterop
{
    /// <summary>Reads a document from an <see cref="XmlReader"/>.</summary>
    /// <param name="reader">The reader, positioned before the document.</param>
    /// <param name="options">The options, <see langword="null"/> for the defaults.</param>
    /// <returns>The document tree.</returns>
    public static XmlTree FromXmlReader(XmlReader reader, XmlInteropOptions? options = null) =>
        throw Stub.For("XML-18");

    /// <summary>Exposes a node of the document model as an <see cref="XmlReader"/>.</summary>
    /// <param name="node">The document or element node.</param>
    /// <returns>The reader over a copy.</returns>
    public static XmlReader ToXmlReader(XmlNode node) => throw Stub.For("XML-18");

    /// <summary>Copies an <see cref="XDocument"/> into the document model.</summary>
    /// <param name="document">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for the defaults.</param>
    /// <returns>The document tree.</returns>
    public static XmlTree FromXDocument(XDocument document, XmlInteropOptions? options = null) =>
        throw Stub.For("XML-18");

    /// <summary>Copies a node of the document model into an <see cref="XDocument"/>.</summary>
    /// <param name="node">The document or element node.</param>
    /// <returns>The document.</returns>
    public static XDocument ToXDocument(XmlNode node) => throw Stub.For("XML-18");

    /// <summary>Copies an <see cref="XmlDocument"/> into the document model.</summary>
    /// <param name="document">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for the defaults.</param>
    /// <returns>The document tree.</returns>
    public static XmlTree FromXmlDocument(XmlDocument document, XmlInteropOptions? options = null) =>
        throw Stub.For("XML-18");

    /// <summary>Copies a node of the document model into an <see cref="XmlDocument"/>.</summary>
    /// <param name="node">The document or element node.</param>
    /// <returns>The document.</returns>
    public static XmlDocument ToXmlDocument(XmlNode node) => throw Stub.For("XML-18");
}
