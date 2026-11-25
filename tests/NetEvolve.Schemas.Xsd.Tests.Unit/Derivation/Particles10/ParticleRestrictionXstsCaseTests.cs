namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Diagnostics;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;

/// <summary>The restrictions of the XSTS 1.0 schema tests that the check reported although the suite accepts them (#863).</summary>
public class ParticleRestrictionXstsCaseTests
{
    private readonly ParticleRestriction _check = Checker();

    // mgH014: a member that never occurs (maxOccurs 0) corresponds to no particle.
    [Test]
    public async Task Choice_WithAMemberThatNeverOccurs_RestrictsTheChoiceOfTheBase() =>
        await AssertValid(_check.Check(Cho([El("e1", 0, 0), El("e2")]), Cho([El("e1"), El("e2")])));

    // particlesJq010
    [Test]
    public async Task Sequence_OfOnlyAMemberThatNeverOccurs_RestrictsAnOptionalWildcard() =>
        await AssertValid(_check.Check(Seq([El("e1", 0, 0)]), Seq([Any(Only(Tns), min: 0)])));

    // particlesHa070
    [Test]
    public async Task All_OfThreeElements_RestrictsASequenceOfOneWildcardOccurringThreeTimes() =>
        await AssertValid(_check.Check(All([El("e1"), El("e2"), El("e3")]), Seq([Any(min: 3, max: 3)])));

    // particlesHa080
    [Test]
    public async Task Sequence_OfTwoElementsAndAWildcard_RestrictsAWildcardOccurringTwoToThreeTimes() =>
        await AssertValid(_check.Check(Seq([El("e"), El("e"), Any(Only(Tns))]), Seq([Any(min: 2, max: 3)])));

    // particlesHa084
    [Test]
    public async Task NestedSequence_OfTwoElementsAndAWildcard_RestrictsAWildcardOccurringTwoToThreeTimes() =>
        await AssertValid(_check.Check(Seq([Seq([El("e"), El("e"), Any(Only(Tns))])]), Seq([Any(min: 2, max: 3)])));

    // particlesQ013
    [Test]
    public async Task Sequence_WithARepeatedSequence_RestrictsAWildcardOfTheBase() =>
        await AssertValid(
            _check.Check(
                Seq([El("foo"), Seq([El("e1", 2, 2), El("e2", 2, 2)], 1, 2)]),
                Seq([El("foo"), Any(min: 4, max: 8)])
            )
        );

    // particlesR013
    [Test]
    public async Task Sequence_WithARepeatedChoice_RestrictsAWildcardOfTheBase() =>
        await AssertValid(
            _check.Check(Seq([El("foo"), Cho([El("e1"), El("e2")], 3, 4)]), Seq([El("foo"), Any(min: 3, max: 8)]))
        );

    // The members that never occur are no particles: they neither map to a base particle nor count in the total range.
    [Test]
    public async Task Choice_WithOnlyMembersThatNeverOccur_RestrictsAnEmptiableBaseOnly()
    {
        await AssertValid(_check.Check(Cho([El("e1", 0, 0), El("e2", 0, 0)], 0, 1), Seq([El("a", 0, 1)])));
        await AssertViolation(
            _check.Check(Cho([El("e1", 0, 0)], 1, 1, 10), Seq([El("a"), El("b")])),
            XsdRuleCodes.CosParticleRestrict,
            "cos-particle-restrict.2",
            10
        );
    }

    [Test]
    public async Task Choice_InAWildcardRestriction_IgnoresAMemberThatNeverOccursInTheTotalRange()
    {
        await AssertValid(_check.Check(Cho([El("e1", 0, 0), El("e2", 2, 3)]), Seq([Any(min: 2, max: 3)])));
        await AssertViolation(
            _check.Check(Cho([El("e1", 0, 0), El("e2", 1, 3, 10)]), Seq([Any(min: 2, max: 3)])),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );
    }

    [Test]
    public async Task Sequence_WithAMemberOutsideTheWildcard_StillFailsTheCardinalityRule() =>
        await AssertViolation(
            _check.Check(Seq([El("e1", ns: "urn:other", line: 11), El("e2")], 1, 1, 10), Seq([Any(Only(Tns), min: 0)])),
            XsdRuleCodes.RcaseNSRecurseCheckCardinality,
            "rcase-NSRecurseCheckCardinality.1",
            11
        );
}
