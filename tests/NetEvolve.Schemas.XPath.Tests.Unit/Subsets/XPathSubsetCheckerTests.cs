namespace NetEvolve.Schemas.XPath.Tests.Unit.Subsets;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Subsets;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Accept and reject tables transcribed from XML Schema Part 1, 3.11.6: Selector ::= Path ('|' Path)*,
/// Path ::= ('.//')? Step ('/' Step)*, Step ::= '.' | NameTest; Field adds a final '@' NameTest step.
/// </summary>
public class XPathSubsetCheckerTests
{
    [Test]
    [Arguments(".")]
    [Arguments("a")]
    [Arguments("*")]
    [Arguments("p:a")]
    [Arguments("p:*")]
    [Arguments("a/b")]
    [Arguments("a/./b")]
    [Arguments("./a")]
    [Arguments(".//a")]
    [Arguments(".//a/b")]
    [Arguments(".//.")]
    [Arguments("a/*/p:*")]
    [Arguments("a|b")]
    [Arguments(".//a | b/c | p:*")]
    public async Task Selector_Accepts_TheSelectorGrammar(string text) =>
        await Assert.That(Check(XPathSubsetChecker.Selector, text, out _)).IsTrue();

    [Test]
    [Arguments("/a", "absolute-path", 0, 1)]
    [Arguments("//a", "absolute-path", 0, 2)]
    [Arguments("a//b", "descendant-position", 1, 2)]
    [Arguments(".//a//b", "descendant-position", 4, 2)]
    [Arguments("././/a", "descendant-position", 3, 2)]
    [Arguments("..", "axis", 0, 2)]
    [Arguments("descendant::a", "axis", 0, 13)]
    [Arguments("a/descendant-or-self::node()/b", "axis", 2, 26)]
    [Arguments("@a", "attribute-step", 0, 2)]
    [Arguments("a/@b", "attribute-step", 2, 2)]
    [Arguments("a[1]", "predicate", 2, 1)]
    [Arguments("a[b]/c", "predicate", 2, 1)]
    [Arguments("text()", "node-test", 0, 6)]
    [Arguments("a/node()", "node-test", 2, 6)]
    [Arguments("*:a", "name-test", 0, 3)]
    [Arguments("(a)", "step-kind", 0, 3)]
    [Arguments("a/(b)", "step-kind", 2, 3)]
    [Arguments("1", "step-kind", 0, 1)]
    [Arguments("a + b", "step-kind", 0, 5)]
    [Arguments("a intersect b", "set-operator", 1, 11)]
    [Arguments("a except b", "set-operator", 1, 8)]
    [Arguments("a|/b", "absolute-path", 2, 1)]
    public async Task Selector_Rejects_AndNamesTheViolatedRule(string text, string rule, int offset, int length)
    {
        var accepted = Check(XPathSubsetChecker.Selector, text, out var violation);

        await Assert.That(accepted).IsFalse();
        await AssertViolation(violation, rule, offset, length);
    }

    [Test]
    [Arguments(".")]
    [Arguments("a")]
    [Arguments("a/b")]
    [Arguments(".//a")]
    [Arguments("@a")]
    [Arguments("@*")]
    [Arguments("@p:*")]
    [Arguments("@p:a")]
    [Arguments("a/@b")]
    [Arguments("./@a")]
    [Arguments(".//@a")]
    [Arguments(".//a/b/@c")]
    [Arguments("a|@b")]
    [Arguments(".//a/@b | c | @d")]
    public async Task Field_Accepts_TheFieldGrammar(string text) =>
        await Assert.That(Check(XPathSubsetChecker.Field, text, out _)).IsTrue();

    [Test]
    [Arguments("@a/b", "attribute-step-position", 0, 2)]
    [Arguments("@a/@b", "attribute-step-position", 0, 2)]
    [Arguments("a/@b/c", "attribute-step-position", 2, 2)]
    [Arguments("attribute::a/b", "attribute-step-position", 0, 12)]
    [Arguments("@a//b", "attribute-step-position", 0, 2)]
    [Arguments("@a[1]", "predicate", 3, 1)]
    [Arguments("a[@b]", "predicate", 2, 2)]
    [Arguments("@node()", "node-test", 0, 7)]
    [Arguments("@*:a", "name-test", 0, 4)]
    [Arguments("/a", "absolute-path", 0, 1)]
    [Arguments("a//@b", "descendant-position", 1, 2)]
    [Arguments("..", "axis", 0, 2)]
    [Arguments("following::a", "axis", 0, 12)]
    [Arguments("a intersect @b", "set-operator", 1, 11)]
    [Arguments("@a|(b)", "step-kind", 3, 3)]
    public async Task Field_Rejects_AndNamesTheViolatedRule(string text, string rule, int offset, int length)
    {
        var accepted = Check(XPathSubsetChecker.Field, text, out var violation);

        await Assert.That(accepted).IsFalse();
        await AssertViolation(violation, rule, offset, length);
    }

    [Test]
    public async Task Violation_PointsAtTheOffendingStep_IgnoringLeadingWhitespace()
    {
        var accepted = Check(XPathSubsetChecker.Selector, "  a / @b", out var violation);

        await Assert.That(accepted).IsFalse();
        await AssertViolation(violation, "attribute-step", 6, 2);
    }

    [Test]
    public async Task Violation_IsReportedAtTheFirstOffendingNode()
    {
        _ = Check(XPathSubsetChecker.Selector, "a/@b/(c)", out var violation);

        await AssertViolation(violation, "attribute-step", 2, 2);
    }

    [Test]
    public async Task Profile_WithAnExtraRuleFromAnotherAssembly_AppliesTheRuleAndKeepsTheBuiltInOnes()
    {
        var extended = XPathSubsetChecker.Selector.With(new NoWildcardRule());

        var plainAccepts = Check(XPathSubsetChecker.Selector, "a/*", out _);
        var extendedAccepts = Check(extended, "a/*", out var wildcard);
        var extendedKeepsBase = Check(extended, "/a", out var absolute);
        var extendedAcceptsRest = Check(extended, ".//a/b", out _);

        await Assert.That(plainAccepts).IsTrue();
        await Assert.That(extendedAccepts).IsFalse();
        await AssertViolation(wildcard, "no-wildcard", 2, 1);
        await Assert.That(extendedKeepsBase).IsFalse();
        await AssertViolation(absolute, "absolute-path", 0, 1);
        await Assert.That(extendedAcceptsRest).IsTrue();
    }

    [Test]
    public async Task With_DoesNotChangeTheProfileItWasCalledOn()
    {
        _ = XPathSubsetChecker.Selector.With(new NoWildcardRule());

        await Assert.That(Check(XPathSubsetChecker.Selector, "a/*", out _)).IsTrue();
    }

    [Test]
    public void Accepts_AnAcceptedExpression_AllocatesNothing()
    {
        var selector = Parse(".//a/b | c/*");
        var field = Parse(".//a/b/@c | d");

        // Recorded bound: the walk over the tree allocates no bytes.
        AllocationAssert.AtMost(
            0,
            () =>
            {
                _ = XPathSubsetChecker.Selector.Accepts(selector, out _);
                _ = XPathSubsetChecker.Field.Accepts(field, out _);
            }
        );
    }

    private static bool Check(XPathSubsetChecker profile, string text, out XPathDiagnostic violation) =>
        profile.Accepts(Parse(text), out violation);

    private static Expression Parse(string text) =>
        XPathParser.TryParse(text, out var expression, out var diagnostic)
            ? expression!
            : throw new InvalidOperationException($"'{text}' does not parse: {diagnostic.Message}");

    private static async Task AssertViolation(XPathDiagnostic violation, string rule, int offset, int length)
    {
        await Assert.That(violation.QName.LocalName).IsEqualTo(rule);
        await Assert.That(violation.Kind).IsEqualTo(XPathErrorKind.Static);
        await Assert.That(violation.Offset).IsEqualTo(offset);
        await Assert.That(violation.Length).IsEqualTo(length);
    }

    private sealed class NoWildcardRule : IXPathSubsetRule
    {
        public string Name => "no-wildcard";

        public string Description => "The wildcard name test is not allowed here.";

        public bool IsViolatedBy(in XPathSubsetSite site, out SourceSpan location)
        {
            if (
                site.Kind == XPathSubsetSiteKind.Step
                && site.Node is AxisStep { Test: NameTest { Kind: NameTestKind.AnyName } test }
            )
            {
                location = test.Location;
                return true;
            }

            location = default;
            return false;
        }
    }
}
