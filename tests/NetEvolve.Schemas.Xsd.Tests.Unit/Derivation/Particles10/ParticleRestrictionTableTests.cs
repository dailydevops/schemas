namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Elements;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;

/// <summary>One test per cell of the Particle Valid (Restriction) table (Structures 1.0, 3.9.6): derived particle in the rows, base in the columns.</summary>
public class ParticleRestrictionTableTests
{
    private const string Forbidden = "cos-particle-restrict.2";

    private readonly ParticleRestriction _check = Checker();

    private static Particle Pair() => All([El("a", line: 11), El("b", line: 12)], line: 10);

    private static Particle Pair(ModelGroupCompositor compositor) =>
        Group(compositor, [El("a", line: 11), El("b", line: 12)], line: 10);

    private async Task AssertForbidden(Particle restriction, Particle baseParticle) =>
        await AssertViolation(_check.Check(restriction, baseParticle), XsdRuleCodes.CosParticleRestrict, Forbidden, 10);

    [Test]
    public async Task Cell_Element_Element_NameAndTypeOK()
    {
        await AssertValid(_check.Check(El("a"), El("a", 0, 1)));
        await AssertViolation(
            _check.Check(El("b", line: 10), El("a")),
            XsdRuleCodes.RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK.1",
            10
        );
    }

    [Test]
    public async Task Cell_Element_Any_NSCompat()
    {
        await AssertValid(_check.Check(El("a"), Any(Only(Tns))));
        await AssertViolation(
            _check.Check(El("a", line: 10), Any(Only("urn:other"))),
            XsdRuleCodes.RcaseNSCompat,
            "rcase-NSCompat.1",
            10
        );
    }

    [Test]
    public async Task Cell_Element_All_RecurseAsIfGroup()
    {
        await AssertValid(_check.Check(El("a"), All([El("a"), El("b", 0, 1)])));
        await AssertViolation(
            _check.Check(El("c", line: 10), All([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurseAsIfGroup,
            "rcase-RecurseAsIfGroup",
            10
        );
    }

    [Test]
    public async Task Cell_Element_Choice_RecurseAsIfGroup()
    {
        await AssertValid(_check.Check(El("a"), Cho([El("a"), El("b")])));
        await AssertViolation(
            _check.Check(El("c", line: 10), Cho([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurseAsIfGroup,
            "rcase-RecurseAsIfGroup",
            10
        );
    }

    [Test]
    public async Task Cell_Element_Sequence_RecurseAsIfGroup()
    {
        await AssertValid(_check.Check(El("a"), Seq([El("b", 0, 1), El("a")])));
        await AssertViolation(
            _check.Check(El("a", line: 10), Seq([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurseAsIfGroup,
            "rcase-RecurseAsIfGroup",
            10
        );
    }

    [Test]
    public async Task Cell_Any_Element_Forbidden() => await AssertForbidden(Any(line: 10), El("a"));

    [Test]
    public async Task Cell_Any_Any_NSSubset()
    {
        await AssertValid(_check.Check(Any(Only(Tns), XsdProcessContents.Strict), Any()));
        await AssertViolation(
            _check.Check(Any(line: 10), Any(Only(Tns))),
            XsdRuleCodes.RcaseNSSubset,
            "rcase-NSSubset.2",
            10
        );
    }

    [Test]
    public async Task Cell_Any_All_Forbidden() => await AssertForbidden(Any(line: 10), Pair());

    [Test]
    public async Task Cell_Any_Choice_Forbidden() =>
        await AssertForbidden(Any(line: 10), Pair(ModelGroupCompositor.Choice));

    [Test]
    public async Task Cell_Any_Sequence_Forbidden() =>
        await AssertForbidden(Any(line: 10), Pair(ModelGroupCompositor.Sequence));

    [Test]
    public async Task Cell_All_Element_Forbidden() => await AssertForbidden(Pair(), El("a"));

    [Test]
    public async Task Cell_All_Any_NSRecurseCheckCardinality()
    {
        await AssertValid(_check.Check(Pair(), Any(null, XsdProcessContents.Lax, 0, Unbounded)));
        await AssertViolation(
            _check.Check(
                All([El("a", line: 11), El("x", line: 12, ns: "urn:other")], line: 10),
                Any(Only(Tns), XsdProcessContents.Lax, 0, Unbounded)
            ),
            XsdRuleCodes.RcaseNSRecurseCheckCardinality,
            "rcase-NSRecurseCheckCardinality.1",
            12
        );
    }

    [Test]
    public async Task Cell_All_All_Recurse()
    {
        await AssertValid(_check.Check(All([El("a"), El("b")]), All([El("a"), El("b"), El("c", 0, 1)])));
        await AssertViolation(
            _check.Check(All([El("b", line: 11), El("a", line: 12)], line: 10), All([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            11
        );
    }

    [Test]
    public async Task Cell_All_Choice_Forbidden() => await AssertForbidden(Pair(), Pair(ModelGroupCompositor.Choice));

    [Test]
    public async Task Cell_All_Sequence_Forbidden() =>
        await AssertForbidden(Pair(), Pair(ModelGroupCompositor.Sequence));

    [Test]
    public async Task Cell_Choice_Element_Forbidden() =>
        await AssertForbidden(Pair(ModelGroupCompositor.Choice), El("a"));

    [Test]
    public async Task Cell_Choice_Any_NSRecurseCheckCardinality()
    {
        await AssertValid(_check.Check(Cho([El("a"), El("b")]), Any(null, XsdProcessContents.Lax, 0, Unbounded)));

        // The members fit the wildcard range; the effective total range of the choice, 2 to 6, does not.
        await AssertViolation(
            _check.Check(
                Cho([El("a", 2, 3), El("b", 2, 3)], 1, 2, line: 10),
                Any(Only(Tns), XsdProcessContents.Lax, 2, 3)
            ),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
    }

    [Test]
    public async Task Cell_Choice_All_Forbidden() => await AssertForbidden(Pair(ModelGroupCompositor.Choice), Pair());

    [Test]
    public async Task Cell_Choice_Choice_RecurseLax()
    {
        await AssertValid(_check.Check(Cho([El("a"), El("b")]), Cho([El("a"), El("b"), El("c")])));
        await AssertViolation(
            _check.Check(Cho([El("a", line: 11), El("x", line: 12)], line: 10), Cho([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurseLax,
            "rcase-RecurseLax.2",
            12
        );
    }

    [Test]
    public async Task Cell_Choice_Sequence_Forbidden() =>
        await AssertForbidden(Pair(ModelGroupCompositor.Choice), Pair(ModelGroupCompositor.Sequence));

    [Test]
    public async Task Cell_Sequence_Element_Forbidden() =>
        await AssertForbidden(Pair(ModelGroupCompositor.Sequence), El("a"));

    [Test]
    public async Task Cell_Sequence_Any_NSRecurseCheckCardinality()
    {
        await AssertValid(_check.Check(Seq([El("a"), El("b")]), Any(null, XsdProcessContents.Lax, 0, Unbounded)));

        // The members fit the wildcard range; the effective total range of the sequence, 6 to 6, does not.
        await AssertViolation(
            _check.Check(Seq([El("a", 3, 3), El("b", 3, 3)], line: 10), Any(Only(Tns), XsdProcessContents.Lax, 3, 3)),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
    }

    [Test]
    public async Task Cell_Sequence_All_RecurseUnordered()
    {
        await AssertValid(_check.Check(Seq([El("b"), El("a")]), All([El("a"), El("b")])));
        await AssertViolation(
            _check.Check(Seq([El("a", line: 11), El("x", line: 12)], line: 10), All([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurseUnordered,
            "rcase-RecurseUnordered.2.2",
            12
        );
    }

    [Test]
    public async Task Cell_Sequence_Choice_MapAndSum()
    {
        await AssertValid(_check.Check(Seq([El("a"), El("b")]), Cho([El("a"), El("b")], 2, 2)));
        await AssertViolation(
            _check.Check(Seq([El("a"), El("b")], line: 10), Cho([El("a"), El("b")])),
            XsdRuleCodes.RangeOk,
            "range-ok.2",
            10
        );
    }

    [Test]
    public async Task Cell_Sequence_Sequence_Recurse()
    {
        await AssertValid(_check.Check(Seq([El("a"), El("b")]), Seq([El("a"), El("x", 0, 1), El("b")])));
        await AssertViolation(
            _check.Check(Seq([El("a", line: 11), El("c", line: 12)], line: 10), Seq([El("a"), El("b")])),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            12
        );
    }
}
