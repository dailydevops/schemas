namespace NetEvolve.Schemas.Benchmarks.Xml.Pipeline;

using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>Bytes-to-document load, against <c>XDocument.Load</c> of <c>System.Xml.Linq</c> on the same bytes.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DocumentLoadBenchmarks
#pragma warning restore CA1515
{
    private readonly byte[] _document = PipelineBenchmarkInput.Document;

    /// <summary>Proves that both loaders read the same document.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var tree = XmlDocumentLoader.Load(_document.AsSpan());
        if (!string.Equals(tree.Document.StringValue, LoadDocumentSystemXml().Root!.Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The loaders disagree on the text of the document.");
        }
    }

    /// <summary>Baseline: <c>XDocument.Load</c> reads the bytes through a stream.</summary>
    /// <returns>The document.</returns>
    [Benchmark(Baseline = true)]
    public XDocument LoadDocumentSystemXml()
    {
        using var stream = new MemoryStream(_document, writable: false);
        return XDocument.Load(stream);
    }

    /// <summary>Loads the bytes into the document tree.</summary>
    /// <returns>The tree.</returns>
    [Benchmark]
    public XmlDocumentTree LoadDocument() => XmlDocumentLoader.Load(_document.AsSpan());
}
