namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class DerivationValidRestrictionTests
{
    private const XsdDerivationMethod None = XsdDerivationMethod.None;
    private const XsdDerivationMethod Ext = XsdDerivationMethod.Extension;
    private const XsdDerivationMethod Res = XsdDerivationMethod.Restriction;
    private const string Rule = "derivation-ok-restriction";

    private readonly TypeFixture _f = new();

    private async Task AssertSingle(string clause)
    {
        await Assert.That(_f.Issues).Count().IsEqualTo(1);
        await Assert.That(_f.Issues[0].Constraint).IsEqualTo(Rule + "." + clause);
        await Assert.That(FakeRules.Find(_f.Issues[0].Code).Id).IsEqualTo(Rule);
        await Assert.That(_f.Issues[0].Location.Line).IsEqualTo(7);
    }

    private async Task AssertNone() => await Assert.That(_f.Issues).Count().IsEqualTo(0);

    private static Particle Seq(params Particle[] particles) => TypeFixture.Sequence(particles);

    private static Particle El(string name, long min = 1, long max = 1) => TypeFixture.Element(name, min, max);

    private ComplexTypeDefinition Restricted(
        IXsdComponent baseType,
        Particle? content = null,
        bool mixed = false,
        IReadOnlyList<AttributeUse>? uses = null,
        IWildcard? wildcard = null
    )
    {
        var type = _f.Complex("d", Res, baseType, content, mixed, uses, wildcard);
        TypeFixture.Checker().Report(type, _f.Reporter);
        return type;
    }

    [Test]
    [Arguments(0, false)]
    [Arguments(1, false)]
    [Arguments(2, true)]
    [Arguments(3, true)]
    public async Task Restriction_OfABaseThatIsFinalForRestriction_IsALocatedSchemaError(int final, bool error)
    {
        var parent = _f.Root("a", Seq(El("x")), final: (XsdDerivationMethod)final);

        _ = Restricted(parent, Seq(El("x")));

        if (error)
        {
            await AssertSingle("1");
        }
        else
        {
            await AssertNone();
        }
    }

    [Test]
    public async Task Restriction_OfTheUrType_IsAlwaysValid()
    {
        var type = _f.Root(
            "r",
            Seq(El("x")),
            true,
            [_f.Use("p", AttributeUseKind.Required)],
            TypeFixture.Wild(TypeFixture.Namespaces("urn:a"), XsdProcessContents.Skip)
        );

        TypeFixture.Checker().Report(type, _f.Reporter);
        TypeFixture.Checker().Report(_f.Complex("c", Res, TypeFixture.Registered("anyType")), _f.Reporter);

        await AssertNone();
    }

    [Test]
    public async Task Restriction_ThatKeepsTheAttributeUses_IsValid()
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("req", AttributeUseKind.Required), _f.Use("opt")]);

        _ = Restricted(parent, Seq(El("x")), false, [_f.Use("req", AttributeUseKind.Required)]);

        await AssertNone();
    }

    [Test]
    public async Task Restriction_ThatAddsAnAttribute_ToABaseWithoutAWildcard_IsAnError()
    {
        var parent = _f.Root("a", Seq(El("x")));

        _ = Restricted(parent, Seq(El("x")), false, [_f.Use("extra")]);

        await AssertSingle("2.2");
    }

    [Test]
    public async Task Restriction_ThatAddsAnAttribute_ToABaseWithAnotherNamespaceWildcard_IsAnError()
    {
        var parent = _f.Root("a", Seq(El("x")), false, null, TypeFixture.Wild(TypeFixture.Namespaces("urn:other")));

        _ = Restricted(
            parent,
            Seq(El("x")),
            false,
            [_f.Use("extra")],
            TypeFixture.Wild(TypeFixture.Namespaces("urn:other"))
        );

        await AssertSingle("2.2");
    }

    [Test]
    public async Task Restriction_ThatAddsAnAttributeTheBaseWildcardAllows_IsValid()
    {
        var parent = _f.Root("a", Seq(El("x")), false, null, TypeFixture.Wild(TypeFixture.Namespaces(TypeFixture.Tns)));

        _ = Restricted(
            parent,
            Seq(El("x")),
            false,
            [_f.Use("extra")],
            TypeFixture.Wild(TypeFixture.Namespaces(TypeFixture.Tns))
        );

        await AssertNone();
    }

    [Test]
    public async Task Restriction_ThatRelaxesARequiredAttribute_IsAnError()
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("req", AttributeUseKind.Required)]);

        _ = Restricted(parent, Seq(El("x")), false, [_f.Use("req")]);

        await AssertSingle("2.1.1");
    }

    [Test]
    public async Task Restriction_ThatProhibitsARequiredAttribute_IsAnError()
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("req", AttributeUseKind.Required)]);

        _ = Restricted(parent, Seq(El("x")), false, [_f.Use("req", AttributeUseKind.Prohibited)]);

        await AssertSingle("3");
    }

    [Test]
    public async Task Restriction_ThatRequiresAnOptionalAttribute_IsValid()
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("opt")]);

        _ = Restricted(parent, Seq(El("x")), false, [_f.Use("opt", AttributeUseKind.Required)]);

        await AssertNone();
    }

    [Test]
    public async Task Restriction_ThatProhibitsAnOptionalAttribute_IsValid()
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("opt")]);

        var type = Restricted(parent, Seq(El("x")), false, [_f.Use("opt", AttributeUseKind.Prohibited)]);

        await Assert.That(type.AttributeUses).Count().IsEqualTo(0);
        await AssertNone();
    }

    [Test]
    [Arguments("int", "int", true)]
    [Arguments("int", "short", true)]
    [Arguments("int", "decimal", false)]
    [Arguments("int", "string", false)]
    public async Task Restriction_OfAnAttributeType_NeedsATypeDerivedFromTheBaseType(
        string baseType,
        string derivedType,
        bool valid
    )
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("p", type: baseType)]);

        _ = Restricted(parent, Seq(El("x")), false, [_f.Use("p", type: derivedType)]);

        if (valid)
        {
            await AssertNone();
        }
        else
        {
            await AssertSingle("2.1.2");
        }
    }

    [Test]
    [Arguments((int)AttributeValueConstraintKind.None, null, (int)AttributeValueConstraintKind.None, null, true)]
    [Arguments((int)AttributeValueConstraintKind.Default, "1", (int)AttributeValueConstraintKind.None, null, true)]
    [Arguments((int)AttributeValueConstraintKind.Default, "1", (int)AttributeValueConstraintKind.Fixed, "2", true)]
    [Arguments((int)AttributeValueConstraintKind.Fixed, "1", (int)AttributeValueConstraintKind.Fixed, "1", true)]
    [Arguments((int)AttributeValueConstraintKind.Fixed, "1", (int)AttributeValueConstraintKind.Fixed, "2", false)]
    [Arguments((int)AttributeValueConstraintKind.Fixed, "1", (int)AttributeValueConstraintKind.Default, "1", false)]
    [Arguments((int)AttributeValueConstraintKind.Fixed, "1", (int)AttributeValueConstraintKind.None, null, false)]
    public async Task Restriction_OfAFixedAttributeValue_MustKeepItFixed(
        int baseKind,
        string? baseValue,
        int kind,
        string? value,
        bool valid
    )
    {
        var parent = _f.Root(
            "a",
            Seq(El("x")),
            false,
            [_f.Use("p", AttributeUseKind.Optional, "string", (AttributeValueConstraintKind)baseKind, baseValue)]
        );

        _ = Restricted(
            parent,
            Seq(El("x")),
            false,
            [_f.Use("p", AttributeUseKind.Optional, "string", (AttributeValueConstraintKind)kind, value)]
        );

        if (valid)
        {
            await AssertNone();
        }
        else
        {
            await AssertSingle("2.1.3");
        }
    }

    [Test]
    public async Task Restriction_ThatAddsAWildcard_ToABaseWithoutOne_IsAnError()
    {
        var parent = _f.Root("a", Seq(El("x")));

        _ = Restricted(parent, Seq(El("x")), false, null, TypeFixture.Wild(TypeFixture.Namespaces("urn:a")));

        await AssertSingle("4.1");
    }

    [Test]
    public async Task Restriction_ThatWidensTheAttributeWildcard_IsAnError()
    {
        var parent = _f.Root("a", Seq(El("x")), false, null, TypeFixture.Wild(TypeFixture.Namespaces("urn:a")));

        _ = Restricted(parent, Seq(El("x")), false, null, TypeFixture.Wild(TypeFixture.Namespaces("urn:a", "urn:b")));

        await AssertSingle("4.2");
    }

    [Test]
    public async Task Restriction_ThatNarrowsTheAttributeWildcard_IsValid()
    {
        var parent = _f.Root(
            "a",
            Seq(El("x")),
            false,
            null,
            TypeFixture.Wild(TypeFixture.Namespaces("urn:a", "urn:b"))
        );

        _ = Restricted(parent, Seq(El("x")), false, null, TypeFixture.Wild(TypeFixture.Namespaces("urn:a")));

        await AssertNone();
    }

    [Test]
    [Arguments((int)XsdProcessContents.Strict, (int)XsdProcessContents.Strict, true)]
    [Arguments((int)XsdProcessContents.Lax, (int)XsdProcessContents.Strict, true)]
    [Arguments((int)XsdProcessContents.Skip, (int)XsdProcessContents.Strict, true)]
    [Arguments((int)XsdProcessContents.Skip, (int)XsdProcessContents.Lax, true)]
    [Arguments((int)XsdProcessContents.Lax, (int)XsdProcessContents.Lax, true)]
    [Arguments((int)XsdProcessContents.Skip, (int)XsdProcessContents.Skip, true)]
    [Arguments((int)XsdProcessContents.Strict, (int)XsdProcessContents.Lax, false)]
    [Arguments((int)XsdProcessContents.Strict, (int)XsdProcessContents.Skip, false)]
    [Arguments((int)XsdProcessContents.Lax, (int)XsdProcessContents.Skip, false)]
    public async Task Restriction_OfTheWildcardProcessContents_MustBeIdenticalOrStronger(
        int baseProcess,
        int process,
        bool valid
    )
    {
        var ns = TypeFixture.Namespaces("urn:a");
        var parent = _f.Root("a", Seq(El("x")), false, null, TypeFixture.Wild(ns, (XsdProcessContents)baseProcess));

        _ = Restricted(parent, Seq(El("x")), false, null, TypeFixture.Wild(ns, (XsdProcessContents)process));

        if (valid)
        {
            await AssertNone();
        }
        else
        {
            await AssertSingle("4.3");
        }
    }

    [Test]
    public async Task Restriction_ToSimpleContent_WithADerivedSimpleType_IsValid()
    {
        var parent = _f.SimpleContent("a", Ext, TypeFixture.Xs("int"));

        var type = _f.SimpleContent("d", Res, parent, _f.Restrict("small", TypeFixture.Xs("short")));
        TypeFixture.Checker().Report(type, _f.Reporter);

        await AssertNone();
    }

    [Test]
    public async Task Restriction_ToSimpleContent_KeepingTheBaseSimpleType_IsValid()
    {
        var parent = _f.SimpleContent("a", Ext, TypeFixture.Xs("int"));

        var type = _f.SimpleContent("d", Res, parent);
        TypeFixture.Checker().Report(type, _f.Reporter);

        await AssertNone();
    }

    [Test]
    public async Task Restriction_ToSimpleContent_WithAnUnrelatedSimpleType_IsAnError()
    {
        var parent = _f.SimpleContent("a", Ext, TypeFixture.Xs("int"));

        var type = _f.SimpleContent("d", Res, parent, TypeFixture.Xs("string"));
        TypeFixture.Checker().Report(type, _f.Reporter);

        await AssertSingle("5.2.2.1");
    }

    [Test]
    public async Task Restriction_OfMixedContent_ToSimpleContent_NeedsAnEmptiableParticle()
    {
        var emptiable = _f.Root("m", Seq(El("x", 0, 1)), mixed: true);
        var required = _f.Root("n", Seq(El("x")), mixed: true);

        TypeFixture.Checker().Report(_f.SimpleContent("d", Res, emptiable, TypeFixture.Xs("string")), _f.Reporter);
        await AssertNone();

        TypeFixture.Checker().Report(_f.SimpleContent("e", Res, required, TypeFixture.Xs("string")), _f.Reporter);
        await AssertSingle("5.2.2.2");
    }

    [Test]
    public async Task Restriction_ToEmpty_OfAnEmptyBase_IsValid()
    {
        _ = Restricted(_f.Root("a"));

        await AssertNone();
    }

    [Test]
    [Arguments("a?", true)]
    [Arguments("a", false)]
    [Arguments("a?,b?", true)]
    [Arguments("a?,b", false)]
    [Arguments("a|b?", true)]
    [Arguments("a|b", false)]
    [Arguments("empty-sequence", true)]
    [Arguments("empty-choice", false)]
    [Arguments("group?", true)]
    [Arguments("group-2x-of-emptiable", true)]
    [Arguments("group-required", false)]
    public async Task Restriction_ToEmpty_NeedsAnEmptiableBaseParticle(string shape, bool valid)
    {
        var parent = _f.Root("a", Shape(shape));

        _ = Restricted(parent);

        if (valid)
        {
            await AssertNone();
        }
        else
        {
            await AssertSingle("5.3.2");
        }
    }

    private static Particle Shape(string shape) =>
        shape switch
        {
            "a?" => Seq(El("a", 0, 1)),
            "a" => Seq(El("a")),
            "a?,b?" => Seq(El("a", 0, 1), El("b", 0, 1)),
            "a?,b" => Seq(El("a", 0, 1), El("b")),
            "a|b?" => TypeFixture.Group(ModelGroupCompositor.Choice, 1, 1, El("a"), El("b", 0, 1)),
            "a|b" => TypeFixture.Group(ModelGroupCompositor.Choice, 1, 1, El("a"), El("b")),
            "empty-sequence" => Seq(),
            "empty-choice" => TypeFixture.Group(ModelGroupCompositor.Choice, 1, 1),
            "group?" => Seq(TypeFixture.Group(ModelGroupCompositor.Sequence, 0, 1, El("a"))),
            "group-2x-of-emptiable" => Seq(TypeFixture.Group(ModelGroupCompositor.Sequence, 2, 2, El("a", 0, 1))),
            "group-required" => Seq(TypeFixture.Group(ModelGroupCompositor.Sequence, 1, 1, El("a"))),
            _ => throw new InvalidOperationException(shape),
        };

    [Test]
    public async Task Restriction_FromMixedToElementOnly_IsValid()
    {
        var parent = _f.Root("a", Seq(El("x")), mixed: true);

        _ = Restricted(parent, Seq(El("x")));

        await AssertNone();
    }

    [Test]
    public async Task Restriction_FromElementOnlyToMixed_IsAnError()
    {
        var parent = _f.Root("a", Seq(El("x")));

        _ = Restricted(parent, Seq(El("x")), mixed: true);

        await AssertSingle("5.4.1");
    }

    [Test]
    public async Task Restriction_ToMixedContent_OfASimpleContentBase_IsAnError()
    {
        var parent = _f.SimpleContent("a", Ext, TypeFixture.Xs("string"));

        _ = Restricted(parent, Seq(El("x")), mixed: true);

        await AssertSingle("5.4.1");
    }

    [Test]
    public async Task Restriction_ToSimpleContent_OfAnotherKindOfContent_IsAnError()
    {
        var other = _f.Root("o", Seq(El("x")));
        var type = _f.SimpleContent("d", Res, _f.SimpleContent("a", Ext, TypeFixture.Xs("string")));

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("5.2.2");
    }

    [Test]
    public async Task Restriction_OfElementContent_IsNotJudgedAsAParticleRestrictionHere()
    {
        // Particle Valid (Restriction) is XSD-62; this check leaves the particle of the content alone, also against
        // a base that has none.
        var parent = _f.Root("a", Seq(El("x")));
        var simple = _f.SimpleContent("s", Ext, TypeFixture.Xs("string"));

        _ = Restricted(parent, Seq(El("unrelated", 3, 4)));
        _ = Restricted(simple, Seq(El("unrelated", 3, 4)));

        await AssertNone();
    }

    [Test]
    public async Task Restriction_AgainstASimpleBase_IsAnError()
    {
        var type = _f.Complex("d", Res, _f.Root("a"));

        TypeFixture.Checker().Report(type, TypeFixture.Xs("string"), _f.Reporter);

        await AssertSingle("1");
    }

    [Test]
    public async Task Check_RestrictionMethod_NamesTheRestrictionRule()
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("req", AttributeUseKind.Required)]);
        var type = _f.Complex("d", Res, parent, Seq(El("x")), false, [_f.Use("req")]);

        var result = TypeFixture.Checker().Check(type, parent, Res, None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ViolatedRule!.Id).IsEqualTo(Rule);
    }

    [Test]
    public async Task Check_RestrictionMethod_IgnoresTheBlockSet()
    {
        var parent = _f.Root("a", Seq(El("x")));
        var type = _f.Complex("d", Res, parent, Seq(El("x")));

        await Assert.That(TypeFixture.Checker().Check(type, parent, Res, Ext | Res).IsValid).IsTrue();
    }

    [Test]
    public async Task Check_RestrictionMethod_OfASimpleDerivedType_FollowsTheSimpleChain()
    {
        var myInt = _f.Restrict("myInt", TypeFixture.Xs("int"));
        var check = TypeFixture.Checker();

        await Assert.That(check.Check(myInt, TypeFixture.Xs("decimal"), Res, None).IsValid).IsTrue();
        await Assert.That(check.Check(myInt, TypeFixture.Xs("string"), Res, None).IsValid).IsFalse();
    }

    [Test]
    [Arguments("int", "1", "01", true)]
    [Arguments("decimal", "1.0", "1", true)]
    [Arguments("decimal", "1.0", "1.5", false)]
    [Arguments("string", "1", "01", false)]
    public async Task Restriction_OfAFixedAttributeValue_ComparesTheValueSpace(
        string type,
        string baseValue,
        string value,
        bool valid
    )
    {
        var parent = _f.Root(
            "a",
            Seq(El("x")),
            false,
            [_f.Use("p", AttributeUseKind.Optional, type, AttributeValueConstraintKind.Fixed, baseValue)]
        );

        _ = Restricted(
            parent,
            Seq(El("x")),
            false,
            [_f.Use("p", AttributeUseKind.Optional, type, AttributeValueConstraintKind.Fixed, value)]
        );

        if (valid)
        {
            await AssertNone();
        }
        else
        {
            await AssertSingle("2.1.3");
        }
    }

    [Test]
    public async Task Restriction_ThatBreaksSeveralClauses_ReportsTheFirstOnly()
    {
        var parent = _f.Root("a", Seq(El("x")), false, [_f.Use("req", AttributeUseKind.Required)]);

        // Relaxes the required attribute (2.1.1), adds one the base does not allow (2.2), and widens the wildcard (4.1).
        _ = Restricted(
            parent,
            Seq(El("x")),
            false,
            [_f.Use("req"), _f.Use("extra")],
            TypeFixture.Wild(TypeFixture.Namespaces("urn:a"))
        );

        await AssertSingle("2.1.1");
    }
}
