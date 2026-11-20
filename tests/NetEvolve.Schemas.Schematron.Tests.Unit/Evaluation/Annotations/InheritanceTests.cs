namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations;

using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations.AnnotationModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>
/// An assertion without a value takes it from its rule, then from its pattern, per attribute and never merged (design #137 AP-11,
/// #150 EP-6): <c>flag</c> from the assertion, then the rule; <c>role</c> from the assertion, the rule, then the pattern or group;
/// <c>severity</c> from the assertion only.
/// </summary>
public class InheritanceTests
{
    [Test]
    [Arguments("a", "r", "p", "a")]
    [Arguments(null, "r", "p", "r")]
    [Arguments(null, null, "p", "p")]
    [Arguments(null, null, null, "")]
    [Arguments("a", null, "p", "a")]
    [Arguments("a", "r", null, "a")]
    [Arguments(null, "r", null, "r")]
    public async Task Role_FollowsAssertionThenRuleThenPattern(
        string? assertionRole,
        string? ruleRole,
        string? patternRole,
        string expected
    )
    {
        var schema = Schema(
            PatternWith("p", patternRole, null, RuleWith("r", "a", ruleRole, null, null, Failing(role: assertionRole)))
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo(expected);
    }

    [Test]
    public async Task Role_OfAGroup_IsInheritedLikeThatOfAPattern()
    {
        var schema = Schema(GroupWith("g", "group-role", null, Rule("r", "a", Failing())));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo("group-role");
    }

    [Test]
    public async Task Role_EmptyAttributeOnTheAssertion_StillWinsOverTheRule()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", "rule-role", null, null, Failing(role: ""))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Role_DynamicOnTheAssertion_WinsOverStaticValuesOfRuleAndPattern()
    {
        var schema = Schema(
            PatternWith(
                "p",
                "pattern-role",
                null,
                RuleWith("r", "a", "rule-role", null, [Let("v")], Failing(role: "$v"))
            )
        );
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$v"] = "dynamic";

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo("dynamic");
    }

    [Test]
    public async Task Role_StaticOnTheAssertion_NeverEvaluatesTheDynamicValuesAbove()
    {
        var schema = Schema(
            PatternWith("p", "$p", [Let("p")], RuleWith("r", "a", "$r", null, [Let("r")], Failing(role: "own")))
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo("own");
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["false"]);
    }

    [Test]
    public async Task Flags_AssertionWinsAndIsNeverMergedWithTheRule()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, ["r1", "r2"], null, Failing(flags: ["a1"]))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("a1");
    }

    [Test]
    public async Task Flags_ListsExactlyTheFlagsThatWereSet()
    {
        var schema = Schema(
            Pattern(
                "p",
                RuleWith(
                    "r",
                    "a",
                    null,
                    ["rule-flag"],
                    null,
                    Failing(flags: ["a"], message: "own"),
                    Failing(message: "inherited"),
                    Failing(flags: ["b", "c"], message: "two")
                ),
                RuleWith("r2", "b", null, null, null, Failing(message: "none"))
            )
        );
        using var h = new PatternHarness(schema, E("r", E("a"), E("b")));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details.Count).IsEqualTo(4);
        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("a");
        await Assert.That(h.Log.Details[1].Flags).IsEqualTo("rule-flag");
        await Assert.That(h.Log.Details[2].Flags).IsEqualTo("b c");
        await Assert.That(h.Log.Details[3].Flags).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Flags_DynamicOnTheRule_AreInheritedByTheAssertion()
    {
        var schema = Schema(
            Pattern("p", RuleWith("r", "a", null, ["$f"], [Let("f")], Failing(), Failing(flags: ["own"])))
        );
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$f"] = "x  y";

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("x y");
        await Assert.That(h.Log.Details[1].Flags).IsEqualTo("own");
    }

    [Test]
    public async Task Severity_IsNotInherited_AndNeverLeaksFromASibling()
    {
        var schema = Schema(
            Pattern("p", Rule("r", "a", Failing(severity: "high", message: "one"), Failing(message: "two")))
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Severity).IsEqualTo("high");
        await Assert.That(h.Log.Details[1].Severity).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Values_DoNotLeakFromOneFiringToTheNext()
    {
        var schema = Schema(
            Pattern(
                "p",
                RuleWith("r1", "a", "first", ["f"], null, Failing()),
                RuleWith("r2", "b", null, null, null, Failing())
            )
        );
        using var h = new PatternHarness(schema, E("r", E("a"), E("b")));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo("first");
        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("f");
        await Assert.That(h.Log.Details[1].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Details[1].Flags).IsEqualTo(string.Empty);
    }
}
