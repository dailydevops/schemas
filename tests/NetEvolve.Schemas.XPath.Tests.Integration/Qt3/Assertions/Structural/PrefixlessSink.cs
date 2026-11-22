namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Forwards the events to an inner sink with every prefix and every namespace declaration removed, so that the writer issues its own
/// prefixes in event order; two trees that differ only in their prefixes then serialize to the same text. This is the
/// <c>ignore-prefixes</c> flag of <c>assert-xml</c>.
/// </summary>
internal sealed class PrefixlessSink(IXmlSink inner) : IXmlSink
{
    public XmlNameTable Names => inner.Names;

    public void StartDocument(in XmlDeclaration declaration) => inner.StartDocument(in declaration);

    public void EndDocument() => inner.EndDocument();

    public void StartElement(XmlName name, XmlPosition position) => inner.StartElement(Strip(name), position);

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        // Dropped on purpose: the inner writer adds the declarations the names need.
    }

    public void Attribute(XmlName name, ReadOnlySpan<char> value) => inner.Attribute(Strip(name), value);

    public void EndAttributes() => inner.EndAttributes();

    public void EndElement(XmlName name) => inner.EndElement(Strip(name));

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => inner.Text(text, kind);

    public void Comment(ReadOnlySpan<char> text) => inner.Comment(text);

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        inner.ProcessingInstruction(target, data);

    public void Doctype(in XmlDoctypeInfo doctype) => inner.Doctype(in doctype);

    private static XmlName Strip(XmlName name) => new(default, name.Local, name.NamespaceUri);
}
