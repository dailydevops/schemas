namespace NetEvolve.Schemas.Benchmarks.Datatypes;

using System.Xml;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Duration;

/// <summary>Parses <c>xs:duration</c> forms from a span, against <c>XmlConvert.ToTimeSpan</c> on the same text.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DurationParseBenchmarks
#pragma warning restore CA1515
{
    private string _lexical = "";

    /// <summary>Gets or sets the input set: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the lexical form and proves that both parsers accept it.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _lexical = DatatypeBenchmarkInput.Duration(Size);
        if (!XsdDuration.Parse(_lexical.AsSpan()).Success)
        {
            throw new InvalidOperationException($"The library rejects '{_lexical}'.");
        }

        _ = XmlConvert.ToTimeSpan(_lexical);
    }

    /// <summary>Baseline: the <c>System.Xml</c> conversion of the same lexical form.</summary>
    /// <returns>The parsed value.</returns>
    [Benchmark(Baseline = true)]
    public TimeSpan ParseDurationSystemXml() => XmlConvert.ToTimeSpan(_lexical);

    /// <summary>Parses the lexical form from a span.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseDuration() => XsdDuration.Parse(_lexical.AsSpan()).Success;
}
