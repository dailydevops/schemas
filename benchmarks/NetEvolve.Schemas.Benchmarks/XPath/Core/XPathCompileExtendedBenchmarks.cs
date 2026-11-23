namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Api;

/// <summary>Compilation of XPath 2.0 expressions; <c>System.Xml.XPath</c> cannot compile them, so there is no baseline.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class XPathCompileExtendedBenchmarks
#pragma warning restore CA1515
{
    private readonly string[] _expressions = XPathBenchmarkInput.Extended;

    /// <summary>Proves that every expression compiles.</summary>
    [GlobalSetup]
    public void Setup()
    {
        if (CompileExtended() != _expressions.Length)
        {
            throw new InvalidOperationException("An expression of the benchmark input does not compile.");
        }
    }

    /// <summary>Compiles the expressions with this library.</summary>
    /// <returns>The number of compiled expressions.</returns>
    [Benchmark]
    public int CompileExtended()
    {
        var compiled = 0;
        foreach (var expression in _expressions)
        {
            compiled += XPathExpression.TryCompile(expression).Success ? 1 : 0;
        }

        return compiled;
    }
}
