namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.V11;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.V11;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>
/// A type that restricts a date or time type with the explicitTimezone facet accepts or rejects a value by its timezone: required
/// rejects a value without one, prohibited rejects a value with one, optional accepts both (Datatypes 1.1, 4.3.14).
/// </summary>
public class ExplicitTimezoneValueTests
{
    [Test]
    [Arguments("dateTime", "2020-01-02T03:04:05", "2020-01-02T03:04:05Z")]
    [Arguments("date", "2020-01-02", "2020-01-02Z")]
    [Arguments("time", "03:04:05", "03:04:05+01:00")]
    [Arguments("gYear", "2020", "2020Z")]
    [Arguments("gYearMonth", "2020-01", "2020-01-05:00")]
    [Arguments("gMonth", "--01", "--01Z")]
    [Arguments("gMonthDay", "--01-02", "--01-02Z")]
    [Arguments("gDay", "---02", "---02Z")]
    public async Task RequiredProhibitedAndOptional_DecideByTheTimezoneOfTheValue(
        string type,
        string withoutTimezone,
        string withTimezone
    )
    {
        var required = Derive(type, "required");
        var prohibited = Derive(type, "prohibited");
        var optional = Derive(type, "optional");

        await Assert.That(required.Validate(withoutTimezone.AsSpan()).IsValid).IsFalse();
        await Assert.That(required.Validate(withTimezone.AsSpan()).IsValid).IsTrue();
        await Assert.That(prohibited.Validate(withoutTimezone.AsSpan()).IsValid).IsTrue();
        await Assert.That(prohibited.Validate(withTimezone.AsSpan()).IsValid).IsFalse();
        await Assert.That(optional.Validate(withoutTimezone.AsSpan()).IsValid).IsTrue();
        await Assert.That(optional.Validate(withTimezone.AsSpan()).IsValid).IsTrue();
    }

    [Test]
    public async Task TheViolatedFacet_IsExplicitTimezone()
    {
        var required = Derive("dateTime", "required");
        var prohibited = Derive("dateTime", "prohibited");

        await Assert
            .That(required.Validate("2020-01-02T03:04:05".AsSpan()).FailedFacet)
            .IsEqualTo(FacetKind.ExplicitTimezone);
        await Assert
            .That(prohibited.Validate("2020-01-02T03:04:05Z".AsSpan()).FailedFacet)
            .IsEqualTo(FacetKind.ExplicitTimezone);
    }

    [Test]
    public async Task ARestrictionOfARestriction_KeepsTheTimezoneOfTheBase()
    {
        var narrower = new AtomicDescriptor<XsdDateTime>("narrower", Derive("dateTime", "required"), null, []);

        await Assert.That(narrower.Validate("2020-01-02T03:04:05".AsSpan()).IsValid).IsFalse();
        await Assert.That(narrower.Validate("2020-01-02T03:04:05Z".AsSpan()).IsValid).IsTrue();
    }

    [Test]
    [Arguments("2020-01-02T03:04:05", false)]
    [Arguments("2020-01-02T03:04:05Z", true)]
    [Arguments("2020-01-02T03:04:05+14:00", true)]
    [Arguments("2020-01-02T03:04:05-14:00", true)]
    [Arguments("2020-01-02T03:04:05+14:01", false)]
    [Arguments("2020-01-02", false)]
    public async Task DateTimeStamp_RejectsAValueWithoutTimezone(string lexical, bool valid)
    {
        var fixture = new FacetFixture();

        var stamp = fixture.Descriptor("dateTimeStamp");

        await Assert.That(stamp.Validate(lexical.AsSpan()).IsValid).IsEqualTo(valid);
    }

    [Test]
    public async Task DateTimeStamp_ExistsInVersion11Only()
    {
        await Assert.That(BuiltInDatatypes.For(XsdVersion.V11).TryGet("dateTimeStamp", out _)).IsTrue();
        await Assert.That(BuiltInDatatypes.For(XsdVersion.V10).TryGet("dateTimeStamp", out _)).IsFalse();
    }

    // The facet that the check returns for one declaration, applied to the datatype of the Xml package.
    private static AtomicDescriptor<XsdDateTime> Derive(string type, string mode)
    {
        var fixture = new FacetFixture();
        var result = ExplicitTimezoneCheck.Check(
            fixture.Base(type),
            default,
            [FacetFixture.Decl(FacetKind.ExplicitTimezone, mode)],
            fixture.Version,
            fixture.Reporter,
            out var facet
        );

        if (!result.IsValid || facet is null)
        {
            throw new InvalidOperationException("The explicitTimezone declaration was rejected.");
        }

        return new AtomicDescriptor<XsdDateTime>(
            "derived",
            (AtomicDescriptor<XsdDateTime>)fixture.Descriptor(type),
            null,
            [facet]
        );
    }
}
