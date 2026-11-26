namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using BenchmarkDotNet.Attributes;

/// <summary>Validates one asserted element whose subtree holds 1000 items; the buffered subtree is ten times larger.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class AssertionSubtree1000Benchmarks
#pragma warning restore CA1515
{
    private IXsdValidator _validator = null!;
    private byte[] _instance = null!;

    /// <summary>Compiles the schema and proves that the assertion runs.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _instance = AssertionDocuments.Bundle(1000);
        _validator = AssertionDocuments.Prove(
            AssertionDocuments.BundleSchema,
            _instance,
            AssertionDocuments.BrokenBundle
        );
    }

    /// <summary>Validates through a stream.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStream() => Xsd11Validators.ValidateStream(_validator, _instance);
}
