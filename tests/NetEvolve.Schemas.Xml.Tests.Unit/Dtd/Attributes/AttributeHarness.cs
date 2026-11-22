namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;
using NetEvolve.Schemas.Xml.Input.Stream;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>Reads a document with DTD processing on and dumps the events of <see cref="InfoRecordingSink"/>.</summary>
internal static class AttributeHarness
{
    internal static readonly XmlReaderOptions DtdOn = XmlReaderOptions.Default.WithDtdProcessing(true);

    /// <summary>Reads the characters through the pipeline reader.</summary>
    internal static AttributeRun Read(string xml, XmlReaderOptions? options = null)
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), options ?? DtdOn);
        var sink = new InfoRecordingSink();
        var status = reader.Pump(ref sink);

        return new AttributeRun(status, reader.Diagnostic?.Code.Alias, reader.Diagnostic?.Position, sink.Dump, sink);
    }

    /// <summary>Reads the characters with the limits.</summary>
    internal static AttributeRun Read(string xml, XmlReaderLimits limits) => Read(xml, DtdOn.WithLimits(limits));

    /// <summary>Reads the document as a sequence of one-character segments.</summary>
    internal static string ReadSequence(string xml)
    {
        using var reader = XmlSequenceReader.Create(Segments(xml), DtdOn);
        var sink = new InfoRecordingSink();
        var status = reader.Pump(ref sink);

        return status == XmlReadStatus.Completed ? sink.Dump : status + " " + reader.Diagnostic?.Code.Alias;
    }

    /// <summary>Reads the document from a stream that returns one byte per read.</summary>
    internal static async Task<string> ReadStreamAsync(string xml, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new OneByteStream(Encoding.UTF8.GetBytes(xml));
        var sink = new InfoRecordingSink();
        using var reader = new XmlStreamReader<InfoRecordingSink>(stream, sink, DtdOn, bufferSize: 1, leaveOpen: true);
        var status = await reader.ReadAsync(cancellationToken);

        return status == XmlReadStatus.Completed ? sink.Dump : status + " " + reader.Diagnostic?.Code.Alias;
    }

    private static ReadOnlySequence<char> Segments(string xml)
    {
        Segment? first = null;
        Segment? last = null;
        foreach (var c in xml)
        {
            last = last is null ? (first = new Segment(c)) : last.Append(c);
        }

        return new ReadOnlySequence<char>(first!, 0, last!, 1);
    }

    private sealed class Segment : ReadOnlySequenceSegment<char>
    {
        internal Segment(char c) => Memory = new[] { c };

        internal Segment Append(char c)
        {
            var next = new Segment(c) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    private sealed class OneByteStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
    }
}
