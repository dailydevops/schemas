namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations.AnnotationModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>
/// A <c>flag</c>, <c>role</c>, or <c>severity</c> that is exactly <c>$name</c> is resolved for the fired assertion (decided by #106 Q3;
/// design #150 EP-6, examples E23 to E28).
/// </summary>
public class DynamicAnnotationTests
{
    [Test]
    public async Task Severity_VariableReference_ResolvedPerNode()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("s")], Failing(severity: "$s"))));
        using var h = new PatternHarness(schema, E("r", E("a"), E("a")));
        h.Matcher.OnMatch = calls => h.Binding.Texts["$s"] = "v" + calls;

        _ = h.Evaluate();

        // E23: r is the first matched node, so the two a elements are the second and third.
        await Assert.That(h.Log.Details.Count).IsEqualTo(2);
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo("v2");
        await Assert.That(h.Log.Details[1].Severity).IsEqualTo("v3");
        await Assert.That(h.Collector.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Role_VariableNotReadWhenAssertionDoesNotFire()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("bad")], Passing(role: "$bad"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$bad"] = "never";

        _ = h.Evaluate();

        // E24: the attribute is read only when the assertion fires.
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["true"]);
        await Assert.That(h.Collector.Count).IsEqualTo(0);
        await Assert.That(h.Calls).DoesNotContain("bind:0");
    }

    [Test]
    public async Task Evaluation_IsInTheOrderTestThenAttributes()
    {
        var schema = Schema(
            Pattern("p", RuleWith("r", "a", null, null, [Let("r"), Let("f"), Let("s")], Failing("$r", ["$f"], "$s")))
        );
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$r"] = "x";
        h.Binding.Texts["$f"] = "y";
        h.Binding.Texts["$s"] = "z";

        _ = h.Evaluate();

        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["false", "$r", "$f", "$s"]);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo("x");
        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("y");
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo("z");
    }

    [Test]
    public async Task PatternRole_DynamicVariable_IgnoresRuleLetOfSameName()
    {
        var schema = Schema(PatternWith("p", "$r", [Let("r")], RuleWith("r1", "a", null, null, [Let("r")], Failing())));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$r"] = "p";

        _ = h.Evaluate();

        // E24b: the role is compiled in the scope of the pattern, where the variable of the rule is not visible.
        var (_, kind, scope) = h.Binding.Compiled.Find(c => string.Equals(c.Text, "$r", StringComparison.Ordinal));
        await Assert.That(kind).IsEqualTo(QueryExpressionKind.Value);
        await Assert.That(scope).IsSameReferenceAs(h.Plan.Patterns[0].Scope);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo("p");
    }

    [Test]
    public async Task RuleAndAssertionRole_AreCompiledInTheScopeOfTheRule()
    {
        var schema = Schema(
            Pattern("p", RuleWith("r1", "a", "$r", null, [Let("r"), Let("f")], Failing(role: "$r", flags: ["$f"])))
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        var expected = h.Plan.Patterns[0].Rules[0].Scope;
        await Assert.That(h.Binding.Compiled.FindAll(c => c.Text is "$r" or "$f").Count).IsEqualTo(3);
        foreach (var (_, kind, scope) in h.Binding.Compiled.FindAll(c => c.Text is "$r" or "$f"))
        {
            await Assert.That(scope).IsSameReferenceAs(expected);
            await Assert.That(kind).IsEqualTo(QueryExpressionKind.Value);
        }
    }

    [Test]
    public async Task Role_MultiItemValue_ReportsEvalFailed_FindingStillProduced()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("r")], Failing(role: "$r"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Items["$r"] = [XdmItem.FromString("x"), XdmItem.FromString("y")];

        _ = h.Evaluate();

        // E26
        await Assert.That(h.Log.Details.Count).IsEqualTo(1);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Log.Errors[0].Code).IsEqualTo("NES3301");
        await Assert.That(h.Log.Errors[0].Message).Contains("role");
        await Assert.That(h.Collector.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Flag_EmptyValue_ReportsEvalFailed_FindingStillProduced()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("r")], Failing(flags: ["$r"]))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        // E27: the variable yields the empty sequence.
        await Assert.That(h.Log.Details.Count).IsEqualTo(1);
        await Assert.That(h.Log.Details[0].Flags).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Log.Errors[0].Code).IsEqualTo("NES3301");
        await Assert.That(h.Log.Errors[0].Message).Contains("flag");
    }

    [Test]
    [Arguments(" ")]
    [Arguments(" \t\r\n ")]
    public async Task Flag_WhitespaceOnlyValue_ReportsEvalFailed_FindingStillProduced(string value)
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("f")], Failing(flags: ["$f"]))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$f"] = value;

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Flags).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Log.Errors[0].Code).IsEqualTo("NES3301");
    }

    [Test]
    public async Task Flag_DynamicValue_IsSplitIntoTokensJoinedWithOneSpace()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("f")], Failing(flags: ["$f"]))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$f"] = "  a \t b\r\nc  ";

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("a b c");
        await Assert.That(h.Collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Role_DynamicValue_IsCopiedWithoutNormalizing()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("r")], Failing(role: "$r"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$r"] = " a  b ";

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo(" a  b ");
    }

    [Test]
    public async Task Severity_NonStringSingleValue_IsConverted()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("n")], Failing(severity: "$n"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Items["$n"] = [XdmItem.FromInteger(3)];

        _ = h.Evaluate();

        // E28
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo("3");
        await Assert.That(h.Log.Errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Role_NodeValue_IsAtomized()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("n")], Failing(role: "$n"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Items["$n"] = [AssertionHarness.NodeItem("item1")];

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo("1");
    }

    [Test]
    public async Task Role_EvaluationFailure_ReportsTheProblem_FindingStillProduced()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("r")], Failing(role: "$r"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$r"] = "partial";
        _ = h.Binding.FailAfterPush.Add("$r");

        _ = h.Evaluate();

        await Assert.That(h.Log.Details.Count).IsEqualTo(1);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Log.Errors[0].Code).IsEqualTo("NES3301");
        await Assert.That(h.Log.Errors[0].Message).IsEqualTo("[r#a1] bad expression");
    }

    [Test]
    public async Task Role_ValueWithoutTextForm_ReportsEvalFailed_FindingStillProduced()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("r")], Failing(role: "$r"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Items["$r"] = [XdmItem.FromDouble(1.5)];

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Log.Errors[0].Message).Contains("text form");
    }

    [Test]
    public async Task Role_ValueLongerThanTheLimit_ReportsEvalFailed_FindingStillProduced()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("r")], Failing(role: "$r"))));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$r"] = new string('x', 1001);

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Log.Errors[0].Message).Contains("longer");
    }

    [Test]
    public async Task Role_FailedVariable_OmitsTheAttribute_AndAddsNoSecondIssue()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("v", "bad")], Failing(role: "$v"))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        // The binder reported the variable once; the attribute is only omitted.
        await Assert.That(h.Log.Details.Count).IsEqualTo(1);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Collector.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ErrorMessage_NamesTheAssertionInBrackets()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", null, null, [Let("r")], Failing(role: "$r"))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Errors[0].Message).StartsWith("[r#a1] ");
    }

    [Test]
    public async Task RuleLevelDynamicRole_ResolvedOncePerFiring()
    {
        var schema = Schema(
            Pattern("p", RuleWith("r", "a", "$r", null, [Let("r")], Failing(message: "one"), Failing(message: "two")))
        );
        using var h = new PatternHarness(schema, E("r", E("a"), E("a")));
        h.Matcher.OnMatch = calls => h.Binding.Texts["$r"] = "v" + calls;

        _ = h.Evaluate();

        await Assert.That(h.Log.Details.Count).IsEqualTo(4);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo("v2");
        await Assert.That(h.Log.Details[1].Role).IsEqualTo("v2");
        await Assert.That(h.Log.Details[2].Role).IsEqualTo("v3");
        await Assert.That(h.Log.Details[3].Role).IsEqualTo("v3");
        await Assert
            .That(h.Binding.Evaluated.FindAll(e => string.Equals(e, "$r", StringComparison.Ordinal)).Count)
            .IsEqualTo(2);
    }

    [Test]
    public async Task PatternLevelDynamicRole_ResolvedOncePerPatternPass()
    {
        var schema = Schema(
            PatternWith("p", "$r", [Let("r")], Rule("r1", "a", Failing(message: "one"), Failing(message: "two")))
        );
        using var h = new PatternHarness(schema, E("r", E("a"), E("a")));
        h.Binding.Texts["$r"] = "pat";

        _ = h.Evaluate();

        await Assert.That(h.Log.Details.Count).IsEqualTo(4);
        await Assert
            .That(h.Log.Details.TrueForAll(f => string.Equals(f.Role, "pat", StringComparison.Ordinal)))
            .IsTrue();
        await Assert
            .That(h.Binding.Evaluated.FindAll(e => string.Equals(e, "$r", StringComparison.Ordinal)).Count)
            .IsEqualTo(1);
    }

    [Test]
    public async Task PatternLevelDynamicRole_IsResolvedAgainForTheNextPattern()
    {
        var schema = Schema(
            PatternWith("p1", "$r", [Let("r")], Rule("r1", "a", Failing())),
            PatternWith("p2", "$r", [Let("r")], Rule("r2", "a", Failing()))
        );
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$r"] = "x";

        _ = h.Evaluate();

        await Assert.That(h.Log.Details.Count).IsEqualTo(2);
        await Assert
            .That(h.Binding.Evaluated.FindAll(e => string.Equals(e, "$r", StringComparison.Ordinal)).Count)
            .IsEqualTo(2);
    }

    [Test]
    public async Task GroupLevelDynamicRole_IsResolvedInTheScopeOfTheGroup()
    {
        var schema = Schema(GroupWith("g", "$r", [Let("r")], Rule("r1", "a", Failing())));
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$r"] = "grp";

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo("grp");
        await Assert
            .That(h.Binding.Compiled.Find(c => string.Equals(c.Text, "$r", StringComparison.Ordinal)).Scope)
            .IsSameReferenceAs(h.Plan.Patterns[0].Scope);
    }

    [Test]
    public async Task RuleLevelFailure_IsReportedPerFinding_AndDoesNotFallBackToThePattern()
    {
        var schema = Schema(
            PatternWith(
                "p",
                "pattern-role",
                null,
                RuleWith("r", "a", "$r", null, [Let("r")], Failing(message: "one"), Failing(message: "two"))
            )
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        // The rule role fails (empty sequence); the attribute of the rule wins, so it is omitted and the pattern role is not used.
        await Assert.That(h.Log.Details.Count).IsEqualTo(2);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Details[1].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(2);
    }

    [Test]
    public async Task FiredRuleAndActivePattern_LeaveADynamicValueEmpty_AndNeverEvaluateIt()
    {
        var schema = Schema(
            PatternWith("p", "$pr", [Let("pr")], RuleWith("r", "a", "$rr", ["$rf"], [Let("rr"), Let("rf")], Passing()))
        );
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$pr"] = "x";
        h.Binding.Texts["$rr"] = "y";
        h.Binding.Texts["$rf"] = "z";

        _ = h.Evaluate();

        // Decision: the events carry static values only, so nothing is evaluated when no assertion fires.
        await Assert.That(h.Log.PatternRoles[0]).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Rules[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Rules[0].Flags).IsEqualTo(string.Empty);
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["true"]);
    }

    [Test]
    [Arguments("assertion")]
    [Arguments("rule")]
    [Arguments("pattern")]
    public async Task Role_UndeclaredVariable_FailsCompile_WithVariableUndefined(string level)
    {
        var schema = Schema(
            PatternWith(
                "p",
                level is "pattern" ? "$nope" : null,
                null,
                RuleWith(
                    "r",
                    "a",
                    level is "rule" ? "$nope" : null,
                    null,
                    null,
                    Failing(role: level is "assertion" ? "$nope" : null)
                )
            )
        );

        var (ok, problems) = Compile(schema);

        // E25: there is no pipeline yet, so "throws at Build" is pinned as a failed compile with the undefined-variable code.
        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
    }

    [Test]
    [Arguments("assertion")]
    [Arguments("rule")]
    [Arguments("pattern")]
    public async Task Flag_AndSeverity_UndeclaredVariable_FailsCompile_AndEveryProblemIsReported(string level)
    {
        var schema = Schema(
            PatternWith(
                "p",
                level is "pattern" ? "$a" : null,
                null,
                RuleWith(
                    "r",
                    "a",
                    level is "rule" ? "$b" : null,
                    level is "rule" ? ["$c"] : null,
                    null,
                    Failing(
                        role: level is "assertion" ? "$d" : null,
                        flags: level is "assertion" ? ["$e"] : null,
                        severity: level is "assertion" ? "$f" : null
                    )
                )
            )
        );

        var (ok, problems) = Compile(schema);

        await Assert.That(ok).IsFalse();
        await Assert
            .That(problems.Count)
            .IsEqualTo(
                level is "assertion" ? 3
                : level is "rule" ? 2
                : 1
            );
    }

    [Test]
    public async Task Role_ExpressionThatDoesNotCompile_ReportsTheBindingProblem()
    {
        var schema = Schema(Pattern("p", Rule("r", "a", Failing(role: "$invalid"))));

        var (ok, problems) = Compile(schema);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }

    private static (bool Ok, List<SchematronProblem> Problems) Compile(SchematronSchema schema)
    {
        var binding = new AssertionTestBinding();
        var matcher = new ScriptedMatcher([]);
        var problems = new FakeProblemSink();
        var ok = PhaseSelector.TryResolve(schema, "#ALL", problems, out var selection);
        ok &= ScopePlanner.TryPlan(schema, selection, binding, problems, out var plan);
        foreach (var scope in plan.Patterns)
        {
            ok &= RuleSetCompiler.TryCompile(schema, scope, matcher, binding, problems, out _);
        }

        return (ok, problems.Problems);
    }
}
