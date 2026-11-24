namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Binary;

/// <summary>Parses <c>xs:base64Binary</c> forms from a span, against <c>Convert.FromBase64String</c> on the same text.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class Base64ParseBenchmarks
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
        _lexical = DatatypeBenchmarkInput.Base64(Size);
        if (!XsdBinary.Parse(_lexical.AsSpan(), BinaryKind.Base64Binary).Success)
        {
            throw new InvalidOperationException($"The library rejects '{_lexical}'.");
        }

        _ = Convert.FromBase64String(_lexical);
    }

    /// <summary>Baseline: the BCL conversion that <c>System.Xml</c> uses for the same lexical form.</summary>
    /// <returns>The decoded octets.</returns>
    [Benchmark(Baseline = true)]
    public byte[] ParseBase64SystemXml() => Convert.FromBase64String(_lexical);

    /// <summary>Parses the lexical form from a span.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseBase64() => XsdBinary.Parse(_lexical.AsSpan(), BinaryKind.Base64Binary).Success;
}
