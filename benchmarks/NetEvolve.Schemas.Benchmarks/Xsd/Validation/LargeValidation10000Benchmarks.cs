namespace NetEvolve.Schemas.Benchmarks.Xsd.Validation;

using BenchmarkDotNet.Attributes;

/// <summary>Validates a UBL invoice with 10000 repeated lines; the large size of the allocation-independence pair.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class LargeValidation10000Benchmarks
#pragma warning restore CA1515
{
    private XsdScenario _scenario = null!;

    /// <summary>Compiles both engines and proves that both accept the instance.</summary>
    [GlobalSetup]
    public void Setup() =>
        _scenario = new XsdScenario(
            XsdBenchmarkInput.Ubl,
            XsdBenchmarkInput.Repeat(XsdBenchmarkInput.Ubl.Instance, "cac:InvoiceLine", 10000)
        );

    /// <summary>Baseline: <c>System.Xml</c> validates through a stream.</summary>
    /// <returns>The number of validation events.</returns>
    [Benchmark(Baseline = true)]
    public int ValidateStreamSystemXml() => _scenario.ValidateSystemXml();

    /// <summary>Validates through a stream.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStream() => _scenario.ValidateStream();

    /// <summary>Validates through a span.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateSpan() => _scenario.ValidateSpan();
}
