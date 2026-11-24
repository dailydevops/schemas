namespace NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>Renders a markdown table of results against recorded targets.</summary>
internal static class BenchmarkSummary
{
    public static string Render(IReadOnlyList<BenchmarkResult> results, IReadOnlyList<BenchmarkTarget> targets) =>
        throw new NotImplementedException();

    public static int Run(string resultsDirectory, string targetsDirectory, TextWriter output) =>
        throw new NotImplementedException();
}
