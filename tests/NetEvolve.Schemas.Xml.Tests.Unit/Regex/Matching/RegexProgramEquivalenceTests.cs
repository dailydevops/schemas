namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Matching;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// Differential tests: the match without a step report must give the answer of the automaton simulation, which the overload
/// with the step report always runs.
/// </summary>
public class RegexProgramEquivalenceTests
{
    private static readonly RegexSyntax Syntax = new(RegexDialect.Xsd, NameEdition.Xml10FifthEdition);

    private static RegexProgram Compile(string pattern)
    {
        var parsed = RegexParser.Parse(pattern.AsSpan(), Syntax);
        _ = RegexProgram.TryCompile(parsed.Tree!, out var program);
        return program!;
    }

    private static RegexMatchResult Reference(RegexProgram program, string input, RegexStepLimits limits) =>
        program.Match(input.AsSpan(), limits, out _);

    private static readonly (string Pattern, string[] Inputs)[] Cases =
    [
        ("", ["", "a"]),
        ("abc", ["abc", "ab", "abcd", "", "xabc"]),
        ("[A-Za-z0-9]+", ["abc123", "", "ab-c", "ABC", "\u00e4"]),
        ("\\d{3}-\\d{4}", ["123-4567", "123-456", "123-45678", "12a-4567"]),
        ("\\d{1,3}(\\.\\d{1,3}){3}", ["192.168.0.1", "1.2.3", "1.2.3.4.5", "999.999.999.999"]),
        ("a|ab|abc", ["a", "ab", "abc", "abcd", ""]),
        ("(foo|bar|baz)+", ["foo", "foobarbaz", "fooba", "", "bazbaz"]),
        ("a{2,5}b?", ["aa", "aaaaa", "aaaaaa", "a", "aab", "aaaaab"]),
        ("a*", ["", "a", "aaaa", "b"]),
        ("(a*)*b", ["b", "aaab", "aaa", ""]),
        ("[a-z-[aeiou]]+", ["bcd", "bad", "xyz", ""]),
        ("\\i\\c*", ["name", "_x-1.2", "1abc", "", "a b"]),
        ("\\w+@\\w+\\.\\w+", ["user@example.com", "user@example", "@a.b", "a@b.c.d"]),
        ("\\p{Lu}\\p{Ll}+", ["Hello", "hello", "HELLO", "H", "\u00c4rger", "\u00c4rger\u00e4"]),
        ("[^a]+", ["bcd", "a", "", "b\u00e4"]),
        (".+", ["abc", "", "a\nb", "a\rb", "\u00e4\u00f6"]),
        ("\\P{L}+", ["123", "abc", "1a", ""]),
        ("\u00e4+", ["\u00e4\u00e4", "\u00e4", "a", ""]),
        ("[\U00010000-\U0010FFFF]+", ["\U00010000", "\U0001F600\U0001F601", "a", "\ud800", ""]),
        ("\\p{L}*", ["abc", "\U00010400", "abc\ud801", "\ud801\udc00"]),
        ("(a|b)*a(a|b){6}", ["abababababab", "aaaaaaa", "bbbbbbb", "ababaabbbbab", "aaaaaaaaaaaaaaaaaaaa", ""]),
        ("(a|b)*a(a|b){12}", ["abababababababab", "bbbbbbbbbbbbb", "abbbbbbbbbbbb", "baaaaaaaaaaaa"]),
        ("a{0,1000}", ["", "a", new string('a', 1000), new string('a', 1001)]),
        ("(ab){2,}|c", ["abab", "ab", "c", "cc", "ababab"]),
    ];

    public static IEnumerable<(string Pattern, string Input)> Corpus() =>
        Cases.SelectMany(c => c.Inputs.Select(input => (c.Pattern, input)));

    [Test]
    [MethodDataSource(nameof(Corpus))]
    public async Task Match_GivesTheResultOfTheSimulation(string pattern, string input)
    {
        var program = Compile(pattern);
        var limits = RegexStepLimits.Default;

        await Assert.That(program.Match(input.AsSpan(), limits)).IsEqualTo(Reference(program, input, limits));
    }

    [Test]
    public async Task Match_RepeatedCalls_GiveStableResults()
    {
        var program = Compile("[a-c]+\\d{2}");
        var limits = RegexStepLimits.Default;

        for (var i = 0; i < 3; i++)
        {
            await Assert.That(program.Match("abc12".AsSpan(), limits)).IsEqualTo(RegexMatchResult.Match);
            await Assert.That(program.Match("abc1".AsSpan(), limits)).IsEqualTo(RegexMatchResult.NoMatch);
            await Assert.That(program.Match("abcd12".AsSpan(), limits)).IsEqualTo(RegexMatchResult.NoMatch);
        }
    }

    [Test]
    public async Task Match_StateExplosion_FallsBackToTheSimulation()
    {
        // The deterministic automaton of this pattern has 2^21 states; the cache is capped, the answers stay exact.
        var program = Compile("(a|b)*a(a|b){20}");
        var limits = RegexStepLimits.Default;
        var random = new Random(17);

        for (var round = 0; round < 40; round++)
        {
            var chars = new char[10 + random.Next(60)];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = random.Next(2) == 0 ? 'a' : 'b';
            }

            var input = new string(chars);
            await Assert.That(program.Match(input.AsSpan(), limits)).IsEqualTo(Reference(program, input, limits));
        }
    }

    [Test]
    [Arguments(1L)]
    [Arguments(5L)]
    [Arguments(40L)]
    [Arguments(100_000L)]
    public async Task Match_StepLimit_AbortsLikeTheSimulation(long maxSteps)
    {
        var program = Compile("(a|b)*c");
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(maxSteps);
        var input = new string('a', 50) + "c";

        await Assert.That(program.Match(input.AsSpan(), limits)).IsEqualTo(Reference(program, input, limits));
    }

    [Test]
    public async Task Match_FromManyThreads_GivesExactResults()
    {
        var program = Compile("(a|b)*a(a|b){9}");
        var limits = RegexStepLimits.Default;

        var tasks = Enumerable
            .Range(0, 8)
            .Select(seed =>
                Task.Run(() =>
                {
                    var random = new Random(seed);
                    var failures = 0;
                    for (var round = 0; round < 300; round++)
                    {
                        var chars = new char[5 + random.Next(30)];
                        for (var i = 0; i < chars.Length; i++)
                        {
                            chars[i] = random.Next(2) == 0 ? 'a' : 'b';
                        }

                        var input = new string(chars);
                        if (program.Match(input.AsSpan(), limits) != Reference(program, input, limits))
                        {
                            failures++;
                        }
                    }

                    return failures;
                })
            )
            .ToArray();

        var results = await Task.WhenAll(tasks);

        await Assert.That(results.Sum()).IsEqualTo(0);
    }
}
