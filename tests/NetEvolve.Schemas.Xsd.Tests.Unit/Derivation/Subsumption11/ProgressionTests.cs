namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

/// <summary>The progression of zones that a widening may jump over: points on a line that adds zero or one to each counter.</summary>
public class ProgressionTests
{
    private static CounterZone Point(params long[] values) => CounterZone.Point(values);

    [Test]
    public async Task Points_OnALine_ContinueWithTheNextPoint()
    {
        var next = SubsumptionSearch.NextPoint([Point(1, 0), Point(2, 0), Point(3, 0)]);

        await Assert.That(next).IsNotNull();
        await Assert.That(next!.Contains([4, 0])).IsTrue();
        await Assert.That(next.TryPoint(out var values)).IsTrue();
        await Assert.That(values).IsEquivalentTo(new long[] { 4, 0 });
    }

    [Test]
    public async Task Points_ThatAddToSeveralCounters_ContinueTogether()
    {
        var next = SubsumptionSearch.NextPoint([Point(1, 5, 7), Point(2, 6, 7), Point(3, 7, 7)]);

        await Assert.That(next!.Contains([4, 8, 7])).IsTrue();
    }

    [Test]
    public async Task Points_WhoseStepChanges_AreNotALine() =>
        await Assert.That(SubsumptionSearch.NextPoint([Point(0, 0), Point(1, 0), Point(1, 1)])).IsNull();

    [Test]
    public async Task Points_ThatGoBackOrJumpAhead_AreNotALine()
    {
        await Assert.That(SubsumptionSearch.NextPoint([Point(2, 0), Point(1, 0)])).IsNull();
        await Assert.That(SubsumptionSearch.NextPoint([Point(0, 0), Point(2, 0)])).IsNull();
    }

    [Test]
    public async Task Zone_WithSeveralValues_IsNotAPoint() =>
        await Assert
            .That(SubsumptionSearch.NextPoint([Point(1, 0), Point(2, 0).Hull(Point(3, 0)), Point(4, 0)]))
            .IsNull();

    [Test]
    public async Task OneZone_HasNoStep() => await Assert.That(SubsumptionSearch.NextPoint([Point(1, 0)])).IsNull();

    [Test]
    public async Task NextPoint_OfNull_Throws() =>
        await Assert.That(() => SubsumptionSearch.NextPoint(null!)).Throws<ArgumentNullException>();
}
