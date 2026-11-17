namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Contracts;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Regex;
using NetEvolve.Schemas.Xml.Tests.Unit.Regex.Contracts.Fakes;

public class RegexContractTests
{
    private static RegexNode[] OneOfEachKind() =>
        [
            RegexNode.Empty(),
            RegexNode.Char('a'),
            RegexNode.Set(0),
            RegexNode.Concat(1),
            RegexNode.Alternation(1),
            RegexNode.Repeat(1, 0, RegexNode.Unbounded, lazy: true),
            RegexNode.Group(1, RegexNode.NoGroup),
            RegexNode.AnyChar(),
            RegexNode.BackReference(1),
            RegexNode.Anchor(RegexAnchor.End),
        ];

    [Test]
    public async Task NodeKinds_AreTheTenOfTheDesign()
    {
        string[] expected =
        [
            "Empty",
            "Char",
            "Set",
            "Concat",
            "Alternation",
            "Repeat",
            "Group",
            "AnyChar",
            "BackReference",
            "Anchor",
        ];

        await Assert.That(Enum.GetNames<RegexNodeKind>()).IsEquivalentTo(expected);
    }

    [Test]
    public async Task EveryNodeKind_CanBeBuiltAndVisited()
    {
        var seen = new List<RegexNodeKind>();
        var visitor = new RecordingNodeVisitor(seen);
        foreach (var node in OneOfEachKind())
        {
            node.Accept(ref visitor);
        }

        await Assert.That(seen).IsEquivalentTo(Enum.GetValues<RegexNodeKind>());
        await Assert.That(seen.Count).IsEqualTo(10);
    }

    [Test]
    public async Task NodeFactories_CarryTheirFields()
    {
        var repeat = RegexNode.Repeat(4, 2, 5, lazy: true);
        var group = RegexNode.Group(3, 7).WithNextSibling(9);

        await Assert.That(repeat.Kind).IsEqualTo(RegexNodeKind.Repeat);
        await Assert.That(repeat.FirstChild).IsEqualTo(4);
        await Assert.That(repeat.Min).IsEqualTo(2);
        await Assert.That(repeat.Max).IsEqualTo(5);
        await Assert.That(repeat.IsLazy).IsTrue();
        await Assert.That(group.Value).IsEqualTo(7);
        await Assert.That(group.FirstChild).IsEqualTo(3);
        await Assert.That(group.NextSibling).IsEqualTo(9);
        await Assert.That(RegexNode.Char(0x1F600).Value).IsEqualTo(0x1F600);
        await Assert.That(RegexNode.Empty().FirstChild).IsEqualTo(RegexNode.NoNode);
        await Assert.That(RegexNode.Empty().NextSibling).IsEqualTo(RegexNode.NoNode);
        await Assert.That(RegexNode.Group(1, RegexNode.NoGroup).Value).IsEqualTo(RegexNode.NoGroup);
        await Assert.That(RegexNode.Anchor(RegexAnchor.End).Value).IsEqualTo((int)RegexAnchor.End);
        await Assert.That(RegexNode.Repeat(1, 0, RegexNode.Unbounded, false).Max).IsEqualTo(RegexNode.Unbounded);
    }

    [Test]
    public async Task NodeFactories_RejectInvalidArguments()
    {
        await Assert.That(() => RegexNode.Char(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.Char(0x110000)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.Repeat(1, -1, 2, false)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.Repeat(1, 3, 2, false)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.Set(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.BackReference(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.Group(1, -2)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.Concat(-2)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexNode.Empty().WithNextSibling(-2)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task DefaultNode_IsEmpty() => await Assert.That(default(RegexNode).Kind).IsEqualTo(RegexNodeKind.Empty);

    [Test]
    public async Task Matcher_ReportsTheStepLimitAsAborted()
    {
        IRegexMatcher matcher = new FakeRegexMatcher("abc", RegexDialect.Xsd);
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(2);

        await Assert.That(matcher.Match("abc", limits)).IsEqualTo(RegexMatchResult.Aborted);
        await Assert.That(matcher.Match("ab", limits)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(matcher.Match("abc", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task Matcher_DialectSelectsWholeMatchOrSearch()
    {
        var xsd = new FakeRegexMatcher("b", RegexDialect.Xsd);
        var xpath = new FakeRegexMatcher("b", RegexDialect.XPath);

        await Assert.That(xsd.Dialect).IsEqualTo(RegexDialect.Xsd);
        await Assert.That(xsd.Match("abc", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.NoMatch);
        await Assert.That(xpath.Match("abc", RegexStepLimits.Default)).IsEqualTo(RegexMatchResult.Match);
    }

    [Test]
    public async Task StepLimits_DefaultsAreTheApprovedCeilings()
    {
        var limits = RegexStepLimits.Default;

        await Assert.That(limits.MaxNfaSteps).IsEqualTo(100_000_000L);
        await Assert.That(limits.MaxBacktrackSteps).IsEqualTo(1_000_000L);
    }

    [Test]
    public async Task StepLimits_WithMethodsCopyAndNeverAllowZero()
    {
        var changed = RegexStepLimits.Default.WithMaxNfaSteps(5).WithMaxBacktrackSteps(7);

        await Assert.That(changed.MaxNfaSteps).IsEqualTo(5L);
        await Assert.That(changed.MaxBacktrackSteps).IsEqualTo(7L);
        await Assert.That(RegexStepLimits.Default.MaxNfaSteps).IsEqualTo(100_000_000L);
        await Assert.That(() => RegexStepLimits.Default.WithMaxNfaSteps(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => RegexStepLimits.Default.WithMaxBacktrackSteps(0)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RegexError_HasTheStableCodesRx0001ToRx0011()
    {
        var names = new Dictionary<int, string>
        {
            [1] = "UnexpectedEnd",
            [2] = "Unbalanced",
            [3] = "InvalidQuantifier",
            [4] = "InvalidEscape",
            [5] = "UnknownCategoryOrBlock",
            [6] = "InvalidCharacterClass",
            [7] = "ConstructNotAllowed",
            [8] = "InvalidBackReference",
            [9] = "LimitExceeded",
            [10] = "InvalidFlag",
            [11] = "InvalidUtf16",
        };

        foreach (var (number, name) in names)
        {
            await Assert.That(Enum.GetName((RegexError)number)).IsEqualTo(name);
        }

        await Assert.That((int)RegexError.None).IsEqualTo(0);
        await Assert.That(Enum.GetValues<RegexError>().Length).IsEqualTo(12);
    }

    [Test]
    public async Task Matcher_SignatureAcceptsReadOnlySpanOfCharAndNeverAString()
    {
        var match = typeof(IRegexMatcher).GetMethod(nameof(IRegexMatcher.Match))!;
        var parameters = match.GetParameters();

        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(ReadOnlySpan<char>));
        await Assert.That(Array.Exists(parameters, p => p.ParameterType == typeof(string))).IsFalse();
    }
}
