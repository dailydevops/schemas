namespace NetEvolve.Schemas.Xml.Contracts;

using System;

/// <summary>
/// The consumer of reader events, shared by the tree builder and the writer. A reader pumps into a class sink or a
/// mutable struct sink passed by <see langword="ref"/>; ref struct sinks are not supported.
/// </summary>
/// <remarks>
/// An exception thrown by a sink is the sink's own failure; it propagates unchanged and the reader is unusable afterwards.
/// Events arrive in document order: start and attribute events of an element are emitted when its start tag is complete.
/// </remarks>
public interface IXmlSink
{
    /// <summary>Gets the table that resolves and issues every <see cref="XmlSymbol"/> and <see cref="XmlName"/> of this sink.</summary>
    XmlNameTable Names { get; }

    /// <summary>Starts the document.</summary>
    /// <param name="declaration">The XML declaration, empty when the document has none.</param>
    void StartDocument(in XmlDeclaration declaration);

    /// <summary>Ends the document.</summary>
    void EndDocument();

    /// <summary>Starts an element.</summary>
    /// <param name="name">The resolved name.</param>
    /// <param name="position">The position of the start tag.</param>
    void StartElement(XmlName name, XmlPosition position);

    /// <summary>Reports a namespace declaration of the current start tag; all declarations precede the attributes.</summary>
    /// <param name="prefix">The prefix, empty for the default namespace.</param>
    /// <param name="uri">The namespace URI, empty when the default namespace is undeclared.</param>
    void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri);

    /// <summary>Reports an attribute of the current start tag.</summary>
    /// <param name="name">The resolved name.</param>
    /// <param name="value">The normalized value with references resolved.</param>
    void Attribute(XmlName name, ReadOnlySpan<char> value);

    /// <summary>Ends the attributes of the current start tag.</summary>
    void EndAttributes();

    /// <summary>Ends an element.</summary>
    /// <param name="name">The resolved name of the element.</param>
    void EndElement(XmlName name);

    /// <summary>Reports text; consecutive text events with no other event between them belong to one text node.</summary>
    /// <param name="text">The characters.</param>
    /// <param name="kind">The origin of the characters.</param>
    void Text(ReadOnlySpan<char> text, XmlTextKind kind);

    /// <summary>Reports a comment.</summary>
    /// <param name="text">The comment text without the delimiters.</param>
    void Comment(ReadOnlySpan<char> text);

    /// <summary>Reports a processing instruction.</summary>
    /// <param name="target">The target.</param>
    /// <param name="data">The data without the leading white space.</param>
    void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data);

    /// <summary>Reports the document type declaration; only with a doctype handler.</summary>
    /// <param name="doctype">The declaration.</param>
    void Doctype(in XmlDoctypeInfo doctype);
}
