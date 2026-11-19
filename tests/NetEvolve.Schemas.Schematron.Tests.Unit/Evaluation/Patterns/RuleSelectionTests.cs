namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>Per pattern the first matching rule wins for a node; in a group every matching rule fires (design #150 EP-2, examples E4 to E8).</summary>
public class RuleSelectionTests
{
    private static readonly PatternElement RWithAAndB = E("r", E("a"), E("b"));

    [Test]
    public async Task Pattern_FirstMatchWins_AcrossNodes()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Fails("x")), Rule("r2", "*", Fails("y"))));
        using var h = new PatternHarness(schema, RWithAAndB);

        _ = h.Evaluate();

        // The node a matches both rules; only r1 fires for it.
        await Assert.That(h.Log.Fired).IsEqualTo("r2,r1,r2");
        await Assert.That(h.Log.Messages).IsEqualTo("y,x,y");
    }

    [Test]
    public async Task Group_AllMatchingRulesFire()
    {
        var schema = Schema(Group("g", Rule("r1", "a", Fails("x")), Rule("r2", "*", Fails("y"))));
        using var h = new PatternHarness(schema, RWithAAndB);

        _ = h.Evaluate();

        await Assert.That(h.Log.Fired).IsEqualTo("r2,r1,r2,r2");
        await Assert.That(h.Log.Messages).IsEqualTo("y,x,y,y");
    }

    [Test]
    public async Task Pattern_StopsAtTheFirstMatch_AndNeverTriesTheLaterRulesForThatNode()
    {
        var schema = Schema(Pattern("p", Rule("r1", "*", Fails("x")), Rule("r2", "*", Fails("y"))));
        using var h = new PatternHarness(schema, E("r", E("a")));

        _ = h.Evaluate();

        // Two elements, one match call each: the second rule is never asked.
        await Assert.That(h.Matcher.MatchCalls).IsEqualTo(2);
        await Assert.That(h.Log.Fired).IsEqualTo("r1,r1");
    }

    [Test]
    public async Task Pattern_NodeThatMatchesNoRule_FiresNothing()
    {
        var schema = Schema(Pattern("p", Rule("r1", "c", Fails("x"))));
        using var h = new PatternHarness(schema, RWithAAndB);

        _ = h.Evaluate();

        await Assert.That(h.Log.Fired).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Messages).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Rule_DoesNotEvaluateLetsOrAssertions_WhenNotFirstMatch()
    {
        var schema = Schema(
            Pattern("p", Rule("r1", "*", Holds()), RuleWithLets("r2", "*", [Holds("never")], [Let("z", "true")]))
        );
        using var h = new PatternHarness(schema, E("r", E("a")));

        _ = h.Evaluate();

        await Assert.That(h.Binding.Evaluated).DoesNotContain("never");
        await Assert.That(h.Calls).DoesNotContain("bind:" + h.Plan.RuleStart);
        await Assert.That(h.Log.Fired).IsEqualTo("r1,r1");
    }

    [Test]
    public async Task Pattern_ContextError_EndsNodeForPattern()
    {
        var schema = Schema(Pattern("p", Rule("r1", "err:a", Fails("x")), Rule("r2", "a", Fails("y"))));
        using var h = new PatternHarness(schema, E("r", E("a", A("n", "1"))));

        _ = h.Evaluate();

        // E7: r2 does not fire in place of the rule whose verdict is unknown.
        await Assert.That(h.Log.Fired).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Messages).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Log.Errors[0].Code).IsEqualTo("NES3301");
        await Assert.That(h.Collector.Count).IsEqualTo(1);
        await Assert.That(h.Collector.ToResult().Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(h.Collector.ToResult().Issues[0].Constraint).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task Group_ContextError_OtherRulesStillRun()
    {
        var schema = Schema(Group("g", Rule("r1", "err:a", Fails("x")), Rule("r2", "a", Fails("y"))));
        using var h = new PatternHarness(schema, E("r", E("a", A("n", "1"))));

        _ = h.Evaluate();

        // E8: the rules of a group are independent.
        await Assert.That(h.Log.Fired).IsEqualTo("r2");
        await Assert.That(h.Log.Messages).IsEqualTo("y");
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
        await Assert.That(h.Collector.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ContextError_Event_IsPatternScoped_AndCarriesTheIssue()
    {
        var schema = Schema(Pattern("p", Rule("r1", "err:a", Fails("x"))));
        using var h = new PatternHarness(schema, E("r", E("a")));

        _ = h.Evaluate();

        await Assert.That(h.Log.Sequence).IsEqualTo("pattern:p error:NES3301");
        await Assert.That(h.Log.Errors[0].HasIssue).IsTrue();
        await Assert.That(h.Log.Errors[0].Location).IsEqualTo("/");
    }

    [Test]
    public async Task ContextError_Flooding_CappedPerRuleAndCode_ThenCounted()
    {
        var children = new object[15];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = E("a");
        }

        var schema = Schema(Pattern("p", Rule("r1", "err:a", Fails("x"))));
        using var h = new PatternHarness(schema, E("r", children));

        _ = h.Evaluate();

        // The first ten errors are reported; the other five are counted and named by one issue at the end of the pass.
        await Assert.That(h.Log.Errors.Count).IsEqualTo(11);
        await Assert.That(h.Log.Errors[10].Message).Contains("5");
        await Assert.That(h.Collector.Count).IsEqualTo(11);
    }

    [Test]
    public async Task ContextError_Flooding_IsCountedPerRule()
    {
        var children = new object[12];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = E("a");
        }

        var schema = Schema(Group("g", Rule("r1", "err:a", Fails("x")), Rule("r2", "err:a", Fails("y"))));
        using var h = new PatternHarness(schema, E("r", children));

        _ = h.Evaluate();

        // Ten per rule plus one summary per rule: the second rule is not hidden by the first.
        await Assert.That(h.Log.Errors.Count).IsEqualTo(22);
    }

    [Test]
    public async Task Rule_Context_ReadingLazyLet_BindsBeforeFirstMatch()
    {
        var schema = Schema(PatternWith("p", [Rule("r1", "a[$v]", Fails("x"))], [Let("v", "true")]));
        using var h = new PatternHarness(schema, E("r", E("a")));

        _ = h.Evaluate();

        var bind = h.Calls.IndexOf("bind:" + h.Plan.PatternStart);
        var match = h.Calls.IndexOf("match:a");
        await Assert.That(bind).IsGreaterThanOrEqualTo(0);
        await Assert.That(bind).IsLessThan(match);
        await Assert.That(h.Log.Fired).IsEqualTo("r1");
    }

    [Test]
    public async Task Rule_Context_ReadingALetThatCannotBeBound_EndsTheNodeForTheRule()
    {
        var schema = Schema(PatternWith("p", [Rule("r1", "a[$v]", Fails("x"))], [Let("v", "bad")]));
        using var h = new PatternHarness(schema, E("r", E("a"), E("a")));

        _ = h.Evaluate();

        // The binder reports its failure once; an unbound slot is a context error, so the rule does not match and no second error is raised.
        await Assert.That(h.Log.Fired).IsEqualTo(string.Empty);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Events_AreInDocumentOrder_PatternThenFiredRuleThenFinding()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Fails("x"))));
        using var h = new PatternHarness(schema, E("r", E("a")));

        _ = h.Evaluate();

        await Assert.That(h.Log.Sequence).IsEqualTo("pattern:p fired:r1 failed:x");
    }
}
