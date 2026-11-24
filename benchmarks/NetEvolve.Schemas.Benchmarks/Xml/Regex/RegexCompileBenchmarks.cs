namespace NetEvolve.Schemas.Benchmarks.Xml.Regex;

using BenchmarkDotNet.Attributes;

/// <summary>
/// Compiling every pattern of the set (parse and program emission), against compiling the same patterns as pattern
/// facets of one <c>System.Xml.Schema</c> schema set.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class RegexCompileBenchmarks
#pragma warning restore CA1515
{
    private readonly string[] _patterns = RegexBenchmarkInput.Patterns;

    /// <summary>Baseline: the <c>System.Xml.Schema</c> compilation of the patterns.</summary>
    /// <returns>The number of compiled types.</returns>
    [Benchmark(Baseline = true)]
    public int CompileSystemXml() => SystemXmlPatternValidation.Compile(_patterns).Length;

    /// <summary>Parses and compiles each pattern for the XSD dialect.</summary>
    /// <returns>The number of compiled programs.</returns>
    [Benchmark]
    public int Compile() => RegexBenchmarkCompiler.CompileAll(_patterns).Length;
}
