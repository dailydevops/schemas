namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Operations;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public class RegexOperationsCreationTests
{
    [Test]
    public async Task TryCreate_WithoutATree_IsRejected() =>
        await Assert.That(() => RegexOperations.TryCreate(null!, out _)).Throws<ArgumentNullException>();

    [Test]
    public async Task TryCreate_OfAnXsdDialectTree_IsNotAllowedInTheDialect()
    {
        var tree = RegexParser.Parse("a(b)", new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10FifthEdition)).Tree!;

        var error = RegexOperations.TryCreate(tree, out var operations);

        await Assert.That(error).IsEqualTo(RegexError.ConstructNotAllowed);
        await Assert.That(operations).IsNull();
    }

    [Test]
    public async Task TryCreate_OfAPatternBeyondTheProgramLimit_ReportsTheLimit()
    {
        var tree = XPathRegex.Tree("(a{1,2000}){1,2000}");

        var error = RegexOperations.TryCreate(tree, out var operations);

        await Assert.That(error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(operations).IsNull();
    }

    [Test]
    public async Task TryCreate_OfAPatternWithTooManyGroupsForItsSize_ReportsTheLimit()
    {
        var tree = XPathRegex.Tree(RegexOps.Repeat("()", 1_000));

        var error = RegexOperations.TryCreate(tree, out var operations);

        await Assert.That(error).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(operations).IsNull();
    }

    [Test]
    public async Task TryCreate_OfAPatternWithManyGroups_Works()
    {
        var operations = RegexOps.Create(RegexOps.Repeat("(a)", 500));

        await Assert.That(operations.GroupCount).IsEqualTo(500);
        await Assert.That(RegexOps.Matches(operations, RegexOps.Repeat("a", 500)).Length).IsGreaterThan(2_000);
    }

    [Test]
    [Arguments("a", false)]
    [Arguments("a*", true)]
    [Arguments("(a)?", true)]
    [Arguments("^", true)]
    [Arguments("", true)]
    [Arguments("a|", true)]
    [Arguments("(a)?\\1", true)]
    [Arguments("(a)\\1", false)]
    [Arguments("a+", false)]
    public async Task MatchesEmptyString_ReportsWhetherThePatternMatchesTheEmptyString(string pattern, bool expected) =>
        await Assert.That(RegexOps.Create(pattern).MatchesEmptyString).IsEqualTo(expected);
}
