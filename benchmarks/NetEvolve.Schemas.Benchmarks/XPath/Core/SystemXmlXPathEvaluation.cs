namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using System.Xml.XPath;

/// <summary>The baseline of the evaluation benchmarks: compiled <c>System.Xml.XPath</c> expressions on the same invoice.</summary>
internal sealed class SystemXmlXPathEvaluation
{
    private readonly XPathExpression[] _expressions;
    private readonly XPathNavigator _navigator = XPathBenchmarkDocument.CreateNavigator();

    /// <summary>Initializes a new instance of the <see cref="SystemXmlXPathEvaluation"/> class.</summary>
    /// <param name="expressions">The XPath 1.0 compatible expressions.</param>
    public SystemXmlXPathEvaluation(string[] expressions)
    {
        _expressions = new XPathExpression[expressions.Length];
        for (var i = 0; i < expressions.Length; i++)
        {
            _expressions[i] = XPathExpression.Compile(expressions[i]);
        }
    }

    /// <summary>Evaluates every expression.</summary>
    /// <returns>The total number of result items.</returns>
    public int Run()
    {
        var count = 0;
        foreach (var expression in _expressions)
        {
            count += XPathBenchmarkDocument.Count(_navigator.Evaluate(expression));
        }

        return count;
    }
}
