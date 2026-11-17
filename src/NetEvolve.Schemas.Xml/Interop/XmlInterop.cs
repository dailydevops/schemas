namespace NetEvolve.Schemas.Xml.Interop;

using System;
using System.Xml;
using System.Xml.Linq;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Traversal;
using XmlNode = NetEvolve.Schemas.Xml.Contracts.XmlNode;

/// <summary>
/// Copies between the library's document model and the base class library types <see cref="XmlReader"/>,
/// <see cref="XDocument"/>, and <see cref="XmlDocument"/>. This is the only place of the package that references System.Xml.
/// </summary>
/// <remarks>
/// <para>
/// Adapters copy; they are not views over the foreign tree. The library has no type named <c>XmlDocument</c>: the document is
/// an <see cref="XmlTree"/>. Input with a document type declaration is rejected with <c>XML2001</c> unless
/// <see cref="XmlInteropOptions.AllowDocumentType"/> is set; the declaration itself is not part of the model.
/// </para>
/// <para>
/// Limitations: the model keeps positions of elements only (a start tag position, taken from <see cref="IXmlLineInfo"/> when
/// the source provides it; <see cref="XmlDocument"/> has none), so attributes carry none. It keeps no XML declaration, no
/// distinction between text and CDATA after the import, no redundant namespace redeclarations, and the declarations of an
/// element come before its attributes, whatever their source order. Entity reference nodes and document type content are
/// not mapped. An <see cref="XName"/> has no prefix, so an <see cref="XDocument"/> yields the first prefix bound to a namespace.
/// </para>
/// </remarks>
public static class XmlInterop
{
    /// <summary>Reads a document from an <see cref="XmlReader"/>.</summary>
    /// <param name="reader">The reader, positioned before the document.</param>
    /// <param name="options">The options, <see langword="null"/> for the defaults.</param>
    /// <returns>The document tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <see langword="null"/>.</exception>
    /// <exception cref="XmlParseException">The input has a document type declaration and the options do not allow it.</exception>
    public static XmlTree FromXmlReader(XmlReader reader, XmlInteropOptions? options = null) =>
        XmlReaderImporter.Import(reader, options);

    /// <summary>Exposes a node of the document model as an <see cref="XmlReader"/>.</summary>
    /// <param name="node">The document or element node.</param>
    /// <returns>The reader over a copy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="default"/>.</exception>
    public static XmlReader ToXmlReader(XmlNode node) => ToXDocument(node).CreateReader();

    /// <summary>Copies an <see cref="XDocument"/> into the document model.</summary>
    /// <param name="document">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for the defaults.</param>
    /// <returns>The document tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="XmlParseException">The input has a document type declaration and the options do not allow it.</exception>
    public static XmlTree FromXDocument(XDocument document, XmlInteropOptions? options = null) =>
        XDocumentImporter.Import(document, options);

    /// <summary>Copies a node of the document model into an <see cref="XDocument"/>.</summary>
    /// <param name="node">The document or element node.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="default"/>.</exception>
    public static XDocument ToXDocument(XmlNode node)
    {
        Argument.ThrowIfNull(node.Tree, nameof(node));

        var sink = new XDocumentSink(node.Tree.Names);
        XmlTreePump.Pump(node, sink);
        return sink.Document;
    }

    /// <summary>Copies an <see cref="XmlDocument"/> into the document model.</summary>
    /// <param name="document">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for the defaults.</param>
    /// <returns>The document tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="XmlParseException">The input has a document type declaration and the options do not allow it.</exception>
    public static XmlTree FromXmlDocument(XmlDocument document, XmlInteropOptions? options = null) =>
        XmlDocumentImporter.Import(document, options);

    /// <summary>Copies a node of the document model into an <see cref="XmlDocument"/>.</summary>
    /// <param name="node">The document or element node.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="default"/>.</exception>
    public static XmlDocument ToXmlDocument(XmlNode node)
    {
        Argument.ThrowIfNull(node.Tree, nameof(node));

        var sink = new XmlDocumentSink(node.Tree.Names);
        XmlTreePump.Pump(node, sink);
        return sink.Document;
    }
}
