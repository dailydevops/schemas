namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Api;
using SystemXPathExpression = System.Xml.XPath.XPathExpression;

/// <summary>Compilation of the XPath 1.0 compatible benchmark expressions, against <c>System.Xml.XPath</c> on the same text.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class XPathCompileBenchmarks
#pragma warning restore CA1515
{
    private readonly string[] _expressions = XPathBenchmarkInput.Compatible;

    /// <summary>Proves that every expression compiles.</summary>
    [GlobalSetup]
    public void Setup()
    {
        if (CompileCompatible() != _expressions.Length)
        {
            throw new InvalidOperationException("An expression of the benchmark input does not compile.");
        }
    }

    /// <summary>Baseline: <c>System.Xml.XPath</c> compiles the expressions.</summary>
    /// <returns>The number of compiled expressions.</returns>
    [Benchmark(Baseline = true)]
    public int CompileCompatibleSystemXml()
    {
        var compiled = 0;
        foreach (var expression in _expressions)
        {
            compiled += SystemXPathExpression.Compile(expression) is null ? 0 : 1;
        }

        return compiled;
    }

    /// <summary>Compiles the expressions with this library: parsing and lowering.</summary>
    /// <returns>The number of compiled expressions.</returns>
    [Benchmark]
    public int CompileCompatible()
    {
        var compiled = 0;
        foreach (var expression in _expressions)
        {
            compiled += XPathExpression.TryCompile(expression).Success ? 1 : 0;
        }

        return compiled;
    }
}
