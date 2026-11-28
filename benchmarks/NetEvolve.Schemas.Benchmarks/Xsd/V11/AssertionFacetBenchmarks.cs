namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using BenchmarkDotNet.Attributes;

/// <summary>Validates 1000 values of a simple type with an assertion facet.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class AssertionFacetBenchmarks
#pragma warning restore CA1515
{
    private IXsdValidator _validator = null!;
    private byte[] _instance = null!;

    /// <summary>Compiles the schema and proves that the feature runs.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _instance = FeatureDocuments.Facet();
        _validator = FeatureDocuments.Prove(
            FeatureDocuments.FacetSchema,
            _instance,
            FeatureDocuments.Facet(broken: true)
        );
    }

    /// <summary>Validates through a stream.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStream() => Xsd11Validators.ValidateStream(_validator, _instance);
}
