namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>
/// Parses <c>xs:decimal</c> forms beyond the range of <see cref="decimal"/>. <c>System.Xml</c> overflows on them, so there is no
/// baseline; the recorded values gate regressions (limitation).
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DecimalBeyondRangeBenchmarks
#pragma warning restore CA1515
{
    private string _lexical = "";

    /// <summary>Gets or sets the input set: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the lexical form.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _lexical = DatatypeBenchmarkInput.WideDecimal(Size);
        if (!XsdDecimal.Parse(_lexical.AsSpan()).Success)
        {
            throw new InvalidOperationException($"The library rejects '{_lexical}'.");
        }
    }

    /// <summary>Parses the lexical form from a span.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseWideDecimal() => XsdDecimal.Parse(_lexical.AsSpan()).Success;
}
