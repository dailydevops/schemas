namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Ordering;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Ordering;

public class DateTimePlusDurationTests
{
    private static string Add(string duration, string dateTime, XsdVersion version = XsdVersion.V11)
    {
        var d = XsdDuration.Parse(duration).Value;
        var dt = XsdDateTime.Parse(dateTime, XsdDateTimeKind.DateTime, version).Value;
        return DateTimeTimeline.Add(in d, in dt).ToString();
    }

    [Test]
    [Arguments("P1Y3M5DT7H10M3.3S", "2000-01-12T12:13:14Z", "2001-04-17T19:23:17.3Z")]
    [Arguments("P1M", "2000-01-31T00:00:00", "2000-02-29T00:00:00")]
    [Arguments("P1M", "2001-01-31T00:00:00", "2001-02-28T00:00:00")]
    [Arguments("P1M", "2000-03-31T00:00:00Z", "2000-04-30T00:00:00Z")]
    [Arguments("PT33H", "2000-01-12T00:00:00", "2000-01-13T09:00:00")]
    [Arguments("P3M", "1999-10-12T00:00:00", "2000-01-12T00:00:00")]
    [Arguments("-P1M", "2000-03-31T00:00:00Z", "2000-02-29T00:00:00Z")]
    [Arguments("-P1D", "2000-01-01T00:00:00Z", "1999-12-31T00:00:00Z")]
    [Arguments("-PT0.5S", "2000-01-01T00:00:00Z", "1999-12-31T23:59:59.5Z")]
    [Arguments("PT1M", "2000-12-31T23:59:30Z", "2001-01-01T00:00:30Z")]
    [Arguments("PT60S", "2000-12-31T23:59:30Z", "2001-01-01T00:00:30Z")]
    [Arguments("P1D", "2000-02-28T10:00:00Z", "2000-02-29T10:00:00Z")]
    [Arguments("P1D", "1900-02-28T10:00:00Z", "1900-03-01T10:00:00Z")]
    [Arguments("P1Y", "2000-02-29T00:00:00Z", "2001-02-28T00:00:00Z")]
    [Arguments("P400Y", "2000-02-29T00:00:00Z", "2400-02-29T00:00:00Z")]
    [Arguments("-P2000Y", "0001-06-15T00:00:00Z", "-1999-06-15T00:00:00Z")]
    [Arguments("P1D", "2000-01-01T00:00:00+05:00", "2000-01-02T00:00:00+05:00")]
    public async Task Add_FollowsTheSpecificationAlgorithm(string duration, string dateTime, string expected) =>
        await Assert.That(Add(duration, dateTime)).IsEqualTo(expected);

    [Test]
    public async Task Add_NonCommutative_MatchesTheSpecificationExample()
    {
        await Assert.That(Add("P1M", Add("P1D", "2000-03-30T00:00:00Z"))).IsEqualTo("2000-04-30T00:00:00Z");
        await Assert.That(Add("P1D", Add("P1M", "2000-03-30T00:00:00Z"))).IsEqualTo("2000-05-01T00:00:00Z");
    }
}
