namespace NetEvolve.Schemas.Benchmarks.Datatypes;

using System.Xml;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Floating;

/// <summary>Parses <c>xs:double</c> forms from a span, against <c>XmlConvert.ToDouble</c> on the same text.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DoubleParseBenchmarks
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
        _lexical = DatatypeBenchmarkInput.Double(Size);
        if (!XsdFloatingPoint.ParseDouble(_lexical.AsSpan(), XsdVersion.V11).Success)
        {
            throw new InvalidOperationException($"The library rejects '{_lexical}'.");
        }

        _ = XmlConvert.ToDouble(_lexical);
    }

    /// <summary>Baseline: the <c>System.Xml</c> conversion of the same lexical form.</summary>
    /// <returns>The parsed value.</returns>
    [Benchmark(Baseline = true)]
    public double ParseDoubleSystemXml() => XmlConvert.ToDouble(_lexical);

    /// <summary>Parses the lexical form from a span.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseDouble() => XsdFloatingPoint.ParseDouble(_lexical.AsSpan(), XsdVersion.V11).Success;
}
