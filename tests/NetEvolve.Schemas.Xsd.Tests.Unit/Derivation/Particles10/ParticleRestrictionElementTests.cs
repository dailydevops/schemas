namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;

/// <summary>The constraints of the element and wildcard cases and the occurrence range check (Structures 1.0, 3.9.6).</summary>
public class ParticleRestrictionElementTests
{
    private const XsdDerivationMethod Block = XsdDerivationMethod.Restriction;

    private readonly ParticleRestriction _check = Checker();
    private readonly TypeFixture _types = new();

    private ParticleViolation Elements(ElementDeclaration restriction, ElementDeclaration baseDeclaration) =>
        _check.Check(Of(restriction, line: 10), Of(baseDeclaration, line: 20));

    [Test]
    [Arguments(1L, 1L, 1L, 2L, true, "")]
    [Arguments(1L, 2L, 1L, 2L, true, "")]
    [Arguments(2L, 2L, 1L, 3L, true, "")]
    [Arguments(1L, Unbounded, 0L, Unbounded, true, "")]
    [Arguments(0L, 1L, 1L, 2L, false, "range-ok.1")]
    [Arguments(1L, 3L, 1L, 2L, false, "range-ok.2")]
    [Arguments(1L, Unbounded, 1L, 5L, false, "range-ok.2")]
    [Arguments(0L, 0L, 1L, 1L, false, "range-ok.1")]
    public async Task OccurrenceRange_OfTheRestriction_MustLieInsideTheBaseRange(
        long min,
        long max,
        long baseMin,
        long baseMax,
        bool valid,
        string clause
    )
    {
        var violation = _check.Check(El("a", min, max, 10), El("a", baseMin, baseMax, 20));

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
    public async Task SameParticle_IsAlwaysAValidRestriction()
    {
        var particle = Seq([El("a"), El("b")]);

        await AssertValid(_check.Check(particle, particle));
    }

    [Test]
    public async Task NameAndTypeOK_NamesInDifferentNamespaces_AreDifferent() =>
        await AssertViolation(
            _check.Check(El("a", ns: "urn:other", line: 10), El("a")),
            XsdRuleCodes.RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK.1",
            10
        );

    [Test]
    public async Task NameAndTypeOK_TwoGlobalDeclarations_AreNotComparedFurther()
    {
        var restriction = Declaration("a", global: true, nillable: true, block: XsdDerivationMethod.None);
        var baseDeclaration = Declaration(
            "a",
            global: true,
            constraint: ElementValueConstraintKind.Fixed,
            value: "1",
            block: Block
        );

        await AssertValid(Elements(restriction, baseDeclaration));
    }

    [Test]
    public async Task NameAndTypeOK_OneGlobalDeclaration_IsComparedLikeALocalOne() =>
        await AssertViolation(
            Elements(Declaration("a", global: true, nillable: true), Declaration("a")),
            XsdRuleCodes.RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK.3.2.1",
            10
        );

    [Test]
    [Arguments(false, false, true)]
    [Arguments(false, true, true)]
    [Arguments(true, true, true)]
    [Arguments(true, false, false)]
    public async Task NameAndTypeOK_Nillable_OfTheRestriction_NeedsTheBaseToBeNillable(
        bool nillable,
        bool baseNillable,
        bool valid
    )
    {
        var violation = Elements(Declaration("a", nillable: nillable), Declaration("a", nillable: baseNillable));

        if (valid)
        {
            await AssertValid(violation);
        }
        else
        {
            await AssertViolation(violation, XsdRuleCodes.RcaseNameAndTypeOK, "rcase-NameAndTypeOK.3.2.1", 10);
        }
    }

    [Test]
    [Arguments("Fixed", "1", "Fixed", "1", true)]
    [Arguments("Fixed", "1", "Fixed", "2", false)]
    [Arguments("Fixed", "1", "Default", "1", false)]
    [Arguments("Fixed", "1", "None", "", false)]
    [Arguments("Default", "1", "Fixed", "2", true)]
    [Arguments("Default", "1", "None", "", true)]
    [Arguments("None", "", "Fixed", "2", true)]
    public async Task NameAndTypeOK_ValueConstraint_OfAFixedBase_MustBeKept(
        string baseKind,
        string baseValue,
        string kind,
        string value,
        bool valid
    )
    {
        var violation = Elements(
            Declaration("a", constraint: Enum.Parse<ElementValueConstraintKind>(kind), value: value),
            Declaration("a", constraint: Enum.Parse<ElementValueConstraintKind>(baseKind), value: baseValue)
        );

        if (valid)
        {
            await AssertValid(violation);
        }
        else
        {
            await AssertViolation(violation, XsdRuleCodes.RcaseNameAndTypeOK, "rcase-NameAndTypeOK.3.2.2", 10);
        }
    }

    [Test]
    [Arguments(2, 2, true)]
    [Arguments(2, 3, true)]
    [Arguments(0, 0, true)]
    [Arguments(2, 0, false)]
    [Arguments(3, 1, false)]
    public async Task NameAndTypeOK_DisallowedSubstitutions_OfTheRestriction_MustBeASuperset(
        int baseBlock,
        int block,
        bool valid
    )
    {
        var violation = Elements(
            Declaration("a", block: (XsdDerivationMethod)block),
            Declaration("a", block: (XsdDerivationMethod)baseBlock)
        );

        if (valid)
        {
            await AssertValid(violation);
        }
        else
        {
            await AssertViolation(violation, XsdRuleCodes.RcaseNameAndTypeOK, "rcase-NameAndTypeOK.3.2.4", 10);
        }
    }

    [Test]
    public async Task NameAndTypeOK_SimpleType_DerivedByRestriction_IsValid()
    {
        var restricted = _types.Restrict("short", TypeFixture.Xs("string"));

        await AssertValid(
            Elements(Declaration("a", type: restricted), Declaration("a", type: TypeFixture.Xs("string")))
        );
        await AssertValid(
            Elements(Declaration("a", type: TypeFixture.Xs("string")), Declaration("a", type: TypeFixture.Xs("string")))
        );
    }

    [Test]
    public async Task NameAndTypeOK_SimpleType_NotDerivedFromTheBaseType_IsAnError() =>
        await AssertViolation(
            Elements(
                Declaration("a", type: TypeFixture.Xs("boolean")),
                Declaration("a", type: TypeFixture.Xs("string"))
            ),
            XsdRuleCodes.RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK.3.2.5",
            10
        );

    [Test]
    public async Task NameAndTypeOK_ComplexType_DerivedByRestriction_IsValid()
    {
        var parent = _types.Root("p");
        var restricted = _types.Complex("r", XsdDerivationMethod.Restriction, parent);

        await AssertValid(Elements(Declaration("a", type: restricted), Declaration("a", type: parent)));
    }

    [Test]
    public async Task NameAndTypeOK_ComplexType_DerivedByExtension_IsAnError()
    {
        var parent = _types.Root("p");
        var extended = _types.Complex("e", XsdDerivationMethod.Extension, parent);

        await AssertViolation(
            Elements(Declaration("a", type: extended), Declaration("a", type: parent)),
            XsdRuleCodes.RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK.3.2.5",
            10
        );
    }

    [Test]
    public async Task NameAndTypeOK_ElementWithoutAType_HasTheUrType()
    {
        var parent = _types.Root("p");
        var restricted = _types.Complex("r", XsdDerivationMethod.Restriction, parent);

        await AssertValid(Elements(Declaration("a"), Declaration("a")));
        await AssertValid(Elements(Declaration("a", type: restricted), Declaration("a")));
        await AssertViolation(
            Elements(Declaration("a"), Declaration("a", type: TypeFixture.Xs("string"))),
            XsdRuleCodes.RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK.3.2.5",
            10
        );
    }

    [Test]
    public async Task NameAndTypeOK_TypeThatIsNotKnownYet_IsNotCompared()
    {
        var syntax = new FakeSyntaxElement("complexType", 3);

        await AssertValid(
            Elements(Declaration("a", unresolvedType: true), Declaration("a", type: TypeFixture.Xs("string")))
        );
        await AssertValid(
            Elements(Declaration("a", type: TypeFixture.Xs("string")), Declaration("a", unresolvedType: true))
        );
        await AssertValid(
            Elements(Declaration("a", inlineType: syntax), Declaration("a", type: TypeFixture.Xs("string")))
        );
    }

    [Test]
    [Arguments("urn:test", true)]
    [Arguments("urn:other", false)]
    public async Task NSCompat_Namespace_OfTheElement_MustBeAllowedByTheWildcard(string allowed, bool valid)
    {
        var violation = _check.Check(El("a", line: 10), Any(Only(allowed), line: 20));

        if (valid)
        {
            await AssertValid(violation);
        }
        else
        {
            await AssertViolation(violation, XsdRuleCodes.RcaseNSCompat, "rcase-NSCompat.1", 10);
        }
    }

    [Test]
    public async Task NSCompat_OccurrenceRange_MustLieInsideTheWildcardRange()
    {
        await AssertValid(_check.Check(El("a", 1, 2), Any(null, XsdProcessContents.Lax, 0, Unbounded)));
        await AssertViolation(
            _check.Check(El("a", 0, 2, 10), Any(null, XsdProcessContents.Lax, 1, 2)),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );
    }

    [Test]
    [Arguments("Strict", "Strict", true)]
    [Arguments("Strict", "Lax", true)]
    [Arguments("Strict", "Skip", true)]
    [Arguments("Lax", "Skip", true)]
    [Arguments("Lax", "Strict", false)]
    [Arguments("Skip", "Lax", false)]
    public async Task NSSubset_ProcessContents_MustBeIdenticalOrStronger(string process, string baseProcess, bool valid)
    {
        var violation = _check.Check(
            Any(null, Enum.Parse<XsdProcessContents>(process), line: 10),
            Any(null, Enum.Parse<XsdProcessContents>(baseProcess), line: 20)
        );

        if (valid)
        {
            await AssertValid(violation);
        }
        else
        {
            await AssertViolation(violation, XsdRuleCodes.RcaseNSSubset, "rcase-NSSubset.3", 10);
        }
    }

    [Test]
    public async Task NSSubset_ProcessContents_AreNotComparedAgainstTheWildcardOfTheUrType()
    {
        var anyType = ComplexTypeDefinition.AnyType.ContentParticle!;

        // The wildcard of the ur-type is lax, so a wildcard that skips could not restrict it otherwise.
        await AssertValid(_check.Check(Seq([Any(null, XsdProcessContents.Skip, 0, Unbounded)]), anyType));
        await AssertViolation(
            _check.Check(
                Seq([Any(null, XsdProcessContents.Skip, 0, Unbounded, 10)]),
                Seq([Any(null, XsdProcessContents.Lax, 0, Unbounded, 20)])
            ),
            XsdRuleCodes.RcaseNSSubset,
            "rcase-NSSubset.3",
            10
        );
    }

    [Test]
    public async Task NSSubset_OccurrenceRange_MustLieInsideTheBaseRange() =>
        await AssertViolation(
            _check.Check(Any(Only(Tns), XsdProcessContents.Lax, 0, 2, 10), Any(null, XsdProcessContents.Lax, 1, 2, 20)),
            XsdRuleCodes.RangeOk,
            "range-ok.1",
            10
        );

    [Test]
    [Arguments("urn:a", "urn:a", true)]
    [Arguments("urn:a", "urn:b", false)]
    public async Task NSSubset_Namespaces_MustBeASubset(string restricted, string baseNamespace, bool valid)
    {
        var violation = _check.Check(Any(Only(restricted), line: 10), Any(Only(baseNamespace, "urn:c"), line: 20));

        if (valid)
        {
            await AssertValid(violation);
        }
        else
        {
            await AssertViolation(violation, XsdRuleCodes.RcaseNSSubset, "rcase-NSSubset.2", 10);
        }
    }

    [Test]
    public async Task Forbidden_ReportsTheRestrictingParticle_AndNamesTheKinds()
    {
        var violation = _check.Check(Any(line: 10), El("a", line: 20));

        await AssertViolation(violation, XsdRuleCodes.CosParticleRestrict, "cos-particle-restrict.2", 10);
        await Assert.That(violation.Message).Contains("wildcard");
        await Assert.That(violation.Message).Contains("element");
        await Assert.That(_check.Check(All([El("a"), El("b")], line: 10), El("a")).Message).Contains("all group");
        await Assert.That(_check.Check(Cho([El("a"), El("b")], line: 10), El("a")).Message).Contains("choice group");
        await Assert.That(_check.Check(Seq([El("a"), El("b")], line: 10), El("a")).Message).Contains("sequence group");
    }
}
