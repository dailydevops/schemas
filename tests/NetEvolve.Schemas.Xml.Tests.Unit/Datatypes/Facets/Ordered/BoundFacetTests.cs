namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Ordered;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;

public class BoundFacetTests
{
    // Per facet: is a value below the bound, at the bound, and above the bound inside the value space.
    private static readonly (FacetKind Kind, bool Below, bool At, bool Above)[] Expectations =
    [
        (FacetKind.MinInclusive, false, true, true),
        (FacetKind.MinExclusive, false, false, true),
        (FacetKind.MaxInclusive, true, true, false),
        (FacetKind.MaxExclusive, true, false, false),
    ];

    private static async Task AssertBounds<T>(
        string typeName,
        string bound,
        string below,
        string at,
        string above,
        XsdVersion version = XsdVersion.V11
    )
        where T : struct
    {
        foreach (var (kind, belowOk, atOk, aboveOk) in Expectations)
        {
            await AssertRow<T>(kind, typeName, bound, below, belowOk, version);
            await AssertRow<T>(kind, typeName, bound, at, atOk, version);
            await AssertRow<T>(kind, typeName, bound, above, aboveOk, version);
        }
    }

    private static async Task AssertRow<T>(
        FacetKind kind,
        string typeName,
        string bound,
        string lexical,
        bool expected,
        XsdVersion version
    )
        where T : struct
    {
        var result = OrderedFacetSupport.Validate<T>(kind, typeName, bound, lexical, version);

        await Assert.That(result.IsValid).IsEqualTo(expected).Because($"{kind} {bound} on {typeName}, value {lexical}");
        if (!expected)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(kind);
        }
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Decimal_BelowAtAboveEachBound(XsdVersion version) =>
        await AssertBounds<XsdDecimal>("decimal", "10.5", "10.4999", "10.50", "10.5001", version);

    [Test]
    public async Task Decimal_NegativeBoundsAndZero()
    {
        await AssertBounds<XsdDecimal>("decimal", "-0.5", "-0.51", "-0.50", "-0.49");
        await AssertBounds<XsdDecimal>("decimal", "0", "-0.0001", "-0.0", "0.0001");
    }

    [Test]
    [Arguments("integer", "100", "99", "+0100", "101")]
    [Arguments("int", "-5", "-6", "-5", "-4")]
    [Arguments("byte", "0", "-1", "-0", "1")]
    [Arguments("nonNegativeInteger", "7", "6", "7", "8")]
    public async Task Integer_BelowAtAboveEachBound(string type, string bound, string below, string at, string above) =>
        await AssertBounds<XsdInteger>(type, bound, below, at, above);

    [Test]
    public async Task Integer_BoundsBeyond64Bits()
    {
        const string Bound = "123456789012345678901234567890";
        var above = (BigInteger.Parse(Bound, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture);
        var below = (BigInteger.Parse(Bound, CultureInfo.InvariantCulture) - 1).ToString(CultureInfo.InvariantCulture);

        await AssertBounds<XsdInteger>("integer", Bound, below, Bound, above);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Double_BelowAtAboveEachBound(XsdVersion version)
    {
        await AssertBounds<double>("double", "1.5", "1.4999999999999998", "1.50", "1.5000000000000002", version);
        await AssertBounds<double>("double", "-0", "-1E-300", "0", "1E-300", version);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Float_BelowAtAboveEachBound(XsdVersion version) =>
        await AssertBounds<float>("float", "2.5", "2.4999998", "2.50", "2.5000002", version);

    [Test]
    public async Task Infinities_AreOrderedAgainstFiniteBounds()
    {
        await AssertRow<double>(FacetKind.MaxInclusive, "double", "1E308", "INF", false, XsdVersion.V11);
        await AssertRow<double>(FacetKind.MinInclusive, "double", "1E308", "INF", true, XsdVersion.V11);
        await AssertRow<double>(FacetKind.MinInclusive, "double", "-1E308", "-INF", false, XsdVersion.V11);
        await AssertRow<double>(FacetKind.MaxExclusive, "double", "-1E308", "-INF", true, XsdVersion.V11);
        await AssertRow<float>(FacetKind.MaxInclusive, "float", "INF", "INF", true, XsdVersion.V11);
        await AssertRow<float>(FacetKind.MaxExclusive, "float", "INF", "INF", false, XsdVersion.V11);
    }

    [Test]
    [Arguments(
        "dateTime",
        "2000-01-01T00:00:00Z",
        "1999-12-31T23:59:59Z",
        "2000-01-01T01:00:00+01:00",
        "2000-01-01T00:00:01Z"
    )]
    [Arguments(
        "dateTime",
        "2000-01-01T00:00:00.5",
        "2000-01-01T00:00:00.4999",
        "2000-01-01T00:00:00.500",
        "2000-01-01T00:00:00.5001"
    )]
    [Arguments("date", "2000-06-15", "2000-06-14", "2000-06-15", "2000-06-16")]
    [Arguments("time", "12:00:00", "11:59:59", "12:00:00", "12:00:01")]
    [Arguments("gYear", "2000", "1999", "2000", "2001")]
    [Arguments("gYearMonth", "2000-06", "2000-05", "2000-06", "2000-07")]
    [Arguments("gMonth", "--06", "--05", "--06", "--07")]
    [Arguments("gMonthDay", "--06-15", "--06-14", "--06-15", "--06-16")]
    [Arguments("gDay", "---15", "---14", "---15", "---16")]
    [Arguments(
        "dateTimeStamp",
        "2000-01-01T00:00:00Z",
        "1999-12-31T23:59:59Z",
        "2000-01-01T00:00:00+00:00",
        "2000-01-01T00:00:01Z"
    )]
    public async Task DateAndTimeTypes_BelowAtAboveEachBound(
        string type,
        string bound,
        string below,
        string at,
        string above
    ) => await AssertBounds<XsdDateTime>(type, bound, below, at, above);

    [Test]
    public async Task DateTime_BoundsBeyondFourDigitYears() =>
        await AssertBounds<XsdDateTime>(
            "dateTime",
            "12345-01-01T00:00:00Z",
            "12344-12-31T23:59:59Z",
            "12345-01-01T00:00:00Z",
            "12345-01-01T00:00:01Z"
        );

    [Test]
    [Arguments("duration", "P1Y", "P364D", "P12M", "P367D")]
    [Arguments("duration", "PT1.5S", "PT1.4S", "PT1.50S", "PT1.6S")]
    [Arguments("yearMonthDuration", "P1Y", "P11M", "P12M", "P13M")]
    [Arguments("dayTimeDuration", "P1D", "PT23H", "PT24H", "P1DT1S")]
    public async Task DurationTypes_BelowAtAboveEachBound(
        string type,
        string bound,
        string below,
        string at,
        string above
    ) => await AssertBounds<XsdDuration>(type, bound, below, at, above);

    [Test]
    public async Task BoundsAccumulateOverDerivationSteps()
    {
        var type = OrderedFacetSupport.Base<XsdDecimal>("decimal", XsdVersion.V11);
        var upper = new AtomicDescriptor<XsdDecimal>(
            "upper",
            type,
            null,
            [
                new BoundFacet<XsdDecimal>(
                    FacetKind.MaxInclusive,
                    type,
                    OrderedFacetSupport.Value<XsdDecimal>("decimal", "100", XsdVersion.V11)
                ),
            ]
        );
        var both = new AtomicDescriptor<XsdDecimal>(
            "both",
            upper,
            null,
            [
                new BoundFacet<XsdDecimal>(
                    FacetKind.MinInclusive,
                    upper,
                    OrderedFacetSupport.Value<XsdDecimal>("decimal", "10", XsdVersion.V11)
                ),
            ]
        );

        await Assert.That(both.Validate("50".AsSpan()).IsValid).IsTrue();
        await Assert.That(both.Validate("5".AsSpan()).FailedFacet).IsEqualTo(FacetKind.MinInclusive);
        await Assert.That(both.Validate("101".AsSpan()).FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
    }

    [Test]
    [Arguments(FacetKind.MinInclusive)]
    [Arguments(FacetKind.MinExclusive)]
    [Arguments(FacetKind.MaxInclusive)]
    [Arguments(FacetKind.MaxExclusive)]
    public async Task Bound_ExposesKindAndValue(FacetKind kind)
    {
        var type = OrderedFacetSupport.Base<XsdDecimal>("decimal", XsdVersion.V11);
        var bound = OrderedFacetSupport.Value<XsdDecimal>("decimal", "7", XsdVersion.V11);

        var facet = new BoundFacet<XsdDecimal>(kind, type, bound);

        await Assert.That(facet.Kind).IsEqualTo(kind);
        await Assert.That(facet.Bound).IsEqualTo(bound);
    }

    [Test]
    public async Task Constructor_RejectsANullType() =>
        await Assert
            .That(() => new BoundFacet<XsdDecimal>(FacetKind.MinInclusive, null!, XsdDecimal.Zero))
            .Throws<ArgumentNullException>();

    [Test]
    [Arguments(FacetKind.None)]
    [Arguments(FacetKind.Length)]
    [Arguments(FacetKind.Enumeration)]
    [Arguments(FacetKind.TotalDigits)]
    [Arguments(FacetKind.ExplicitTimezone)]
    public async Task Constructor_RejectsAFacetThatIsNotABound(FacetKind kind)
    {
        var type = OrderedFacetSupport.Base<XsdDecimal>("decimal", XsdVersion.V11);

        await Assert
            .That(() => new BoundFacet<XsdDecimal>(kind, type, XsdDecimal.Zero))
            .Throws<ArgumentOutOfRangeException>();
    }
}
