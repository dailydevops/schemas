namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

/// <summary>One document in a given encoding: the sample of every sequence parity test.</summary>
internal sealed record SequenceSample(string Name, string Xml, Encoding Encoding, bool Bom = false, bool Dtd = false)
{
    internal XmlReaderOptions Options => Dtd ? XmlReaderOptions.Default.WithDtdProcessing(true) : XmlReaderOptions.Default;

    internal byte[] Bytes
    {
        get
        {
            var body = Encoding.GetBytes(Xml);
            if (!Bom)
            {
                return body;
            }

            var preamble = Encoding.GetPreamble();
            var all = new byte[preamble.Length + body.Length];
            preamble.CopyTo(all, 0);
            body.CopyTo(all, preamble.Length);
            return all;
        }
    }

    public override string ToString() => Name;
}

/// <summary>Runs one document through the span path and through the sequence path and dumps the events for comparison.</summary>
internal static class SequenceRuns
{
    // CR and CRLF, surrogate pairs and two-byte characters in text, references, CDATA, a comment, and a processing
    // instruction: every hazard of a segment edge.
    internal const string Hazards =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<!-- cé --><?pi däta?>\n"
        + "<r xmlns=\"urn:d\" xmlns:p=\"urn:p\" a=\"x&amp;y\t\r\nz&#10;\" p:b='\U0001F600'>\r\n  café \U0001D11E &lt; &#x1F600;\r"
        + "<p:e/><x\U0001F600y/><![CDATA[a]b]]c]]]]><![CDATA[\r\nq]]>t<!-- - --><?pi2 ? ??>?>d?>  </r>\r\n<!--tail-->\n";

    internal const string Doctype =
        "<!DOCTYPE a [<!ENTITY e \"x&amp;<b/>é\"><!ENTITY v \"1\"><!ELEMENT a (#PCDATA)>]>"
        + "<a x=\"&v;\">&e;<![CDATA[z]]></a>";

    internal static string Medium(int items = 25)
    {
        var builder = new StringBuilder("<?xml version=\"1.0\"?><root xmlns:p=\"urn:p\">");
        for (var i = 0; i < items; i++)
        {
            builder
                .Append("<p:item id=\"")
                .Append(i)
                .Append("\" t='é&lt;'>text ")
                .Append(i)
                .Append(" €\U0001D11E</p:item>\n");
        }

        return builder.Append("</root>").ToString();
    }

    internal static IEnumerable<Func<SequenceSample>> Samples()
    {
        yield return () => new SequenceSample("hazards utf-8", Hazards, Encoding.UTF8);
        yield return () => new SequenceSample("hazards utf-8 with bom", Hazards, new UTF8Encoding(false), Bom: true);
        yield return () => new SequenceSample("medium utf-8", Medium(), Encoding.UTF8);
        yield return () => new SequenceSample("no declaration", "<a x=\"1\"><b/>café</a>", Encoding.UTF8);
        yield return () =>
            new SequenceSample(
                "hazards utf-16le with bom",
                Hazards.Replace(" encoding=\"UTF-8\"", string.Empty, StringComparison.Ordinal),
                new UnicodeEncoding(false, false),
                Bom: true
            );
        yield return () =>
            new SequenceSample("medium utf-16be with bom", Medium(5), new UnicodeEncoding(true, false), Bom: true);
        yield return () =>
            new SequenceSample(
                "declared iso-8859-1",
                "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><a b=\"é\">café</a>",
                Encoding.GetEncoding("ISO-8859-1")
            );
        yield return () =>
            new SequenceSample(
                "declared us-ascii",
                "<?xml version='1.0' encoding='us-ascii'?><a>plain</a>",
                Encoding.ASCII
            );
        yield return () => new SequenceSample("doctype with entities", Doctype, Encoding.UTF8, Dtd: true);
    }

    internal static StreamRun Span(SequenceSample sample)
    {
        using var reader = XmlPipelineReader.Create(sample.Bytes.AsSpan(), sample.Options);
        return Pump(reader, sample.Options);
    }

    internal static StreamRun SpanChars(string xml, XmlReaderOptions? options = null)
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), options);
        return Pump(reader, options);
    }

    internal static StreamRun Sequence(ReadOnlySequence<byte> input, XmlReaderOptions? options = null)
    {
        using var reader = XmlSequenceReader.Create(input, options);
        return Pump(reader, options);
    }

    internal static StreamRun Sequence(ReadOnlySequence<char> input, XmlReaderOptions? options = null)
    {
        using var reader = XmlSequenceReader.Create(input, options);
        return Pump(reader, options);
    }

    /// <summary>The outcome that does not depend on the cut: after an error the position follows the token boundaries (#598).</summary>
    internal static StreamRun WithoutPosition(StreamRun run) => run with { Position = default };

    private static StreamRun Pump<TReader>(TReader reader, XmlReaderOptions? options)
        where TReader : IXmlReader
    {
        var sink = StreamRuns.NewSink(options);
        var status = reader.Pump(ref sink);
        return new StreamRun(
            status,
            StreamEventDump.Of(sink),
            reader.Position,
            reader.Diagnostic?.Code,
            reader.Diagnostic?.Position
        );
    }
}
