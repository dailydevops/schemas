namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using System.Globalization;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>
/// The string <c>enumeration</c> facet on a code list of 10 (<c>Short</c>), 50 (<c>Typical</c>), or 200 (<c>Long</c>) values: a hit near the end of the list and a miss.
/// There is no <c>System.Xml</c> counterpart, so the recorded values gate regressions (limitation).
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class EnumerationFacetBenchmarks
#pragma warning restore CA1515
{
    private LexicalEnumerationFacet _facet = null!;
    private string _hit = null!;
    private string _miss = null!;

    /// <summary>Gets or sets the input set: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the facet and the probes.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var count =
            Size == DatatypeBenchmarkInput.Short ? 10
            : Size == DatatypeBenchmarkInput.Typical ? 50
            : 200;
        var values = new string[count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = "C" + i.ToString("D3", CultureInfo.InvariantCulture);
        }

        _facet = new LexicalEnumerationFacet(values);
        _hit = values[count - 2];
        _miss = "X999";
    }

    /// <summary>Checks a listed value near the end of the list.</summary>
    /// <returns>Whether the value is listed.</returns>
    [Benchmark]
    public bool Hit() => _facet.IsSatisfiedBy(_hit.AsSpan());

    /// <summary>Checks a value that is not listed.</summary>
    /// <returns>Whether the value is listed.</returns>
    [Benchmark]
    public bool Miss() => _facet.IsSatisfiedBy(_miss.AsSpan());
}
