namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Matching;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;

public class RegexProgramTests
{
    private static readonly RegexStepLimits Limits = RegexStepLimits.Default;

    private static RegexMatchResult Run(TreeBuilder tree, string input) => tree.Compile().Match(input, Limits);

    private static RegexMatchResult Expected(bool match) => match ? RegexMatchResult.Match : RegexMatchResult.NoMatch;

    private static TreeBuilder NestedStarThenB()
    {
        var t = new TreeBuilder();
        var inner = t.Repeat(t.Char('a'), 0, RegexNode.Unbounded);
        var outer = t.Repeat(t.Group(inner), 0, RegexNode.Unbounded);
        return t.WithRoot(t.Concat(outer, t.Char('b')));
    }

    [Test]
    public async Task Character_MatchesTheWholeInputOnly()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Text("abc"));
        var program = t.Compile();

        await Assert.That(program.Match("abc", Limits)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("abcd", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("xabc", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("ab", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Dialect).IsEqualTo(RegexDialect.Xsd);
    }

    [Test]
    public async Task EmptyTree_MatchesOnlyTheEmptyInput()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Add(RegexNode.Empty()));
        var none = new TreeBuilder();

        await Assert.That(Run(t, "")).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(Run(t, "a")).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(Run(none, "")).IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task Alternation_AndGroup_MatchEitherBranch()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Group(t.Alt(t.Text("ab"), t.Text("cd"))));
        var program = t.Compile();

        await Assert.That(program.Match("ab", Limits)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("cd", Limits)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("ad", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("abcd", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    [Arguments(0, 0, "", true)]
    [Arguments(0, 0, "a", false)]
    [Arguments(2, 2, "a", false)]
    [Arguments(2, 2, "aa", true)]
    [Arguments(2, 2, "aaa", false)]
    [Arguments(2, RegexNode.Unbounded, "a", false)]
    [Arguments(2, RegexNode.Unbounded, "aa", true)]
    [Arguments(2, RegexNode.Unbounded, "aaaaaaaaaa", true)]
    [Arguments(0, RegexNode.Unbounded, "", true)]
    [Arguments(1, 3, "", false)]
    [Arguments(1, 3, "a", true)]
    [Arguments(1, 3, "aaa", true)]
    [Arguments(1, 3, "aaaa", false)]
    public async Task Repeat_HonorsItsBounds(int min, int max, string input, bool expected)
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Char('a'), min, max));

        await Assert.That(Run(t, input)).IsEqualTo(Expected(expected));
    }

    [Test]
    [Arguments(1, 1000, 0, false)]
    [Arguments(1, 1000, 1, true)]
    [Arguments(1, 1000, 1000, true)]
    [Arguments(1, 1000, 1001, false)]
    [Arguments(500, 500, 499, false)]
    [Arguments(500, 500, 500, true)]
    [Arguments(500, 500, 501, false)]
    [Arguments(1000, RegexNode.Unbounded, 999, false)]
    [Arguments(1000, RegexNode.Unbounded, 1000, true)]
    [Arguments(1000, RegexNode.Unbounded, 5000, true)]
    public async Task Repeat_LargeCounts_MatchAtTheirBounds(int min, int max, int length, bool expected)
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Char('a'), min, max));

        await Assert.That(Run(t, new string('a', length))).IsEqualTo(Expected(expected));
    }

    [Test]
    public async Task Repeat_AboveTheProgramSizeCap_IsRejectedWithTheLimitError()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Char('a'), 1, 1_000_000));

        await Assert.That(t.TryCompile()).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That((int)RegexError.LimitExceeded).IsEqualTo(9);
        await Assert.That(RegexProgram.MaxProgramSize).IsEqualTo(100_000);
    }

    [Test]
    public async Task Repeat_NestedCounts_AreCappedByTheProgramSize()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Repeat(t.Char('a'), 1, 1000), 1, 1000));

        await Assert.That(t.TryCompile()).IsEqualTo(RegexError.LimitExceeded);
    }

    [Test]
    public async Task Repeat_OfAnEmptyChild_TerminatesAtCompileTime()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Add(RegexNode.Empty()), 1_000_000_000, 1_000_000_000));

        await Assert.That(Run(t, "")).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(Run(t, "a")).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task Repeat_StarOverANullableChild_Terminates()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Repeat(t.Char('a'), 0, RegexNode.Unbounded), 0, RegexNode.Unbounded));

        await Assert.That(Run(t, "aaa")).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(Run(t, "aab")).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task AnyChar_ExcludesLineBreaksOnly_AndCountsASurrogatePairOnce()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Any(), 3, 3));
        var program = t.Compile();

        await Assert.That(program.Match("a\U0001F600b", Limits)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("a\U0001F600", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("a\nb", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("a\rb", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("a\tb", Limits)).IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task Character_SupplementaryCodePoint_MatchesAsOneCharacter()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.CodePoint(0x1F600));
        var program = t.Compile();

        await Assert.That(program.Match("\U0001F600", Limits)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("\uD83D", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match("\U0001F601", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task Set_MatchesExactlyTheCodePointsOfTheSet_IncludingSupplementary()
    {
        // A hand-built stand-in for the parser output: a-c, U+0100, and U+1F600 to U+1F60F.
        var t = new TreeBuilder();
        t.WithRoot(t.Set('a', 'c', 0x100, 0x100, 0x1F600, 0x1F60F));
        var program = t.Compile();

        var wrong = 0;
        for (var cp = 0; cp < 0x1F700; cp++)
        {
            if (cp is >= 0xD800 and <= 0xDFFF)
            {
                continue;
            }

            var inSet = cp is >= 'a' and <= 'c' or 0x100 or (>= 0x1F600 and <= 0x1F60F);
            if (program.Match(char.ConvertFromUtf32(cp), Limits) != Expected(inSet))
            {
                wrong++;
            }
        }

        await Assert.That(wrong).IsEqualTo(0);
    }

    [Test]
    public async Task Set_SupplementaryCodePoint_IsOneCharacterForCounts()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Repeat(t.Set(0x1F600, 0x1F60F), 2, 2));
        var program = t.Compile();

        await Assert.That(program.Match("\U0001F600\U0001F601", Limits)).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(program.Match("\U0001F600", Limits)).IsEqualTo(RegexMatchResult.NoMatch);
    }

    [Test]
    public async Task RegexCharacterSet_Contains_UsesTheRangePairs()
    {
        var set = new RegexCharacterSet([10, 20, 30, 30, 0x10000, 0x10FFFF]);

        await Assert.That(set.Contains(9)).IsFalse();
        await Assert.That(set.Contains(10)).IsTrue();
        await Assert.That(set.Contains(20)).IsTrue();
        await Assert.That(set.Contains(21)).IsFalse();
        await Assert.That(set.Contains(30)).IsTrue();
        await Assert.That(set.Contains(31)).IsFalse();
        await Assert.That(set.Contains(0x10FFFF)).IsTrue();
        await Assert.That(new RegexCharacterSet([]).Contains(0)).IsFalse();
    }

    [Test]
    public async Task Set_SubtractionAndNegation_AreResolvedIntoTheSet()
    {
        // [a-z-[aeiou]] resolved to the consonant ranges; [^a-c] resolved to the complement ranges.
        var subtraction = new TreeBuilder();
        subtraction.WithRoot(subtraction.Set('b', 'd', 'f', 'h', 'j', 'n', 'p', 't', 'v', 'z'));
        var negated = new TreeBuilder();
        negated.WithRoot(negated.Set(0, 'a' - 1, 'd', 0x10FFFF));

        await Assert.That(Run(subtraction, "b")).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(Run(subtraction, "a")).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(Run(subtraction, "e")).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(Run(subtraction, "z")).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(Run(negated, "a")).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(Run(negated, "c")).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(Run(negated, "d")).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(Run(negated, "\U0001F600")).IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task Anchor_BackReference_AndLazyRepeat_AreRejectedInTheXsdDialect()
    {
        var anchor = new TreeBuilder();
        anchor.WithRoot(anchor.Add(RegexNode.Anchor(RegexAnchor.Start)));
        var backReference = new TreeBuilder();
        backReference.WithRoot(backReference.Add(RegexNode.BackReference(1)));
        var lazy = new TreeBuilder();
        lazy.WithRoot(lazy.Add(RegexNode.Repeat(lazy.Char('a'), 0, 1, lazy: true)));

        await Assert.That(anchor.TryCompile()).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(backReference.TryCompile()).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(lazy.TryCompile()).IsEqualTo(RegexError.ConstructNotAllowed);
    }

    [Test]
    public async Task XPathDialect_IsNotCompiledYet()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Char('a'));

        await Assert.That(t.TryCompile(RegexDialect.XPath)).IsEqualTo(RegexError.ConstructNotAllowed);
    }

    [Test]
    public async Task Nesting_BeyondTheCap_IsRejectedWithTheLimitError()
    {
        var t = new TreeBuilder();
        var node = t.Char('a');
        for (var i = 0; i < RegexProgram.MaxNesting + 1; i++)
        {
            node = t.Group(node);
        }

        t.WithRoot(node);

        await Assert.That(t.TryCompile()).IsEqualTo(RegexError.LimitExceeded);
    }

    [Test]
    public async Task NestedQuantifiers_OnAdversarialInput_EndWithinTheStepBudget()
    {
        // (a*)*b against aaaa...: exponential for a backtracker, bounded by the step ceiling here.
        var program = NestedStarThenB().Compile();
        var input = new string('a', 10_000);
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(5_000);

        var result = program.Match(input, limits, out var steps);

        await Assert.That(result).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(steps).IsLessThanOrEqualTo(5_000L);
    }

    [Test]
    public async Task NestedQuantifiers_BelowTheBudget_AnswerExactly()
    {
        var program = NestedStarThenB().Compile();
        var input = new string('a', 200);

        await Assert.That(program.Match(input, Limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(program.Match(input + "b", Limits)).IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task StepCount_IsDeterministic_AndTheBudgetIsInclusive()
    {
        var t = new TreeBuilder();
        t.WithRoot(t.Text("abc"));
        var program = t.Compile();

        _ = program.Match("abc", Limits, out var exact);
        var atBudget = program.Match("abc", Limits.WithMaxNfaSteps(exact), out _);
        var belowBudget = program.Match("abc", Limits.WithMaxNfaSteps(exact - 1), out _);

        await Assert.That(exact).IsGreaterThan(0L);
        await Assert.That(atBudget).IsEqualTo(RegexMatchResult.Match);
        await Assert.That(belowBudget).IsEqualTo(RegexMatchResult.Aborted);
    }

    [Test]
    public async Task Match_OnACompiledProgram_AllocatesNothing()
    {
        var small = new TreeBuilder();
        small.WithRoot(small.Concat(small.Repeat(small.Set('a', 'z'), 1, 5), small.Text("-x")));
        var smallProgram = small.Compile();
        var large = new TreeBuilder();
        large.WithRoot(large.Repeat(large.Char('a'), 1, 1000));
        var largeProgram = large.Compile();
        var largeInput = new string('a', 1000);

        AllocationAssert.NoAllocation(() =>
        {
            _ = smallProgram.Match("abc-x", Limits);
            _ = smallProgram.Match("abcdefg", Limits);
            _ = largeProgram.Match(largeInput, Limits);
        });

        await Assert.That(smallProgram.Match("abc-x", Limits)).IsEqualTo(RegexMatchResult.Match);
    }
}
