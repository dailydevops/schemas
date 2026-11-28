namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;

/// <summary>Evaluation of sequence functions over a large range: <c>fn:subsequence</c> reads the range only up to the end of its window (#650). <c>System.Xml.XPath</c> has no range expression, so there is no second engine.</summary>
#pragma warning disable CA1515, CA1001 // BenchmarkDotNet requires public, unsealed benchmark classes; GlobalCleanup disposes the evaluation
public class XPathSequenceStreamBenchmarks
#pragma warning restore CA1515, CA1001
{
    private readonly XPathEvaluation _evaluation = new(XPathBenchmarkInput.SequenceStreams);

    /// <summary>Evaluates the expressions with this library.</summary>
    /// <returns>The number of result items.</returns>
    [Benchmark]
    public int EvaluateSequenceStreams() => _evaluation.Run();

    /// <summary>Releases the evaluation context.</summary>
    [GlobalCleanup]
    public void Cleanup() => _evaluation.Dispose();
}
