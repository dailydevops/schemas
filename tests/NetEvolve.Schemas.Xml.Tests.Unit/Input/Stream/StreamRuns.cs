namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Stream;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Runs one document through the span path and through the stream path and dumps the events for comparison.</summary>
internal static class StreamRuns
{
    internal static RecordingXmlSink NewSink(XmlReaderOptions? options = null) =>
        new(new SpanInterningNameTable((options ?? XmlReaderOptions.Default).Limits.MaxDistinctNames));

    internal static StreamRun Span(byte[] bytes, XmlReaderOptions? options = null)
    {
        using var reader = XmlPipelineReader.Create(bytes.AsSpan(), options);
        var sink = NewSink(options);
        var status = reader.Pump(ref sink);
        return new StreamRun(
            status,
            StreamEventDump.Of(sink),
            reader.Position,
            reader.Diagnostic?.Code,
            reader.Diagnostic?.Position
        );
    }

    internal static async Task<StreamRun> StreamAsync(
        byte[] bytes,
        Func<int, int> chunkSize,
        int bufferSize,
        XmlReaderOptions? options,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ChunkedReadStream(bytes, chunkSize);
        using var reader = new XmlStreamReader<RecordingXmlSink>(stream, NewSink(options), options, bufferSize);
        var status = await reader.ReadAsync(cancellationToken);
        return new StreamRun(
            status,
            StreamEventDump.Of(reader.Sink),
            reader.Position,
            reader.Diagnostic?.Code,
            reader.Diagnostic?.Position
        );
    }

    /// <summary>Chunk sizes from a seeded generator, so a failure repeats.</summary>
    internal static Func<int, int> Seeded(int seed, int max)
    {
        var random = new Random(seed);
        return _ => random.Next(1, max + 1);
    }
}
