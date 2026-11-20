namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Elements;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;

/// <summary>Pointless occurrences of sequence, choice, and all are ignored before the table is applied (Structures 1.0, 3.9.6, clause 2.2).</summary>
public class PointlessParticleTests
{
    private readonly ParticleRestriction _check = Checker();

    private static Particle Unresolved(int line = 1) =>
        Particle.CreateReference(At(line), 1, 1, XsdSymbolSpace.Elements, new XPathQName(Tns, "ref"));

    [Test]
    public async Task Sequence_WithOneMember_OccurringOnce_IsIgnored()
    {
        await AssertValid(_check.Check(Seq([Seq([El("a")])]), El("a")));
        await AssertValid(_check.Check(El("a"), Seq([El("a")])));
        await AssertValid(_check.Check(Ref(ModelGroupCompositor.Sequence, [El("a")]), El("a")));
    }

    [Test]
    public async Task Choice_WithOneMember_OccurringOnce_IsIgnored()
    {
        await AssertValid(_check.Check(Cho([El("a")]), El("a")));
        await AssertValid(_check.Check(Seq([Cho([Seq([El("a")])])]), El("a")));
    }

    [Test]
    public async Task SequenceAndChoice_WithOneMember_NotOccurringOnce_AreNotIgnored()
    {
        await AssertViolation(
            _check.Check(Seq([El("a")], 0, 1, 10), El("a", 0, 1)),
            XsdRuleCodes.CosParticleRestrict,
            "cos-particle-restrict.2",
            10
        );
        await AssertViolation(
            _check.Check(Cho([El("a")], 1, 2, 10), El("a", 1, 2)),
            XsdRuleCodes.CosParticleRestrict,
            "cos-particle-restrict.2",
            10
        );
    }

    [Test]
    public async Task Violation_AfterAPointlessGroupIsIgnored_IsLocatedAtTheParticleThatTookItsPlace() =>
        await AssertViolation(
            _check.Check(Seq([El("b", line: 10)], line: 5), El("a", line: 20)),
            XsdRuleCodes.RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK.1",
            10
        );

    [Test]
    public async Task All_WithOneMember_OccurringOnce_IsIgnored() =>
        await AssertValid(_check.Check(All([El("a")]), El("a")));

    [Test]
    public async Task All_WithOneMember_PassesItsOccurrenceOnToTheMember()
    {
        await AssertValid(_check.Check(All([El("a")], 0, 1), El("a", 0, 1)));
        await AssertValid(_check.Check(All([El("a", 1, 2)], 0, 1), El("a", 0, 2)));
        await AssertViolation(
            _check.Check(All([El("a", line: 10)], 0, 1, 5), El("a")),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );
    }

    [Test]
    public async Task All_WithOneMember_ThatIsNotKnownYet_IsKept()
    {
        await AssertViolation(
            _check.Check(All([Unresolved()], 0, 1, 10), El("a")),
            XsdRuleCodes.CosParticleRestrict,
            "cos-particle-restrict.2",
            10
        );
        await AssertValid(_check.Check(Seq([Unresolved()]), El("a")));
    }

    [Test]
    public async Task EmptyGroup_AtTheTop_IsIgnored_SoOnlyAnEmptiableBaseCanBeRestrictedByIt()
    {
        await AssertValid(_check.Check(Seq([], 1, 1), El("a", 0, 1)));
        await AssertValid(_check.Check(All([], 0, 1), Seq([El("a", 0, 1), El("b", 0, 1)])));
        await AssertValid(_check.Check(Cho([], 0, 1), Seq([])));
        await AssertViolation(
            _check.Check(Seq([], line: 10), El("a")),
            XsdRuleCodes.CosParticleRestrict,
            "cos-particle-restrict.2",
            10
        );
    }

    [Test]
    public async Task EmptyGroup_AsTheBase_AllowsAnEmptyRestrictionOnly() =>
        await AssertViolation(
            _check.Check(El("a", 0, 1, 10), Seq([], line: 20)),
            XsdRuleCodes.CosParticleRestrict,
            "cos-particle-restrict.2",
            10
        );

    [Test]
    public async Task EmptyGroups_InsideAGroup_AreIgnored()
    {
        await AssertValid(_check.Check(Seq([El("a"), Seq([]), El("b")]), Seq([El("a"), El("b")])));
        await AssertValid(_check.Check(Seq([El("a"), All([]), El("b")]), Seq([El("a"), El("b")])));
        await AssertValid(_check.Check(Seq([El("a"), Cho([], 0, 1), El("b")]), Seq([El("a"), El("b")])));
        await AssertValid(_check.Check(Seq([El("a"), El("b")]), Seq([El("a"), Seq([], 0, Unbounded), El("b")])));
    }

    [Test]
    public async Task EmptyChoice_ThatMustOccur_IsNotIgnored() =>
        await AssertViolation(
            _check.Check(
                Seq([El("a", line: 11), Cho([], 1, 1, 12), El("b", line: 13)], line: 10),
                Seq([El("a"), El("b")])
            ),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            12
        );

    [Test]
    public async Task Sequence_OccurringOnce_InsideASequence_IsSpliced()
    {
        await AssertValid(_check.Check(Seq([El("a"), Seq([El("b"), El("c")])]), Seq([El("a"), El("b"), El("c")])));
        await AssertValid(_check.Check(Seq([El("a"), El("b"), El("c")]), Seq([El("a"), Seq([El("b"), El("c")])])));
    }

    [Test]
    public async Task Choice_OccurringOnce_InsideAChoice_IsSpliced() =>
        await AssertValid(_check.Check(Cho([Cho([El("a"), El("b")]), El("c")]), Cho([El("a"), El("b"), El("c")])));

    [Test]
    public async Task Sequence_NotOccurringOnce_InsideASequence_IsNotSpliced() =>
        await AssertViolation(
            _check.Check(
                Seq([El("a"), Seq([El("b"), El("c")], 0, 1, 12)], line: 10),
                Seq([El("a"), El("b", 0, 1), El("c", 0, 1)])
            ),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.1",
            12
        );

    [Test]
    public async Task All_InsideAnAll_IsNotSpliced() =>
        await AssertViolation(
            _check.Check(All([El("a"), All([El("b"), El("c")], line: 12)], line: 10), All([El("a"), El("b"), El("c")])),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            12
        );

    [Test]
    public async Task Sequence_InsideAChoice_IsNotSpliced() =>
        await AssertViolation(
            _check.Check(Cho([El("a"), Seq([El("b"), El("c")], line: 12)], line: 10), Cho([El("a"), El("b"), El("c")])),
            XsdRuleCodes.RcaseRecurseLax,
            "rcase-RecurseLax.2",
            12
        );

    [Test]
    public async Task Group_ThatIsIgnored_LeavesTheSameParticleAsTheBase()
    {
        var particle = El("a");

        await AssertValid(_check.Check(Seq([particle]), particle));
    }

    [Test]
    public async Task Sequence_ThatMayRepeat_InsideASequence_IsNotSpliced() =>
        await AssertViolation(
            _check.Check(Seq([El("a"), Seq([El("b"), El("c")], 1, 2, 12)], line: 10), Seq([El("a"), El("b"), El("c")])),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            12
        );
}
