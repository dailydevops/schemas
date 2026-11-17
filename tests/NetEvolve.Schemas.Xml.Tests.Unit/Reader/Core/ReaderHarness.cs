namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Parses text through <see cref="XmlParser"/> in chunks and dumps the events as a compact string.</summary>
internal static class ReaderHarness
{
    /// <summary>Runs a parse; <paramref name="split"/> cuts the input once, <paramref name="chunkSize"/> cuts it evenly.</summary>
    internal static ReaderRun Run(
        string xml,
        XmlReaderLimits? limits = null,
        int split = -1,
        int chunkSize = 0,
        IXmlDoctypeHandler? doctype = null,
        IXmlEntityInputStack? stack = null,
        XmlNameTable? names = null
    )
    {
        limits ??= XmlReaderLimits.Default;
        using var parser = new XmlParser(limits, doctype, stack);
        var sink = new RecordingXmlSink(names ?? new SpanInterningNameTable(limits.MaxDistinctNames));
        var status = XmlReadStatus.NeedMoreData;
        foreach (var (chunk, isFinal) in Chunks(xml, split, chunkSize))
        {
            parser.Feed(chunk.AsMemory(), isFinal);
            status = parser.Pump(ref sink);
            if (status != XmlReadStatus.NeedMoreData)
            {
                break;
            }
        }

        return new ReaderRun(
            Dump(sink),
            DumpPositions(sink),
            status,
            parser.Diagnostic?.Code,
            parser.Diagnostic?.Position,
            parser.Position
        );
    }

    private static IEnumerable<(string Chunk, bool IsFinal)> Chunks(string xml, int split, int chunkSize)
    {
        if (split >= 0)
        {
            yield return (xml.Substring(0, split), false);
            yield return (xml.Substring(split), true);
            yield break;
        }

        if (chunkSize <= 0)
        {
            yield return (xml, true);
            yield break;
        }

        for (var i = 0; i < xml.Length; i += chunkSize)
        {
            var length = Math.Min(chunkSize, xml.Length - i);
            yield return (xml.Substring(i, length), i + length >= xml.Length);
        }

        if (xml.Length == 0)
        {
            yield return (string.Empty, true);
        }
    }

    private static string Name(XmlNameTable names, XmlName name)
    {
        var uri = names.GetString(name.NamespaceUri);
        var prefix = names.GetString(name.Prefix);
        return (uri.Length > 0 ? "{" + uri + "}" : string.Empty)
            + (prefix.Length > 0 ? prefix + ":" : string.Empty)
            + names.GetString(name.Local);
    }

    // Consecutive text events of one kind are one node, so chunk edges must not show.
    private static string Dump(RecordingXmlSink sink)
    {
        var builder = new StringBuilder();
        var names = sink.Names;
        var pendingKind = (XmlTextKind?)null;
        var pending = new StringBuilder();

        void FlushText()
        {
            if (pendingKind is not null)
            {
                builder.Append(pendingKind == XmlTextKind.CData ? "CD'" : "T'").Append(pending).Append("'|");
                pending.Clear();
                pendingKind = null;
            }
        }

        foreach (var e in sink.Events)
        {
            if (e.Kind == XmlEventKind.Text)
            {
                var kind = e.TextKind == XmlTextKind.CData ? XmlTextKind.CData : XmlTextKind.Text;
                if (pendingKind != kind)
                {
                    FlushText();
                    pendingKind = kind;
                }

                pending.Append(e.Text);
                continue;
            }

            FlushText();
            builder.Append(
                e.Kind switch
                {
                    XmlEventKind.StartDocument => "SD(" + e.Text + ")",
                    XmlEventKind.EndDocument => "ED",
                    XmlEventKind.StartElement => "<" + Name(names, e.Name),
                    XmlEventKind.NamespaceDeclaration => "xmlns:"
                        + names.GetString(e.Prefix)
                        + "="
                        + names.GetString(e.Uri),
                    XmlEventKind.Attribute => "@" + Name(names, e.Name) + "=" + e.Text,
                    XmlEventKind.EndAttributes => ">",
                    XmlEventKind.EndElement => "/" + Name(names, e.Name),
                    XmlEventKind.Comment => "C'" + e.Text + "'",
                    XmlEventKind.ProcessingInstruction => "PI(" + names.GetString(e.Prefix) + "," + e.Text + ")",
                    XmlEventKind.Doctype => "DT(" + e.Text + ")",
                    _ => "?",
                }
            );
            builder.Append('|');
        }

        FlushText();
        return builder.Length > 0 ? builder.ToString(0, builder.Length - 1) : string.Empty;
    }

    private static string DumpPositions(RecordingXmlSink sink)
    {
        var builder = new StringBuilder();
        foreach (var e in sink.Events)
        {
            if (e.Kind == XmlEventKind.StartElement)
            {
                builder.Append(e.Position).Append(';');
            }
        }

        return builder.ToString();
    }
}
