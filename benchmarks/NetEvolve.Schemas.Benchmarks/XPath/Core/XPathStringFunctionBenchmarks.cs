namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The case-folding string functions as a rule host uses them on a compared value: <c>translate</c> with the 26-letter maps,
/// <c>upper-case</c>, and <c>lower-case</c>, on short codes and on a 60-character name. The baseline is <c>System.Xml.XPath</c>, which
/// knows <c>translate</c> only (XPath 1.0), so the ratios of the case functions compare against that baseline too.
/// </summary>
#pragma warning disable CA1515, CA1001 // BenchmarkDotNet requires public, unsealed benchmark classes; GlobalCleanup disposes the context
public class XPathStringFunctionBenchmarks
#pragma warning restore CA1515, CA1001
{
    private const string Upper = "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'";
    private const string Lower = "'abcdefghijklmnopqrstuvwxyz'";
    private const string LongName =
        "concat(/Invoice/Seller/Name, ' ', /Invoice/Buyer/Name, ' ', /Invoice/Seller/Address/Street, ' ', /Invoice/Seller/Address/City)";

    private static readonly string[] Values =
    [
        "/Invoice/ID",
        "/Invoice/DocumentCurrencyCode",
        "/Invoice/Seller/VatId",
        "/Invoice/BuyerReference",
        LongName,
    ];

    private static readonly string[] Translates = [.. Values.Select(v => $"translate({v}, {Upper}, {Lower})")];
    private static readonly string[] UpperCases = [.. Values.Select(v => $"upper-case({v})")];
    private static readonly string[] LowerCases = [.. Values.Select(v => $"lower-case({v})")];

    private readonly XPathExpression[] _translates = [.. Translates.Select(e => XPathExpression.Compile(e))];
    private readonly XPathExpression[] _upperCases = [.. UpperCases.Select(e => XPathExpression.Compile(e))];
    private readonly XPathExpression[] _lowerCases = [.. LowerCases.Select(e => XPathExpression.Compile(e))];
    private readonly System.Xml.XPath.XPathExpression[] _systemXml =
    [
        .. Translates.Select(e => System.Xml.XPath.XPathExpression.Compile(e)),
    ];
    private readonly System.Xml.XPath.XPathNavigator _navigator = XPathBenchmarkDocument.CreateNavigator();
    private readonly XPathEvaluationContext _context = new();

    /// <summary>Proves that <c>translate</c> agrees with <c>System.Xml.XPath</c> on every value.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _context.SetContextItem(XdmItem.FromNode(XPathBenchmarkDocument.Root(XPathBenchmarkDocument.LoadTree())));
        for (var i = 0; i < _translates.Length; i++)
        {
            var expected = (string)_navigator.Evaluate(_systemXml[i]);
            var actual = _translates[i].Evaluate(_context);
            if (
                actual.Count != 1
                || !actual[0].TryGetString(out var text)
                || !string.Equals(text, expected, StringComparison.Ordinal)
            )
            {
                throw new InvalidOperationException($"The engines disagree on {Translates[i]}.");
            }
        }
    }

    /// <summary>Baseline: <c>System.Xml.XPath</c> folds the case of each value with <c>translate</c>.</summary>
    /// <returns>The total length of the results.</returns>
    [Benchmark(Baseline = true)]
    public int TranslateSystemXml()
    {
        var length = 0;
        foreach (var expression in _systemXml)
        {
            length += ((string)_navigator.Evaluate(expression)).Length;
        }

        return length;
    }

    /// <summary>Folds the case of each value with <c>translate</c>.</summary>
    /// <returns>The number of result items.</returns>
    [Benchmark]
    public int Translate() => Run(_translates);

    /// <summary>Maps each value with <c>upper-case</c>.</summary>
    /// <returns>The number of result items.</returns>
    [Benchmark]
    public int UpperCase() => Run(_upperCases);

    /// <summary>Maps each value with <c>lower-case</c>.</summary>
    /// <returns>The number of result items.</returns>
    [Benchmark]
    public int LowerCase() => Run(_lowerCases);

    /// <summary>Releases the evaluation context.</summary>
    [GlobalCleanup]
    public void Cleanup() => _context.Dispose();

    private int Run(XPathExpression[] expressions)
    {
        var count = 0;
        foreach (var expression in expressions)
        {
            count += expression.Evaluate(_context).Count;
        }

        return count;
    }
}
