namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Parsing;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.Xml.Text.Unicode;

public class RegexParserTests
{
    private static readonly RegexSyntax Xsd10 = new(RegexDialect.Xsd, NameEdition.Xml10SecondEdition);
    private static readonly RegexSyntax Xsd11 = new(RegexDialect.Xsd, NameEdition.Xml10FifthEdition);

    private static RegexTree Tree(string pattern, RegexSyntax? syntax = null)
    {
        var result = RegexParser.Parse(pattern, syntax ?? Xsd10);
        return result.Tree ?? throw new InvalidOperationException($"{pattern}: {result.Error} at {result.Position}");
    }

    private static RegexCharacterSet SetOf(string pattern, RegexSyntax? syntax = null)
    {
        var tree = Tree(pattern, syntax);
        return tree.GetSet(tree.Nodes[tree.Root].Value);
    }

    [Test]
    [Arguments("", "E")]
    [Arguments("a", "c61")]
    [Arguments("ab", "cat(c61 c62)")]
    [Arguments("abc", "cat(c61 c62 c63)")]
    [Arguments("a|b", "alt(c61 c62)")]
    [Arguments("a|b|c", "alt(c61 c62 c63)")]
    [Arguments("a|", "alt(c61 E)")]
    [Arguments("|a", "alt(E c61)")]
    [Arguments("ab|c", "alt(cat(c61 c62) c63)")]
    [Arguments("(a)", "grp(c61)")]
    [Arguments("()", "grp(E)")]
    [Arguments("(a|b)c", "cat(grp(alt(c61 c62)) c63)")]
    [Arguments("a?", "rep{0,1}(c61)")]
    [Arguments("a*", "rep{0,-1}(c61)")]
    [Arguments("a+", "rep{1,-1}(c61)")]
    [Arguments("a{3}", "rep{3,3}(c61)")]
    [Arguments("a{2,}", "rep{2,-1}(c61)")]
    [Arguments("a{2,5}", "rep{2,5}(c61)")]
    [Arguments("a{0,0}", "rep{0,0}(c61)")]
    [Arguments("(ab)*c", "cat(rep{0,-1}(grp(cat(c61 c62))) c63)")]
    [Arguments(".", ".")]
    [Arguments("a.b", "cat(c61 . c62)")]
    [Arguments("\\^", "c5E")]
    [Arguments("\\n", "cA")]
    [Arguments("\\r", "cD")]
    [Arguments("\\t", "c9")]
    [Arguments("\\|", "c7C")]
    [Arguments("\\\\", "c5C")]
    [Arguments("\\.", "c2E")]
    [Arguments("\\-", "c2D")]
    [Arguments("\\{", "c7B")]
    [Arguments("\\}", "c7D")]
    [Arguments("\\(", "c28")]
    [Arguments("\\)", "c29")]
    [Arguments("\\[", "c5B")]
    [Arguments("\\]", "c5D")]
    [Arguments("\\?", "c3F")]
    [Arguments("\\*", "c2A")]
    [Arguments("\\+", "c2B")]
    [Arguments("[abc]", "[61-63]")]
    [Arguments("[a-c]", "[61-63]")]
    [Arguments("[acb]", "[61-63]")]
    [Arguments("[^a]", "[0-60,62-10FFFF]")]
    [Arguments("[ac-e]", "[61,63-65]")]
    [Arguments("[\\n\\-]", "[A,2D]")]
    [Arguments("[$^]", "[24,5E]")]
    [Arguments("[a.]", "[2E,61]")]
    [Arguments("\\s", "[9-A,D,20]")]
    [Arguments("\\S", "[0-8,B-C,E-1F,21-10FFFF]")]
    [Arguments("[\\s]", "[9-A,D,20]")]
    [Arguments("[a-z-[aeiou]]", "[62-64,66-68,6A-6E,70-74,76-7A]")]
    [Arguments("[a-z-[m]]", "[61-6C,6E-7A]")]
    [Arguments("[a-[b]]", "[61]")]
    [Arguments("[a-z-[^m-p]]", "[6D-70]")]
    [Arguments("[a-z-[m-z-[p]]]", "[61-6C,70]")]
    [Arguments("[^a-z-[m]]", "[0-60,7B-10FFFF]")]
    [Arguments("[-a]", "[2D,61]")]
    [Arguments("[a-]", "[2D,61]")]
    [Arguments("[-]", "[2D]")]
    [Arguments("[^-a]", "[0-2C,2E-60,62-10FFFF]")]
    [Arguments("[a\\-z]", "[2D,61,7A]")]
    [Arguments("[a-z-]", "[2D,61-7A]")]
    public async Task Parse_Production_ProducesTheExpectedTree(string pattern, string expected)
    {
        var tree = Tree(pattern);

        await Assert.That(RegexTreeDump.Of(tree)).IsEqualTo(expected);
    }

    [Test]
    public async Task Parse_SupplementaryCharacter_IsOneNode()
    {
        var tree = Tree("\U0001D11E");

        await Assert.That(RegexTreeDump.Of(tree)).IsEqualTo("c1D11E");
    }

    [Test]
    public async Task Parse_SupplementaryRangeInClass_IsOneRange()
    {
        var tree = Tree("[\U00010000-\U0001FFFF]");

        await Assert.That(RegexTreeDump.Of(tree)).IsEqualTo("[10000-1FFFF]");
    }

    [Test]
    [Arguments("^abc$", "cat(c5E c61 c62 c63 c24)")]
    [Arguments("a^b", "cat(c61 c5E c62)")]
    public async Task Parse_CaretAndDollarOutsideAClass_AreLiteralCharactersInTheXsdDialect(
        string pattern,
        string expected
    )
    {
        var tree = Tree(pattern);

        await Assert.That(RegexTreeDump.Of(tree)).IsEqualTo(expected);
    }

    [Test]
    public async Task Parse_Group_DoesNotCaptureInTheXsdDialect()
    {
        var tree = Tree("(a)");

        await Assert.That(tree.Nodes[tree.Root].Value).IsEqualTo(RegexNode.NoGroup);
    }

    [Test]
    public async Task Parse_Success_ReportsNoErrorAndPosition()
    {
        var result = RegexParser.Parse("a", Xsd10);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Error).IsEqualTo(RegexError.None);
        await Assert.That(result.Position).IsEqualTo(-1);
    }

    [Test]
    public async Task Parse_MultiCharacterEscapes_ResolveToTheirSets()
    {
        var digit = SetOf("\\d");
        var notDigit = SetOf("\\D");
        var word = SetOf("\\w");
        var notWord = SetOf("\\W");

        await Assert.That(digit.Contains('7')).IsTrue();
        await Assert.That(digit.Contains(0x0663)).IsTrue();
        await Assert.That(digit.Contains('a')).IsFalse();
        await Assert.That(notDigit.Contains('7')).IsFalse();
        await Assert.That(notDigit.Contains('a')).IsTrue();
        await Assert.That(word.Contains('a')).IsTrue();
        await Assert.That(word.Contains('5')).IsTrue();
        await Assert.That(word.Contains('$')).IsTrue();
        await Assert.That(word.Contains(' ')).IsFalse();
        await Assert.That(word.Contains('_')).IsFalse();
        await Assert.That(word.Contains('-')).IsFalse();
        await Assert.That(notWord.Contains('_')).IsTrue();
        await Assert.That(notWord.Contains('a')).IsFalse();
    }

    [Test]
    public async Task Parse_MultiCharacterEscapeInsideClass_IsMergedIntoTheSet()
    {
        var set = SetOf("[\\d_]");

        await Assert.That(set.Contains('3')).IsTrue();
        await Assert.That(set.Contains('_')).IsTrue();
        await Assert.That(set.Contains('x')).IsFalse();
    }

    [Test]
    public async Task Parse_CategoryAndComplement_AreOpposites()
    {
        var upper = SetOf("\\p{Lu}");
        var notUpper = SetOf("\\P{Lu}");
        var letter = SetOf("\\p{L}");

        await Assert.That(upper.Contains('A')).IsTrue();
        await Assert.That(upper.Contains('a')).IsFalse();
        await Assert.That(notUpper.Contains('A')).IsFalse();
        await Assert.That(notUpper.Contains('a')).IsTrue();
        await Assert.That(letter.Contains('a')).IsTrue();
        await Assert.That(letter.Contains('A')).IsTrue();
        await Assert.That(letter.Contains('1')).IsFalse();
    }

    private static readonly string[] CategoryEscapeNames =
    [
        "L",
        "Lu",
        "Ll",
        "Lt",
        "Lm",
        "Lo",
        "M",
        "Mn",
        "Mc",
        "Me",
        "N",
        "Nd",
        "Nl",
        "No",
        "P",
        "Pc",
        "Pd",
        "Ps",
        "Pe",
        "Pi",
        "Pf",
        "Po",
        "Z",
        "Zs",
        "Zl",
        "Zp",
        "S",
        "Sm",
        "Sc",
        "Sk",
        "So",
        "C",
        "Cc",
        "Cf",
        "Co",
        "Cn",
    ];

    // XSD knows no surrogate category: the group C does not contain Cs either.
    private static bool Selects(string name, UnicodeGeneralCategory category) =>
        category != UnicodeGeneralCategory.Cs
        && (
            name.Length == 2
                ? string.Equals(category.ToString(), name, StringComparison.Ordinal)
                : category.ToString().StartsWith(name, StringComparison.Ordinal)
        );

    [Test]
    public async Task Parse_EveryCategoryEscape_ResolvesThroughTheEmbeddedTables()
    {
        var failures = new List<string>();
        foreach (var name in CategoryEscapeNames)
        {
            var set = SetOf("\\p{" + name + "}");
            var complement = SetOf("\\P{" + name + "}");
            for (var run = 0; run < UnicodeTables.CategoryRunCount; run++)
            {
                var category = UnicodeTables.GetCategoryRun(run, out var first, out var last);
                var expected = Selects(name, category);
                foreach (var cp in new[] { first, last })
                {
                    if (set.Contains(cp) != expected || complement.Contains(cp) == expected)
                    {
                        failures.Add($"\\p{{{name}}} U+{cp:X} ({category})");
                    }
                }
            }
        }

        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Parse_EveryBlockEscape_ResolvesThroughTheEmbeddedTables()
    {
        var firstOf = new Dictionary<int, int>();
        for (var cp = 0; cp <= 0x10FFFF; cp++)
        {
            var block = UnicodeTables.GetBlock(cp);
            if (block != UnicodeTables.NoBlock)
            {
                _ = firstOf.TryAdd(block, cp);
            }
        }

        var failures = new List<string>();
        foreach (var (block, firstCodePoint) in firstOf)
        {
            var name = UnicodeTables
                .GetBlockName(block)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal);
            if (!SetOf("\\p{Is" + name + "}").Contains(firstCodePoint))
            {
                failures.Add(name);
            }

            if (SetOf("\\P{Is" + name + "}").Contains(firstCodePoint))
            {
                failures.Add("complement " + name);
            }
        }

        await Assert.That(firstOf.Count).IsGreaterThan(300);
        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Parse_SupersededBlockName_ResolvesThroughItsAlias()
    {
        var greek = SetOf("\\p{IsGreek}");

        await Assert.That(greek.Contains(0x3B1)).IsTrue();
        await Assert.That(greek.Contains('a')).IsFalse();
    }

    [Test]
    [Arguments("\\p{Foo}")]
    [Arguments("\\p{IsFoo}")]
    [Arguments("\\p{Cs}")]
    [Arguments("\\p{l}")]
    [Arguments("\\p{}")]
    [Arguments("\\P{Is}")]
    [Arguments("[\\p{Xx}]")]
    public async Task Parse_UnknownCategoryOrBlock_IsAnError(string pattern)
    {
        var result = RegexParser.Parse(pattern, Xsd10);

        await Assert.That(result.Error).IsEqualTo(RegexError.UnknownCategoryOrBlock);
    }

    [Test]
    [Arguments(NameEdition.Xml10SecondEdition)]
    [Arguments(NameEdition.Xml10FifthEdition)]
    public async Task Parse_NameEscapes_UseTheSharedCharacterClasses(NameEdition edition)
    {
        var syntax = new RegexSyntax(RegexDialect.Xsd, edition);
        var initial = SetOf("\\i", syntax);
        var notInitial = SetOf("\\I", syntax);
        var name = SetOf("\\c", syntax);
        var notName = SetOf("\\C", syntax);
        var failures = new List<string>();
        for (var cp = 0; cp <= 0x10FFFF; cp++)
        {
            var start = XmlNameClass.IsNameStartChar(edition, cp);
            var part = XmlNameClass.IsNameChar(edition, cp);
            if (
                initial.Contains(cp) != start
                || notInitial.Contains(cp) == start
                || name.Contains(cp) != part
                || notName.Contains(cp) == part
            )
            {
                failures.Add($"U+{cp:X}");
            }
        }

        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments(0x0132, false, true)]
    [Arguments(0x200C, false, true)]
    [Arguments(0x10000, false, true)]
    [Arguments(0x0131, true, true)]
    [Arguments('a', true, true)]
    public async Task Parse_I_NameEditionsDifferAtThePinningRows(int codePoint, bool second, bool fifth)
    {
        await Assert.That(SetOf("\\i", Xsd10).Contains(codePoint)).IsEqualTo(second);
        await Assert.That(SetOf("\\i", Xsd11).Contains(codePoint)).IsEqualTo(fifth);
    }

    [Test]
    [Arguments(0x0132, false, true)]
    [Arguments(0x200C, false, true)]
    [Arguments(0x203F, false, true)]
    [Arguments(0x10000, false, true)]
    [Arguments(0x00B7, true, true)]
    [Arguments('-', true, true)]
    public async Task Parse_C_NameEditionsDifferAtThePinningRows(int codePoint, bool second, bool fifth)
    {
        await Assert.That(SetOf("\\c", Xsd10).Contains(codePoint)).IsEqualTo(second);
        await Assert.That(SetOf("\\c", Xsd11).Contains(codePoint)).IsEqualTo(fifth);
    }

    [Test]
    [Arguments("(a)\\1", 3)]
    [Arguments("\\9", 0)]
    public async Task Parse_BackReference_IsRejectedInTheXsdDialect(string pattern, int position)
    {
        var result = RegexParser.Parse(pattern, Xsd10);

        await Assert.That(result.Error).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(result.Position).IsEqualTo(position);
    }

    [Test]
    [Arguments("a*?", 2)]
    [Arguments("a+?", 2)]
    [Arguments("a??", 2)]
    [Arguments("a{2}?", 4)]
    [Arguments("a{2,3}?", 6)]
    public async Task Parse_ReluctantQuantifier_IsRejectedInTheXsdDialect(string pattern, int position)
    {
        var result = RegexParser.Parse(pattern, Xsd10);

        await Assert.That(result.Error).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(result.Position).IsEqualTo(position);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task Limits_BelowOne_Throw(int value)
    {
        await Assert.That(() => RegexLimits.Default.WithMaxPatternLength(value)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexLimits.Default.WithMaxNesting(value)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(1)]
    public async Task Tree_GetSet_OutsideTheSets_Throws(int index)
    {
        var tree = Tree("a");

        await Assert.That(() => tree.GetSet(index)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Parse_NullLimits_Throws() =>
        await Assert.That(() => RegexParser.Parse("a", Xsd10, null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Parse_PatternLongerThanTheLimit_IsRejectedAtTheLimit()
    {
        var limits = RegexLimits.Default.WithMaxPatternLength(3);

        await Assert.That(RegexParser.Parse("abc", Xsd10, limits).Succeeded).IsTrue();
        var result = RegexParser.Parse("abcd", Xsd10, limits);

        await Assert.That(result.Error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(result.Position).IsEqualTo(3);
    }

    [Test]
    public async Task Parse_NestingBeyondTheLimit_IsRejected()
    {
        var limits = RegexLimits.Default.WithMaxNesting(2);

        await Assert.That(RegexParser.Parse("((a))", Xsd10, limits).Succeeded).IsTrue();
        var groups = RegexParser.Parse("(((a)))", Xsd10, limits);
        var classes = RegexParser.Parse("[a-z-[b-y-[c]]]", Xsd10, limits);

        await Assert.That(groups.Error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(groups.Position).IsEqualTo(2);
        await Assert.That(classes.Error).IsEqualTo(RegexError.LimitExceeded);
    }

    [Test]
    public async Task Parse_DeepNestingAtTheDefaultLimit_DoesNotOverflowTheStack()
    {
        var deep = new string('(', 65) + "a" + new string(')', 65);

        await Assert.That(RegexParser.Parse(deep, Xsd10).Error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(RegexParser.Parse(new string('(', 64) + "a" + new string(')', 64), Xsd10).Succeeded).IsTrue();
    }

    [Test]
    public async Task Limits_Defaults()
    {
        await Assert.That(RegexLimits.Default.MaxPatternLength).IsEqualTo(4096);
        await Assert.That(RegexLimits.Default.MaxNesting).IsEqualTo(64);
        await Assert.That(Tree("[a]").SetCount).IsEqualTo(1);
    }

    [Test]
    [Arguments("[a-c]x", "bx", RegexMatchResult.Match)]
    [Arguments("[a-c]x", "dx", RegexMatchResult.NoMatch)]
    [Arguments("[^a-c]+", "xyz", RegexMatchResult.Match)]
    [Arguments(@"\d{2}-\p{Lu}", "42-Q", RegexMatchResult.Match)]
    [Arguments(@"[a-z-[aeiou]]+", "xyz", RegexMatchResult.Match)]
    [Arguments(@"[a-z-[aeiou]]+", "xaz", RegexMatchResult.NoMatch)]
    public async Task Tree_CompilesInTheMatcher(string pattern, string input, RegexMatchResult expected)
    {
        var tree = Tree(pattern);
        var sets = new RegexCharacterSet[tree.SetCount];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = tree.GetSet(i);
        }

        var error = RegexProgram.TryCompile(tree.Nodes, tree.Root, sets, RegexDialect.Xsd, out var program);

        await Assert.That(error).IsEqualTo(RegexError.None);
        await Assert.That(program!.Match(input, RegexStepLimits.Default)).IsEqualTo(expected);
    }

    [Test]
    public async Task Parse_LongPattern_GrowsTheNodeBuffers()
    {
        var pattern = new string('a', 2000) + "|" + new string('b', 2000);

        var result = RegexParser.Parse(pattern, Xsd10);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Tree!.Nodes.Length).IsGreaterThan(4000);
    }
}
