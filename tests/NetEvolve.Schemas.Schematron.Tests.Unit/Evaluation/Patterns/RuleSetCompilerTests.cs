namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>Compiling the rules of a pattern: contexts through the matcher in the pattern scope, assertions in the rule scope.</summary>
public class RuleSetCompilerTests
{
    [Test]
    public async Task Compile_ContextsGoThroughTheMatcher_InThePatternScope()
    {
        var schema = Schema(
            PatternWith("p", [Rule("r1", "a", Fails("x")), Rule("r2", "b", Fails("y"))], [Let("v", "true")])
        );
        using var h = new PatternHarness(schema, E("a"));

        await Assert.That(h.Matcher.Compiled.Count).IsEqualTo(2);
        await Assert.That(h.Matcher.Compiled[0].Use).IsEqualTo(PatternUse.RuleContext);
        await Assert.That(h.Matcher.Compiled[0].Scope).IsSameReferenceAs(h.Plan.Patterns[0].Scope);
        await Assert.That(h.Matcher.Compiled[1].Scope).IsSameReferenceAs(h.Plan.Patterns[0].Scope);
    }

    [Test]
    public async Task Compile_KeepsTheRulesInDocumentOrder_WithTheirAssertions()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Fails("x"), Reports("y")), Rule("r2", "b", Holds())));
        using var h = new PatternHarness(schema, E("a"));

        var set = h.Sets[0];

        await Assert.That(set.Id).IsEqualTo("p");
        await Assert.That(set.IsGroup).IsFalse();
        await Assert.That(set.Rules.Length).IsEqualTo(2);
        await Assert.That(set.Rules[0].Id).IsEqualTo("r1");
        await Assert.That(set.Rules[0].ContextText).IsEqualTo("a");
        await Assert.That(set.Rules[0].Assertions.Length).IsEqualTo(2);
        await Assert.That(set.Rules[0].Assertions[1].IsReport).IsTrue();
        await Assert.That(set.Rules[1].Assertions.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_Group_IsMarkedAsAGroup()
    {
        var schema = Schema(Group("g", Rule("r1", "a", Holds())));
        using var h = new PatternHarness(schema, E("a"));

        await Assert.That(h.Sets[0].IsGroup).IsTrue();
    }

    [Test]
    public async Task Compile_AssertionOrdinals_CountEveryPatternOfTheSchema_NotTheActiveOnes()
    {
        var abstractPattern = PatternWith("abs", [Rule("rx", "a", Holds())], isAbstract: true);
        var schema = Schema(
            abstractPattern,
            Pattern("p1", Rule(null, "a", Holds())),
            Pattern("p2", Rule(null, "b", Holds()))
        );
        using var h = new PatternHarness(schema, E("a"));

        // The abstract definition is not counted; the ordinal does not depend on which patterns a phase makes active.
        await Assert.That(h.Sets[0].Rules[0].Assertions[0].Constraint).IsEqualTo("#p1.r1.a1");
        await Assert.That(h.Sets[1].Rules[0].Assertions[0].Constraint).IsEqualTo("#p2.r1.a1");
    }

    [Test]
    public async Task Compile_AssertionOrdinals_CountRulesAndAssertionsWithinTheirParent()
    {
        var schema = Schema(Pattern("p", Rule(null, "a", Holds()), Rule(null, "b", Holds(), Fails("x"))));
        using var h = new PatternHarness(schema, E("a"));

        await Assert.That(h.Sets[0].Rules[1].Assertions[1].Constraint).IsEqualTo("#p1.r2.a2");
    }

    [Test]
    public async Task Compile_ReportsEveryProblem_NotJustTheFirst()
    {
        var schema = Schema(Pattern("p", Rule("r1", "invalid", Holds()), Rule("r2", "invalid", Holds("invalid"))));
        var problems = new FakeProblemSink();
        var binding = new AssertionTestBinding();
        var matcher = new ScriptedMatcher([]);
        var plan = PlanOf(schema, binding);

        var ok = RuleSetCompiler.TryCompile(schema, plan.Patterns[0], matcher, binding, problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(3);
    }

    [Test]
    public async Task CompiledPattern_ReferencedSlots_IsEmptyByDefault()
    {
        var pattern = new CompiledPattern(QueryNodeKinds.Element, []);
        var count = pattern.ReferencedSlots.Length;

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Compile_NullArguments_Throw()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Holds())));
        var problems = new FakeProblemSink();
        var binding = new AssertionTestBinding();
        var matcher = new ScriptedMatcher([]);
        var scope = PlanOf(schema, binding).Patterns[0];

        await Assert
            .That(() => RuleSetCompiler.TryCompile(null!, scope, matcher, binding, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => RuleSetCompiler.TryCompile(schema, null!, matcher, binding, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => RuleSetCompiler.TryCompile(schema, scope, null!, binding, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => RuleSetCompiler.TryCompile(schema, scope, matcher, null!, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => RuleSetCompiler.TryCompile(schema, scope, matcher, binding, null!, out _))
            .Throws<ArgumentNullException>();
    }

    private static NetEvolve.Schemas.Schematron.Scopes.ScopePlan PlanOf(
        NetEvolve.Schemas.Schematron.Model.SchematronSchema schema,
        AssertionTestBinding binding
    )
    {
        var problems = new FakeProblemSink();
        _ = NetEvolve.Schemas.Schematron.Phases.PhaseSelector.TryResolve(schema, "#ALL", problems, out var selection);
        _ = NetEvolve.Schemas.Schematron.Scopes.ScopePlanner.TryPlan(
            schema,
            selection,
            binding,
            problems,
            out var plan
        );
        return plan;
    }
}
