namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

/// <summary>The sets of counter values that the subsumption check keeps as difference bounds.</summary>
public class CounterZoneTests
{
    private const long Infinity = CounterZone.Infinity;

    [Test]
    public async Task Origin_HoldsOnlyZero()
    {
        var zone = CounterZone.Origin(2);

        await Assert.That(zone.Variables).IsEqualTo(2);
        await Assert.That(zone.Minimum(1)).IsEqualTo(0);
        await Assert.That(zone.Maximum(2)).IsEqualTo(0);
        await Assert.That(zone.Contains([0, 0])).IsTrue();
        await Assert.That(zone.Contains([1, 0])).IsFalse();
    }

    [Test]
    public async Task Bounds_RestrictTheValues_AndEmptyZonesAreNull()
    {
        var zone = CounterZone.Origin(1).Assign(1, 5).AtLeast(1, 3)!.AtMost(1, 9)!;

        await Assert.That(zone.Minimum(1)).IsEqualTo(5);
        await Assert.That(zone.Maximum(1)).IsEqualTo(5);
        await Assert.That(zone.AtLeast(1, 6)).IsNull();
        await Assert.That(zone.AtMost(1, 4)).IsNull();
        await Assert.That(ReferenceEquals(zone.AtLeast(1, 5), zone)).IsTrue();
    }

    [Test]
    public async Task Assign_ForgetsTheOldValue_AndKeepsTheOthers()
    {
        var zone = CounterZone.Origin(2).Increment(1).Increment(1).Assign(2, 7);

        await Assert.That(zone.Contains([2, 7])).IsTrue();
        await Assert.That(zone.Contains([2, 6])).IsFalse();
        await Assert.That(zone.Assign(1, 1).Contains([1, 7])).IsTrue();
    }

    [Test]
    public async Task Increment_KeepsTheDifferenceOfTwoCounters()
    {
        var zone = CounterZone.Origin(2).Assign(1, 1).Assign(2, 1);
        for (var i = 0; i < 4; i++)
        {
            zone = zone.Increment(1).Increment(2);
        }

        await Assert.That(zone.Contains([5, 5])).IsTrue();
        await Assert.That(zone.Contains([5, 4])).IsFalse();
        await Assert.That(zone.Increment(1).Contains([6, 5])).IsTrue();
        await Assert.That(zone.Increment(1).Contains([5, 5])).IsFalse();
    }

    [Test]
    public async Task Bound_OnOneCounter_ReachesTheCounterThatIsEqualToIt()
    {
        var zone = CounterZone.Origin(2).Assign(1, 1).Assign(2, 1);
        zone = zone.Increment(1).Increment(2);

        var narrowed = zone.AtLeast(1, 2)!.AtMost(1, 2)!;

        await Assert.That(narrowed.Minimum(2)).IsEqualTo(2);
        await Assert.That(narrowed.Maximum(2)).IsEqualTo(2);
    }

    [Test]
    public async Task IsSubsetOf_ComparesTheBounds()
    {
        var small = CounterZone.Origin(1).Assign(1, 3);
        var large = CounterZone.Origin(1).Assign(1, 1).Hull(small);

        await Assert.That(small.IsSubsetOf(large)).IsTrue();
        await Assert.That(large.IsSubsetOf(small)).IsFalse();
        await Assert.That(small.IsSubsetOf(small)).IsTrue();
    }

    [Test]
    public async Task Hull_HoldsBothZones()
    {
        var hull = CounterZone.Origin(1).Assign(1, 1).Hull(CounterZone.Origin(1).Assign(1, 5));

        await Assert.That(hull.Minimum(1)).IsEqualTo(1);
        await Assert.That(hull.Maximum(1)).IsEqualTo(5);
        await Assert.That(hull.Contains([3])).IsTrue();
    }

    [Test]
    public async Task Widen_GivesUpTheBoundThatGrows_AndStaysBelowTheCap()
    {
        var older = CounterZone.Origin(2).Assign(1, 1).Assign(2, 1);
        var newer = older.Increment(1).Increment(2);
        var widened = older.Hull(newer).Widen(older.Hull(newer).Hull(newer.Increment(1).Increment(2)), [10, 10]);

        await Assert.That(widened.Maximum(1)).IsEqualTo(10);
        await Assert.That(widened.Minimum(1)).IsEqualTo(1);
        await Assert.That(widened.Contains([7, 7])).IsTrue();
        await Assert.That(widened.Contains([7, 6])).IsFalse();
    }

    [Test]
    public async Task Widen_ClosesTheBoundsThatAreLeft()
    {
        var older = CounterZone
            .Origin(2)
            .Assign(1, 1)
            .Assign(2, 1)
            .Hull(CounterZone.Origin(2).Assign(1, 2).Assign(2, 2));
        var widened = older.Widen(CounterZone.Origin(2).Assign(1, 1).Assign(2, 2), [10, 10]);

        // The bound on y - x was given up, but the bounds of both counters still limit it; x - y stays.
        await Assert.That(widened.Contains([1, 2])).IsTrue();
        await Assert.That(widened.Contains([2, 1])).IsFalse();
        await Assert.That(widened.Maximum(2)).IsEqualTo(2);
    }

    [Test]
    public async Task Widen_KeepsABoundThatTheNewerZoneRespects()
    {
        var zone = CounterZone.Origin(1).Assign(1, 2).Hull(CounterZone.Origin(1).Assign(1, 4));
        var widened = zone.Widen(CounterZone.Origin(1).Assign(1, 3), [100]);

        await Assert.That(widened.Minimum(1)).IsEqualTo(2);
        await Assert.That(widened.Maximum(1)).IsEqualTo(4);
    }

    [Test]
    public async Task Maximum_OfACounterWithoutACap_IsInfinity()
    {
        var zone = CounterZone.Origin(1).Assign(1, 1).Hull(CounterZone.Origin(1).Assign(1, 2));
        var widened = zone.Widen(CounterZone.Origin(1).Assign(1, 3), [Infinity]);

        await Assert.That(widened.Maximum(1)).IsEqualTo(Infinity);
        await Assert.That(widened.Minimum(1)).IsEqualTo(1);
    }

    [Test]
    public async Task Guards_RejectInvalidArguments()
    {
        var zone = CounterZone.Origin(1);

        await Assert.That(() => CounterZone.Origin(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => zone.Contains(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => zone.Contains([1, 2])).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => zone.IsSubsetOf(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => zone.Hull(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => zone.Widen(null!, [1])).Throws<ArgumentNullException>();
        await Assert.That(() => zone.Widen(zone, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => zone.Widen(zone, [1, 2])).Throws<ArgumentOutOfRangeException>();
    }
}
