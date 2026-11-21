namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

public class DifferentialComparerTests
{
    private static readonly DifferentialCase Case = new("c1", ["a.xsd"], "a.xml", Verdict.Valid, Verdict.Valid);

    [Test]
    public async Task Compare_SamePositionsInAnotherOrder_HasNoDifference()
    {
        var ours = new EngineResult(Verdict.Valid, Verdict.Invalid, [new(7, 2), new(3, 4)]);
        var baseline = new EngineResult(Verdict.Valid, Verdict.Invalid, [new(3, 4), new(7, 2)]);

        await Assert.That(DifferentialComparer.Compare(Case, ours, baseline)).IsEmpty();
    }

    [Test]
    public async Task Compare_DifferentPositions_ReportsBothSortedLists()
    {
        var ours = new EngineResult(Verdict.Valid, Verdict.Invalid, [new(3, 5)]);
        var baseline = new EngineResult(Verdict.Valid, Verdict.Invalid, [new(7, 2), new(3, 6)]);

        var differences = DifferentialComparer.Compare(Case, ours, baseline);

        await Assert
            .That(differences)
            .IsEquivalentTo([
                new Difference("c1", DifferenceKind.IssuePositions, "issue positions: ours=[3:5], baseline=[3:6, 7:2]"),
            ]);
    }

    [Test]
    public async Task Compare_DifferentVerdicts_ReportsOneDifferencePerVerdict()
    {
        var ours = new EngineResult(Verdict.Valid, Verdict.Valid, []);
        var baseline = new EngineResult(Verdict.Invalid, Verdict.NotRun, [new(1, 1)]);

        var differences = DifferentialComparer.Compare(Case, ours, baseline);

        await Assert
            .That(differences)
            .IsEquivalentTo([
                new Difference("c1", DifferenceKind.SchemaVerdict, "schema verdict: ours=Valid, baseline=Invalid"),
                new Difference("c1", DifferenceKind.InstanceVerdict, "instance verdict: ours=Valid, baseline=NotRun"),
            ]);
    }
}
