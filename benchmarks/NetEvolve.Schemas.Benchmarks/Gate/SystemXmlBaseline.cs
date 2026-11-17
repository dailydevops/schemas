namespace NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>
/// Marks the <c>System.Xml</c> baseline of a benchmark class: a baseline method is named
/// <c>&lt;Method&gt;</c> + <see cref="MethodSuffix"/> and carries <c>[Benchmark(Baseline = true)]</c>.
/// The JSON report has no attribute data, so the gate recognizes the baseline by this suffix.
/// </summary>
internal static class SystemXmlBaseline
{
    public const string MethodSuffix = "SystemXml";
}
