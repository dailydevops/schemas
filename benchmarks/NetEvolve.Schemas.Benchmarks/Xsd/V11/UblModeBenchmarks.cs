namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Benchmarks.Xsd.Validation;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Validates the UBL invoice of XSD-66 in strict 1.0 mode and in 1.1 mode: 1.1 mode must cost nothing for a schema of 1.0 features.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class UblModeBenchmarks
#pragma warning restore CA1515
{
    private IXsdValidator _v10 = null!;
    private IXsdValidator _v11 = null!;
    private byte[] _instance = null!;

    /// <summary>Compiles the schema in both versions and proves that both accept the instance.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var corpus = XsdBenchmarkInput.Ubl;
        _instance = corpus.Instance;
        _v10 = Xsd11Validators.Compile(corpus, XsdVersion.V10);
        _v11 = Xsd11Validators.Compile(corpus, XsdVersion.V11);
        if (
            Xsd11Validators.ValidateStream(_v10, _instance) != 0
            || Xsd11Validators.ValidateStream(_v11, _instance) != 0
        )
        {
            throw new InvalidOperationException("A version rejects the instance of the benchmark.");
        }
    }

    /// <summary>Validates through a stream in strict XSD 1.0 mode.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStreamV10() => Xsd11Validators.ValidateStream(_v10, _instance);

    /// <summary>Validates through a stream in XSD 1.1 mode, the default.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStreamV11() => Xsd11Validators.ValidateStream(_v11, _instance);
}
