namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public class RegexXPathDialectTests
{
    private static RegexParseResult Parse(string pattern, string flags = "")
    {
        var flagError = RegexFlagParser.TryParse(flags, out var parsed, out _);
        if (flagError != RegexError.None)
        {
            throw new InvalidOperationException($"flags {flags}: {flagError}");
        }

        return RegexParser.Parse(pattern, new RegexSyntax(RegexDialect.XPath, NameEdition.Xml10FifthEdition, parsed));
    }

    private static RegexProgram Compile(string pattern, string flags = "")
    {
        _ = RegexFlagParser.TryParse(flags, out var parsed, out _);
        var result = Parse(pattern, flags);
        var tree = result.Tree
            ?? throw new InvalidOperationException($"{pattern}: {result.Error} at {result.Position}");
        var sets = new RegexCharacterSet[tree.SetCount];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = tree.GetSet(i);
        }

        var error = RegexProgram.TryCompile(tree.Nodes, tree.Root, sets, RegexDialect.XPath, parsed, out var program);
        return error == RegexError.None ? program! : throw new InvalidOperationException($"{pattern}: {error}");
    }

    private static (RegexMatchResult Result, int Index, int Length) Find(
        string pattern,
        string input,
        string flags = ""
    )
    {
        var result = Compile(pattern, flags).Search(input, RegexStepLimits.Default, out var index, out var length);
        return (result, index, length);
    }

    private static async Task AssertMatch(string pattern, string input, int index, int length, string flags = "")
    {
        var found = Find(pattern, input, flags);

        await Assert.That(found.Result).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(found.Index).IsEqualTo(index);
        await Assert.That(found.Length).IsEqualTo(length);
    }

    private static async Task AssertNoMatch(string pattern, string input, string flags = "") =>
        await Assert.That(Find(pattern, input, flags).Result).IsEqualTo(RegexMatchResult.NoMatch);

    [Test]
    [Arguments("", RegexFlagSet.None)]
    [Arguments("s", RegexFlagSet.DotAll)]
    [Arguments("m", RegexFlagSet.MultiLine)]
    [Arguments("i", RegexFlagSet.IgnoreCase)]
    [Arguments("x", RegexFlagSet.Extended)]
    [Arguments("q", RegexFlagSet.Literal)]
    [Arguments("sm", RegexFlagSet.DotAll | RegexFlagSet.MultiLine)]
    [Arguments("ix", RegexFlagSet.IgnoreCase | RegexFlagSet.Extended)]
    [Arguments("qi", RegexFlagSet.Literal | RegexFlagSet.IgnoreCase)]
    [Arguments(
        "smixq",
        RegexFlagSet.DotAll
            | RegexFlagSet.MultiLine
            | RegexFlagSet.IgnoreCase
            | RegexFlagSet.Extended
            | RegexFlagSet.Literal
    )]
    [Arguments("ssii", RegexFlagSet.DotAll | RegexFlagSet.IgnoreCase)]
    public async Task Flags_ValidLetters_Parse(string text, RegexFlagSet expected)
    {
        var error = RegexFlagParser.TryParse(text, out var flags, out var position);

        await Assert.That(error).IsEqualTo(RegexError.None);
        await Assert.That(flags).IsEqualTo(expected);
        await Assert.That(position).IsEqualTo(-1);
    }

    [Test]
    [Arguments("a", 0)]
    [Arguments("sa", 1)]
    [Arguments("S", 0)]
    [Arguments("s m", 1)]
    [Arguments("smiXq", 3)]
    public async Task Flags_InvalidLetter_IsACodedError(string text, int expectedPosition)
    {
        var error = RegexFlagParser.TryParse(text, out _, out var position);

        await Assert.That(error).IsEqualTo(RegexError.InvalidFlag);
        await Assert.That(position).IsEqualTo(expectedPosition);
    }

    [Test]
    public async Task Flags_InTheXsdDialect_AreNotAllowed()
    {
        var result = RegexParser.Parse(
            "a",
            new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition, RegexFlagSet.IgnoreCase)
        );

        await Assert.That(result.Error).IsEqualTo(RegexError.ConstructNotAllowed);
    }

    [Test]
    public async Task Parse_XPathDialect_BuildsTheSharedTree()
    {
        var tree = Parse("^a$").Tree!;

        await Assert.That(tree.Nodes[tree.Root].Kind).IsEqualTo(RegexNodeKind.Concat);
        await Assert.That(tree.Nodes[0].Kind).IsEqualTo(RegexNodeKind.Anchor);
        await Assert.That(tree.Nodes[0].Value).IsEqualTo((int)RegexAnchor.Start);
        await Assert.That(tree.Nodes[2].Value).IsEqualTo((int)RegexAnchor.End);
    }

    [Test]
    [Arguments("^abc$", "abc", true)]
    [Arguments("^abc$", "xabc", false)]
    [Arguments("^abc$", "abcx", false)]
    [Arguments("^abc$", "abc\n", false)]
    [Arguments("^abc", "xabc", false)]
    [Arguments("abc$", "xabc", true)]
    [Arguments("abc$", "abc\n", false)]
    [Arguments("^$", "", true)]
    [Arguments("^$", "a", false)]
    public async Task Anchors_MatchAtTheStringBounds(string pattern, string input, bool expected)
    {
        var found = Find(pattern, input).Result == RegexMatchResult.Match;

        await Assert.That(found).IsEqualTo(expected);
    }

    [Test]
    public async Task Anchors_WithoutTheMultiLineFlag_IgnoreLineBreaks()
    {
        await AssertNoMatch("^b", "a\nb");
        await AssertNoMatch("a$", "a\nb");
    }

    [Test]
    public async Task Anchors_WithTheMultiLineFlag_MatchAtLineBounds()
    {
        await AssertMatch("^b", "a\nb", 2, 1, "m");
        await AssertMatch("a$", "a\nb", 0, 1, "m");
        await AssertMatch("^$", "a\n\nb", 2, 0, "m");
        await AssertNoMatch("^b", "a\rb", "m");
    }

    [Test]
    public async Task Dot_MatchesALineFeedOnlyWithTheDotAllFlag()
    {
        await AssertNoMatch("^a.b$", "a\nb");
        await AssertNoMatch("^a.b$", "a\rb");
        await AssertMatch("^a.b$", "a\nb", 0, 3, "s");
        await AssertMatch("^a.b$", "a\rb", 0, 3, "s");
        await AssertMatch("^a.b$", "axb", 0, 3);
    }

    [Test]
    public async Task Dot_MatchesASurrogatePairAsOneCodePoint()
    {
        await AssertMatch("^.$", "\U0001F600", 0, 2);
        await AssertMatch("^.$", "\U0001F600", 0, 2, "s");
    }

    [Test]
    public async Task Search_FindsTheLeftmostMatch()
    {
        await AssertMatch("abc", "xxabcxx", 2, 3);
        await AssertMatch("", "abc", 0, 0);
        await AssertMatch("b*", "abc", 0, 0);
        await AssertMatch("b+", "abbc", 1, 2);
        await AssertNoMatch("abc", "ab");
        await AssertNoMatch("a", "");
    }

    [Test]
    public async Task Search_PrefersTheFirstAlternative()
    {
        await AssertMatch("a|ab", "ab", 0, 1);
        await AssertMatch("ab|a", "ab", 0, 2);
    }

    [Test]
    [Arguments("a+?", "aaa", 0, 1)]
    [Arguments("a+", "aaa", 0, 3)]
    [Arguments("a*?", "aaa", 0, 0)]
    [Arguments("a??", "aaa", 0, 0)]
    [Arguments("a{2,3}?", "aaaa", 0, 2)]
    [Arguments("a{2,3}", "aaaa", 0, 3)]
    [Arguments("a{2,}?", "aaaa", 0, 2)]
    [Arguments("<.+?>", "<a><b>", 0, 3)]
    [Arguments("<.+>", "<a><b>", 0, 6)]
    [Arguments("a*?b", "aaab", 0, 4)]
    [Arguments("(a|ab)+?c", "abac", 0, 4)]
    public async Task Reluctant_QuantifiersReturnTheShortestMatch(
        string pattern,
        string input,
        int index,
        int length
    ) => await AssertMatch(pattern, input, index, length);

    [Test]
    public async Task Reluctant_IsRejectedInTheXsdDialect()
    {
        var result = RegexParser.Parse("a+?", new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition));

        await Assert.That(result.Error).IsEqualTo(RegexError.ConstructNotAllowed);
    }

    [Test]
    [Arguments("(a)\\1", "aa", true)]
    [Arguments("(a)\\1", "ab", false)]
    [Arguments("(a+)b\\1", "aabaa", true)]
    [Arguments("(a+)b\\1", "aabab", true)]
    [Arguments("^(a+)b\\1$", "aabaa", true)]
    [Arguments("^(a+)b\\1$", "aaba", false)]
    [Arguments("(a)(b)\\2\\1", "abba", true)]
    [Arguments("(a)|b\\1", "b", true)]
    public async Task BackReference_MatchesTheCapturedText(string pattern, string input, bool expected) =>
        await Assert
            .That(Find(pattern, input).Result)
            .IsEqualTo(expected ? RegexMatchResult.Match : RegexMatchResult.NoMatch);

    [Test]
    public async Task BackReference_ExtentsFollowThePriorityOrder()
    {
        await AssertMatch("(a|b)*\\1", "abb", 0, 3);
        await AssertMatch("(a*)*\\1", "b", 0, 0);
        await AssertMatch("(a)\\10", "aa0", 0, 3);
    }

    [Test]
    [Arguments("(a\\1)", 2)]
    [Arguments("\\1", 0)]
    [Arguments("(a)\\2", 3)]
    [Arguments("\\1(a)", 0)]
    [Arguments("(?:a)\\1", 5)]
    [Arguments("((a)\\1)", 4)]
    public async Task BackReference_ToAnUnclosedOrMissingGroup_IsAStaticError(string pattern, int position)
    {
        var result = Parse(pattern);

        await Assert.That(result.Error).IsEqualTo(RegexError.InvalidBackReference);
        await Assert.That(result.Position).IsEqualTo(position);
    }

    [Test]
    public async Task BackReference_IsRejectedInTheXsdDialect()
    {
        var result = RegexParser.Parse("(a)\\1", new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition));

        await Assert.That(result.Error).IsEqualTo(RegexError.ConstructNotAllowed);
    }

    [Test]
    public async Task BackReference_OnAdversarialInput_EndsWithTheStepLimit()
    {
        var program = Compile("(a+)+b\\1");
        var input = new string('a', 40);

        var result = program.Match(input, RegexStepLimits.Default.WithMaxBacktrackSteps(50_000), out var steps);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(steps).IsEqualTo(50_000L);
    }

    [Test]
    public async Task BackReference_BelowTheLimit_IsNotAborted()
    {
        var program = Compile("(a+)+b\\1");

        var result = program.Match("aaabaaa", RegexStepLimits.Default.WithMaxBacktrackSteps(50_000), out _);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task Nfa_OnAdversarialInput_EndsWithTheStepLimitOfTheAutomaton()
    {
        var program = Compile("(a+)+b");
        var input = new string('a', 2_000);

        var result = program.Match(input, RegexStepLimits.Default.WithMaxNfaSteps(10_000), out var steps);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(steps).IsEqualTo(10_000L);
    }

    [Test]
    public async Task NonCapturingGroup_DoesNotNumberAndDoesNotCapture()
    {
        var tree = Parse("(?:ab)+").Tree!;
        var group = tree.Nodes[tree.Root].FirstChild;

        await Assert.That(tree.Nodes[group].Kind).IsEqualTo(RegexNodeKind.Group);
        await Assert.That(tree.Nodes[group].Value).IsEqualTo(RegexNode.NoGroup);
        await AssertMatch("(?:ab)+", "ababx", 0, 4);
        await AssertMatch("(?:a)(b)\\1", "abb", 0, 3);
    }

    [Test]
    public async Task CapturingGroups_AreNumberedByTheirOpeningParenthesis()
    {
        var tree = Parse("((a)b)").Tree!;
        var outer = tree.Nodes[tree.Root];
        var inner = tree.Nodes[tree.Nodes[outer.FirstChild].FirstChild];

        await Assert.That(outer.Value).IsEqualTo(1);
        await Assert.That(inner.Value).IsEqualTo(2);
    }

    [Test]
    [Arguments("(?=a)")]
    [Arguments("(?!a)")]
    [Arguments("(?<n>a)")]
    [Arguments("(?i)a")]
    public async Task GroupConstructsBeyondTheNonCapturingGroup_AreNotAllowed(string pattern)
    {
        var result = Parse(pattern);

        await Assert.That(result.Error).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(result.Position).IsEqualTo(1);
    }

    [Test]
    public async Task Escapes_TheDollarSignIsAnEscapeOnlyInTheXPathDialect()
    {
        await AssertMatch("\\$1", "a$1", 1, 2);
        await AssertMatch("[$^]+", "$^", 0, 2);

        var xsd = RegexParser.Parse("\\$", new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition));
        await Assert.That(xsd.Error).IsEqualTo(RegexError.InvalidEscape);
    }

    [Test]
    public async Task IgnoreCase_UsesTheEmbeddedSimpleFoldingTables()
    {
        await AssertMatch("abc", "xABCx", 1, 3, "i");
        await AssertMatch("[a-c]+", "xAbCx", 1, 3, "i");
        await AssertMatch("[^a]", "Ab", 1, 1, "i");
        await AssertMatch("k", "K", 0, 1, "i");
        await AssertMatch("K", "k", 0, 1, "i");
        await AssertMatch("ǅ", "Ǆǅǆ", 0, 1, "i");
        await AssertMatch("\\p{Lu}+", "abc", 0, 3, "i");
        await AssertMatch("[a-z-[aeiou]]+", "BCD", 0, 3, "i");
        await AssertMatch("σ+", "Σςσ", 0, 3, "i");
        await AssertNoMatch("abc", "ABC");
        await AssertNoMatch("[^a]", "A", "i");
    }

    [Test]
    public async Task IgnoreCase_DoesNotApplyFullFoldings()
    {
        await AssertNoMatch("^straße$", "STRASSE", "i");
        await AssertMatch("^straße$", "STRAẞe", 0, 6, "i");
    }

    [Test]
    public async Task IgnoreCase_AppliesToBackReferences()
    {
        await AssertMatch("(a)\\1", "aA", 0, 2, "i");
        await AssertMatch("(σ)\\1", "σΣ", 0, 2, "i");
        await AssertNoMatch("(a)\\1", "aA");
    }

    [Test]
    public async Task Extended_RemovesWhitespaceOutsideCharacterClasses()
    {
        await AssertMatch("a b\tc\n d", "abcd", 0, 4, "x");
        await AssertMatch("a +b", "aab", 0, 3, "x");
        await AssertMatch("a+ ?", "aa", 0, 1, "x");
        await AssertMatch("[ ]b", "a b", 1, 2, "x");
        await AssertMatch("( a | b )c", "bc", 0, 2, "x");
        await AssertNoMatch("a b", "ab");
        await AssertMatch("a b", "a b", 0, 3);
    }

    [Test]
    public async Task Literal_TreatsThePatternAsPlainText()
    {
        await AssertMatch("a.c*", "xa.c*x", 1, 4, "q");
        await AssertNoMatch("a.c*", "abccc", "q");
        await AssertMatch("^a$", "x^a$x", 1, 3, "q");
        await AssertMatch("(a)\\1", "(a)\\1", 0, 5, "q");
        await AssertMatch("A.C", "a.c", 0, 3, "qi");
        await AssertMatch("a b", "a b", 0, 3, "qx");
        await AssertMatch("", "abc", 0, 0, "q");
    }

    [Test]
    public async Task Literal_WithALoneSurrogate_IsAnInvalidUtf16Error()
    {
        var result = Parse("a\uD800", "q");

        await Assert.That(result.Error).IsEqualTo(RegexError.InvalidUtf16);
        await Assert.That(result.Position).IsEqualTo(1);
    }

    [Test]
    public async Task XPathProgram_ReportsItsDialect() =>
        await Assert.That(Compile("a").Dialect).IsEqualTo(RegexDialect.XPath);

    [Test]
    public async Task Compile_ABackReferenceBeyondTheGroups_IsAnError()
    {
        var nodes = new[] { RegexNode.BackReference(1) };

        var error = RegexProgram.TryCompile(nodes, 0, [], RegexDialect.XPath, RegexFlagSet.None, out var program);

        await Assert.That(error).IsEqualTo(RegexError.InvalidBackReference);
        await Assert.That(program).IsNull();
    }

    [Test]
    public async Task Compile_WithoutFlags_KeepsTheXsdOverloadWorking()
    {
        var nodes = new[] { RegexNode.Character('a') };

        var error = RegexProgram.TryCompile(nodes, 0, [], RegexDialect.Xsd, out var program);

        await Assert.That(error).IsEqualTo(RegexError.None);
        await Assert.That(program!.Match("a", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.Match);
    }
}
