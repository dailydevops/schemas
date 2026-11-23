namespace NetEvolve.Schemas.Benchmarks.Xml.Pipeline;

using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>
/// Async stream load against <c>XDocument.LoadAsync</c> on the same bytes. The span load of the same input is
/// benchmarked next to it, so the ratio of <see cref="LoadStream"/> to <see cref="LoadSpan"/> is the cost of the
/// buffering adapter over the span path.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class StreamLoadBenchmarks
#pragma warning restore CA1515
{
    private readonly byte[] _document = PipelineBenchmarkInput.Document;

    /// <summary>Proves that the stream and the span path read the same document.</summary>
    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable VSTHRD002 // Setup is synchronous.
        var text = LoadStream().AsTask().GetAwaiter().GetResult().Document.StringValue;
#pragma warning restore VSTHRD002
        if (!string.Equals(text, LoadSpan().Document.StringValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The stream and the span path disagree on the document.");
        }
    }

    /// <summary>Baseline: <c>XDocument.LoadAsync</c> reads the stream.</summary>
    /// <returns>The document.</returns>
    [Benchmark(Baseline = true)]
#pragma warning disable NE0010, VSTHRD200 // A benchmark takes no parameters; the name is the target name.
    public async Task<XDocument> LoadStreamSystemXml()
    {
        using var stream = new MemoryStream(_document, writable: false);
        return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>Loads the stream into the document tree.</summary>
    /// <returns>The tree.</returns>
    [Benchmark]
    public async ValueTask<XmlDocumentTree> LoadStream()
    {
        using var stream = new MemoryStream(_document, writable: false);
        return await XmlDocumentLoader.LoadAsync(stream).ConfigureAwait(false);
    }
#pragma warning restore NE0010, VSTHRD200

    /// <summary>Loads the same bytes from a span; the core that the stream path buffers into.</summary>
    /// <returns>The tree.</returns>
    [Benchmark]
    public XmlDocumentTree LoadSpan() => XmlDocumentLoader.Load(_document.AsSpan());
}
