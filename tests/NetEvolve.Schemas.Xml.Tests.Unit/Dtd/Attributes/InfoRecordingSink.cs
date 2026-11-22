namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// A class sink that dumps the events compactly: <c>&lt;name</c> for a start tag (with <c>{uri}</c> for a namespace),
/// <c>#prefix=uri</c> for a namespace declaration, <c>@name=value</c> for an attribute, and <c>/name</c> for an end tag.
/// The attribute info events are appended to the entry they belong to: <c>{Type}</c>, <c>{-}</c> without a declared type,
/// <c>!</c> for a defaulted attribute, <c>~</c> for a value that the normalization by the declared type changed.
/// </summary>
internal sealed class InfoRecordingSink : IXmlAttributeInfoSink
{
    private readonly List<string> _parts = [];

    public XmlNameTable Names { get; } = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

    public List<ValidationAttributeInfo> Infos { get; } = [];

    public List<string> InfoNames { get; } = [];

    public List<string> InfoValues { get; } = [];

    public string Dump => string.Join("|", _parts);

    public void StartDocument(in XmlDeclaration declaration) { }

    public void EndDocument() { }

    public void StartElement(XmlName name, XmlPosition position) => _parts.Add("<" + Qualified(name));

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) =>
        _parts.Add("#" + Names.GetString(prefix) + "=" + Names.GetString(uri));

    public void Attribute(XmlName name, ReadOnlySpan<char> value) =>
        _parts.Add("@" + Qualified(name) + "=" + value.ToString());

    public void AttributeInfo(in ValidationAttributeInfo attribute)
    {
        var text = new StringBuilder("{")
            .Append(attribute.DeclaredType?.ToString() ?? "-")
            .Append(attribute.IsSpecified ? string.Empty : "!")
            .Append(attribute.IsChangedByNormalization ? "~" : string.Empty)
            .Append('}')
            .ToString();
        _parts[^1] += text;
        Infos.Add(attribute);
        InfoNames.Add(attribute.Name.ToString());
        InfoValues.Add(attribute.Value.ToString());
    }

    public void EndAttributes() { }

    public void EndElement(XmlName name) => _parts.Add("/" + Qualified(name));

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) { }

    public void Comment(ReadOnlySpan<char> text) { }

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

    public void Doctype(in XmlDoctypeInfo doctype) { }

    private string Qualified(XmlName name)
    {
        var prefix = Names.GetString(name.Prefix);
        var uri = Names.GetString(name.NamespaceUri);
        return (uri.Length == 0 ? string.Empty : "{" + uri + "}")
            + (prefix.Length == 0 ? string.Empty : prefix + ":")
            + Names.GetString(name.Local);
    }
}
