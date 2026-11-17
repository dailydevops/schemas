namespace NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>One measured benchmark from a BenchmarkDotNet full JSON report.</summary>
internal sealed record BenchmarkResult(
    string Type,
    string Method,
    string Parameters,
    double MeanNs,
    long AllocatedBytes
)
{
    /// <summary>Target key: <c>Type.Method</c>, plus <c>(Parameters)</c> when the benchmark has parameters.</summary>
    public string Name => Parameters.Length == 0 ? $"{Type}.{Method}" : $"{Type}.{Method}({Parameters})";

    /// <summary>Benchmarks of one type and parameter set are compared with each other.</summary>
    public string Group => Parameters.Length == 0 ? Type : $"{Type}({Parameters})";

    /// <summary>True for the <c>System.Xml</c> baseline method (see <see cref="SystemXmlBaseline"/>).</summary>
    public bool IsBaseline => Method.EndsWith(SystemXmlBaseline.MethodSuffix, StringComparison.Ordinal);
}
