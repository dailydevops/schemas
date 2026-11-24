namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Evaluates compiled expressions of this library against the benchmark invoice; the work of the benchmark is the evaluation only.</summary>
internal sealed class XPathEvaluation : IDisposable
{
    private readonly XPathExpression[] _expressions;
    private readonly XPathEvaluationContext _context = new();

    /// <summary>Initializes a new instance of the <see cref="XPathEvaluation"/> class.</summary>
    /// <param name="expressions">The expressions; each must compile.</param>
    public XPathEvaluation(string[] expressions)
    {
        _expressions = new XPathExpression[expressions.Length];
        for (var i = 0; i < expressions.Length; i++)
        {
            _expressions[i] = XPathExpression.Compile(expressions[i]);
        }

        var root = XPathBenchmarkDocument.Root(XPathBenchmarkDocument.LoadTree());
        _context.SetContextItem(XdmItem.FromNode(root));
    }

    /// <summary>Evaluates every expression.</summary>
    /// <returns>The total number of result items.</returns>
    public int Run()
    {
        var count = 0;
        foreach (var expression in _expressions)
        {
            count += expression.Evaluate(_context).Count;
        }

        return count;
    }

    /// <inheritdoc />
    public void Dispose() => _context.Dispose();
}
