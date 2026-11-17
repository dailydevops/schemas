namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A class sink that records every event it receives.</summary>
internal sealed class RecordingXmlSink : IXmlSink
{
    public RecordingXmlSink(XmlNameTable? names = null) => Names = names ?? new FakeNameTable();

    public XmlNameTable Names { get; }

    public List<XmlEventRecord> Events { get; } = [];

    public void StartDocument(in XmlDeclaration declaration) =>
        Events.Add(new XmlEventRecord(XmlEventKind.StartDocument, Text: declaration.Version.ToString()));

    public void EndDocument() => Events.Add(new XmlEventRecord(XmlEventKind.EndDocument));

    public void StartElement(XmlName name, XmlPosition position) =>
        Events.Add(new XmlEventRecord(XmlEventKind.StartElement, name, position));

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) =>
        Events.Add(new XmlEventRecord(XmlEventKind.NamespaceDeclaration, Prefix: prefix, Uri: uri));

    public void Attribute(XmlName name, ReadOnlySpan<char> value) =>
        Events.Add(new XmlEventRecord(XmlEventKind.Attribute, name, Text: value.ToString()));

    public void EndAttributes() => Events.Add(new XmlEventRecord(XmlEventKind.EndAttributes));

    public void EndElement(XmlName name) => Events.Add(new XmlEventRecord(XmlEventKind.EndElement, name));

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) =>
        Events.Add(new XmlEventRecord(XmlEventKind.Text, Text: text.ToString(), TextKind: kind));

    public void Comment(ReadOnlySpan<char> text) =>
        Events.Add(new XmlEventRecord(XmlEventKind.Comment, Text: text.ToString()));

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        Events.Add(new XmlEventRecord(XmlEventKind.ProcessingInstruction, Prefix: target, Text: data.ToString()));

    public void Doctype(in XmlDoctypeInfo doctype) =>
        Events.Add(new XmlEventRecord(XmlEventKind.Doctype, Text: doctype.Name.ToString()));
}
