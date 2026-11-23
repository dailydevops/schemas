namespace NetEvolve.Schemas.Benchmarks.Xsd.Validation;

using BenchmarkDotNet.Attributes;

/// <summary>Compiles the CII D16B invoice schema (the root document and its imports) with this library and <c>System.Xml.Schema</c>.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class CiiCompilationBenchmarks
#pragma warning restore CA1515
{
    private XsdScenario _scenario = null!;

    /// <summary>Loads the corpus and proves that both engines accept the example.</summary>
    [GlobalSetup]
    public void Setup() => _scenario = new XsdScenario(XsdBenchmarkInput.Cii, XsdBenchmarkInput.Cii.Instance);

    /// <summary>Baseline: compiles with <c>System.Xml.Schema</c>.</summary>
    /// <returns>The compiled set.</returns>
    [Benchmark(Baseline = true)]
    public object CompileCiiSystemXml() => _scenario.CompileSystemXml();

    /// <summary>Compiles with this library.</summary>
    /// <returns>The validator.</returns>
    [Benchmark]
    public object CompileCii() => _scenario.CompileOwn();
}
