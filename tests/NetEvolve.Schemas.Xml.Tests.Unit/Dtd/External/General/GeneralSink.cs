namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// A sink that dumps the events compactly (text events of one kind are coalesced) and samples the base URI of the reader
/// while it handles an element event or starts a text node; it can throw at the start of an element.
/// </summary>
internal sealed class GeneralSink : IXmlSink
{
    private readonly Func<string?>? _baseUri;
    private readonly string? _throwAtElement;
    private readonly List<string> _events = [];
    private readonly List<string> _bases = [];
    private readonly StringBuilder _text = new();
    private XmlTextKind? _kind;

    internal GeneralSink(Func<string?>? baseUri = null, string? throwAtElement = null)
    {
        _baseUri = baseUri;
        _throwAtElement = throwAtElement;
    }

    public XmlNameTable Names { get; } = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

    /// <summary>Gets the events, one entry each.</summary>
    internal string Events
    {
        get
        {
            Flush();
            return string.Join("|", _events);
        }
    }

    /// <summary>Gets the base URI sampled at the start of every element, the end of every element, and the start of every text node.</summary>
    internal string Bases => string.Join("|", _bases);

    public void StartDocument(in XmlDeclaration declaration) { }

    public void EndDocument() => Flush();

    public void StartElement(XmlName name, XmlPosition position)
    {
        Flush();
        var local = Names.GetString(name.Local);
        _events.Add("<" + local);
        Sample("<" + local);
        if (string.Equals(local, _throwAtElement, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("scripted sink failure");
        }
    }

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

    public void Attribute(XmlName name, ReadOnlySpan<char> value) =>
        _events.Add("@" + Names.GetString(name.Local) + "=" + value.ToString());

    public void EndAttributes() { }

    public void EndElement(XmlName name)
    {
        Flush();
        var local = Names.GetString(name.Local);
        _events.Add("/" + local);
        Sample("/" + local);
    }

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        if (_kind != kind)
        {
            Flush();
            _kind = kind;
            Sample("T");
        }

        _ = _text.Append(text);
    }

    public void Comment(ReadOnlySpan<char> text)
    {
        Flush();
        _events.Add("!" + text.ToString());
    }

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data)
    {
        Flush();
        _events.Add("?" + Names.GetString(target) + " " + data.ToString());
    }

    public void Doctype(in XmlDoctypeInfo doctype) { }

    private void Sample(string what) => _bases.Add(what + "@" + (_baseUri?.Invoke() ?? "(none)"));

    private void Flush()
    {
        if (_kind is not { } kind)
        {
            return;
        }

        _events.Add(
            (
                kind == XmlTextKind.Text ? "T'"
                : kind == XmlTextKind.CData ? "C'"
                : "R'"
            )
                + _text
                + "'"
        );
        _ = _text.Clear();
        _kind = null;
    }
}
