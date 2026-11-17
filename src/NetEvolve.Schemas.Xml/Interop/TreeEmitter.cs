namespace NetEvolve.Schemas.Xml.Interop;

using System;
using System.Xml;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using XmlDeclaration = NetEvolve.Schemas.Xml.Contracts.XmlDeclaration;
using XmlNameTable = NetEvolve.Schemas.Xml.Contracts.XmlNameTable;

/// <summary>Turns the string-based nodes of a base class library tree into sink events, shared by the three importers.</summary>
/// <remarks>Text outside the document element is dropped: the data model has no such nodes.</remarks>
internal sealed class TreeEmitter : IDisposable
{
    internal const string XmlnsUri = "http://www.w3.org/2000/xmlns/";

    private readonly XmlTreeBuilder _builder = new();
    private readonly bool _allowDocumentType;
    private int _depth;

    internal TreeEmitter(XmlInteropOptions? options) => _allowDocumentType = options?.AllowDocumentType ?? false;

    private XmlNameTable Names => _builder.Names;

    /// <summary>Gets the position of a line-info source; <paramref name="columnOffset"/> is added to its column.</summary>
    internal static XmlPosition Position(IXmlLineInfo? info, int columnOffset) =>
        info is not null && info.HasLineInfo()
            ? new XmlPosition(info.LineNumber, Math.Max(info.LinePosition + columnOffset, 1), 0)
            : default;

    /// <summary>Maps the <c>standalone</c> pseudo-attribute text to its value.</summary>
    internal static bool? ParseStandalone(string? value) =>
        string.Equals(value, "yes", StringComparison.Ordinal) ? true
        : string.Equals(value, "no", StringComparison.Ordinal) ? false
        : null;

    internal void StartDocument(string? version, string? encoding, bool? standalone) =>
        _builder.StartDocument(new XmlDeclaration(version, encoding, standalone));

    internal void Doctype(string name, string? publicId, string? systemId, string? internalSubset, XmlPosition position)
    {
        if (!_allowDocumentType)
        {
            throw new XmlParseException(new XmlDiagnostic(XmlErrorCodes.XML2001, position));
        }

        _builder.Doctype(new XmlDoctypeInfo(name, publicId, systemId, internalSubset));
    }

    internal void StartElement(string prefix, string local, string namespaceUri, XmlPosition position)
    {
        _depth++;
        _builder.StartElement(NameOf(prefix, local, namespaceUri), position);
    }

    internal void NamespaceDeclaration(string prefix, string uri) =>
        _builder.NamespaceDeclaration(Names.Intern(prefix), Names.Intern(uri));

    internal void Attribute(string prefix, string local, string namespaceUri, string value) =>
        _builder.Attribute(NameOf(prefix, local, namespaceUri), value);

    internal void EndAttributes() => _builder.EndAttributes();

    internal void EndElement(string prefix, string local, string namespaceUri)
    {
        _depth--;
        _builder.EndElement(NameOf(prefix, local, namespaceUri));
    }

    internal void Text(string text, XmlTextKind kind)
    {
        if (_depth > 0)
        {
            _builder.Text(text, kind);
        }
    }

    internal void Comment(string text) => _builder.Comment(text);

    internal void ProcessingInstruction(string target, string data) =>
        _builder.ProcessingInstruction(Names.Intern(target), data);

    internal XmlTree Complete()
    {
        _builder.EndDocument();
        return _builder.Complete();
    }

    public void Dispose() => _builder.Dispose();

    private XmlName NameOf(string prefix, string local, string namespaceUri) =>
        Names.Intern(prefix, local, namespaceUri);
}
