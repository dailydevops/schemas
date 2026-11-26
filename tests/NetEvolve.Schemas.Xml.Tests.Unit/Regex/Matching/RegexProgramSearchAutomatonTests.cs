namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Matching;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// Differential tests of the boolean match of the XPath dialect: it must give the answer of the leftmost-first simulation,
/// which the overload with the step report always runs.
/// </summary>
public class RegexProgramSearchAutomatonTests
{
    private static RegexTree Parse(string pattern, RegexOptions options)
    {
        var syntax = new RegexSyntax(RegexDialect.XPath, NameEdition.Xml10FifthEdition, options);
        return RegexParser.Parse(pattern.AsSpan(), syntax).Tree!;
    }

    private static RegexProgram Compile(string pattern, RegexOptions options = RegexOptions.None)
    {
        _ = RegexProgram.TryCompile(Parse(pattern, options), out var program);
        return program!;
    }

    private static RegexMatchResult Reference(RegexProgram program, string input, RegexStepLimits limits) =>
        program.Match(input.AsSpan(), limits, out _);

    private static readonly (string Pattern, RegexOptions Options, string[] Inputs)[] Cases =
    [
        ("", RegexOptions.None, ["", "a"]),
        ("abc", RegexOptions.None, ["abc", "xxabcxx", "ab", "", "abdabc", "ABC"]),
        ("abc", RegexOptions.IgnoreCase, ["ABC", "xAbCx", "ab", "ä"]),
        ("^abc$", RegexOptions.None, ["abc", "abcd", "xabc", "", "abc\n"]),
        ("^abc", RegexOptions.None, ["abc", "abcd", "xabc", "", "\nabc"]),
        ("abc$", RegexOptions.None, ["abc", "xabc", "abcd", "abc\n"]),
        ("^$", RegexOptions.None, ["", "a", "\n"]),
        ("$^", RegexOptions.None, ["", "a"]),
        ("^^a$$", RegexOptions.None, ["a", "aa", ""]),
        ("a$|^b", RegexOptions.None, ["a", "b", "ab", "ba", "ca", "bc", ""]),
        ("(^a|b$)c", RegexOptions.None, ["ac", "bc", "bbc", "cac", "ab", ""]),
        ("(a|b$)+c?$", RegexOptions.None, ["a", "ab", "ba", "b", "bb", "cb", ""]),
        ("a*$", RegexOptions.None, ["", "b", "ba", "baa", "aab"]),
        ("a.c", RegexOptions.None, ["abc", "a\nc", "a\rc", "ac", "aäc"]),
        ("a.c", RegexOptions.DotAll, ["abc", "a\nc", "a\rc", "ac"]),
        ("a b c", RegexOptions.FreeSpacing, ["abc", "a b c", "xabcx"]),
        ("[a-c]+\\d{2}", RegexOptions.None, ["abc12", "zzb99zz", "abc1", "d12", ""]),
        ("a{2,5}b?", RegexOptions.None, ["aa", "a", "aaaaaa", "baab", ""]),
        ("a{0}", RegexOptions.None, ["", "a"]),
        ("(ab){2,}|c", RegexOptions.None, ["abab", "ab", "c", "xxcxx", "ababab", ""]),
        ("a+?b", RegexOptions.None, ["aab", "ab", "b", "aaa"]),
        ("(a*)*b", RegexOptions.None, ["b", "aaab", "aaa", ""]),
        ("(a|b)*a(a|b){6}", RegexOptions.None, ["abababababab", "aaaaaaa", "bbbbbbb", "ababaabbbbab", ""]),
        ("\\p{Lu}\\p{Ll}+", RegexOptions.None, ["Hello", "hello", "xHellox", "Ärger", "Ärgerä"]),
        ("\\p{IsBasicLatin}+x", RegexOptions.None, ["abcx", "äx", "x"]),
        ("\\P{L}+", RegexOptions.None, ["123", "abc", "a1a", ""]),
        ("[^a]+", RegexOptions.None, ["bcd", "a", "", "bä"]),
        ("\\i\\c*", RegexOptions.None, ["name", "1abc", "", "a b"]),
        ("ä+", RegexOptions.None, ["ää", "a", "", "xxä"]),
        ("[\U00010000-\U0010FFFF]+", RegexOptions.None, ["\U00010000", "a\U0001F600b", "a", "\ud800", ""]),
        ("^\\p{L}*$", RegexOptions.None, ["abc", "\U00010400", "abc\ud801", "𐐀"]),
        ("^(INV|CRN|DBN)-[0-9]{4,8}$", RegexOptions.None, ["INV-20240131", "INV-123", "XINV-20240131", ""]),
        ("^([A-Z]{2})([0-9]{2})(DE)?[0-9A-Z]{10,16}$", RegexOptions.None, ["DE89DE3704004405320", "DE89"]),
        ("^abc$", RegexOptions.MultiLine, ["abc", "x\nabc\ny", "abc\n", "\nabc", ""]),
        ("a$", RegexOptions.MultiLine, ["a\nb", "ba", "a\n", "b\na"]),
        ("(a)\\1", RegexOptions.None, ["aa", "a", "xaax", ""]),
        ("(a|b)\\1$", RegexOptions.None, ["aa", "ab", "bb", "xbb"]),
    ];

    public static IEnumerable<(string Pattern, RegexOptions Options, string Input)> Corpus() =>
        Cases.SelectMany(c => c.Inputs.Select(input => (c.Pattern, c.Options, input)));

    [Test]
    [MethodDataSource(nameof(Corpus))]
    public async Task Match_GivesTheResultOfTheSimulation(string pattern, RegexOptions options, string input)
    {
        var program = Compile(pattern, options);
        var limits = RegexStepLimits.Default;

        await Assert.That(program.Match(input.AsSpan(), limits)).IsEqualTo(Reference(program, input, limits));
    }

    [Test]
    [MethodDataSource(nameof(Corpus))]
    public async Task IsMatch_OfTheOperations_GivesTheResultOfTheEnumeration(
        string pattern,
        RegexOptions options,
        string input
    )
    {
        _ = RegexOperations.TryCreate(Parse(pattern, options), out var operations);
        var expected = RegexMatchResult.NoMatch;
        var matches = operations!.EnumerateMatches(input.AsSpan());
        try
        {
            if (matches.MoveNext())
            {
                expected = RegexMatchResult.Match;
            }
        }
        finally
        {
            matches.Dispose();
        }

        await Assert.That(operations.IsMatch(input.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("^(INV|CRN)-[0-9]{4}$", RegexOptions.None)]
    [Arguments("abc", RegexOptions.IgnoreCase)]
    [Arguments("a$|^b", RegexOptions.DotAll)]
    [Arguments("(a)(b)", RegexOptions.None)]
    public async Task Compile_WithoutBackReferenceAndMultiLine_RunsDeterministically(
        string pattern,
        RegexOptions options
    ) => await Assert.That(Compile(pattern, options).IsDeterministic).IsTrue();

    [Test]
    [Arguments("(a)\\1", RegexOptions.None)]
    [Arguments("^a$", RegexOptions.MultiLine)]
    [Arguments("a$", RegexOptions.MultiLine)]
    public async Task Compile_WithBackReferenceOrMultiLineAnchor_KeepsTheSimulation(
        string pattern,
        RegexOptions options
    ) => await Assert.That(Compile(pattern, options).IsDeterministic).IsFalse();

    [Test]
    public async Task Compile_ProgramWithCaptureInstructions_KeepsTheSimulation()
    {
        _ = RegexOperations.TryCreate(Parse("(a)(b)", RegexOptions.None), out var operations);

        await Assert.That(operations!.Program.IsDeterministic).IsFalse();
    }

    [Test]
    public async Task Match_RepeatedCalls_GiveStableResults()
    {
        var program = Compile("^[a-c]+\\d{2}$");
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
        var program = Compile("(a|b)*a(a|b){20}$");
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
        var program = Compile("(a|b)*c$");
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(maxSteps);
        var input = new string('a', 50) + "c";

        await Assert.That(program.Match(input.AsSpan(), limits)).IsEqualTo(Reference(program, input, limits));
    }

    [Test]
    public async Task Match_FromManyThreads_GivesExactResults()
    {
        var program = Compile("(a|b)*a(a|b){9}$");
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

    [Test]
    public async Task Match_RandomInputs_GiveTheResultOfTheSimulation()
    {
        string[] patterns = ["^a(b|c)*d?$", "b+c|^a", "(a|b)*c$", "a?b?c?", "^(ab|a)(bc|c)?$"];
        var random = new Random(5);
        var limits = RegexStepLimits.Default;
        foreach (var pattern in patterns)
        {
            var program = Compile(pattern);
            for (var round = 0; round < 200; round++)
            {
                var chars = new char[random.Next(8)];
                for (var i = 0; i < chars.Length; i++)
                {
                    chars[i] = "abcd"[random.Next(4)];
                }

                var input = new string(chars);
                await Assert.That(program.Match(input.AsSpan(), limits)).IsEqualTo(Reference(program, input, limits));
            }
        }
    }
}
