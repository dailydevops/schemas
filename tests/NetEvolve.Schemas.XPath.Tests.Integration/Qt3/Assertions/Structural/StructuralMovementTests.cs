namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner.Qt3Fixtures;

/// <summary>The per-set count of the structural cases that moved from skipped to passed or failed.</summary>
public class StructuralMovementTests
{
    private static Qt3Case Structural(string kind, string name, string testSet) =>
        Case(new Qt3Assertion(kind), name, testSet);

    private static CaseResult Result(Qt3Case testCase, CaseOutcome outcome) =>
        new(testCase.Id, testCase.TestSet, outcome);

    [Test]
    public async Task Count_StructuralCases_AreCountedPerSetAndOutcome()
    {
        var a1 = Structural("assert-deep-eq", "a1", "set-a");
        var a2 = Structural("assert-xml", "a2", "set-a");
        var a3 = Structural("assert-type", "a3", "set-a");
        var a4 = Case(new Qt3Assertion("assert-true"), "a4", "set-a");
        var b1 = Structural("assert-permutation", "b1", "set-b");
        var b2 = Structural("assert-xml", "b2", "set-b");
        var c1 = Case(new Qt3Assertion("assert-eq"), "c1", "set-c");
        Qt3Case[] cases = [b1, a1, c1, a2, a3, a4, b2];
        CaseResult[] results =
        [
            Result(b1, CaseOutcome.Fail),
            Result(a1, CaseOutcome.Pass),
            Result(c1, CaseOutcome.Pass),
            Result(a2, CaseOutcome.Fail),
            Result(a3, CaseOutcome.Pass),
            Result(a4, CaseOutcome.Pass),
            Result(b2, CaseOutcome.Skipped),
        ];

        var movement = StructuralMovement.Count(cases, results);

        await Assert.That(movement.Count).IsEqualTo(2);
        await Assert.That(movement[0]).IsEqualTo(new SetMovement("set-a", Passed: 2, Failed: 1, Skipped: 0));
        await Assert.That(movement[1]).IsEqualTo(new SetMovement("set-b", Passed: 0, Failed: 1, Skipped: 1));
        await Assert.That(movement[0].Moved).IsEqualTo(3);
        await Assert.That(movement[1].Moved).IsEqualTo(1);
    }

    [Test]
    public async Task Count_NoStructuralCase_IsEmpty()
    {
        var testCase = Case(new Qt3Assertion("assert-true"));

        var movement = StructuralMovement.Count([testCase], [Result(testCase, CaseOutcome.Pass)]);

        await Assert.That(movement).IsEmpty();
    }

    [Test]
    public async Task Render_NamesEverySetAndTheTotal()
    {
        var table = StructuralMovement.Render([new("set-a", 2, 1, 0), new("set-b", 0, 1, 1)]);

        await Assert.That(table).Contains("| set-a | 2 | 1 | 3 |");
        await Assert.That(table).Contains("| set-b | 0 | 1 | 1 |");
        await Assert.That(table).Contains("| total | 2 | 2 | 4 |");
        await Assert.That(table).EndsWith("\n");
    }
}
