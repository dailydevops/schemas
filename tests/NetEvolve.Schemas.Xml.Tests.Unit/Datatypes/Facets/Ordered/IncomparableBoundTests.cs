namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Ordered;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Duration;

public class IncomparableBoundTests
{
    private static readonly FacetKind[] AllKinds =
    [
        FacetKind.MinInclusive,
        FacetKind.MinExclusive,
        FacetKind.MaxInclusive,
        FacetKind.MaxExclusive,
    ];

    private static async Task AssertEveryBoundFails<T>(string typeName, string bound, string value, XsdVersion version)
        where T : struct
    {
        foreach (var kind in AllKinds)
        {
            var result = OrderedFacetSupport.Validate<T>(kind, typeName, bound, value, version);

            await Assert.That(result.IsValid).IsFalse().Because($"{kind} {bound} on {typeName}, value {value}");
            await Assert.That(result.FailedFacet).IsEqualTo(kind);
        }
    }

    [Test]
    [Arguments("2000-01-01T12:00:00Z", "2000-01-01T12:00:00")]
    [Arguments("2000-01-01T12:00:00Z", "2000-01-01T20:00:00")]
    [Arguments("2000-01-01T12:00:00Z", "2000-01-01T00:00:00")]
    [Arguments("2000-01-01T12:00:00Z", "2000-01-01T22:00:00")]
    [Arguments("2000-01-01T12:00:00Z", "2000-01-02T01:59:59")]
    [Arguments("2000-01-01T12:00:00Z", "1999-12-31T22:00:01")]
    public async Task DateTimeWithoutTimezone_InsideTheFourteenHourWindowOfTheBound_FailsEveryBound(
        string bound,
        string value
    ) => await AssertEveryBoundFails<XsdDateTime>("dateTime", bound, value, XsdVersion.V11);

    [Test]
    public async Task DateTimeWithoutTimezone_ExactlyFourteenHoursFromTheBound_IsStillIncomparable()
    {
        await AssertEveryBoundFails<XsdDateTime>(
            "dateTime",
            "2000-01-01T12:00:00Z",
            "2000-01-02T02:00:00",
            XsdVersion.V11
        );
        await AssertEveryBoundFails<XsdDateTime>(
            "dateTime",
            "2000-01-01T12:00:00Z",
            "1999-12-31T22:00:00",
            XsdVersion.V11
        );
    }

    [Test]
    public async Task DateTimeBoundWithoutTimezone_AgainstAValueWithTimezoneInsideTheWindow_FailsEveryBound() =>
        await AssertEveryBoundFails<XsdDateTime>(
            "dateTime",
            "2000-01-01T12:00:00",
            "2000-01-01T20:00:00Z",
            XsdVersion.V11
        );

    [Test]
    public async Task DateTimeWithoutTimezone_OutsideTheWindow_IsOrderedAgainstTheBound()
    {
        const string Bound = "2000-01-01T12:00:00Z";
        const string After = "2000-01-02T12:00:00";
        const string Before = "1999-12-31T12:00:00";

        await AssertValid(FacetKind.MinInclusive, Bound, After, true);
        await AssertValid(FacetKind.MinExclusive, Bound, After, true);
        await AssertValid(FacetKind.MaxInclusive, Bound, After, false);
        await AssertValid(FacetKind.MaxExclusive, Bound, After, false);
        await AssertValid(FacetKind.MinInclusive, Bound, Before, false);
        await AssertValid(FacetKind.MinExclusive, Bound, Before, false);
        await AssertValid(FacetKind.MaxInclusive, Bound, Before, true);
        await AssertValid(FacetKind.MaxExclusive, Bound, Before, true);
    }

    private static async Task AssertValid(FacetKind kind, string bound, string value, bool expected)
    {
        var result = OrderedFacetSupport.Validate<XsdDateTime>(kind, "dateTime", bound, value, XsdVersion.V11);

        await Assert.That(result.IsValid).IsEqualTo(expected).Because($"{kind} {bound}, value {value}");
    }

    [Test]
    [Arguments("date", "2000-06-15Z", "2000-06-15")]
    [Arguments("time", "12:00:00Z", "12:00:00")]
    [Arguments("gYear", "2000Z", "2000")]
    [Arguments("gDay", "---15Z", "---15")]
    public async Task OtherDateAndTimeTypes_AreIncomparableAcrossTimezonePresenceInsideTheWindow(
        string type,
        string bound,
        string value
    ) => await AssertEveryBoundFails<XsdDateTime>(type, bound, value, XsdVersion.V11);

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Duration_IncomparableToTheBound_FailsEveryBound(XsdVersion version)
    {
        await AssertEveryBoundFails<XsdDuration>("duration", "P1M", "P30D", version);
        await AssertEveryBoundFails<XsdDuration>("duration", "P1M", "P31D", version);
        await AssertEveryBoundFails<XsdDuration>("duration", "P30D", "P1M", version);
        await AssertEveryBoundFails<XsdDuration>("duration", "P1Y", "P365D", version);
    }

    [Test]
    public async Task Duration_OrderedAgainstTheBound_PassesAccordingToTheFacet()
    {
        // P32D is longer than every month and P27D is shorter than every month, so both are ordered against P1M.
        var greater = OrderedFacetSupport.Validate<XsdDuration>(
            FacetKind.MinInclusive,
            "duration",
            "P1M",
            "P32D",
            XsdVersion.V11
        );
        var notSmaller = OrderedFacetSupport.Validate<XsdDuration>(
            FacetKind.MaxInclusive,
            "duration",
            "P1M",
            "P32D",
            XsdVersion.V11
        );
        var smaller = OrderedFacetSupport.Validate<XsdDuration>(
            FacetKind.MaxInclusive,
            "duration",
            "P1M",
            "P27D",
            XsdVersion.V11
        );

        await Assert.That(greater.IsValid).IsTrue();
        await Assert.That(notSmaller.IsValid).IsFalse();
        await Assert.That(smaller.IsValid).IsTrue();
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Double_NaN_FailsEveryBoundsFacet(XsdVersion version)
    {
        await AssertEveryBoundFails<double>("double", "0", "NaN", version);
        await AssertEveryBoundFails<double>("double", "INF", "NaN", version);
        await AssertEveryBoundFails<double>("double", "-INF", "NaN", version);
        await AssertEveryBoundFails<double>("double", "1.7976931348623157E308", "NaN", version);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Float_NaN_FailsEveryBoundsFacet(XsdVersion version)
    {
        await AssertEveryBoundFails<float>("float", "0", "NaN", version);
        await AssertEveryBoundFails<float>("float", "INF", "NaN", version);
        await AssertEveryBoundFails<float>("float", "-INF", "NaN", version);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Double_AFiniteValue_AgainstANaNBound_FailsEveryBoundsFacet(XsdVersion version)
    {
        await AssertEveryBoundFails<double>("double", "NaN", "0", version);
        await AssertEveryBoundFails<double>("double", "NaN", "INF", version);
    }

    // Pins the decision for NaN as a bound: the facet follows the order of the type, where NaN equals itself (#172), so an
    // inclusive NaN bound accepts NaN and an exclusive one does not.
    [Test]
    public async Task Double_NaNAgainstANaNBound_FollowsTheOrderOfTheType()
    {
        var minInclusive = OrderedFacetSupport.Validate<double>(
            FacetKind.MinInclusive,
            "double",
            "NaN",
            "NaN",
            XsdVersion.V11
        );
        var maxInclusive = OrderedFacetSupport.Validate<double>(
            FacetKind.MaxInclusive,
            "double",
            "NaN",
            "NaN",
            XsdVersion.V11
        );
        var minExclusive = OrderedFacetSupport.Validate<double>(
            FacetKind.MinExclusive,
            "double",
            "NaN",
            "NaN",
            XsdVersion.V11
        );
        var maxExclusive = OrderedFacetSupport.Validate<double>(
            FacetKind.MaxExclusive,
            "double",
            "NaN",
            "NaN",
            XsdVersion.V11
        );

        await Assert.That(minInclusive.IsValid).IsTrue();
        await Assert.That(maxInclusive.IsValid).IsTrue();
        await Assert.That(minExclusive.IsValid).IsFalse();
        await Assert.That(maxExclusive.IsValid).IsFalse();
    }
}
