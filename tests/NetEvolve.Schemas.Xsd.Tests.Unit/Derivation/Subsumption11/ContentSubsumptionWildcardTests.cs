namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11.SubsumptionKit;

/// <summary>Wildcards: namespaces, disallowed names, bindings of the process contents, and the priority of an element declaration.</summary>
public class ContentSubsumptionWildcardTests
{
    private const string Subsumes = "cos-content-act-restrict.2.subsumes.";

    private static Particle Not(string localName, XsdProcessContents process = XsdProcessContents.Lax, int line = 1) =>
        Particle.Create(
            At(line),
            1,
            1,
            NegatedWildcard.Create(NameConstraint.Create(true, [], [new XPathQName(Tns, localName)]), process, At(line))
        );

    [Test]
    public async Task Wildcard_OfTheBase_CoversTheElementsOfTheRestriction()
    {
        await AssertSubsumed(Seq([El("a"), El("b")]), Any(min: 0, max: Unbounded));
        await AssertSubsumed(El("a", ns: "urn:other"), Any());
    }

    [Test]
    public async Task Wildcard_OfTheRestriction_IsNotCoveredByAnElementOfTheBase()
    {
        // A name that both allow is bound by a keyword in the restriction and by a declaration in the base (clause 4).
        await AssertNotSubsumed(Any(line: 3), El("a"), Subsumes + "4", 3);

        // A name that only the wildcard allows is not accepted by the base at all.
        await AssertNotSubsumed(Any(Only("urn:x"), line: 3), El("a"), line: 3);
    }

    [Test]
    public async Task Wildcard_OfTheRestriction_AllowingTheNameOfAnElementOfTheBase_IsStillNotSubsumed() =>
        await AssertNotSubsumed(Any(Only(Tns), line: 3), El("a"), Subsumes + "4", 3);

    [Test]
    public async Task Wildcard_WithFewerNamespaces_IsSubsumed()
    {
        await AssertSubsumed(Any(Only("urn:a")), Any());
        await AssertSubsumed(Any(Only("urn:a")), Any(Only("urn:a", "urn:b")));
        await AssertSubsumed(Any(Only("urn:a", "urn:b")), Any(NamespaceConstraint.Not(Tns)));
    }

    [Test]
    public async Task Wildcard_WithMoreNamespaces_IsNotSubsumed()
    {
        await AssertNotSubsumed(Any(line: 3), Any(Only("urn:a")), line: 3);
        await AssertNotSubsumed(Any(Only("urn:a", "urn:c"), line: 3), Any(Only("urn:a", "urn:b")), line: 3);
        await AssertNotSubsumed(Any(Only("urn:a"), line: 3), Any(NamespaceConstraint.Not("urn:a")), line: 3);
    }

    [Test]
    public async Task Element_OutsideTheNamespacesOfTheWildcard_IsNotSubsumed() =>
        await AssertNotSubsumed(El("a", ns: "urn:other", line: 3), Any(Only("urn:a")), line: 3);

    [Test]
    public async Task Wildcard_WithADisallowedName_IsComparedByName()
    {
        await AssertSubsumed(El("b"), Not("a"));
        await AssertNotSubsumed(El("a", line: 3), Not("a"), line: 3);
        await AssertSubsumed(Not("a"), Any());
        await AssertNotSubsumed(Any(line: 3), Not("a"), line: 3);
        await AssertSubsumed(Not("a"), Not("a"));
    }

    [Test]
    public async Task Wildcard_WithAnOccurrenceRange_IsComparedByTheRange()
    {
        await AssertSubsumed(Any(min: 2, max: 3), Any(min: 1, max: 5));
        await AssertNotSubsumed(Any(min: 2, max: 6, line: 3), Any(min: 1, max: 5), line: 3);
    }

    [Test]
    public async Task ProcessContents_OfTheRestriction_MustBeAsStrongAsThatOfTheBase()
    {
        await AssertSubsumed(Any(process: XsdProcessContents.Strict), Any(process: XsdProcessContents.Lax));
        await AssertSubsumed(Any(process: XsdProcessContents.Strict), Any(process: XsdProcessContents.Skip));
        await AssertSubsumed(Any(process: XsdProcessContents.Lax), Any(process: XsdProcessContents.Skip));
        await AssertSubsumed(Any(process: XsdProcessContents.Lax), Any(process: XsdProcessContents.Lax));
        await AssertSubsumed(Any(process: XsdProcessContents.Skip), Any(process: XsdProcessContents.Skip));
        await AssertSubsumed(Any(process: XsdProcessContents.Strict), Any(process: XsdProcessContents.Strict));
    }

    [Test]
    public async Task ProcessContents_Lax_AgainstAStrictBase_IsNotSubsumed() =>
        await AssertNotSubsumed(
            Any(process: XsdProcessContents.Lax, line: 3),
            Any(process: XsdProcessContents.Strict),
            Subsumes + "3",
            3
        );

    [Test]
    public async Task ProcessContents_Skip_AgainstAStrictBase_IsNotSubsumed() =>
        await AssertNotSubsumed(
            Any(process: XsdProcessContents.Skip, line: 3),
            Any(process: XsdProcessContents.Strict),
            Subsumes + "3",
            3
        );

    [Test]
    public async Task ProcessContents_Skip_AgainstALaxBase_IsNotSubsumed() =>
        await AssertNotSubsumed(
            Any(process: XsdProcessContents.Skip, line: 3),
            Any(process: XsdProcessContents.Lax),
            Subsumes + "2",
            3
        );

    [Test]
    public async Task Element_AgainstALaxOrSkippedWildcard_IsSubsumed()
    {
        await AssertSubsumed(El("a"), Any(process: XsdProcessContents.Lax));
        await AssertSubsumed(El("a"), Any(process: XsdProcessContents.Skip));
    }

    [Test]
    public async Task Element_AgainstAStrictWildcard_IsNotSubsumed() =>
        await AssertNotSubsumed(El("a", line: 3), Any(process: XsdProcessContents.Strict), Subsumes + "3", 3);

    [Test]
    public async Task Wildcard_InPlaceOfAnElement_IsBoundByAKeyword_AndNotSubsumed() =>
        await AssertNotSubsumed(Not("b", line: 3), Seq([El("a"), El("b")], 0, 1), Subsumes + "4", 3);

    [Test]
    public async Task Priority_OfAnElementDeclaration_FollowsTheElementOfTheBase()
    {
        // The base takes the element declaration for a, so a d after it is not accepted, although the wildcard of the
        // other alternative would take both: the content model is read as deterministic with the priority of XSD 1.1.
        await AssertNotSubsumed(
            Seq([El("a"), El("d", line: 6)]),
            Cho([Seq([El("a"), El("c")]), Seq([Any(), El("d")])]),
            line: 6
        );
        await AssertSubsumed(Seq([El("a"), El("c")]), Cho([Seq([El("a"), El("c")]), Seq([Any(), El("d")])]));
        await AssertSubsumed(Seq([El("x"), El("d")]), Cho([Seq([El("a"), El("c")]), Seq([Any(), El("d")])]));
    }

    [Test]
    public async Task Priority_OfAnElementDeclaration_EndsWhereItsOccurrenceIsUsedUp()
    {
        // The third a does not fit a{0,2} any more, so the wildcard behind it takes it.
        await AssertSubsumed(El("a", 3, 3), Seq([El("a", 0, 2), Any(min: 0, max: Unbounded)]));
        await AssertNotSubsumed(
            El("a", 3, 3, line: 4),
            Seq([El("a", 0, 2), Any(Only("urn:other"), min: 0, max: Unbounded)]),
            line: 4
        );
    }

    [Test]
    public async Task Priority_OfAnElementDeclaration_AlsoHoldsInTheRestriction()
    {
        // The restriction binds a to its declaration where it also has a wildcard that allows a: the same model is its own restriction.
        Particle Model() => Seq([El("a", 0, 1), Any(min: 0, max: Unbounded)]);
        var same = Model();

        await AssertSubsumed(same, same);
        await AssertSubsumed(Model(), Model());
        await AssertSubsumed(
            Seq([El("a", 0, 2), Any(min: 0, max: Unbounded)]),
            Seq([El("a", 0, 2), Any(min: 0, max: Unbounded)])
        );
    }

    [Test]
    public async Task Priority_OfAnElementDeclaration_StillLetsTheRestrictionWildcardTakeOtherNames() =>
        await AssertNotSubsumed(
            Seq([El("a", 0, 1), Any(Only("urn:x"), min: 0, max: Unbounded, line: 5)]),
            Seq([El("a", 0, 1), Any(Only("urn:y"), min: 0, max: Unbounded)]),
            line: 5
        );

    [Test]
    public async Task GlobalElement_AgainstAStrictWildcard_IsItsOwnGoverningDeclaration_AndIsSubsumed()
    {
        // The wildcard of the base binds an element named a to the global declaration a, which is the one of the restriction.
        await AssertSubsumed(Of(Declaration("a", global: true)), Any(Only(Tns), process: XsdProcessContents.Strict));
        await AssertSubsumed(
            Seq([Of(Declaration("a", global: true)), Of(Declaration("b", global: true))]),
            Any(Only(Tns), process: XsdProcessContents.Strict, min: 0, max: Unbounded)
        );
    }
}
