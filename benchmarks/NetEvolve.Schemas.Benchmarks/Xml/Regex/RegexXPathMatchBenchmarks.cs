namespace NetEvolve.Schemas.Benchmarks.Xml.Regex;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The boolean match of <c>fn:matches</c> on anchored XPath dialect patterns with and without capture groups: the boolean entry
/// point of the operations against the enumeration that <c>fn:matches</c> used before it. <c>System.Xml</c> has no XPath
/// dialect matcher, so there is no baseline.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class RegexXPathMatchBenchmarks
#pragma warning restore CA1515
{
    private static readonly string[] Patterns =
    [
        "^(INV|CRN|DBN)-[0-9]{4,8}$",
        "^[A-Z]{2}[0-9]{2}[A-Z0-9]{4,30}$",
        "^(a|b)*c$",
        "^([A-Z]{2})([0-9]{2})(DE)?[0-9A-Z]{10,16}$",
    ];

    private static readonly string[] Inputs =
    [
        "INV-20240131",
        "DE89370400440532013000",
        "ababababababababababababc",
        "DE89DE3704004405320",
    ];

    private readonly RegexOperations[] _operations = Compile();

    /// <summary>Proves that both routes accept every input of the set.</summary>
    [GlobalSetup]
    public void Setup()
    {
        if (MatchBoolean() != Inputs.Length || MatchEnumerated() != Inputs.Length)
        {
            throw new InvalidOperationException("The routes disagree on the accepted inputs.");
        }
    }

    /// <summary>Asks each pattern for a boolean match.</summary>
    /// <returns>The number of matching inputs.</returns>
    [Benchmark]
    public int MatchBoolean()
    {
        var matched = 0;
        for (var i = 0; i < _operations.Length; i++)
        {
            if (_operations[i].IsMatch(Inputs[i].AsSpan()) == RegexMatchResult.Match)
            {
                matched++;
            }
        }

        return matched;
    }

    /// <summary>Takes the first match of the enumeration, with its capture groups, as <c>fn:matches</c> did before.</summary>
    /// <returns>The number of matching inputs.</returns>
    [Benchmark]
    public int MatchEnumerated()
    {
        var matched = 0;
        for (var i = 0; i < _operations.Length; i++)
        {
            var matches = _operations[i].EnumerateMatches(Inputs[i].AsSpan());
            try
            {
                if (matches.MoveNext())
                {
                    matched++;
                }
            }
            finally
            {
                matches.Dispose();
            }
        }

        return matched;
    }

    private static RegexOperations[] Compile()
    {
        var syntax = new RegexSyntax(RegexDialect.XPath, NameEdition.Xml10FifthEdition);
        var operations = new RegexOperations[Patterns.Length];
        for (var i = 0; i < operations.Length; i++)
        {
            var parsed = RegexParser.Parse(Patterns[i].AsSpan(), syntax);
            if (
                !parsed.Succeeded
                || RegexOperations.TryCreate(parsed.Tree!, out var created) != RegexError.None
                || created is null
            )
            {
                throw new InvalidOperationException($"The pattern '{Patterns[i]}' does not compile.");
            }

            operations[i] = created;
        }

        return operations;
    }
}
