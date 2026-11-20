namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Elements;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;

/// <summary>The group cases of Particle Valid (Restriction): mapping rules, occurrence ranges, and the effective total range (Structures 1.0, 3.9.6).</summary>
public class ParticleRestrictionGroupTests
{
    private const long Huge = long.MaxValue - 1;

    private readonly ParticleRestriction _check = Checker();

    [Test]
    public async Task Recurse_OccurrenceRange_OfTheGroup_MustLieInsideTheBaseRange()
    {
        await AssertValid(_check.Check(Seq([El("a"), El("b")], 1, 2), Seq([El("a"), El("b")], 0, 3)));
        await AssertViolation(
            _check.Check(Seq([El("a"), El("b")], 0, 1, 10), Seq([El("a"), El("b")])),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );
        await AssertViolation(
            _check.Check(All([El("a"), El("b")], 1, 1, 10), All([El("a"), El("b")], 0, 0)),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
    }

    [Test]
    public async Task Recurse_SkipsEmptiableBaseParticles_BeforeBetweenAndAfterTheMapped()
    {
        await AssertValid(
            _check.Check(
                Seq([El("b"), El("c")]),
                Seq([El("a", 0, 1), El("b"), El("x", 0, Unbounded), El("c"), El("d", 0, 1)])
            )
        );
        await AssertValid(_check.Check(All([El("b"), El("c")]), All([El("a", 0, 1), El("b"), El("c"), El("d", 0, 1)])));
    }

    [Test]
    public async Task Recurse_ParticleWithoutARemainingBaseParticle_IsNotMapped() =>
        await AssertViolation(
            _check.Check(
                Seq([El("a", line: 11), El("b", line: 12), El("c", line: 13)], line: 10),
                Seq([El("a"), El("b", 0, 1)])
            ),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.1",
            13
        );

    [Test]
    public async Task Recurse_BaseParticleAtTheEnd_MustBeEmptiable()
    {
        await AssertViolation(
            _check.Check(Seq([El("a"), El("b")], line: 10), Seq([El("a"), El("b"), El("c")])),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            10
        );
        await AssertValid(_check.Check(Seq([El("a"), El("b")]), Seq([El("a"), El("b"), El("c", 0, 1)])));
    }

    [Test]
    public async Task Recurse_MapsGroupsToGroups_AndReportsTheMemberThatFails()
    {
        await AssertValid(
            _check.Check(Seq([El("a"), Cho([El("b"), El("c")])]), Seq([El("a"), Cho([El("b"), El("c"), El("d")])]))
        );
        await AssertViolation(
            _check.Check(
                Seq([El("a"), Cho([El("b"), El("x", line: 13)], line: 12)], line: 10),
                Seq([El("a"), Cho([El("b"), El("c")])])
            ),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            12
        );
    }

    [Test]
    public async Task RecurseLax_Order_IsPreserved_ButBaseParticlesMayBeLeftOut()
    {
        await AssertValid(_check.Check(Cho([El("a"), El("c")]), Cho([El("a"), El("b"), El("c")])));
        await AssertViolation(
            _check.Check(Cho([El("c", line: 11), El("a", line: 12)], line: 10), Cho([El("a"), El("b"), El("c")])),
            XsdRuleCodes.RcaseRecurseLax,
            "rcase-RecurseLax.2",
            12
        );
    }

    [Test]
    public async Task RecurseLax_OccurrenceRange_OfTheGroup_MustLieInsideTheBaseRange()
    {
        await AssertValid(_check.Check(Cho([El("a"), El("b")], 1, 1), Cho([El("a"), El("b")], 0, 2)));
        await AssertViolation(
            _check.Check(Cho([El("a"), El("b")], 0, 1, 10), Cho([El("a"), El("b")])),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );
    }

    [Test]
    public async Task RecurseUnordered_Order_IsFree_AndEmptiableBaseParticlesMayBeLeftOut() =>
        await AssertValid(_check.Check(Seq([El("b"), El("a")]), All([El("a"), El("b"), El("c", 0, 1)])));

    [Test]
    public async Task RecurseUnordered_BaseParticle_IsMappedByOneParticleOnly() =>
        await AssertViolation(
            _check.Check(Seq([El("a", line: 11), El("a", line: 12)], line: 10), All([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurseUnordered,
            "rcase-RecurseUnordered.2.1",
            12
        );

    [Test]
    public async Task RecurseUnordered_UnmappedBaseParticle_MustBeEmptiable() =>
        await AssertViolation(
            _check.Check(Seq([El("a"), El("b")], line: 10), All([El("a"), El("b"), El("c")])),
            XsdRuleCodes.RcaseRecurseUnordered,
            "rcase-RecurseUnordered.2.3",
            10
        );

    [Test]
    public async Task RecurseUnordered_OccurrenceRange_OfTheSequence_MustLieInsideTheBaseRange() =>
        await AssertViolation(
            _check.Check(Seq([El("a"), El("b")], 1, 2, 10), All([El("a"), El("b")])),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );

    [Test]
    public async Task MapAndSum_EveryParticle_MustRestrictSomeBaseParticle() =>
        await AssertViolation(
            _check.Check(Seq([El("a", line: 11), El("x", line: 12)], line: 10), Cho([El("a"), El("b")], 0, Unbounded)),
            XsdRuleCodes.RcaseMapAndSum,
            "rcase-MapAndSum.1",
            12
        );

    [Test]
    [Arguments(1L, Unbounded, 0L, Unbounded, true, "")]
    [Arguments(1L, Unbounded, 0L, 5L, false, "range-ok.2")]
    [Arguments(1L, 1L, 3L, 5L, false, "range-ok.1")]
    [Arguments(2L, 2L, 4L, 4L, true, "")]
    [Arguments(2L, 3L, 4L, 5L, false, "range-ok.2")]
    public async Task MapAndSum_SumOfTheOccurrences_MustLieInsideTheBaseRange(
        long min,
        long max,
        long baseMin,
        long baseMax,
        bool valid,
        string clause
    )
    {
        var violation = _check.Check(Seq([El("a"), El("b")], min, max, 10), Cho([El("a"), El("b")], baseMin, baseMax));

        if (valid)
        {
            await AssertValid(violation);
        }
        else
        {
            await AssertViolation(violation, XsdRuleCodes.RangeOk, clause, 10);
        }
    }

    [Test]
    public async Task NSRecurseCheckCardinality_NestedGroups_AddTheirOwnRanges()
    {
        // The inner choice counts 0 to 6 (0 to 3, twice), the outer sequence 1 to 8.
        Particle Restriction() => Seq([El("a", 1, 2), Cho([El("b", 0, 1), El("c", 2, 3)], 1, 2)], line: 10);

        await AssertValid(_check.Check(Restriction(), Any(Only(Tns), XsdProcessContents.Lax, 0, 8)));
        await AssertViolation(
            _check.Check(Restriction(), Any(Only(Tns), XsdProcessContents.Lax, 0, 7)),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
    }

    [Test]
    public async Task NSRecurseCheckCardinality_GroupThatMayBeRepeatedWithoutEnd_NeedsAnUnboundedWildcard()
    {
        await AssertValid(
            _check.Check(
                Seq([El("a", 1, 2), El("b", 1, 2)], 1, Unbounded),
                Any(null, XsdProcessContents.Lax, 0, Unbounded)
            )
        );
        await AssertViolation(
            _check.Check(
                Seq([El("a", 1, 2), El("b", 1, 2)], 1, Unbounded, 10),
                Any(null, XsdProcessContents.Lax, 0, 100)
            ),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
    }

    [Test]
    public async Task NSRecurseCheckCardinality_SumsAndProducts_SaturateAtUnbounded()
    {
        var wildcard = Any(null, XsdProcessContents.Lax, 0, Huge);

        // The sum of two maxima and the product with the occurrence of the group both overflow.
        await AssertViolation(
            _check.Check(Seq([El("a", 0, Huge), El("b", 0, Huge)], 1, 1, 10), wildcard),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
        await AssertViolation(
            _check.Check(Seq([El("a", 0, 1), El("b", 0, 1)], 1, Huge, 10), wildcard),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
        await AssertValid(_check.Check(Seq([El("a", 0, 1), El("b", 0, 1)], 1, 2), wildcard));
    }

    [Test]
    public async Task NSRecurseCheckCardinality_MinimumOfTheGroup_IsTheProductWithItsOccurrence()
    {
        await AssertViolation(
            _check.Check(Seq([El("a", 2, 2), El("b", 2, 2)], 0, 1, 10), Any(Only(Tns), XsdProcessContents.Lax, 1, 5)),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );

        // A choice without particles can never match, so it counts 0 to 0 and cannot restrict a wildcard that must occur.
        await AssertViolation(
            _check.Check(Cho([], 1, 1, 10), Any(Only(Tns), XsdProcessContents.Lax, 1, 5)),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );
    }

    [Test]
    public async Task RecurseAsIfGroup_OccurrenceRange_OfTheElement_IsComparedAsAGroupOfOne()
    {
        await AssertValid(_check.Check(El("a", 1, 2), Seq([El("a", 1, 2), El("b", 0, 1)])));
        await AssertViolation(
            _check.Check(El("a", line: 10), Seq([El("a"), El("b", 0, 1)], 2, 3)),
            XsdRuleCodes.RcaseRecurseAsIfGroup,
            "rcase-RecurseAsIfGroup",
            10
        );
    }

    [Test]
    public async Task Restriction_ThroughGroupReferences_ComparesTheGroupsOfTheDefinitions()
    {
        await AssertValid(
            _check.Check(Ref(ModelGroupCompositor.Sequence, [El("a"), El("b")]), Seq([El("a"), El("b"), El("c", 0, 1)]))
        );
        await AssertViolation(
            _check.Check(
                Ref(ModelGroupCompositor.Sequence, [El("a", line: 11), El("x", line: 12)], 10),
                Seq([El("a"), El("b")])
            ),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            12
        );
    }
}
