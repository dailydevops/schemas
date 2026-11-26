namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Boolean tests as a rule host evaluates them: only the truth of each expression is needed. The baseline is <c>System.Xml.XPath</c>; this
/// library is measured through <c>Evaluate</c> followed by the effective boolean value, and through <c>EvaluateBoolean</c>.
/// </summary>
#pragma warning disable CA1515, CA1001 // BenchmarkDotNet requires public, unsealed benchmark classes; GlobalCleanup disposes the context
public class XPathBooleanBenchmarks
#pragma warning restore CA1515, CA1001
{
    private readonly XPathExpression[] _expressions =
    [
        .. XPathBenchmarkInput.Booleans.Select(e => XPathExpression.Compile(e)),
    ];
    private readonly System.Xml.XPath.XPathExpression[] _systemXml =
    [
        .. XPathBenchmarkInput.Booleans.Select(e => System.Xml.XPath.XPathExpression.Compile(e)),
    ];
    private readonly System.Xml.XPath.XPathNavigator _navigator = XPathBenchmarkDocument.CreateNavigator();
    private readonly XPathEvaluationContext _context = new();

    /// <summary>Proves that all routes agree on every test.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _context.SetContextItem(XdmItem.FromNode(XPathBenchmarkDocument.Root(XPathBenchmarkDocument.LoadTree())));
        for (var i = 0; i < _expressions.Length; i++)
        {
            var expected = SystemXmlTruth(_systemXml[i]);
            if (
                _expressions[i].EvaluateBoolean(_context) != expected
                || XPathAtomization.EffectiveBooleanValue(_expressions[i].Evaluate(_context)) != expected
            )
            {
                throw new InvalidOperationException($"The routes disagree on {XPathBenchmarkInput.Booleans[i]}.");
            }
        }
    }

    /// <summary>Baseline: <c>System.Xml.XPath</c> evaluates each test.</summary>
    /// <returns>The number of true tests.</returns>
    [Benchmark(Baseline = true)]
    public int EvaluateBooleansSystemXml()
    {
        var count = 0;
        foreach (var expression in _systemXml)
        {
            count += SystemXmlTruth(expression) ? 1 : 0;
        }

        return count;
    }

    /// <summary>Evaluates each test to a sequence and reduces it to its effective boolean value, as a host did before <c>EvaluateBoolean</c>.</summary>
    /// <returns>The number of true tests.</returns>
    [Benchmark]
    public int EvaluateThenReduce()
    {
        var count = 0;
        foreach (var expression in _expressions)
        {
            count += XPathAtomization.EffectiveBooleanValue(expression.Evaluate(_context)) ? 1 : 0;
        }

        return count;
    }

    /// <summary>Evaluates each test with <c>EvaluateBoolean</c>.</summary>
    /// <returns>The number of true tests.</returns>
    [Benchmark]
    public int EvaluateBooleans()
    {
        var count = 0;
        foreach (var expression in _expressions)
        {
            count += expression.EvaluateBoolean(_context) ? 1 : 0;
        }

        return count;
    }

    /// <summary>Releases the evaluation context.</summary>
    [GlobalCleanup]
    public void Cleanup() => _context.Dispose();

    private bool SystemXmlTruth(System.Xml.XPath.XPathExpression expression) =>
        expression.ReturnType == System.Xml.XPath.XPathResultType.NodeSet
            ? _navigator.Select(expression).MoveNext()
            : (bool)_navigator.Evaluate(expression);
}
