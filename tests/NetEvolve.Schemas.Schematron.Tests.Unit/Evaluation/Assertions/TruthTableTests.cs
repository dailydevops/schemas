namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>Assert fires on a false test, report on a true test; the empty sequence is false (ISO/IEC 19757-3:2016, clause 6).</summary>
public class TruthTableTests
{
    [Test]
    [Arguments("true", false)]
    [Arguments("false", true)]
    [Arguments("()", true)]
    public async Task Assert_FiresWhenTheTestIsFalse(string test, bool fires)
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeAssert(test, Message("m")));

        var outcome = h.Evaluate(assertion);

        await Assert.That(outcome).IsEqualTo(fires ? AssertionOutcome.Fired : AssertionOutcome.Silent);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(fires ? 1 : 0);
    }

    [Test]
    [Arguments("true", true)]
    [Arguments("false", false)]
    [Arguments("()", false)]
    public async Task Report_FiresWhenTheTestIsTrue(string test, bool fires)
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeReport(test, Message("m")));

        var outcome = h.Evaluate(assertion);

        await Assert.That(outcome).IsEqualTo(fires ? AssertionOutcome.Fired : AssertionOutcome.Silent);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(fires ? 1 : 0);
    }

    [Test]
    public async Task Test_IsEvaluatedAsABoolean_WithTheContextNodeAsFocus()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeAssert("true", Message("m")));

        _ = h.Evaluate(assertion);

        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["true"]);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Test_DynamicError_ReportsOneProblem_AndDeliversNoFinding(bool isReport)
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(isReport ? MakeReport("bad", Message("m")) : MakeAssert("bad", Message("m")));

        var outcome = h.Evaluate(assertion);

        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Problem);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(0);
        await Assert.That(h.Collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Test_ReadingAFailedSlot_ReportsNoSecondProblem_AndDeliversNoFinding()
    {
        using var h = new AssertionHarness(variables: ["x"]);
        var assertion = h.Compile(MakeAssert("$x", Message("m")));
        h.Binder.Failing.Add(0);

        var outcome = h.Evaluate(assertion);

        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Problem);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(0);
        await Assert.That(h.Binding.Evaluated.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Test_SlotsAreBoundBeforeTheTestIsEvaluated()
    {
        using var h = new AssertionHarness(variables: ["a", "b"]);
        var assertion = h.Compile(MakeReport("$b", Message("m")));

        _ = h.Evaluate(assertion);

        await Assert.That(h.Binder.Bound).IsEquivalentTo([1]);
    }

    [Test]
    public async Task NonFiringAssertion_AllocatesNothingBeyondTheTest()
    {
        using var h = new AssertionHarness();
        h.Binding.Record = false;
        var assertion = h.Compile(MakeAssert("true", Message(Text("a "), ValueOf("v"))));
        var focus = new NetEvolve.Schemas.Schematron.Seams.QueryFocus(AssertionTree.Node("order1"), 1, 1);

        NetEvolve.Schemas.Testing.Allocations.AllocationAssert.NoAllocation(() =>
            _ = h.Evaluator.Evaluate(assertion, in focus)
        );

        await Assert.That(h.Sink.Findings.Count).IsEqualTo(0);
    }
}
