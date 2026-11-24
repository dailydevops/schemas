namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Parsing;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>The committed, hand-written table of valid and invalid patterns; the regex sets of the W3C test suite are XSD-75.</summary>
public class RegexParserTableTests
{
    private static readonly string[] Valid =
    [
        "",
        "a",
        "abc",
        "a|b",
        "a|",
        "|a",
        "|",
        "()",
        "(a|b)*c",
        "((a))",
        "a{0}",
        "a{0,0}",
        "a{3,3}",
        "a{2,}",
        "a{2}b{3,}c{4,5}",
        "a?b*c+",
        "[a-z]+",
        "[^a-z]",
        "[a-z-[m]]",
        "[a-z-[aeiou]]",
        "[a-z-[^m-p]]",
        "[a-z-[m-z-[p]]]",
        "[^a-z-[m]]",
        "[-]",
        "[a-]",
        "[-a]",
        "[a\\-z]",
        "[\\d\\s]",
        "[\\p{L}\\p{N}]",
        "[\\P{L}]",
        "[\\n\\r\\t]",
        "[$]",
        "[\\^]",
        "[a^]",
        "[.]",
        "[(]",
        "[a{]",
        "\\p{L}+",
        "\\P{Nd}",
        "\\p{IsBasicLatin}",
        "\\p{IsGreek}",
        "\\i\\c*",
        "\\I\\C",
        "\\s\\S\\d\\D\\w\\W",
        "\\w+@\\w+\\.\\w+",
        ".*",
        "a\\.b",
        "\\-",
        "\\^",
        "\\(\\)\\[\\]\\{\\}\\|\\?\\*\\+\\\\",
        "\\n\\r\\t",
        "äö",
        "\U0001D11E+",
        "[\U00010000-\U0001FFFF]",
        "[0-9]{3}-[0-9]{4}",
        "\\p{Lu}\\p{Ll}*",
    ];

    private static readonly (string Pattern, RegexError Error, int Position)[] Invalid =
    [
        ("(", RegexError.UnexpectedEnd, 1),
        ("(a", RegexError.UnexpectedEnd, 2),
        ("a)", RegexError.Unbalanced, 1),
        ("(a))", RegexError.Unbalanced, 3),
        ("a]", RegexError.Unbalanced, 1),
        ("[", RegexError.UnexpectedEnd, 1),
        ("[a", RegexError.UnexpectedEnd, 2),
        ("*", RegexError.InvalidQuantifier, 0),
        ("+a", RegexError.InvalidQuantifier, 0),
        ("?", RegexError.InvalidQuantifier, 0),
        ("a**", RegexError.InvalidQuantifier, 2),
        ("a|*", RegexError.InvalidQuantifier, 2),
        ("(?:a)", RegexError.InvalidQuantifier, 1),
        ("{1}", RegexError.InvalidQuantifier, 0),
        ("a}", RegexError.InvalidQuantifier, 1),
        ("a{", RegexError.UnexpectedEnd, 2),
        ("a{1", RegexError.UnexpectedEnd, 3),
        ("a{1,", RegexError.UnexpectedEnd, 4),
        ("a{1,2", RegexError.UnexpectedEnd, 5),
        ("a{x}", RegexError.InvalidQuantifier, 1),
        ("a{,2}", RegexError.InvalidQuantifier, 1),
        ("a{2,1}", RegexError.InvalidQuantifier, 1),
        ("a{1,2,3}", RegexError.InvalidQuantifier, 1),
        ("a{1;2}", RegexError.InvalidQuantifier, 1),
        ("a{99999999999}", RegexError.InvalidQuantifier, 1),
        ("\\", RegexError.UnexpectedEnd, 1),
        ("ab\\", RegexError.UnexpectedEnd, 3),
        ("\\q", RegexError.InvalidEscape, 0),
        ("a\\$", RegexError.InvalidEscape, 1),
        ("\\0", RegexError.InvalidEscape, 0),
        ("[\\1]", RegexError.InvalidEscape, 1),
        ("\\p", RegexError.UnexpectedEnd, 2),
        ("\\pL", RegexError.InvalidEscape, 0),
        ("\\p{", RegexError.UnexpectedEnd, 3),
        ("\\p{L", RegexError.UnexpectedEnd, 4),
        ("\\p{}", RegexError.UnknownCategoryOrBlock, 0),
        ("a\\p{Foo}", RegexError.UnknownCategoryOrBlock, 1),
        ("\\p{Cs}", RegexError.UnknownCategoryOrBlock, 0),
        ("\\P{IsFoo}", RegexError.UnknownCategoryOrBlock, 0),
        ("[]", RegexError.InvalidCharacterClass, 1),
        ("[^]", RegexError.InvalidCharacterClass, 2),
        ("[z-a]", RegexError.InvalidCharacterClass, 1),
        ("[a-z-0]", RegexError.InvalidCharacterClass, 4),
        ("[a--]", RegexError.InvalidCharacterClass, 3),
        ("[[a]]", RegexError.InvalidCharacterClass, 1),
        ("[\\s-z]", RegexError.InvalidCharacterClass, 3),
        ("[a-\\s]", RegexError.InvalidCharacterClass, 3),
        ("[a-z-[]]", RegexError.InvalidCharacterClass, 6),
        ("[a-z-[aeiou]x]", RegexError.InvalidCharacterClass, 12),
        ("[a-z-[aeiou]-[b]]", RegexError.InvalidCharacterClass, 12),
        ("[a-z-[aeiou]", RegexError.UnexpectedEnd, 12),
        ("[a-z-[a", RegexError.UnexpectedEnd, 7),
        ("(a)\\1", RegexError.ConstructNotAllowed, 3),
        ("a*?", RegexError.ConstructNotAllowed, 2),
        ("a{2,3}?", RegexError.ConstructNotAllowed, 6),
        ("\ud800", RegexError.InvalidUtf16, 0),
        ("a\udc00", RegexError.InvalidUtf16, 1),
        ("[\ud800]", RegexError.InvalidUtf16, 1),
        ("[a-\udc00]", RegexError.InvalidUtf16, 3),
    ];

    [Test]
    public async Task Table_EveryValidPattern_ParsesInBothNameEditions()
    {
        var failures = new List<string>();
        foreach (var edition in new[] { NameEdition.Xml10SecondEdition, NameEdition.Xml10FifthEdition })
        {
            foreach (var pattern in Valid)
            {
                var result = RegexParser.Parse(pattern, new RegexSyntax(RegexDialect.Xsd, edition));
                if (!result.Succeeded || result.Tree is null)
                {
                    failures.Add($"{pattern}: {result.Error} at {result.Position}");
                }
            }
        }

        await Assert.That(string.Join("; ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Table_EveryInvalidPattern_FailsWithTheListedCodeAndPosition()
    {
        var failures = new List<string>();
        foreach (var (pattern, error, position) in Invalid)
        {
            var result = RegexParser.Parse(pattern, new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10SecondEdition));
            if (result.Error != error || result.Position != position || result.Tree is not null)
            {
                failures.Add($"{pattern}: {result.Error} at {result.Position}, expected {error} at {position}");
            }
        }

        await Assert.That(string.Join("; ", failures)).IsEqualTo(string.Empty);
    }
}
