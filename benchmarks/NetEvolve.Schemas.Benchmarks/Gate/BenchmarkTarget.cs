namespace NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>One recorded target of a <c>benchmarks/targets/&lt;name&gt;.json</c> file.</summary>
/// <param name="Name">The benchmark name, see <see cref="BenchmarkResult.Name"/>.</param>
/// <param name="MeanNs">Recorded mean in nanoseconds.</param>
/// <param name="AllocatedBytes">Recorded allocation per operation.</param>
/// <param name="BaselineRatio">Recorded mean ratio to the <c>System.Xml</c> baseline, or <see langword="null"/> when none exists.</param>
internal sealed record BenchmarkTarget(string Name, double MeanNs, long AllocatedBytes, double? BaselineRatio);
