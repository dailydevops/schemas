namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.V11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.V11;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>
/// The facet itself in a restriction step: it applies to the date and time types of 1.1 only, it is stated once, and its value is one
/// of required, prohibited, and optional.
/// </summary>
public class ExplicitTimezoneFacetTests
{
    [Test]
    [Arguments("boolean")]
    [Arguments("string")]
    [Arguments("decimal")]
    [Arguments("int")]
    [Arguments("float")]
    [Arguments("double")]
    [Arguments("duration")]
    [Arguments("hexBinary")]
    [Arguments("anyURI")]
    [Arguments("QName")]
    [Arguments("list:dateTime")]
    [Arguments("union:dateTime+date")]
    public async Task AFacetOutsideTheDateAndTimeTypes_IsALocatedSchemaError(string type)
    {
        var fixture = new FacetFixture();

        var result = ExplicitTimezoneCheck.Check(
            fixture.Base(type),
            default,
            [FacetFixture.Decl(FacetKind.ExplicitTimezone, "required", 31)],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(facet).IsNull();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-applicable-facets"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(31);
    }

    [Test]
    public async Task TheFacetInStrictVersion10_IsNotApplicable()
    {
        var fixture = new FacetFixture(XsdVersion.V10);

        var result = ExplicitTimezoneCheck.Check(
            fixture.Base("dateTime"),
            default,
            [FacetFixture.Decl(FacetKind.ExplicitTimezone, "required", 12)],
            XsdVersion.V10,
            fixture.Reporter,
            out _
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-applicable-facets"));
    }

    [Test]
    public async Task AFacetStatedTwice_IsReportedAtTheSecond_AndTheFirstIsKept()
    {
        var fixture = new FacetFixture();

        var result = ExplicitTimezoneCheck.Check(
            fixture.Base("date"),
            default,
            [
                FacetFixture.Decl(FacetKind.ExplicitTimezone, "required", 5),
                FacetFixture.Decl(FacetKind.ExplicitTimezone, "prohibited", 6),
            ],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(facet!.Mode).IsEqualTo(ExplicitTimezoneMode.Required);
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("src-single-facet-value"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(6);
    }

    [Test]
    [Arguments("")]
    [Arguments("Required")]
    [Arguments(" required")]
    [Arguments("true")]
    [Arguments("none")]
    public async Task AValueThatIsNoRepresentationOfTheFacet_IsALocatedSchemaError(string value)
    {
        var fixture = new FacetFixture();

        var result = ExplicitTimezoneCheck.Check(
            fixture.Base("dateTime"),
            default,
            [FacetFixture.Decl(FacetKind.ExplicitTimezone, value, 8)],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(facet).IsNull();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("st-restrict-facets"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(8);
    }

    [Test]
    [Arguments("anySimpleType")]
    [Arguments("anyAtomicType")]
    public async Task ABaseThatIsNoFamily_IsLeftToTheCaller(string type)
    {
        var fixture = new FacetFixture();
        var inherited = FacetFixture.Fixed(FacetKind.WhiteSpace);

        var result = ExplicitTimezoneCheck.Check(
            fixture.BuiltIn(type),
            inherited,
            [FacetFixture.Decl(FacetKind.ExplicitTimezone, "required")],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.WhiteSpace)).IsTrue();
        await Assert.That(facet).IsNull();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OtherFacetsOfTheStep_AreNotThisChecksConcern()
    {
        var fixture = new FacetFixture();

        var result = ExplicitTimezoneCheck.Check(
            fixture.Base("dateTime"),
            default,
            [
                FacetFixture.Decl(FacetKind.MaxLength, "abc"),
                FacetFixture.Decl(FacetKind.Pattern, "["),
                FacetFixture.Decl(FacetKind.ExplicitTimezone, "optional"),
            ],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(facet).IsNotNull();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AStepWithoutTheFacet_ChangesNothing()
    {
        var fixture = new FacetFixture();
        var inherited = FacetFixture.Fixed(FacetKind.ExplicitTimezone);

        var result = ExplicitTimezoneCheck.Check(
            fixture.Base("dateTime"),
            inherited,
            [],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Fixed).IsEqualTo(inherited);
        await Assert.That(facet).IsNull();
    }

    [Test]
    public async Task AGuard_RejectsANullArgumentAndAnUndefinedVersion()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("dateTime");
        IReadOnlyList<FacetDeclaration> step = [];

        await Assert
            .That(() => ExplicitTimezoneCheck.Check(null!, default, step, fixture.Version, fixture.Reporter, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ExplicitTimezoneCheck.Check(baseType, default, null!, fixture.Version, fixture.Reporter, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ExplicitTimezoneCheck.Check(baseType, default, step, fixture.Version, null!, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ExplicitTimezoneCheck.Check(baseType, default, step, (XsdVersion)7, fixture.Reporter, out _))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => ExplicitTimezoneCheck.Check(baseType, default, step, (XsdVersion)(-1), fixture.Reporter, out _))
            .Throws<ArgumentOutOfRangeException>();
    }
}
