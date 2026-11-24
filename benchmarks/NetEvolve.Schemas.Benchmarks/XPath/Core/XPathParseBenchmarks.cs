namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Syntax.Parser;

/// <summary>Parsing of the benchmark expressions into syntax trees. <c>System.Xml.XPath</c> has no separate parse step, so there is no baseline.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class XPathParseBenchmarks
#pragma warning restore CA1515
{
    private readonly string[] _expressions = [.. XPathBenchmarkInput.Compatible, .. XPathBenchmarkInput.Extended];

    /// <summary>Proves that every expression parses.</summary>
    [GlobalSetup]
    public void Setup()
    {
        if (ParseExpressions() != _expressions.Length)
        {
            throw new InvalidOperationException("An expression of the benchmark input does not parse.");
        }
    }

    /// <summary>Parses every expression.</summary>
    /// <returns>The number of expressions that parsed.</returns>
    [Benchmark]
    public int ParseExpressions()
    {
        var parsed = 0;
        foreach (var expression in _expressions)
        {
            if (XPathParser.TryParse(expression.AsSpan(), out _, out _))
            {
                parsed++;
            }
        }

        return parsed;
    }
}
