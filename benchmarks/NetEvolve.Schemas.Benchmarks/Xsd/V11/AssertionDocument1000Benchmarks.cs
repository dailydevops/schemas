namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using BenchmarkDotNet.Attributes;

/// <summary>Validates an order of ten asserted lines and 1000 plain entries. The small size of the document-size pair.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class AssertionDocument1000Benchmarks
#pragma warning restore CA1515
{
    private IXsdValidator _validator = null!;
    private byte[] _instance = null!;

    /// <summary>Compiles the schema and proves that the assertion runs.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _instance = AssertionDocuments.Order(1000);
        _validator = AssertionDocuments.Prove(
            AssertionDocuments.LineSchema,
            _instance,
            AssertionDocuments.Order(1000, brokenLine: 5)
        );
    }

    /// <summary>Validates through a stream.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStream() => Xsd11Validators.ValidateStream(_validator, _instance);

    /// <summary>Validates through a span.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateSpan() => Xsd11Validators.ValidateSpan(_validator, _instance);
}
