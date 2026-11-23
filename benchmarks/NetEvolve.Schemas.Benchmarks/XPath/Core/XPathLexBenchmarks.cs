namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Syntax.Lexer;

/// <summary>Lexing of the benchmark expressions. <c>System.Xml.XPath</c> has no public lexer, so there is no baseline.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class XPathLexBenchmarks
#pragma warning restore CA1515
{
    private readonly string[] _expressions = [.. XPathBenchmarkInput.Compatible, .. XPathBenchmarkInput.Extended];

    /// <summary>Reads every token of every expression.</summary>
    /// <returns>The number of tokens.</returns>
    [Benchmark]
    public int LexExpressions()
    {
        var count = 0;
        foreach (var expression in _expressions)
        {
            var lexer = new XPathLexer(expression.AsSpan());
            XPathTokenKind kind;
            do
            {
                kind = lexer.Next().Kind;
                count++;
            } while (kind is not (XPathTokenKind.EndOfInput or XPathTokenKind.Error));
        }

        return count;
    }
}
