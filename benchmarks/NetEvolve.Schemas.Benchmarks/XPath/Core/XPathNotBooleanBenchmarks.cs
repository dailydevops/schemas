namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:not</c> and <c>fn:boolean</c> over a path with many matches: the effective boolean value of a node sequence is decided by its first
/// node, so the path stops reading there.
/// </summary>
#pragma warning disable CA1515, CA1001 // BenchmarkDotNet requires public, unsealed benchmark classes; GlobalCleanup disposes the context
public class XPathNotBooleanBenchmarks
#pragma warning restore CA1515, CA1001
{
    private static readonly string[] Tests =
    [
        "not(/Invoice/InvoiceLine)",
        "boolean(/Invoice/InvoiceLine/LineAmount)",
        "not(/Invoice/InvoiceLine/Item/Name)",
        "boolean(/Invoice/InvoiceLine)",
    ];

    private readonly XPathExpression[] _expressions = [.. Tests.Select(e => XPathExpression.Compile(e))];
    private readonly XPathEvaluationContext _context = new();

    /// <summary>Loads the invoice.</summary>
    [GlobalSetup]
    public void Setup() =>
        _context.SetContextItem(XdmItem.FromNode(XPathBenchmarkDocument.Root(XPathBenchmarkDocument.LoadTree())));

    /// <summary>Evaluates each test with <c>EvaluateBoolean</c>.</summary>
    /// <returns>The number of true tests.</returns>
    [Benchmark]
    public int EvaluateNotAndBoolean()
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
}
