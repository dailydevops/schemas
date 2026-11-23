namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;

/// <summary>Evaluation of expressions that are dominated by comparisons, against <c>System.Xml.XPath</c> on the same invoice.</summary>
#pragma warning disable CA1515, CA1001 // BenchmarkDotNet requires public, unsealed benchmark classes; GlobalCleanup disposes the evaluation
public class XPathComparisonBenchmarks
#pragma warning restore CA1515, CA1001
{
    private readonly XPathEvaluation _evaluation = new(XPathBenchmarkInput.Comparisons);
    private readonly SystemXmlXPathEvaluation _systemXml = new(XPathBenchmarkInput.Comparisons);

    /// <summary>Proves that both engines do the same work: they return the same number of items.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var expected = _systemXml.Run();
        var actual = _evaluation.Run();
        if (expected != actual)
        {
            throw new InvalidOperationException(
                $"The engines disagree: System.Xml.XPath returns {expected} items, this library {actual}."
            );
        }
    }

    /// <summary>Baseline: <c>System.Xml.XPath</c> evaluates the expressions.</summary>
    /// <returns>The number of result items.</returns>
    [Benchmark(Baseline = true)]
    public int EvaluateComparisonsSystemXml() => _systemXml.Run();

    /// <summary>Evaluates the expressions with this library.</summary>
    /// <returns>The number of result items.</returns>
    [Benchmark]
    public int EvaluateComparisons() => _evaluation.Run();

    /// <summary>Releases the evaluation context.</summary>
    [GlobalCleanup]
    public void Cleanup() => _evaluation.Dispose();
}
