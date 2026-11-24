namespace NetEvolve.Schemas.Benchmarks.Xml.Reader;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Reads the invoice document as characters, against the <c>System.Xml</c> reader on the same input.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
[Config(typeof(ReaderBenchmarkConfig))]
public class InvoiceCharsReaderBenchmarks
#pragma warning restore CA1515
{
    private readonly string _document = ReaderBenchmarkInput.Invoice;

    /// <summary>Proves that both readers accept the document.</summary>
    [GlobalSetup]
    public void Setup() => ReaderRuns.EnsureBothAccept(_document);

    /// <summary>Baseline: the <c>System.Xml</c> reader.</summary>
    /// <returns>The number of nodes.</returns>
    [Benchmark(Baseline = true)]
    public int ReadSystemXml() => ReaderRuns.SystemXml(_document);

    /// <summary>Reads the document with this library.</summary>
    /// <returns>The status.</returns>
    [Benchmark]
    public XmlReadStatus Read() => ReaderRuns.Library(_document);
}
