namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Context;

/// <summary>Evaluation of casts, castability tests, and constructor calls over literals: folded while compiling, against the run-time cast.</summary>
#pragma warning disable CA1515, CA1001 // BenchmarkDotNet requires public, unsealed benchmark classes; GlobalCleanup disposes the context
public class XPathCastFoldBenchmarks
#pragma warning restore CA1515, CA1001
{
    private static readonly string[] Expressions =
    [
        "'42' cast as xs:integer",
        "'1.5e0' cast as xs:double",
        "'2024-05-17' cast as xs:date",
        "'42' castable as xs:integer",
        "xs:integer('42') + xs:integer('8')",
        "xs:boolean('true')",
    ];

    private static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithNamespace("xs", "http://www.w3.org/2001/XMLSchema")
        .Build();

    private readonly XPathEvaluationContext _context = new();
    private CompiledExpression[] _folded = [];
    private CompiledExpression[] _unfolded = [];

    /// <summary>Compiles the expressions with and without the folding rewrite.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _folded = Compile(XPathOptimizations.All);
        _unfolded = Compile(XPathOptimizations.All & ~XPathOptimizations.ConstantFolding);
    }

    /// <summary>Releases the context.</summary>
    [GlobalCleanup]
    public void Cleanup() => _context.Dispose();

    /// <summary>Every cast runs at evaluation.</summary>
    /// <returns>The number of items produced.</returns>
    [Benchmark]
    public int EvaluateUnfolded() => Run(_unfolded);

    /// <summary>The casts were folded while compiling.</summary>
    /// <returns>The number of items produced.</returns>
    [Benchmark]
    public int EvaluateFolded() => Run(_folded);

    private int Run(CompiledExpression[] expressions)
    {
        var count = 0;
        foreach (var expression in expressions)
        {
            count += expression.Evaluate(_context).Count;
        }

        return count;
    }

    private static CompiledExpression[] Compile(XPathOptimizations optimizations) =>
        [
            .. Expressions.Select(e =>
                XPathEngine.TryCompile(e, e, Statics, XPathEngine.AllSlots, optimizations).Expression?.Compiled
                ?? throw new InvalidOperationException("The expression does not compile: " + e)
            ),
        ];
}
