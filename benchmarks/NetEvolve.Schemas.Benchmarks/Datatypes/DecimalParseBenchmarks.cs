namespace NetEvolve.Schemas.Benchmarks.Datatypes;

using System.Xml;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>Parses <c>xs:decimal</c> forms inside the <see cref="decimal"/> range from a span, against <c>XmlConvert.ToDecimal</c> on the same text.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DecimalParseBenchmarks
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
        _lexical = DatatypeBenchmarkInput.Decimal(Size);
        if (!XsdDecimal.Parse(_lexical.AsSpan()).Success)
        {
            throw new InvalidOperationException($"The library rejects '{_lexical}'.");
        }

        _ = XmlConvert.ToDecimal(_lexical);
    }

    /// <summary>Baseline: the <c>System.Xml</c> conversion of the same lexical form.</summary>
    /// <returns>The parsed value.</returns>
    [Benchmark(Baseline = true)]
    public decimal ParseDecimalSystemXml() => XmlConvert.ToDecimal(_lexical);

    /// <summary>Parses the lexical form from a span.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseDecimal() => XsdDecimal.Parse(_lexical.AsSpan()).Success;
}
