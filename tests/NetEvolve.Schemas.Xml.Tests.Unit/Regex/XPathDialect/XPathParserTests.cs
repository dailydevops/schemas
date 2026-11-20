namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public class XPathParserTests
{
    private static RegexCharacterSet SetOf(string pattern, RegexOptions flags = RegexOptions.None)
    {
        var tree = XPathRegex.Tree(pattern, flags);
        return tree.GetSet(tree.Nodes[tree.Root].Value);
    }

    [Test]
    [Arguments("^a$", "cat(^ c61 $)")]
    [Arguments("^", "^")]
    [Arguments("$", "$")]
    [Arguments("a|^b|c$", "alt(c61 cat(^ c62) cat(c63 $))")]
    [Arguments("^*", "rep{0,-1}(^)")]
    [Arguments("\\$", "c24")]
    [Arguments("a\\$b", "cat(c61 c24 c62)")]
    [Arguments("a*?", "rep{0,-1}?(c61)")]
    [Arguments("a+?", "rep{1,-1}?(c61)")]
    [Arguments("a??", "rep{0,1}?(c61)")]
    [Arguments("a{2}?", "rep{2,2}?(c61)")]
    [Arguments("a{2,}?", "rep{2,-1}?(c61)")]
    [Arguments("a{2,3}?", "rep{2,3}?(c61)")]
    [Arguments("a*", "rep{0,-1}(c61)")]
    [Arguments("(a)(b)", "cat(g1(c61) g2(c62))")]
    [Arguments("((a)b)", "g1(cat(g2(c61) c62))")]
    [Arguments("(?:a)", "ng(c61)")]
    [Arguments("(?:a)(b)", "cat(ng(c61) g1(c62))")]
    [Arguments("(a(?:b)(c))", "g1(cat(c61 ng(c62) g2(c63)))")]
    [Arguments("(?:a|b)+", "rep{1,-1}(ng(alt(c61 c62)))")]
    [Arguments("(a)\\1", "cat(g1(c61) \\1)")]
    [Arguments("(a)(b)\\2\\1", "cat(g1(c61) g2(c62) \\2 \\1)")]
    [Arguments("(a)\\1*", "cat(g1(c61) rep{0,-1}(\\1))")]
    [Arguments("(a)\\10", "cat(g1(c61) \\1 c30)")]
    [Arguments("(a)\\11", "cat(g1(c61) \\1 c31)")]
    [Arguments("(a)\\19", "cat(g1(c61) \\1 c39)")]
    [Arguments("(a)(?:b)\\1", "cat(g1(c61) ng(c62) \\1)")]
    [Arguments("(a(b)c)\\2", "cat(g1(cat(c61 g2(c62) c63)) \\2)")]
    public async Task Parse_XPathDialect_ProducesTheDialectNodes(string pattern, string expected) =>
        await Assert.That(XPathTreeDump.Of(XPathRegex.Tree(pattern))).IsEqualTo(expected);

    [Test]
    public async Task Parse_BackReference_TakesFurtherDigitsOnlyForOpenedGroups()
    {
        // Twelve capturing groups, then \12 is the back reference of group 12; with only eleven it is \1 and a literal 2.
        var twelve = string.Empty;
        for (var i = 0; i < 12; i++)
        {
            twelve += "(a)";
        }

        var eleven = twelve.Substring(0, twelve.Length - 3);
        var withTwelve = XPathRegex.Tree(twelve + "\\12");
        var withEleven = XPathRegex.Tree(eleven + "\\12");

        await Assert.That(XPathTreeDump.Of(withTwelve)).EndsWith("\\12)");
        await Assert.That(XPathTreeDump.Of(withEleven)).EndsWith("\\1 c32)");
    }

    [Test]
    [Arguments("\\1", 0)]
    [Arguments("(a)\\2", 3)]
    [Arguments("\\1(a)", 0)]
    [Arguments("(a\\1)", 2)]
    [Arguments("(a(b\\2))", 4)]
    [Arguments("(?:a)\\1", 5)]
    [Arguments("(a)|\\2", 4)]
    [Arguments("(a)\\9", 3)]
    public async Task Parse_BackReference_ToAnUnclosedOrMissingGroup_IsAStaticError(string pattern, int position)
    {
        var result = RegexParser.Parse(pattern, XPathRegex.Syntax());

        await Assert.That(result.Error).IsEqualTo(RegexError.InvalidBackReference);
        await Assert.That(result.Position).IsEqualTo(position);
    }

    [Test]
    [Arguments("[\\1]", RegexError.InvalidEscape, 1)]
    [Arguments("\\0", RegexError.InvalidEscape, 0)]
    [Arguments("(a)\\0", RegexError.InvalidEscape, 3)]
    [Arguments("[a-\\$]", RegexError.InvalidCharacterClass, 1)]
    [Arguments("\\a", RegexError.InvalidEscape, 0)]
    [Arguments("(?=a)", RegexError.InvalidQuantifier, 1)]
    [Arguments("(?a)", RegexError.InvalidQuantifier, 1)]
    [Arguments("a???", RegexError.InvalidQuantifier, 3)]
    [Arguments("a**", RegexError.InvalidQuantifier, 2)]
    [Arguments("(a", RegexError.UnexpectedEnd, 2)]
    [Arguments("(?:a", RegexError.UnexpectedEnd, 4)]
    [Arguments("a)", RegexError.Unbalanced, 1)]
    [Arguments("a{2,1}?", RegexError.InvalidQuantifier, 1)]
    public async Task Parse_XPathDialect_RejectsWithCodeAndPosition(string pattern, RegexError error, int position)
    {
        var result = RegexParser.Parse(pattern, XPathRegex.Syntax());

        await Assert.That(result.Error).IsEqualTo(error);
        await Assert.That(result.Position).IsEqualTo(position);
    }

    [Test]
    public async Task Parse_XPathDialect_ClassWithDollarEscape_ContainsTheDollar()
    {
        var set = SetOf("[\\$a]");

        await Assert.That(set.Contains('$')).IsTrue();
        await Assert.That(set.Contains('a')).IsTrue();
        await Assert.That(set.Contains('b')).IsFalse();
    }

    [Test]
    public async Task Parse_XPathDialect_CarriesTheDialectAndTheFlagsOnTheTree()
    {
        var plain = XPathRegex.Tree("a");
        var flagged = XPathRegex.Tree("a", RegexOptions.DotAll | RegexOptions.MultiLine);

        await Assert.That(plain.Dialect).IsEqualTo(RegexDialect.XPath);
        await Assert.That(plain.Options).IsEqualTo(RegexOptions.None);
        await Assert.That(flagged.Options).IsEqualTo(RegexOptions.DotAll | RegexOptions.MultiLine);
        await Assert
            .That(
                RegexParser.Parse("a", new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition)).Tree!.Dialect
            )
            .IsEqualTo(RegexDialect.Xsd);
    }

    [Test]
    public async Task Parse_XPathDialect_AppliesTheLimits()
    {
        var syntax = XPathRegex.Syntax();
        var tooLong = RegexParser.Parse("aaaa", syntax, RegexLimits.Default.WithMaxPatternLength(3));
        var tooDeep = RegexParser.Parse("(((a)))", syntax, RegexLimits.Default.WithMaxNesting(2));
        var deepNonCapturing = RegexParser.Parse("(?:(?:(?:a)))", syntax, RegexLimits.Default.WithMaxNesting(2));

        await Assert.That(tooLong.Error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(tooDeep.Error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(deepNonCapturing.Error).IsEqualTo(RegexError.LimitExceeded);
    }

    [Test]
    public async Task Parse_XPathDialect_LimitsTheOriginalPatternLengthWithFreeSpacing()
    {
        var syntax = XPathRegex.Syntax(RegexOptions.FreeSpacing);

        var result = RegexParser.Parse("a b c", syntax, RegexLimits.Default.WithMaxPatternLength(4));

        await Assert.That(result.Error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(result.Position).IsEqualTo(4);
    }

    [Test]
    [Arguments("a b", "cat(c61 c62)")]
    [Arguments(" a\tb\r\nc ", "cat(c61 c62 c63)")]
    [Arguments("a{1, 2}", "rep{1,2}(c61)")]
    [Arguments("a* ?", "rep{0,-1}?(c61)")]
    [Arguments("( ?:a)", "ng(c61)")]
    [Arguments("a | b", "alt(c61 c62)")]
    [Arguments("\\ s", "set0")]
    [Arguments("hello\\ sworld", "cat(c68 c65 c6C c6C c6F set0 c77 c6F c72 c6C c64)")]
    [Arguments("\\\\ b", "cat(c5C c62)")]
    [Arguments("\\[ a", "cat(c5B c61)")]
    public async Task Parse_FreeSpacing_RemovesWhitespaceOutsideClasses(string pattern, string expected)
    {
        var result = RegexParser.Parse(pattern, XPathRegex.Syntax(RegexOptions.FreeSpacing));

        await Assert.That(XPathTreeDump.Of(result.Tree!)).IsEqualTo(expected);
    }

    [Test]
    public async Task Parse_FreeSpacing_TrailingBackslash_IsAnIncompleteEscape()
    {
        var result = RegexParser.Parse("a\\", XPathRegex.Syntax(RegexOptions.FreeSpacing));

        await Assert.That(result.Error).IsEqualTo(RegexError.UnexpectedEnd);
        await Assert.That(result.Position).IsEqualTo(2);
    }

    [Test]
    public async Task Parse_FreeSpacing_KeepsWhitespaceInsideClasses()
    {
        var spaced = SetOf("[a b]", RegexOptions.FreeSpacing);
        var subtracted = SetOf("[a b-[b]]", RegexOptions.FreeSpacing);
        var escapedBracket = SetOf("[\\] ]", RegexOptions.FreeSpacing);
        var nested = SetOf("[ab-[ b]]", RegexOptions.FreeSpacing);

        await Assert.That(spaced.Contains(' ')).IsTrue();
        await Assert.That(subtracted.Contains(' ')).IsTrue();
        await Assert.That(subtracted.Contains('a')).IsTrue();
        await Assert.That(subtracted.Contains('b')).IsFalse();
        await Assert.That(escapedBracket.Contains(' ')).IsTrue();
        await Assert.That(escapedBracket.Contains(']')).IsTrue();
        await Assert.That(nested.Contains(' ')).IsFalse();
        await Assert.That(nested.Contains('a')).IsTrue();
    }

    [Test]
    [Arguments("a b)", RegexError.Unbalanced, 3)]
    [Arguments(" a  (b", RegexError.UnexpectedEnd, 6)]
    [Arguments("a  b  {", RegexError.InvalidQuantifier, 6)]
    [Arguments("a  \\9", RegexError.InvalidBackReference, 3)]
    public async Task Parse_FreeSpacing_ReportsPositionsOfTheOriginalPattern(
        string pattern,
        RegexError error,
        int position
    )
    {
        var result = RegexParser.Parse(pattern, XPathRegex.Syntax(RegexOptions.FreeSpacing));

        await Assert.That(result.Error).IsEqualTo(error);
        await Assert.That(result.Position).IsEqualTo(position);
    }

    [Test]
    public async Task Parse_WithoutFreeSpacing_KeepsWhitespaceAsCharacters() =>
        await Assert.That(XPathTreeDump.Of(XPathRegex.Tree("a b"))).IsEqualTo("cat(c61 c20 c62)");

    [Test]
    [Arguments("a", "set0")]
    [Arguments("A", "set0")]
    [Arguments("1", "c31")]
    [Arguments("\\n", "cA")]
    [Arguments("a1", "cat(set0 c31)")]
    [Arguments("(a)", "g1(set0)")]
    public async Task Parse_IgnoreCase_ResolvesCasedCharactersToVariantSets(string pattern, string expected) =>
        await Assert.That(XPathTreeDump.Of(XPathRegex.Tree(pattern, RegexOptions.IgnoreCase))).IsEqualTo(expected);

    [Test]
    public async Task Parse_IgnoreCase_CharacterSetHoldsAllVariants()
    {
        var k = SetOf("k", RegexOptions.IgnoreCase);
        var s = SetOf("S", RegexOptions.IgnoreCase);
        var deseret = SetOf("\U00010400", RegexOptions.IgnoreCase);

        await Assert.That(k.Contains('k')).IsTrue();
        await Assert.That(k.Contains('K')).IsTrue();
        await Assert.That(k.Contains(0x212A)).IsTrue();
        await Assert.That(k.Contains('j')).IsFalse();
        await Assert.That(s.Contains(0x17F)).IsTrue();
        await Assert.That(deseret.Contains(0x10400)).IsTrue();
        await Assert.That(deseret.Contains(0x10428)).IsTrue();
        await Assert.That(deseret.Contains(0x10401)).IsFalse();
    }

    [Test]
    public async Task Parse_IgnoreCase_RangesAndSingleCharactersInClassesGetTheirVariants()
    {
        var range = SetOf("[A-Z]", RegexOptions.IgnoreCase);
        var single = SetOf("[x1]", RegexOptions.IgnoreCase);

        await Assert.That(range.Contains('a')).IsTrue();
        await Assert.That(range.Contains('Z')).IsTrue();
        await Assert.That(range.Contains(0x212A)).IsTrue();
        await Assert.That(range.Contains('0')).IsFalse();
        await Assert.That(single.Contains('X')).IsTrue();
        await Assert.That(single.Contains('1')).IsTrue();
        await Assert.That(single.Contains('y')).IsFalse();
    }

    [Test]
    public async Task Parse_IgnoreCase_NegatedClass_ExcludesTheVariants()
    {
        var negated = SetOf("[^Q]", RegexOptions.IgnoreCase);

        await Assert.That(negated.Contains('Q')).IsFalse();
        await Assert.That(negated.Contains('q')).IsFalse();
        await Assert.That(negated.Contains('a')).IsTrue();
        await Assert.That(negated.Contains('1')).IsTrue();
    }

    [Test]
    public async Task Parse_IgnoreCase_Subtraction_ExcludesTheVariantsOfTheSubtrahend()
    {
        var set = SetOf("[A-Z-[IO]]", RegexOptions.IgnoreCase);

        await Assert.That(set.Contains('A')).IsTrue();
        await Assert.That(set.Contains('B')).IsTrue();
        await Assert.That(set.Contains('a')).IsTrue();
        await Assert.That(set.Contains('b')).IsTrue();
        await Assert.That(set.Contains('I')).IsFalse();
        await Assert.That(set.Contains('O')).IsFalse();
        await Assert.That(set.Contains('i')).IsFalse();
        await Assert.That(set.Contains('o')).IsFalse();
    }

    [Test]
    public async Task Parse_IgnoreCase_CategoryEscapes_AreUnaffected()
    {
        var upper = SetOf("\\p{Lu}", RegexOptions.IgnoreCase);
        var mixed = SetOf("[\\p{Lu}x]", RegexOptions.IgnoreCase);

        await Assert.That(upper.Contains('A')).IsTrue();
        await Assert.That(upper.Contains('a')).IsFalse();
        await Assert.That(mixed.Contains('A')).IsTrue();
        await Assert.That(mixed.Contains('x')).IsTrue();
        await Assert.That(mixed.Contains('X')).IsTrue();
        await Assert.That(mixed.Contains('a')).IsFalse();
    }

    [Test]
    public async Task Parse_WithoutIgnoreCase_KeepsCharactersAndClassesExact()
    {
        var range = SetOf("[A-Z]");

        await Assert.That(XPathTreeDump.Of(XPathRegex.Tree("a"))).IsEqualTo("c61");
        await Assert.That(range.Contains('a')).IsFalse();
        await Assert.That(range.Contains(0x212A)).IsFalse();
    }

    [Test]
    [Arguments(RegexOptions.DotAll)]
    [Arguments(RegexOptions.MultiLine)]
    [Arguments(RegexOptions.IgnoreCase)]
    [Arguments(RegexOptions.FreeSpacing)]
    public async Task Parse_XsdDialect_RejectsFlags(RegexOptions flags)
    {
        var result = RegexParser.Parse("a", new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition, flags));

        await Assert.That(result.Error).IsEqualTo(RegexError.InvalidFlag);
        await Assert.That(result.Position).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_XsdDialect_KeepsItsGroupsAnonymous_AndRejectsTheXPathConstructs()
    {
        var xsd = new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition);

        await Assert.That(XPathTreeDump.Of(RegexParser.Parse("(a)", xsd).Tree!)).IsEqualTo("ng(c61)");
        await Assert.That(RegexParser.Parse("(?:a)", xsd).Error).IsEqualTo(RegexError.InvalidQuantifier);
        await Assert.That(RegexParser.Parse("\\$", xsd).Error).IsEqualTo(RegexError.InvalidEscape);
        await Assert.That(RegexParser.Parse("^", xsd).Error).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(RegexParser.Parse("$", xsd).Error).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(RegexParser.Parse("a*?", xsd).Error).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(RegexParser.Parse("(a)\\1", xsd).Error).IsEqualTo(RegexError.ConstructNotAllowed);
    }

    [Test]
    public async Task Syntax_CarriesTheFlags_AndComparesByThem()
    {
        var plain = new RegexSyntax(RegexDialect.XPath, NameEdition.Xml10FifthEdition);
        var flagged = new RegexSyntax(RegexDialect.XPath, NameEdition.Xml10FifthEdition, RegexOptions.IgnoreCase);

        await Assert.That(plain.Options).IsEqualTo(RegexOptions.None);
        await Assert.That(flagged.Options).IsEqualTo(RegexOptions.IgnoreCase);
        await Assert.That(flagged).IsNotEqualTo(plain);
        await Assert.That(default(RegexSyntax).Options).IsEqualTo(RegexOptions.None);
    }

    [Test]
    public async Task Parse_XPathDialect_InvalidSurrogate_IsRejected()
    {
        var result = RegexParser.Parse("a\uD800", XPathRegex.Syntax(RegexOptions.IgnoreCase));

        await Assert.That(result.Error).IsEqualTo(RegexError.InvalidUtf16);
        await Assert.That(result.Position).IsEqualTo(1);
    }
}
