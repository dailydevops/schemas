namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11.SubsumptionKit;

/// <summary>The element declarations of the restriction and of the base must correspond (clause 2, default binding subsumption).</summary>
public class ContentSubsumptionElementTests
{
    private const string Subsumes = "cos-content-act-restrict.2.subsumes.";

    private readonly TypeFixture _types = new();

    private static Particle Pair(ElementDeclaration declaration, int line = 10) => Of(declaration, 1, 1, line);

    [Test]
    public async Task Element_ThatIsTheSameDeclaration_IsSubsumed()
    {
        var declaration = Declaration("a", global: true, nillable: true);

        await AssertSubsumed(Pair(declaration), Pair(declaration));
    }

    [Test]
    public async Task Element_Nillable_NeedsTheBaseToBeNillable()
    {
        await AssertNotSubsumed(Pair(Declaration("a", nillable: true)), Pair(Declaration("a")), Subsumes + "4.1", 10);
        await AssertSubsumed(Pair(Declaration("a")), Pair(Declaration("a", nillable: true)));
        await AssertSubsumed(Pair(Declaration("a", nillable: true)), Pair(Declaration("a", nillable: true)));
    }

    [Test]
    public async Task Element_FixedInTheBase_MustKeepTheValue()
    {
        var fixedOne = ElementValueConstraintKind.Fixed;

        await AssertNotSubsumed(
            Pair(Declaration("a")),
            Pair(Declaration("a", constraint: fixedOne, value: "1")),
            Subsumes + "4.2",
            10
        );
        await AssertNotSubsumed(
            Pair(Declaration("a", constraint: ElementValueConstraintKind.Default, value: "1")),
            Pair(Declaration("a", constraint: fixedOne, value: "1")),
            Subsumes + "4.2",
            10
        );
        await AssertNotSubsumed(
            Pair(Declaration("a", constraint: fixedOne, value: "2")),
            Pair(Declaration("a", constraint: fixedOne, value: "1")),
            Subsumes + "4.2",
            10
        );
        await AssertSubsumed(
            Pair(Declaration("a", constraint: fixedOne, value: "1")),
            Pair(Declaration("a", constraint: fixedOne, value: "1"))
        );
        await AssertSubsumed(
            Pair(Declaration("a", constraint: fixedOne, value: "1")),
            Pair(Declaration("a", constraint: ElementValueConstraintKind.Default, value: "2"))
        );
    }

    [Test]
    [Arguments("int", "1", "01", true)]
    [Arguments("decimal", "1.0", "1", true)]
    [Arguments("decimal", "1.0", "1.5", false)]
    [Arguments("string", "1", "01", false)]
    public async Task Element_FixedValue_IsComparedInTheValueSpace(
        string type,
        string baseValue,
        string value,
        bool valid
    )
    {
        var fixedKind = ElementValueConstraintKind.Fixed;
        var restriction = Pair(Declaration("a", constraint: fixedKind, value: value, type: TypeFixture.Xs(type)));
        var baseParticle = Pair(Declaration("a", constraint: fixedKind, value: baseValue, type: TypeFixture.Xs(type)));

        if (valid)
        {
            await AssertSubsumed(restriction, baseParticle);
        }
        else
        {
            await AssertNotSubsumed(restriction, baseParticle, Subsumes + "4.2", 10);
        }
    }

    [Test]
    public async Task Element_DisallowedSubstitutions_OfTheRestriction_MustBeASuperset()
    {
        await AssertNotSubsumed(
            Pair(Declaration("a")),
            Pair(Declaration("a", block: XsdDerivationMethod.Extension)),
            Subsumes + "4.4",
            10
        );
        await AssertNotSubsumed(
            Pair(Declaration("a", block: XsdDerivationMethod.Extension)),
            Pair(Declaration("a", block: XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction)),
            Subsumes + "4.4",
            10
        );
        await AssertSubsumed(
            Pair(Declaration("a", block: XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction)),
            Pair(Declaration("a", block: XsdDerivationMethod.Extension))
        );
    }

    [Test]
    public async Task Element_WithASimpleType_DerivedByRestriction_IsSubsumed()
    {
        var restricted = _types.Restrict("short", TypeFixture.Xs("string"));

        await AssertSubsumed(
            Pair(Declaration("a", type: restricted)),
            Pair(Declaration("a", type: TypeFixture.Xs("string")))
        );
    }

    [Test]
    public async Task Element_WithATypeThatIsNotDerivedFromTheBaseType_IsASchemaError() =>
        await AssertNotSubsumed(
            Pair(Declaration("a", type: TypeFixture.Xs("boolean"))),
            Pair(Declaration("a", type: TypeFixture.Xs("string"))),
            Subsumes + "4.5",
            10
        );

    [Test]
    public async Task Element_WithAComplexType_IsComparedByDerivation()
    {
        var parent = _types.Root("p");
        var restricted = _types.Complex("r", XsdDerivationMethod.Restriction, parent);
        var extended = _types.Complex("e", XsdDerivationMethod.Extension, parent);

        await AssertSubsumed(Pair(Declaration("a", type: restricted)), Pair(Declaration("a", type: parent)));
        await AssertNotSubsumed(
            Pair(Declaration("a", type: extended)),
            Pair(Declaration("a", type: parent)),
            Subsumes + "4.5",
            10
        );
    }

    [Test]
    public async Task Element_WithoutAType_HasTheUrType()
    {
        var parent = _types.Root("p");
        var restricted = _types.Complex("r", XsdDerivationMethod.Restriction, parent);

        await AssertSubsumed(Pair(Declaration("a", type: restricted)), Pair(Declaration("a")));
        await AssertNotSubsumed(
            Pair(Declaration("a")),
            Pair(Declaration("a", type: TypeFixture.Xs("string"))),
            Subsumes + "4.5",
            10
        );
    }

    [Test]
    public async Task Element_FixedValue_OfATypeThatIsNotKnownYet_IsComparedAsText()
    {
        var syntax = new FakeSyntaxElement("complexType", 3);
        var fixedKind = ElementValueConstraintKind.Fixed;
        var baseParticle = Pair(Declaration("a", constraint: fixedKind, value: "1", inlineType: syntax));

        await AssertSubsumed(
            Pair(Declaration("a", constraint: fixedKind, value: "1", inlineType: syntax)),
            baseParticle
        );
        await AssertNotSubsumed(
            Pair(Declaration("a", constraint: fixedKind, value: "01", inlineType: syntax)),
            baseParticle,
            Subsumes + "4.2",
            10
        );
    }

    [Test]
    public async Task Element_WithATypeThatIsNotKnownYet_IsNotCompared()
    {
        var syntax = new FakeSyntaxElement("complexType", 3);

        await AssertSubsumed(
            Pair(Declaration("a", unresolvedType: true)),
            Pair(Declaration("a", type: TypeFixture.Xs("string")))
        );
        await AssertSubsumed(
            Pair(Declaration("a", type: TypeFixture.Xs("string"))),
            Pair(Declaration("a", unresolvedType: true))
        );
        await AssertSubsumed(
            Pair(Declaration("a", inlineType: syntax)),
            Pair(Declaration("a", type: TypeFixture.Xs("string")))
        );
    }

    [Test]
    public async Task Element_InAGroup_IsComparedAtEveryPlaceWhereTheBaseMatchesIt() =>
        await AssertNotSubsumed(
            Seq([El("a"), Pair(Declaration("a", nillable: true), 20)]),
            Seq([El("a"), El("a")]),
            Subsumes + "4.1",
            20
        );

    [Test]
    public async Task Element_InARepeatedGroup_IsComparedOnceForEveryPairOfPositions() =>
        await AssertNotSubsumed(
            Seq([Pair(Declaration("a", nillable: true), 30), El("b")], 1, 3),
            Seq([Pair(Declaration("a")), El("b")], 1, 3),
            Subsumes + "4.1",
            30
        );
}
