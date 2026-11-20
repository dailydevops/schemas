namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.V11;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.V11;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>
/// The explicitTimezone facet of a restriction: a required or prohibited base cannot change, optional may become either, and a facet
/// that the base fixes cannot change at all (<c>timezone-valid-restriction</c>, Datatypes 1.1, 4.3.14.4).
/// </summary>
public class ExplicitTimezoneRestrictionTests
{
    private static FacetCheckResult Check(
        FacetFixture fixture,
        IXsdComponent baseType,
        FacetKindSet baseFixed,
        params FacetDeclaration[] step
    ) => ExplicitTimezoneCheck.Check(baseType, baseFixed, step, fixture.Version, fixture.Reporter, out _);

    private static ExplicitTimezoneMode Parse(string lexical) =>
        lexical switch
        {
            "required" => ExplicitTimezoneMode.Required,
            "prohibited" => ExplicitTimezoneMode.Prohibited,
            _ => ExplicitTimezoneMode.Optional,
        };

    [Test]
    [Arguments("required", "required", true)]
    [Arguments("required", "prohibited", false)]
    [Arguments("required", "optional", false)]
    [Arguments("prohibited", "prohibited", true)]
    [Arguments("prohibited", "required", false)]
    [Arguments("prohibited", "optional", false)]
    [Arguments("optional", "required", true)]
    [Arguments("optional", "prohibited", true)]
    [Arguments("optional", "optional", true)]
    public async Task ARequiredOrProhibitedBase_CannotChange_OptionalMayBecomeEither(
        string baseMode,
        string derivedMode,
        bool valid
    )
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("dateTime", new ExplicitTimezoneFacet(Parse(baseMode)));

        var result = Check(fixture, baseType, default, FacetFixture.Decl(FacetKind.ExplicitTimezone, derivedMode, 21));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        await Assert.That(fixture.Issues.Count).IsEqualTo(valid ? 0 : 1);
        if (!valid)
        {
            await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("timezone-valid-restriction"));
            await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(21);
            await Assert.That(fixture.Issues[0].Location.Column).IsEqualTo(9);
        }
    }

    [Test]
    [Arguments("dateTime")]
    [Arguments("date")]
    [Arguments("time")]
    [Arguments("gYear")]
    [Arguments("gYearMonth")]
    [Arguments("gMonth")]
    [Arguments("gMonthDay")]
    [Arguments("gDay")]
    public async Task ABaseWithoutTheFacet_TakesEveryValue(string type)
    {
        var fixture = new FacetFixture();

        foreach (var mode in new[] { "required", "prohibited", "optional" })
        {
            var result = Check(
                fixture,
                fixture.Base(type),
                default,
                FacetFixture.Decl(FacetKind.ExplicitTimezone, mode)
            );

            await Assert.That(result.IsValid).IsTrue();
        }

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheBuiltInTypeItself_CanBeTheBase()
    {
        var fixture = new FacetFixture();

        var result = Check(
            fixture,
            fixture.BuiltIn("date"),
            default,
            FacetFixture.Decl(FacetKind.ExplicitTimezone, "required")
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheBaseOfARestrictionOfARestriction_IsTheNearestFacet()
    {
        var fixture = new FacetFixture();
        var optional = fixture.Base("dateTime", new ExplicitTimezoneFacet(ExplicitTimezoneMode.Optional));
        var required = fixture.Derive(optional, new ExplicitTimezoneFacet(ExplicitTimezoneMode.Required));

        var same = Check(fixture, required, default, FacetFixture.Decl(FacetKind.ExplicitTimezone, "required"));
        var other = Check(fixture, required, default, FacetFixture.Decl(FacetKind.ExplicitTimezone, "prohibited"));

        await Assert.That(same.IsValid).IsTrue();
        await Assert.That(other.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("required", true)]
    [Arguments("prohibited", false)]
    [Arguments("optional", false)]
    public async Task DateTimeStamp_RequiresTheTimezoneInEveryRestriction(string derivedMode, bool valid)
    {
        var fixture = new FacetFixture();

        var onBuiltIn = Check(
            fixture,
            fixture.BuiltIn("dateTimeStamp"),
            default,
            FacetFixture.Decl(FacetKind.ExplicitTimezone, derivedMode, 14)
        );
        var onWrapped = Check(
            fixture,
            fixture.Wrapped("dateTimeStamp"),
            default,
            FacetFixture.Decl(FacetKind.ExplicitTimezone, derivedMode, 15)
        );
        var onRestriction = Check(
            fixture,
            fixture.Derive(fixture.Wrapped("dateTimeStamp")),
            default,
            FacetFixture.Decl(FacetKind.ExplicitTimezone, derivedMode, 16)
        );

        await Assert.That(onBuiltIn.IsValid).IsEqualTo(valid);
        await Assert.That(onWrapped.IsValid).IsEqualTo(valid);
        await Assert.That(onRestriction.IsValid).IsEqualTo(valid);
        await Assert.That(fixture.Issues.Count).IsEqualTo(valid ? 0 : 3);
        if (!valid)
        {
            await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("timezone-valid-restriction"));
            await Assert.That(fixture.Issues[2].Location.Line).IsEqualTo(16);
        }
    }

    [Test]
    [Arguments("optional", "required")]
    [Arguments("optional", "prohibited")]
    [Arguments("required", "prohibited")]
    [Arguments("prohibited", "optional")]
    public async Task AFixedFacet_CannotChange_RepeatingItIsFine(string baseMode, string other)
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("dateTime", new ExplicitTimezoneFacet(Parse(baseMode)));
        var fixedFacets = FacetFixture.Fixed(FacetKind.ExplicitTimezone);

        var repeated = Check(fixture, baseType, fixedFacets, FacetFixture.Decl(FacetKind.ExplicitTimezone, baseMode));
        await Assert.That(repeated.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);

        var changed = Check(fixture, baseType, fixedFacets, FacetFixture.Decl(FacetKind.ExplicitTimezone, other, 33));
        await Assert.That(changed.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("timezone-valid-restriction"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(33);
    }

    [Test]
    public async Task TheFixedFlagOfTheStep_CarriesIntoTheResult()
    {
        var fixture = new FacetFixture();
        var inherited = FacetFixture.Fixed(FacetKind.MaxInclusive);

        var fixedStep = Check(
            fixture,
            fixture.Base("date"),
            inherited,
            FacetFixture.Decl(FacetKind.ExplicitTimezone, "required", isFixed: true)
        );
        var plainStep = Check(
            fixture,
            fixture.Base("date"),
            inherited,
            FacetFixture.Decl(FacetKind.ExplicitTimezone, "required")
        );

        await Assert.That(fixedStep.Fixed.Contains(FacetKind.ExplicitTimezone)).IsTrue();
        await Assert.That(fixedStep.Fixed.Contains(FacetKind.MaxInclusive)).IsTrue();
        await Assert.That(plainStep.Fixed.Contains(FacetKind.ExplicitTimezone)).IsFalse();
        await Assert.That(plainStep.Fixed.Contains(FacetKind.MaxInclusive)).IsTrue();
    }

    [Test]
    public async Task ARejectedFacet_IsNotFixedAndYieldsNoFacet()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("dateTime", new ExplicitTimezoneFacet(ExplicitTimezoneMode.Required));

        var result = ExplicitTimezoneCheck.Check(
            baseType,
            default,
            [FacetFixture.Decl(FacetKind.ExplicitTimezone, "prohibited", isFixed: true)],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Fixed.Contains(FacetKind.ExplicitTimezone)).IsFalse();
        await Assert.That(facet).IsNull();
    }

    [Test]
    [Arguments("required", ExplicitTimezoneMode.Required)]
    [Arguments("prohibited", ExplicitTimezoneMode.Prohibited)]
    [Arguments("optional", ExplicitTimezoneMode.Optional)]
    public async Task TheFacetOfAValidStep_CarriesTheMode(string lexical, ExplicitTimezoneMode mode)
    {
        var fixture = new FacetFixture();

        var result = ExplicitTimezoneCheck.Check(
            fixture.Base("time"),
            default,
            [FacetFixture.Decl(FacetKind.ExplicitTimezone, lexical)],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(facet!.Mode).IsEqualTo(mode);
        await Assert.That(facet.Kind).IsEqualTo(FacetKind.ExplicitTimezone);
    }
}
