namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Xsd.Derivation.Subsumption11;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11.SubsumptionKit;

/// <summary>Occurrence ranges and counters: decided on the bounds, never on an unfolded model.</summary>
public class ContentSubsumptionCounterTests
{
    [Test]
    public async Task Range_InsideTheBaseRange_IsSubsumed()
    {
        await AssertSubsumed(El("a", 5, 12), El("a", 3, 12));
        await AssertSubsumed(El("a", 3, 12), El("a", 3, 12));
    }

    [Test]
    public async Task Range_AboveTheBaseMaximum_IsNotSubsumed() =>
        await AssertNotSubsumed(El("a", 5, 13, line: 4), El("a", 3, 12), line: 4);

    [Test]
    public async Task Range_BelowTheBaseMinimum_IsNotSubsumed() =>
        await AssertNotSubsumed(El("a", 2, 12, line: 4), El("a", 3, 12), line: 4);

    [Test]
    public async Task Range_ThatMayBeEmpty_AgainstARequiredElement_IsNotSubsumed() =>
        await AssertNotSubsumed(El("a", 0, Billion, line: 4), El("a", 1, Billion), line: 4);

    [Test]
    public async Task Range_WithBillionsOfOccurrences_IsDecided()
    {
        await AssertSubsumed(El("a", Billion, Billion), El("a", 1, Unbounded));
        await AssertSubsumed(El("a", Billion, Billion), El("a", Billion - 1, Billion));
        await AssertNotSubsumed(El("a", Billion, Billion, line: 4), El("a", Billion - 1, Billion - 1), line: 4);
        await AssertNotSubsumed(El("a", 1, Unbounded, line: 4), El("a", 1, Billion), line: 4);
    }

    [Test]
    public async Task Range_BelowTheBaseMinimumByOne_IsFoundAtTheEndOfTheContent() =>
        await AssertNotSubsumed(El("a", Billion - 1, Billion, line: 4), El("a", Billion, Billion), line: 4);

    [Test]
    public async Task Range_WithAMinimumOnAnUnboundedMaximum_Saturates()
    {
        await AssertSubsumed(El("a", 3, Unbounded), El("a", 2, Unbounded));
        await AssertSubsumed(El("a", Billion, Unbounded), El("a", 1, Unbounded));
        await AssertNotSubsumed(El("a", 2, Unbounded, line: 4), El("a", 3, Unbounded), line: 4);
        await AssertNotSubsumed(El("a", 1, Unbounded, line: 4), El("a", 2, Unbounded), line: 4);
    }

    [Test]
    public async Task Work_DoesNotGrowWithTheBounds()
    {
        var small = Check(El("a", 100, 100), El("a", 1, Unbounded));
        var large = Check(El("a", Billion, Billion), El("a", 1, Unbounded));
        var largest = Check(El("a", Billion, Billion), El("a", 1, Billion * 4));

        await Assert.That(large.Work).IsEqualTo(small.Work);
        await Assert.That(large.Work).IsLessThan(300);
        await Assert.That(largest.Work).IsLessThan(300);
    }

    [Test]
    public async Task Group_WithCountersOnBothSides_RunsInStep()
    {
        await AssertSubsumed(Seq([El("a"), El("b")], 2, Billion), Seq([El("a"), El("b")], 1, Billion));
        await AssertNotSubsumed(
            Seq([El("a"), El("b", line: 4)], 1, Billion),
            Seq([El("a"), El("b")], 2, Billion),
            line: 4
        );
    }

    [Test]
    public async Task Group_ThatRepeatsASequence_IsComparedWithTheUnfoldedLanguage()
    {
        // The language of the restriction has a gap: two or four a, never three.
        await AssertSubsumed(Seq([Seq([El("a", 2, 2)])], 1, 2), El("a", 2, 4));
        await AssertNotSubsumed(Seq([Seq([El("a", 2, 2)])], 1, 2), El("a", 3, 4));
        await AssertNotSubsumed(El("a", 2, 4), Seq([Seq([El("a", 2, 2)])], 1, 2));
    }

    [Test]
    public async Task Group_RepeatedInsideAGroup_IsComparedWithTheLanguageOfTheBase()
    {
        // (a,b){0,2} accepts a b a b, a sequence of two of them accepts the same words and no more.
        await AssertSubsumed(Seq([El("a"), El("b"), El("a"), El("b")]), Seq([El("a"), El("b")], 0, 2));
        await AssertNotSubsumed(
            Seq([El("a"), El("b"), El("a"), El("b"), El("a", line: 6)]),
            Seq([El("a"), El("b")], 0, 2),
            line: 6
        );
    }

    [Test]
    public async Task Counter_OfAFinishedGroup_DoesNotKeepStatesApart() =>
        await AssertSubsumed(
            Seq([Seq([El("a")], 2, 3), Seq([El("b")], 2, 3)]),
            Seq([Seq([El("a")], 1, 4), Seq([El("b")], 1, 4)])
        );

    [Test]
    public async Task Counter_WithManyValues_IsWidenedAndStaysExact()
    {
        await AssertSubsumed(El("a", 20, 20), El("a", 20, 20));
        await AssertSubsumed(El("a", 3, 20), El("a", 3, 20));
        await AssertNotSubsumed(El("a", 20, 20, line: 4), El("a", 19, 19), line: 4);
        await AssertNotSubsumed(El("a", 3, 21, line: 4), El("a", 3, 20), line: 4);
    }

    [Test]
    public async Task Counter_ThatExitsAndIsEnteredAgain_IsComparedPerVisit() =>
        await AssertSubsumed(Seq([El("a", 2, 3), El("b")], 1, Unbounded), Seq([El("a", 1, 5), El("b")], 0, Unbounded));

    [Test]
    public async Task Counters_ThatAreNotInStep_AreDecidedExactlyWhileTheirValuesAreFew() =>
        // Even counts from two to twenty: the values of the two counters are related by more than a difference bound.
        await AssertSubsumed(Seq([Seq([El("a", 2, 2)])], 1, 10), El("a", 2, 20));

    [Test]
    public async Task Counters_ThatAreNotInStep_AreNeverReportedFromAnOverApproximation()
    {
        // Valid, and too many values to be kept apart: not a violation, whatever the verdict is.
        var valid = Check(Seq([Seq([El("a", 2, 2)])], 1, Billion), El("a", 2, 2 * Billion));

        await Assert.That(valid.Verdict).IsNotEqualTo(SubsumptionVerdict.NotSubsumed);

        // Invalid, and found only in the widened zone: no verdict, instead of a violation that might be wrong.
        var invalid = Check(Seq([Seq([El("a", 2, 2)])], 1, Billion), El("a", 2, Billion));

        await Assert.That(invalid.Verdict).IsEqualTo(SubsumptionVerdict.Undecided);
        await Assert.That(invalid.Violation.Clause).IsEqualTo("cos-content-act-restrict.ceiling");
    }

    [Test]
    public async Task Counters_InStep_AreStillReportedAtTheBillion()
    {
        await AssertNotSubsumed(El("a", Billion, Billion, line: 4), El("a", Billion - 1, Billion - 1), line: 4);
        await AssertNotSubsumed(
            Seq([El("a", line: 4), El("b")], 1, Billion),
            Seq([El("a"), El("b")], 1, Billion - 1),
            line: 4
        );
    }
}
