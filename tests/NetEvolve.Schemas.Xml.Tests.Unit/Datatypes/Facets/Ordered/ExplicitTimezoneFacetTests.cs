namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Ordered;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

public class ExplicitTimezoneFacetTests
{
    private static ValidationResult Validate(string type, ExplicitTimezoneMode mode, string lexical) =>
        OrderedFacetSupport
            .Restrict<XsdDateTime>(type, XsdVersion.V11, new ExplicitTimezoneFacet(mode))
            .Validate(lexical.AsSpan());

    [Test]
    [Arguments("dateTime", "2000-01-01T00:00:00Z", "2000-01-01T00:00:00")]
    [Arguments("dateTime", "2000-01-01T00:00:00+05:30", "2000-01-01T00:00:00.5")]
    [Arguments("date", "2000-01-01+05:30", "2000-01-01")]
    [Arguments("time", "10:00:00-08:00", "10:00:00")]
    [Arguments("gYear", "2000Z", "2000")]
    [Arguments("gYearMonth", "2000-01Z", "2000-01")]
    [Arguments("gMonth", "--01+14:00", "--01")]
    [Arguments("gMonthDay", "--01-01Z", "--01-01")]
    [Arguments("gDay", "---01-14:00", "---01")]
    public async Task Required_AcceptsAValueWithATimezoneAndRejectsOneWithout(
        string type,
        string zoned,
        string zoneless
    )
    {
        var accepted = Validate(type, ExplicitTimezoneMode.Required, zoned);
        var rejected = Validate(type, ExplicitTimezoneMode.Required, zoneless);

        await Assert.That(accepted.IsValid).IsTrue();
        await Assert.That(rejected.IsValid).IsFalse();
        await Assert.That(rejected.FailedFacet).IsEqualTo(FacetKind.ExplicitTimezone);
    }

    [Test]
    [Arguments("dateTime", "2000-01-01T00:00:00Z", "2000-01-01T00:00:00")]
    [Arguments("dateTime", "2000-01-01T00:00:00+05:30", "2000-01-01T00:00:00.5")]
    [Arguments("date", "2000-01-01+05:30", "2000-01-01")]
    [Arguments("time", "10:00:00-08:00", "10:00:00")]
    [Arguments("gYear", "2000Z", "2000")]
    [Arguments("gYearMonth", "2000-01Z", "2000-01")]
    [Arguments("gMonth", "--01+14:00", "--01")]
    [Arguments("gMonthDay", "--01-01Z", "--01-01")]
    [Arguments("gDay", "---01-14:00", "---01")]
    public async Task Prohibited_AcceptsAValueWithoutATimezoneAndRejectsOneWith(
        string type,
        string zoned,
        string zoneless
    )
    {
        var accepted = Validate(type, ExplicitTimezoneMode.Prohibited, zoneless);
        var rejected = Validate(type, ExplicitTimezoneMode.Prohibited, zoned);

        await Assert.That(accepted.IsValid).IsTrue();
        await Assert.That(rejected.IsValid).IsFalse();
        await Assert.That(rejected.FailedFacet).IsEqualTo(FacetKind.ExplicitTimezone);
    }

    [Test]
    [Arguments("2000-01-01T00:00:00Z")]
    [Arguments("2000-01-01T00:00:00")]
    [Arguments("2000-01-01T00:00:00-14:00")]
    public async Task Optional_AcceptsBoth(string lexical)
    {
        var result = Validate("dateTime", ExplicitTimezoneMode.Optional, lexical);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Required_TreatsAnExplicitZeroOffsetAsATimezone()
    {
        await Assert
            .That(Validate("dateTime", ExplicitTimezoneMode.Required, "2000-01-01T00:00:00+00:00").IsValid)
            .IsTrue();
        await Assert
            .That(Validate("dateTime", ExplicitTimezoneMode.Prohibited, "2000-01-01T00:00:00+00:00").IsValid)
            .IsFalse();
    }

    [Test]
    public async Task Facet_ExposesKindAndMode()
    {
        var facet = new ExplicitTimezoneFacet(ExplicitTimezoneMode.Required);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.ExplicitTimezone);
        await Assert.That(facet.Mode).IsEqualTo(ExplicitTimezoneMode.Required);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(3)]
    public async Task Facet_RejectsAnUndefinedMode(int mode) =>
        await Assert
            .That(() => new ExplicitTimezoneFacet((ExplicitTimezoneMode)mode))
            .Throws<ArgumentOutOfRangeException>();
}
