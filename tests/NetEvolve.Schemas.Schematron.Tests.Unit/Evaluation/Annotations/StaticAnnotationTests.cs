namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations;

using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations.AnnotationModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>A static <c>flag</c>, <c>role</c>, or <c>severity</c> is copied to the finding and to the events (design #137 AP-11, Q4, Q9).</summary>
public class StaticAnnotationTests
{
    [Test]
    public async Task Assert_StaticRoleFlagAndSeverity_AppearOnTheFinding()
    {
        var schema = Schema(
            Pattern("p", Rule("r", "a", Failing(role: "error", flags: ["f1", "f2"], severity: "fatal")))
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details.Count).IsEqualTo(1);
        await Assert.That(h.Log.Details[0].Role).IsEqualTo("error");
        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("f1 f2");
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo("fatal");
    }

    [Test]
    public async Task Report_StaticRoleFlagAndSeverity_AppearOnTheFinding()
    {
        var schema = Schema(Pattern("p", Rule("r", "a", Reporting(role: "info", flags: ["x"], severity: "low"))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].IsReport).IsTrue();
        await Assert.That(h.Log.Details[0].Role).IsEqualTo("info");
        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("x");
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo("low");
    }

    [Test]
    public async Task WithoutAttributes_TheFindingCarriesEmptyValues()
    {
        var schema = Schema(Pattern("p", Rule("r", "a", Failing())));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Details[0].Flags).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task StaticValues_AreNeverCompiledOrEvaluated()
    {
        var schema = Schema(
            Pattern(
                "p",
                RuleWith("r", "a", "rr", ["rf"], null, Failing(role: "error", flags: ["f"], severity: "fatal"))
            )
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["false"]);
        await Assert.That(h.Binding.Compiled.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("$")]
    [Arguments("$a b")]
    [Arguments("$a+$b")]
    [Arguments("a$b")]
    [Arguments("{$a}")]
    public async Task Role_ThatIsNotAWholeVariableReference_IsCopiedAsWritten(string role)
    {
        var schema = Schema(Pattern("p", Rule("r", "a", Failing(role: role))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Role).IsEqualTo(role);
        await Assert.That(h.Binding.Compiled.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Flags_WithSeveralTokensThatStartWithADollar_AreStaticTokens()
    {
        var schema = Schema(Pattern("p", Rule("r", "a", Failing(flags: ["$a", "$b"]))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Flags).IsEqualTo("$a $b");
        await Assert.That(h.Binding.Compiled.Count).IsEqualTo(1);
    }

    [Test]
    public async Task FiredRule_CarriesTheStaticRoleAndFlagsOfTheRule()
    {
        var schema = Schema(Pattern("p", RuleWith("r", "a", "rule-role", ["x", "y"], null, Failing())));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Rules[0]).IsEqualTo(("r", "a", "rule-role", "x y", string.Empty));
    }

    [Test]
    public async Task ActivePattern_CarriesTheStaticRoleOfThePatternAndOfTheGroup()
    {
        var schema = Schema(
            PatternWith("p", "pattern-role", null, Rule("r1", "a", Holds())),
            GroupWith("g", "group-role", null, Rule("r2", "a", Holds()))
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.PatternRoles).IsEquivalentTo(["pattern-role", "group-role"]);
    }
}
