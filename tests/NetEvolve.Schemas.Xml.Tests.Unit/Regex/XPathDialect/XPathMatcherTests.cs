namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Tests.Unit.Regex.Matching;

public class XPathMatcherTests
{
    private const RegexOptions I = RegexOptions.IgnoreCase;
    private const RegexOptions M = RegexOptions.MultiLine;
    private const RegexOptions S = RegexOptions.DotAll;
    private const RegexOptions X = RegexOptions.FreeSpacing;

    [Test]
    public async Task Program_OfTheXPathDialect_ReportsTheDialect() =>
        await Assert.That(XPathRegex.Compile("a").Dialect).IsEqualTo(RegexDialect.XPath);

    [Test]
    [Arguments("b", "abc", 1, 1)]
    [Arguments("abc", "abc", 0, 3)]
    [Arguments("c", "abc", 2, 1)]
    [Arguments("", "abc", 0, 0)]
    [Arguments("", "", 0, 0)]
    [Arguments("a*", "baaa", 0, 0)]
    [Arguments("a+", "baaa", 1, 3)]
    [Arguments("b+", "abbbab", 1, 3)]
    [Arguments("a|b", "xxb", 2, 1)]
    [Arguments("(ab)+", "xababab", 1, 6)]
    [Arguments("[0-9]+", "ab 123 cd", 3, 3)]
    [Arguments("\\d+", "ab 123 cd", 3, 3)]
    public async Task Search_FindsTheLeftmostMatchAnywhereInTheInput(
        string pattern,
        string input,
        int index,
        int length
    )
    {
        var (result, foundIndex, foundLength) = XPathRegex.Search(pattern, input);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(foundIndex).IsEqualTo(index);
        await Assert.That(foundLength).IsEqualTo(length);
    }

    [Test]
    [Arguments("x", "abc")]
    [Arguments("abcd", "abc")]
    [Arguments("a", "")]
    [Arguments("b{2}", "abab")]
    public async Task Search_WithoutAMatch_ReportsNoMatch(string pattern, string input)
    {
        var (result, index, length) = XPathRegex.Search(pattern, input);

        await Assert.That(result).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(index).IsEqualTo(-1);
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Match_OfTheXPathDialect_IsASearch()
    {
        var program = XPathRegex.Compile("b+");

        await Assert.That(program.Match("abbc", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("ac", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task Search_FirstAlternativeWinsOverALongerOne()
    {
        // Leftmost, then the preference order of the pattern (the first alternative), as the engines behind the XPath functions do.
        await Assert.That(XPathRegex.Found("a|ab", "ab")).IsEqualTo("a");
        await Assert.That(XPathRegex.Found("ab|a", "ab")).IsEqualTo("ab");
    }

    [Test]
    [Arguments("a+", "caaat", "aaa")]
    [Arguments("a+?", "caaat", "a")]
    [Arguments("a*", "aaa", "aaa")]
    [Arguments("a*?", "aaa", "")]
    [Arguments("a??", "a", "")]
    [Arguments("a?", "a", "a")]
    [Arguments("<.+>", "<a><b>", "<a><b>")]
    [Arguments("<.+?>", "<a><b>", "<a>")]
    [Arguments("a{2,3}", "aaaa", "aaa")]
    [Arguments("a{2,3}?", "aaaa", "aa")]
    [Arguments("a{2,}", "aaaa", "aaaa")]
    [Arguments("a{2,}?", "aaaa", "aa")]
    [Arguments("a{2}?", "aaaa", "aa")]
    [Arguments("a*?b", "aaab", "aaab")]
    [Arguments("(a|ab)*?c", "ababc", "ababc")]
    [Arguments("x(a+?)", "xaaa", "xa")]
    [Arguments("(?:ab)+?", "ababab", "ab")]
    [Arguments("a{0,2}?b", "aab", "aab")]
    public async Task Quantifiers_GreedyTakeTheLongestAndReluctantTheShortestMatch(
        string pattern,
        string input,
        string expected
    ) => await Assert.That(XPathRegex.Found(pattern, input)).IsEqualTo(expected);

    [Test]
    [Arguments("^a", "ab", 0)]
    [Arguments("^a", "ba", -1)]
    [Arguments("a$", "ba", 1)]
    [Arguments("a$", "ab", -1)]
    [Arguments("^a$", "a", 0)]
    [Arguments("^a$", "aa", -1)]
    [Arguments("^$", "", 0)]
    [Arguments("^", "abc", 0)]
    [Arguments("$", "abc", 3)]
    [Arguments("a\\n$", "a\n", 0)]
    [Arguments("a$", "a\n", -1)]
    [Arguments("^b", "a\nb", -1)]
    [Arguments("a$", "a\nb", -1)]
    [Arguments("\\^a", "^a", 0)]
    [Arguments("a\\$", "a$", 0)]
    [Arguments("[$^]+", "x$^", 1)]
    [Arguments("(^a|b)c", "bc", 0)]
    [Arguments("a^b", "a^b", -1)]
    [Arguments("a$b", "ab", -1)]
    public async Task Anchors_MatchAtTheBoundsOfTheStringOnly(string pattern, string input, int index)
    {
        var (result, foundIndex, _) = XPathRegex.Search(pattern, input);

        await Assert.That(result).IsEqualTo(index < 0 ? RegexMatchResult.NoMatch : RegexMatchResult.Match);
        await Assert.That(foundIndex).IsEqualTo(index);
    }

    [Test]
    [Arguments("^b", "a\nb", 2, 1)]
    [Arguments("a$", "a\nb", 0, 1)]
    [Arguments("^$", "a\n\nb", 2, 0)]
    [Arguments("^", "a\nb", 0, 0)]
    [Arguments("$", "ab\nc", 2, 0)]
    [Arguments("^b$", "a\nb\nc", 2, 1)]
    [Arguments("^b$", "a\nb", 2, 1)]
    [Arguments("a\r$", "a\r\nb", 0, 2)]
    [Arguments("^b", "a\r\nb", 3, 1)]
    [Arguments("^c", "a\rc", -1, 0)]
    public async Task Anchors_WithTheMultiLineFlag_MatchAtLineBounds(
        string pattern,
        string input,
        int index,
        int length
    )
    {
        var (result, foundIndex, foundLength) = XPathRegex.Search(pattern, input, M);

        await Assert.That(result).IsEqualTo(index < 0 ? RegexMatchResult.NoMatch : RegexMatchResult.Match);
        await Assert.That(foundIndex).IsEqualTo(index);
        await Assert.That(foundLength).IsEqualTo(length);
    }

    [Test]
    public async Task Anchors_WithTheMultiLineFlag_IgnoreAFinalNewlineAsALineStartAndEnd()
    {
        // The text of the flag m: no new line starts after a final newline, and the end of the string is no line end after it.
        await Assert.That(XPathRegex.IsMatch("a\\n$", "a\n")).IsTrue();
        await Assert.That(XPathRegex.IsMatch("a\\n$", "a\n", M)).IsFalse();
        await Assert.That(XPathRegex.IsMatch("a\\n^", "a\n", M)).IsFalse();
        await Assert.That(XPathRegex.IsMatch("a\\n^", "a\n")).IsFalse();
        await Assert.That(XPathRegex.IsMatch("^$", "", M)).IsTrue();
        await Assert.That(XPathRegex.IsMatch("a$", "a\n", M)).IsTrue();
    }

    [Test]
    [Arguments("a.b", "a\nb", RegexOptions.None, false)]
    [Arguments("a.b", "a\nb", RegexOptions.DotAll, true)]
    [Arguments("a.b", "a\rb", RegexOptions.None, false)]
    [Arguments("a.b", "a\rb", RegexOptions.DotAll, true)]
    [Arguments("a.b", "axb", RegexOptions.None, true)]
    [Arguments("a.b", "axb", RegexOptions.DotAll, true)]
    [Arguments("hello.*world", "hello\nworld", RegexOptions.None, false)]
    [Arguments("hello.*world", "hello\nworld", RegexOptions.DotAll, true)]
    [Arguments("a[^x]b", "a\nb", RegexOptions.None, true)]
    public async Task Dot_MatchesALineBreakOnlyWithTheDotAllFlag(
        string pattern,
        string input,
        RegexOptions flags,
        bool expected
    ) => await Assert.That(XPathRegex.IsMatch(pattern, input, flags)).IsEqualTo(expected);

    [Test]
    public async Task Dot_MatchesASupplementaryCharacterAsOne()
    {
        await Assert.That(XPathRegex.IsMatch("^.$", "\U0001F600")).IsTrue();
        await Assert.That(XPathRegex.IsMatch("^.$", "\uD83D")).IsTrue();
        await Assert.That(XPathRegex.IsMatch("^..$", "\U0001F600")).IsFalse();
        await Assert.That(XPathRegex.Found("a.", "xa\U0001F600")).IsEqualTo("a\U0001F600");
    }

    [Test]
    [Arguments("a b", "ab", RegexOptions.None, false)]
    [Arguments("a b", "a b", RegexOptions.None, true)]
    [Arguments("a b", "ab", RegexOptions.FreeSpacing, true)]
    [Arguments("a b", "a b", RegexOptions.FreeSpacing, false)]
    [Arguments("hello[ ]world", "hello world", RegexOptions.FreeSpacing, true)]
    [Arguments("hello[ ]world", "helloworld", RegexOptions.FreeSpacing, false)]
    [Arguments("hello\\ sworld", "hello world", RegexOptions.FreeSpacing, true)]
    [Arguments("hello world", "hello world", RegexOptions.FreeSpacing, false)]
    [Arguments("\\ \\ s", "ab", RegexOptions.FreeSpacing, false)]
    public async Task FreeSpacing_RemovesWhitespaceOutsideClassesBeforeMatching(
        string pattern,
        string input,
        RegexOptions flags,
        bool expected
    ) => await Assert.That(XPathRegex.IsMatch(pattern, input, flags)).IsEqualTo(expected);

    [Test]
    [Arguments("abc", "xABCx", "ABC")]
    [Arguments("ABC", "xabcx", "abc")]
    [Arguments("a+", "AaAb", "AaA")]
    [Arguments("[a-c]+", "xABCabcx", "ABCabc")]
    [Arguments("[A-Z]", "k", "k")]
    [Arguments("[A-Z]", "K", "K")]
    [Arguments("k", "K", "K")]
    [Arguments("s", "ſ", "ſ")]
    [Arguments("é", "É", "É")]
    [Arguments("σ", "Σ", "Σ")]
    [Arguments("σ", "ς", "ς")]
    [Arguments("\U00010400", "\U00010428", "\U00010428")]
    [Arguments("[\U00010400]", "\U00010428", "\U00010428")]
    [Arguments("ǅ", "ǆ", "ǆ")]
    [Arguments("[^Q]", "q", null)]
    [Arguments("[^Q]", "Q", null)]
    [Arguments("[^Q]", "x", "x")]
    [Arguments("[A-Z-[IO]]", "i", null)]
    [Arguments("[A-Z-[IO]]", "O", null)]
    [Arguments("[A-Z-[IO]]", "b", "b")]
    [Arguments("\\p{Lu}", "a", null)]
    [Arguments("\\p{Lu}", "A", "A")]
    [Arguments("[\\p{Lu}x]", "X", "X")]
    [Arguments("[\\p{Lu}x]", "a", null)]
    [Arguments("\\w+", "AbC", "AbC")]
    [Arguments("straße", "STRASSE", null)]
    public async Task IgnoreCase_MatchesByTheSimpleCaseFolding(string pattern, string input, string? expected) =>
        await Assert.That(XPathRegex.Found(pattern, input, I)).IsEqualTo(expected);

    [Test]
    [Arguments("abc", "ABC")]
    [Arguments("[a-z]", "K")]
    [Arguments("k", "K")]
    public async Task IgnoreCase_IsOffWithoutTheFlag(string pattern, string input) =>
        await Assert.That(XPathRegex.IsMatch(pattern, input)).IsFalse();

    [Test]
    [Arguments("([md])[aeiou]\\1", "Mum")]
    [Arguments("([md])[aeiou]\\1", "Dad")]
    [Arguments("([md])[aeiou]\\1", "DUD")]
    [Arguments("(k)\\1", "kK")]
    [Arguments("(\U00010400)\\1", "\U00010400\U00010428")]
    public async Task BackReference_WithIgnoreCase_ComparesCaseBlind(string pattern, string input)
    {
        await Assert.That(XPathRegex.IsMatch(pattern, input, I)).IsTrue();
        await Assert.That(XPathRegex.IsMatch(pattern, input)).IsFalse();
    }

    [Test]
    [Arguments("(ab)\\1", "abA")]
    [Arguments("(ab)\\1", "abAC")]
    [Arguments("(a)\\1", "ab")]
    [Arguments("(\U00010400)\\1", "\U00010400")]
    public async Task BackReference_WithIgnoreCase_RejectsADifferentOrShortText(string pattern, string input) =>
        await Assert.That(XPathRegex.IsMatch(pattern, input, I)).IsFalse();

    [Test]
    public async Task BackReference_WithIgnoreCase_SameCaseStillMatches() =>
        await Assert.That(XPathRegex.IsMatch("([md])[aeiou]\\1", "mom", I)).IsTrue();

    [Test]
    [Arguments("(a)\\1", "aa", "aa")]
    [Arguments("(a)\\1", "ab", null)]
    [Arguments("(a)\\1", "xaay", "aa")]
    [Arguments("(['\"]).*\\1", "say \"hi\" ok", "\"hi\"")]
    [Arguments("(['\"]).*?\\1", "'a' 'b'", "'a'")]
    [Arguments("(a*)b\\1", "aabaa", "aabaa")]
    [Arguments("(a*)b\\1", "aaba", "aba")]
    [Arguments("(a|b)*\\1", "abb", "abb")]
    [Arguments("(a+?)\\1", "aaaa", "aa")]
    [Arguments("(a)?\\1", "b", "")]
    [Arguments("(a)|b\\1", "b", "b")]
    [Arguments("(x?)*\\1y", "y", "y")]
    [Arguments("(a)(b)\\2\\1", "abba", "abba")]
    [Arguments("(a(b))\\2\\1", "abbab", "abbab")]
    [Arguments("(?:(a)|b)\\1", "ba", "b")]
    [Arguments("(.)\\1", "abccd", "cc")]
    [Arguments("(a)\\1{2}", "aaa", "aaa")]
    [Arguments("^(.+)\\1$", "abcabc", "abcabc")]
    [Arguments("^(.+)\\1$", "abcab", null)]
    [Arguments("(\U0001F600)\\1", "\U0001F600\U0001F600", "\U0001F600\U0001F600")]
    public async Task BackReference_MatchesTheCapturedSubstring(string pattern, string input, string? expected) =>
        await Assert.That(XPathRegex.Found(pattern, input)).IsEqualTo(expected);

    [Test]
    public async Task BackReference_InsideRepeats_UsesTheLastIteration()
    {
        await Assert.That(XPathRegex.Found("(?:(a)|(b))+\\2", "abb")).IsEqualTo("abb");
        await Assert.That(XPathRegex.Found("^(?:(a|b))+\\1$", "abb")).IsEqualTo("abb");
        await Assert.That(XPathRegex.Found("^(?:(a|b))+\\1$", "aba")).IsNull();
    }

    [Test]
    public async Task BackReference_PatternOnAdversarialInput_EndsWithTheStepLimitError()
    {
        var input = new string('a', 40);

        var (result, _, _) = XPathRegex.Search("(a|aa)+\\1b", input);
        var program = XPathRegex.Compile("(a|aa)+\\1b");
        var tight = RegexStepLimits.Default.WithMaxBacktrackSteps(5_000);
        var aborted = program.Match(input, tight, out var steps);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(aborted).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(steps).IsLessThanOrEqualTo(5_000L);
    }

    [Test]
    public async Task BackReference_BelowTheBudget_AnswersExactly()
    {
        var program = XPathRegex.Compile("(a|aa)+\\1b");

        await Assert.That(program.Match("aaaab", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("aaaa", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task BackReference_UsesTheBacktrackCeiling_AndOtherPatternsTheAutomatonCeiling()
    {
        var input = new string('a', 40);
        var nfaOnly = RegexStepLimits.Default.WithMaxBacktrackSteps(1);

        // The same shape without a back reference runs on the automaton and is not exponential.
        var plain = XPathRegex.Compile("(a|aa)+b").Match(input, nfaOnly);
        var withReference = XPathRegex.Compile("(a|aa)+\\1b").Match(input, nfaOnly);

        await Assert.That(plain).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(withReference).IsEqualTo(RegexMatchResult.Aborted);
    }

    // Pinned divergence: a loop whose body can match the empty string. The automaton drops an empty iteration, the backtracking
    // machine (forced here by a back reference to an empty group) stops the loop at it; both results are valid matches.
    [Test]
    public async Task EmptyBodyLoop_AutomatonAndBacktrackerPickDifferentValidMatches()
    {
        var (automaton, automatonIndex, automatonLength) = XPathRegex.Search("(?:b?|c)*", "bcc");
        var (backtracker, backtrackerIndex, backtrackerLength) = XPathRegex.Search("(?:b?|c)*()\\1", "bcc");

        await Assert.That(automaton).IsEqualTo(RegexMatchResult.Match);
        await Assert.That((automatonIndex, automatonLength)).IsEqualTo((0, 3));
        await Assert.That(backtracker).IsEqualTo(RegexMatchResult.Match);
        await Assert.That((backtrackerIndex, backtrackerLength)).IsEqualTo((0, 1));
    }

    [Test]
    public async Task Automaton_PatternOnAdversarialInput_EndsWithTheStepLimitError()
    {
        var program = XPathRegex.Compile("(a*)*b");
        var input = new string('a', 10_000);
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(5_000);

        var result = program.Match(input, limits, out var steps);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(steps).IsLessThanOrEqualTo(5_000L);
        await Assert
            .That(program.Match(new string('a', 200), RegexStepLimits.Default))
            .IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task Automaton_StepCountIsDeterministic_AndTheBudgetIsInclusive()
    {
        var program = XPathRegex.Compile("a+b");

        _ = program.Match("xaab", RegexStepLimits.Default, out var exact);
        var atBudget = program.Match("xaab", RegexStepLimits.Default.WithMaxNfaSteps(exact), out _);
        var below = program.Match("xaab", RegexStepLimits.Default.WithMaxNfaSteps(exact - 1), out _);

        await Assert.That(exact).IsGreaterThan(0L);
        await Assert.That(atBudget).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(below).IsEqualTo(RegexMatchResult.Aborted);
    }

    [Test]
    public async Task Automaton_AbortsWhileClosingTheInitialThread()
    {
        var program = XPathRegex.Compile("(?:a|b|c|d)");

        var result = program.Match("zzz", RegexStepLimits.Default.WithMaxNfaSteps(2), out var steps);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(steps).IsLessThanOrEqualTo(2L);
    }

    [Test]
    public async Task Automaton_MatchOnACompiledProgram_AllocatesNothing()
    {
        var small = XPathRegex.Compile("^a+?[0-9]|b$");
        var flagged = XPathRegex.Compile("a.b\\s", S | M | I);
        var large = XPathRegex.Compile("(?:ab){70}c");
        var largeInput = string.Concat(new string('x', 5), new string('a', 3), "ab", new string('c', 3));
        var largeMatch = string.Concat(Repeated("ab", 70)) + "c";

        AllocationAssert.NoAllocation(() =>
        {
            _ = small.Match("aaa1", RegexStepLimits.Default);
            _ = small.Match("xxxb", RegexStepLimits.Default);
            _ = flagged.Match("A\nB ", RegexStepLimits.Default);
            _ = large.Match(largeInput, RegexStepLimits.Default);
            _ = large.Match(largeMatch, RegexStepLimits.Default);
        });

        await Assert.That(large.Match(largeMatch, RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(large.Match(largeInput, RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task Program_LargerThanTheStackBuffer_SearchesOnPooledMemory()
    {
        var pattern = "(?:ab){70}c";
        var input = "zz" + string.Concat(Repeated("ab", 70)) + "c";

        var expected = input.Substring(2);

        await Assert.That(XPathRegex.Found(pattern, input)).IsEqualTo(expected);
        await Assert.That(XPathRegex.Found("(?:ab){70}(x)\\1", input + "x")).IsNull();
        await Assert.That(XPathRegex.Found("(?:ab){70}(c)\\1", input + "c")).IsEqualTo(expected + "c");
    }

    [Test]
    public async Task Backtracking_GrowsItsStackAsNeeded()
    {
        var input = string.Concat(Repeated("ab", 1_000));

        // Anchored so that one attempt does all the work: about 5,000 instructions and a deep stack.
        var (result, _, _) = XPathRegex.Search("^(?:a|b)*(x)\\1", input);

        await Assert.That(result).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task EmptyIterations_OfALoop_DoNotSpinTheBacktracker()
    {
        await Assert.That(XPathRegex.Found("(a*)*\\1b", "aab")).IsEqualTo("aab");
        await Assert.That(XPathRegex.Found("(?:a?)*(x)\\1", "aax")).IsNull();
        await Assert.That(XPathRegex.Found("(^)*(a)\\2", "aa")).IsEqualTo("aa");
        await Assert.That(XPathRegex.Found("(a)(?:)*\\1", "aa")).IsEqualTo("aa");
    }

    [Test]
    public async Task CombinedFlags_WorkTogether()
    {
        await Assert.That(XPathRegex.Found("^ a . b $", "x\nA\nB\ny", S | M | I | X)).IsEqualTo("A\nB");
        await Assert.That(XPathRegex.Found("^ a $", "x\nA\ny", M | I | X)).IsEqualTo("A");
        await Assert.That(XPathRegex.Found("a . b", "A\nB", S | I | X)).IsEqualTo("A\nB");
        await Assert.That(XPathRegex.Found("a . b", "A\nB", I | X)).IsNull();
        await Assert.That(XPathRegex.Found("^ a", "x\nA", I | X)).IsNull();
        await Assert.That(XPathRegex.Found("([a-c]) \\1", "xaAy", I | X)).IsEqualTo("aA");
    }

    [Test]
    public async Task CasePatterns_GiveTheSameResultOnEveryFramework()
    {
        // Pinned results of the embedded folding tables, executed on all four test legs.
        string[] inputs = ["K", "k", "K", "s", "S", "ſ", "σ", "ς", "Σ", "ß", "ẞ", "İ", "ı", "i", "I"];
        var folded = string.Empty;
        foreach (var input in inputs)
        {
            var pattern = "[kσßiI]";
            folded += XPathRegex.IsMatch(pattern, input, I) ? '1' : '0';
        }

        await Assert.That(folded).IsEqualTo("111000111110011");
    }

    [Test]
    public async Task Find_StartsAtTheGivenIndex_WithAnchorsRelativeToTheWholeInput()
    {
        var program = XPathRegex.Compile("a");
        var anchored = XPathRegex.Compile("^a");

        var second = program.Find("aXa", 1, RegexStepLimits.Default, out var index, out var length);
        var none = program.Find("aXa", 3, RegexStepLimits.Default, out var noneIndex, out _);
        var start = anchored.Find("aXa", 1, RegexStepLimits.Default, out _, out _);

        await Assert.That(second).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(index).IsEqualTo(2);
        await Assert.That(length).IsEqualTo(1);
        await Assert.That(none).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(noneIndex).IsEqualTo(-1);
        await Assert.That(start).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task Find_AtTheEnd_FindsAnEmptyMatch()
    {
        var result = XPathRegex.Compile("x*").Find("ab", 2, RegexStepLimits.Default, out var index, out var length);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(index).IsEqualTo(2);
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Find_OutsideTheInput_Throws()
    {
        var program = XPathRegex.Compile("a");

        await Assert
            .That(() => program.Find("ab", -1, RegexStepLimits.Default, out _, out _))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => program.Find("ab", 3, RegexStepLimits.Default, out _, out _))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Find_WithoutLimits_UsesTheDefaults()
    {
        var result = XPathRegex.Compile("a").Find("a", 0, null!, out var index, out _);
        var backtracking = XPathRegex.Compile("(a)\\1").Find("aa", 0, null!, out var backIndex, out _);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(index).IsEqualTo(0);
        await Assert.That(backtracking).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(backIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Find_OnAnXsdProgram_MatchesTheRestOfTheInputAsAWhole()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Text("bc"));
        var program = t.Compile();

        var whole = program.Find("abc", 1, RegexStepLimits.Default, out var index, out var length);
        var partial = program.Find("abcd", 1, RegexStepLimits.Default, out var noIndex, out var noLength);

        await Assert.That(whole).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(index).IsEqualTo(1);
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(partial).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(noIndex).IsEqualTo(-1);
        await Assert.That(noLength).IsEqualTo(0);
    }

    [Test]
    public async Task TryCompile_OfATree_NullThrows() =>
        await Assert.That(() => RegexProgram.TryCompile(null!, out _)).Throws<ArgumentNullException>();

    [Test]
    public async Task TryCompile_HandBuiltXPathTree_CompilesAnchorsLazyRepeatsAndBackReferences()
    {
        var t = new TreeBuilder();
        var first = t.Add(RegexNode.Group(t.Char('a'), 1));
        var lazy = t.Add(RegexNode.Repeat(t.Char('b'), 0, RegexNode.Unbounded, lazy: true));
        var reference = t.Add(RegexNode.BackReference(1));
        var end = t.Add(RegexNode.Anchor(RegexAnchor.End));
        t.WithRoot(t.Concat(first, lazy, reference, end));

        var error = t.TryCompile(RegexDialect.XPath);

        await Assert.That(error).IsEqualTo(RegexError.None);
        await Assert
            .That(t.Compile(RegexDialect.XPath).Match("abba", RegexStepLimits.Default))
            .IsEqualTo(RegexMatchResult.Match);
        await Assert
            .That(t.Compile(RegexDialect.XPath).Match("abbab", RegexStepLimits.Default))
            .IsEqualTo(RegexMatchResult.NoMatch);
        await Assert
            .That(t.Compile(RegexDialect.XPath).Match("xaa", RegexStepLimits.Default))
            .IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task TryCompile_BackReferenceToAMissingGroup_IsRejected()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Concat(t.Group(t.Char('a')), t.Add(RegexNode.BackReference(2))));

        await Assert.That(t.TryCompile(RegexDialect.XPath)).IsEqualTo(RegexError.InvalidBackReference);
    }

    [Test]
    public async Task TryCompile_XPathProgramBeyondTheProgramSize_IsRejected()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Char('a'), 1, 200_000));

        await Assert.That(t.TryCompile(RegexDialect.XPath)).IsEqualTo(RegexError.LimitExceeded);
    }

    [Test]
    public async Task TryCompile_XPathTreeBeyondTheNesting_IsRejected()
    {
        var t = new TreeBuilder();
        var node = t.Char('a');
        for (var i = 0; i < RegexProgram.MaxNesting + 1; i++)
        {
            node = t.Group(node);
        }

        t.WithRoot(node);

        await Assert.That(t.TryCompile(RegexDialect.XPath)).IsEqualTo(RegexError.LimitExceeded);
    }

    private static string[] Repeated(string value, int count)
    {
        var result = new string[count];
        Array.Fill(result, value);
        return result;
    }
}
