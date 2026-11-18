namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Reads a document through <see cref="XmlPipelineReader"/> with DTD processing on and dumps the events compactly.</summary>
internal static class EntityHarness
{
    private static readonly XmlReaderOptions OptIn = XmlReaderOptions.Default.WithDtdProcessing(true);

    /// <summary>Reads the document; the dump holds start tags, attributes, coalesced text, and end tags.</summary>
    internal static EntityRun Read(string xml, XmlReaderLimits? limits = null)
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), limits is null ? OptIn : OptIn.WithLimits(limits));
        var sink = new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));
        var status = reader.Pump(ref sink);
        var positions = new List<XmlPosition>();
        foreach (var e in sink.Events)
        {
            if (e.Kind == XmlEventKind.StartElement)
            {
                positions.Add(e.Position);
            }
        }

        return new EntityRun(status, reader.Diagnostic?.Code.Alias, reader.Diagnostic?.Position, Dump(sink), positions);
    }

    /// <summary>Reads the document and returns the text of the first event of the given kind (a comment or a processing instruction).</summary>
    internal static string? FirstTextOf(string xml, XmlEventKind kind)
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), OptIn);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));
        _ = reader.Pump(ref sink);
        foreach (var e in sink.Events)
        {
            if (e.Kind == kind)
            {
                return e.Text;
            }
        }

        return null;
    }

    private static string Dump(RecordingXmlSink sink)
    {
        var parts = new List<string>();
        var text = new StringBuilder();

        void Flush()
        {
            if (text.Length > 0)
            {
                parts.Add("'" + text + "'");
                text.Clear();
            }
        }

        foreach (var e in sink.Events)
        {
            switch (e.Kind)
            {
                case XmlEventKind.Text:
                    text.Append(e.Text);
                    break;
                case XmlEventKind.StartElement:
                    Flush();
                    parts.Add("<" + sink.Names.GetString(e.Name.Local));
                    break;
                case XmlEventKind.Attribute:
                    parts.Add("@" + sink.Names.GetString(e.Name.Local) + "=" + e.Text);
                    break;
                case XmlEventKind.EndElement:
                    Flush();
                    parts.Add("/" + sink.Names.GetString(e.Name.Local));
                    break;
                default:
                    break;
            }
        }

        Flush();
        return string.Join("|", parts);
    }
}
