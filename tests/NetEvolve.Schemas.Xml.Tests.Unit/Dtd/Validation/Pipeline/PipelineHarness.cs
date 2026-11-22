namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Documents and sinks for the runs of the validating reader mode.</summary>
internal static class PipelineHarness
{
    internal const string DocumentUri = "http://example.org/doc/main.xml";

    /// <summary>A document type declaration with the given declarations, then the content.</summary>
    internal static string Doc(string declarations, string content) => $"<!DOCTYPE r [{declarations}]>\n{content}";

    /// <summary>Runs the pipeline with scripted steps over characters and an event recorder as the inner sink.</summary>
    internal static (DtdPipelineResult Result, RecordingSink Sink) Run(string xml, params IDtdValidationStep[] steps)
    {
        var sink = new RecordingSink();
        var result = DtdValidatingPipeline.Validate(xml.AsSpan(), ref sink, documentUri: DocumentUri, steps: steps);

        return (result, sink);
    }

    /// <summary>Runs the pipeline with the steps of the validating mode.</summary>
    internal static DtdPipelineResult Validate(string xml, DtdValidationOptions? options = null) =>
        DtdValidatingPipeline.Validate(xml.AsSpan(), options, documentUri: DocumentUri);

    internal static byte[] Utf16(string xml)
    {
        var preamble = Encoding.Unicode.GetPreamble();
        var body = Encoding.Unicode.GetBytes(xml);
        var bytes = new byte[preamble.Length + body.Length];
        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);

        return bytes;
    }

    /// <summary>A class sink that records every event, including the attribute facts.</summary>
    internal sealed class RecordingSink : IXmlAttributeInfoSink
    {
        public List<string> Events { get; } = [];

        public XmlNameTable Names { get; } = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

        public void StartDocument(in XmlDeclaration declaration) => Events.Add("StartDocument");

        public void EndDocument() => Events.Add("EndDocument");

        public void StartElement(XmlName name, XmlPosition position) =>
            Events.Add("<" + Names.GetString(name.Local) + "@" + position.Line + ":" + position.Column);

        public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) =>
            Events.Add("#" + Names.GetString(prefix) + "=" + Names.GetString(uri));

        public void Attribute(XmlName name, ReadOnlySpan<char> value) =>
            Events.Add("@" + Names.GetString(name.Local) + "=" + value.ToString());

        public void AttributeInfo(in ValidationAttributeInfo attribute) =>
            Events.Add(
                "info:" + attribute.Name.ToString() + ":" + attribute.DeclaredType + ":" + attribute.IsSpecified
            );

        public void EndAttributes() => Events.Add("EndAttributes");

        public void EndElement(XmlName name) => Events.Add("/" + Names.GetString(name.Local));

        public void Text(ReadOnlySpan<char> text, XmlTextKind kind) =>
            Events.Add("text:" + kind + ":" + text.ToString());

        public void Comment(ReadOnlySpan<char> text) => Events.Add("comment:" + text.ToString());

        public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
            Events.Add("pi:" + Names.GetString(target) + ":" + data.ToString());

        public void Doctype(in XmlDoctypeInfo doctype) => Events.Add("doctype:" + doctype.Name.ToString());
    }

    /// <summary>A struct sink that counts the elements; it keeps its state only when the pipeline hands it back.</summary>
    internal struct CountingSink : IXmlSink
    {
        private XmlNameTable? _names;

        public int Elements { get; private set; }

        public XmlNameTable Names => _names ??= new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

        public readonly void StartDocument(in XmlDeclaration declaration) { }

        public readonly void EndDocument() { }

        public void StartElement(XmlName name, XmlPosition position) => Elements++;

        public readonly void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

        public readonly void Attribute(XmlName name, ReadOnlySpan<char> value) { }

        public readonly void EndAttributes() { }

        public readonly void EndElement(XmlName name) { }

        public readonly void Text(ReadOnlySpan<char> text, XmlTextKind kind) { }

        public readonly void Comment(ReadOnlySpan<char> text) { }

        public readonly void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

        public readonly void Doctype(in XmlDoctypeInfo doctype) { }
    }

    /// <summary>A step that copies what it is given while the call runs, because the memory is valid only then.</summary>
    internal sealed class CapturingStep : IDtdValidationStep
    {
        public List<string> Elements { get; } = [];

        public void BeginDocument(DtdValidationContext context) { }

        public void StartElement(in ValidationElement element)
        {
            var parts = new List<string>
            {
                element.Name.ToString() + "@" + element.Position.Line + ":" + element.Position.Column,
            };
            foreach (var attribute in element.Attributes.Span)
            {
                parts.Add(
                    attribute.Name.ToString()
                        + "="
                        + attribute.Value.ToString()
                        + ":"
                        + (attribute.DeclaredType?.ToString() ?? "-")
                        + (attribute.IsSpecified ? string.Empty : "!")
                        + (attribute.IsChangedByNormalization ? "~" : string.Empty)
                );
            }

            Elements.Add(string.Join(" ", parts));
        }

        public void EntityReference(in ValidationEntityReference reference) { }

        public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position) { }

        public void Comment(in XmlPosition position) { }

        public void ProcessingInstruction(in XmlPosition position) { }

        public void EndElement(in ValidationElement element) =>
            Elements.Add("/" + element.Name.ToString() + "@" + element.Position.Line + ":" + element.Position.Column);

        public void EndDocument() { }
    }

    /// <summary>A step that does nothing and allocates nothing.</summary>
    internal sealed class NoOpStep : IDtdValidationStep
    {
        public void BeginDocument(DtdValidationContext context) { }

        public void StartElement(in ValidationElement element) { }

        public void EntityReference(in ValidationEntityReference reference) { }

        public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position) { }

        public void Comment(in XmlPosition position) { }

        public void ProcessingInstruction(in XmlPosition position) { }

        public void EndElement(in ValidationElement element) { }

        public void EndDocument() { }
    }
}
