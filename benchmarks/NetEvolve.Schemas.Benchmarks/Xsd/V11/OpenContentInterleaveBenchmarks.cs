namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using BenchmarkDotNet.Attributes;

/// <summary>Validates a sequence of 1000 members with a foreign element between every pair in interleave open content.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class OpenContentInterleaveBenchmarks
#pragma warning restore CA1515
{
    private IXsdValidator _validator = null!;
    private byte[] _instance = null!;

    /// <summary>Compiles the schema and proves that the feature runs.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _instance = FeatureDocuments.OpenContent();
        _validator = FeatureDocuments.Prove(
            FeatureDocuments.OpenContentSchema,
            _instance,
            FeatureDocuments.OpenContent(broken: true)
        );
    }

    /// <summary>Validates through a stream.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStream() => Xsd11Validators.ValidateStream(_validator, _instance);
}
