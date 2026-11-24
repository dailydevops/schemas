namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using System.Xml;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;

/// <summary>Parses bounded integers (<c>xs:int</c>) from a span, against <c>XmlConvert.ToInt32</c> on the same text.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class IntegerParseBenchmarks
#pragma warning restore CA1515
{
    private readonly AtomicDescriptor<XsdInteger> _int =
        (AtomicDescriptor<XsdInteger>)BuiltInDatatypes.For(XsdVersion.V11).Get("int");

    private string _lexical = "";

    /// <summary>Gets or sets the input set: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the lexical form and proves that both parsers accept it.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _lexical = DatatypeBenchmarkInput.Integer(Size);
        if (!_int.Parse(_lexical.AsSpan()).Success)
        {
            throw new InvalidOperationException($"The library rejects '{_lexical}'.");
        }

        _ = XmlConvert.ToInt32(_lexical);
    }

    /// <summary>Baseline: the <c>System.Xml</c> conversion of the same lexical form.</summary>
    /// <returns>The parsed value.</returns>
    [Benchmark(Baseline = true)]
    public int ParseIntegerSystemXml() => XmlConvert.ToInt32(_lexical);

    /// <summary>Parses the lexical form from a span.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseInteger() => _int.Parse(_lexical.AsSpan()).Success;
}
