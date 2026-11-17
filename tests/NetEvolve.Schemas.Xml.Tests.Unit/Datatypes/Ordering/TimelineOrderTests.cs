namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Ordering;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Duration;

public class TimelineOrderTests
{
    private static XsdOrder Cmp<T>(string type, string left, string right)
    {
        var d = (IDatatypeDescriptor<T>)BuiltInDatatypes.For(XsdVersion.V11).Get(type);
        return d.Compare(d.Parse(left).Value, d.Parse(right).Value);
    }

    private static XsdOrder Dt(string a, string b) => Cmp<XsdDateTime>("dateTime", a, b);

    private static XsdOrder Du(string a, string b) => Cmp<XsdDuration>("duration", a, b);

    [Test]
    [Arguments("dateTime")]
    [Arguments("date")]
    [Arguments("time")]
    [Arguments("gYear")]
    [Arguments("gYearMonth")]
    [Arguments("gMonth")]
    [Arguments("gMonthDay")]
    [Arguments("gDay")]
    [Arguments("dateTimeStamp")]
    public async Task IsOrdered_DateTimeFamily_IsTrue(string type) =>
        await Assert.That(BuiltInDatatypes.For(XsdVersion.V11).Get(type).IsOrdered).IsTrue();

    [Test]
    [Arguments("2000-01-01T00:00:00", "2000-01-01T00:00:00Z", XsdOrder.Incomparable)]
    [Arguments("2000-01-01T12:00:00", "2000-01-01T12:00:00Z", XsdOrder.Incomparable)]
    [Arguments("2000-01-01T12:00:00", "2000-01-01T13:59:59Z", XsdOrder.Incomparable)]
    [Arguments("2000-01-01T12:00:00", "2000-01-01T02:00:00Z", XsdOrder.Incomparable)]
    [Arguments("2000-01-01T12:00:00", "2000-01-02T02:00:01Z", XsdOrder.Less)]
    [Arguments("2000-01-01T12:00:00", "1999-12-31T21:59:59Z", XsdOrder.Greater)]
    [Arguments("2000-01-02T02:00:01Z", "2000-01-01T12:00:00", XsdOrder.Greater)]
    [Arguments("1999-12-31T21:59:59Z", "2000-01-01T12:00:00", XsdOrder.Less)]
    public async Task Compare_ZonelessAgainstZoned_UsesThePlusMinus14HourWindow(string a, string b, XsdOrder order) =>
        await Assert.That(Dt(a, b)).IsEqualTo(order);

    [Test]
    public async Task Compare_EqualInstantsAcrossOffsets_AreEqual()
    {
        await Assert.That(Dt("2000-01-01T12:00:00Z", "2000-01-01T13:00:00+01:00")).IsEqualTo(XsdOrder.Equal);
        await Assert.That(Dt("2000-01-01T12:00:00.0Z", "2000-01-01T07:00:00-05:00")).IsEqualTo(XsdOrder.Equal);
        await Assert.That(Dt("2000-01-01T24:00:00Z", "2000-01-02T00:00:00Z")).IsEqualTo(XsdOrder.Equal);
        await Assert.That(Dt("2000-02-29T00:00:00", "2000-02-29T00:00:00")).IsEqualTo(XsdOrder.Equal);
    }

    [Test]
    [Arguments("12345-01-01T00:00:00Z", "9999-12-31T23:59:59Z", XsdOrder.Greater)]
    [Arguments("-12345-01-01T00:00:00Z", "-0001-01-01T00:00:00Z", XsdOrder.Less)]
    [Arguments("99999999999999999-01-01T00:00:00Z", "99999999999999998-12-31T23:59:59.999Z", XsdOrder.Greater)]
    [Arguments("2000-01-01T00:00:00.0000000001Z", "2000-01-01T00:00:00Z", XsdOrder.Greater)]
    [Arguments("2000-01-01T00:00:00.5Z", "2000-01-01T00:00:00.50Z", XsdOrder.Equal)]
    [Arguments(
        "2000-01-01T00:00:00.123456789012345678901Z",
        "2000-01-01T00:00:00.123456789012345678902Z",
        XsdOrder.Less
    )]
    public async Task Compare_LargeYearsAndFractions_OrderExactly(string a, string b, XsdOrder order) =>
        await Assert.That(Dt(a, b)).IsEqualTo(order);

    [Test]
    public async Task Compare_Time_ShiftsTheDayThroughTheOffset()
    {
        await Assert.That(Cmp<XsdDateTime>("time", "23:00:00-05:00", "04:00:00Z")).IsEqualTo(XsdOrder.Greater);
        await Assert.That(Cmp<XsdDateTime>("time", "10:00:00Z", "11:00:00Z")).IsEqualTo(XsdOrder.Less);
        await Assert.That(Cmp<XsdDateTime>("date", "2000-01-01Z", "2000-01-01+05:00")).IsEqualTo(XsdOrder.Greater);
    }

    [Test]
    public async Task Compare_GregorianTypes_FillAbsentPropertiesFromTheReferenceDateTime()
    {
        await Assert.That(Cmp<XsdDateTime>("gYear", "2000Z", "2001Z")).IsEqualTo(XsdOrder.Less);
        await Assert.That(Cmp<XsdDateTime>("gYearMonth", "2000-02Z", "2000-03Z")).IsEqualTo(XsdOrder.Less);
        await Assert.That(Cmp<XsdDateTime>("gMonth", "--02Z", "--12Z")).IsEqualTo(XsdOrder.Less);
        await Assert.That(Cmp<XsdDateTime>("gMonthDay", "--02-29Z", "--03-01Z")).IsEqualTo(XsdOrder.Less);
        await Assert.That(Cmp<XsdDateTime>("gDay", "---01Z", "---31Z")).IsEqualTo(XsdOrder.Less);
        await Assert.That(Cmp<XsdDateTime>("gMonth", "--05Z", "--05+00:00")).IsEqualTo(XsdOrder.Equal);
        await Assert.That(Cmp<XsdDateTime>("gYear", "2000", "2000Z")).IsEqualTo(XsdOrder.Incomparable);
    }

    [Test]
    public async Task Compare_Table_IsAntisymmetric_AndTransitive()
    {
        string[] values =
        [
            "1999-12-31T23:59:59Z",
            "2000-01-01T00:00:00Z",
            "2000-01-01T00:00:00+01:00",
            "2000-01-01T00:00:00-01:00",
            "2000-01-01T00:00:00.5Z",
            "2000-03-01T00:00:00Z",
            "12345-01-01T00:00:00Z",
            "-0044-03-15T00:00:00Z",
        ];

        foreach (var a in values)
        {
            foreach (var b in values)
            {
                var ab = Dt(a, b);
                var ba = Dt(b, a);
                var expected =
                    ab == XsdOrder.Less ? XsdOrder.Greater
                    : ab == XsdOrder.Greater ? XsdOrder.Less
                    : ab;
                await Assert.That(ba).IsEqualTo(expected);
                foreach (var c in values)
                {
                    if (ab == XsdOrder.Less && Dt(b, c) == XsdOrder.Less)
                    {
                        await Assert.That(Dt(a, c)).IsEqualTo(XsdOrder.Less);
                    }
                }
            }
        }
    }

    [Test]
    [Arguments("P1M", "P30D", XsdOrder.Incomparable)]
    [Arguments("P1M", "P28D", XsdOrder.Incomparable)]
    [Arguments("P1M", "P32D", XsdOrder.Less)]
    [Arguments("P1Y", "P364D", XsdOrder.Greater)]
    [Arguments("P1Y", "P365D", XsdOrder.Incomparable)]
    [Arguments("P1Y", "P367D", XsdOrder.Less)]
    [Arguments("P1Y", "P12M", XsdOrder.Equal)]
    [Arguments("PT24H", "P1D", XsdOrder.Equal)]
    [Arguments("P0Y", "PT0S", XsdOrder.Equal)]
    [Arguments("-P1M", "-P30D", XsdOrder.Incomparable)]
    [Arguments("-P1Y", "-P364D", XsdOrder.Less)]
    [Arguments("P2M", "P59D", XsdOrder.Incomparable)]
    [Arguments("P1M", "P27D", XsdOrder.Greater)]
    [Arguments("P1Y2M", "P1Y2M1D", XsdOrder.Less)]
    public async Task Compare_Durations_UseTheFourReferenceDateTimes(string a, string b, XsdOrder order)
    {
        await Assert.That(Du(a, b)).IsEqualTo(order);
        var reverse =
            order == XsdOrder.Less ? XsdOrder.Greater
            : order == XsdOrder.Greater ? XsdOrder.Less
            : order;
        await Assert.That(Du(b, a)).IsEqualTo(reverse);
    }

    [Test]
    [Arguments("yearMonthDuration", "P1Y", "P11M", XsdOrder.Greater)]
    [Arguments("yearMonthDuration", "P12M", "P1Y", XsdOrder.Equal)]
    [Arguments("dayTimeDuration", "P1D", "PT23H", XsdOrder.Greater)]
    [Arguments("dayTimeDuration", "PT60S", "PT1M", XsdOrder.Equal)]
    public async Task Compare_DerivedDurations_AreTotallyOrdered(string type, string a, string b, XsdOrder order) =>
        await Assert.That(Cmp<XsdDuration>(type, a, b)).IsEqualTo(order);
}
