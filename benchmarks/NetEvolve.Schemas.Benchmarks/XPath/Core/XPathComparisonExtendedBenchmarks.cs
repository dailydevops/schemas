namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;

/// <summary>Evaluation of XPath 2.0 expressions with value comparisons and quantifiers; <c>System.Xml.XPath</c> cannot run them, so there is no baseline.</summary>
#pragma warning disable CA1515, CA1001 // BenchmarkDotNet requires public, unsealed benchmark classes; GlobalCleanup disposes the evaluation
public class XPathComparisonExtendedBenchmarks
#pragma warning restore CA1515, CA1001
{
    private readonly XPathEvaluation _evaluation = new(XPathBenchmarkInput.Extended);

    /// <summary>Evaluates the XPath 2.0 expressions.</summary>
    /// <returns>The number of result items.</returns>
    [Benchmark]
    public int EvaluateExtended() => _evaluation.Run();

    /// <summary>Releases the evaluation context.</summary>
    [GlobalCleanup]
    public void Cleanup() => _evaluation.Dispose();
}
