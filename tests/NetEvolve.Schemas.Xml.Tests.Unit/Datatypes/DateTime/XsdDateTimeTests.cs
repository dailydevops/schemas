namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.DateTime;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;

public class XsdDateTimeTests
{
    private const int DecimalDigitCap = 4096;

    private static XsdDateTime P(string lexical, XsdDateTimeKind kind, XsdVersion version = XsdVersion.V11)
    {
        var result = XsdDateTime.Parse(lexical.AsSpan(), kind, version);
        return result.Success ? result.Value : throw new InvalidOperationException($"{lexical}: {result.Error}");
    }

    [Test]
    [Arguments("2024-03-05T07:08:09", XsdDateTimeKind.DateTime, 2024L, 3, 5, 7, 8, "9", null, "2024-03-05T07:08:09")]
    [Arguments("2024-03-05T07:08:09Z", XsdDateTimeKind.DateTime, 2024L, 3, 5, 7, 8, "9", 0, "2024-03-05T07:08:09Z")]
    [Arguments(
        "2024-03-05T07:08:09+00:00",
        XsdDateTimeKind.DateTime,
        2024L,
        3,
        5,
        7,
        8,
        "9",
        0,
        "2024-03-05T07:08:09Z"
    )]
    [Arguments(
        "2024-03-05T07:08:09.500-05:30",
        XsdDateTimeKind.DateTime,
        2024L,
        3,
        5,
        7,
        8,
        "9.5",
        -330,
        "2024-03-05T07:08:09.5-05:30"
    )]
    [Arguments("-0044-03-15T12:00:00", XsdDateTimeKind.DateTime, -44L, 3, 15, 12, 0, "0", null, "-0044-03-15T12:00:00")]
    [Arguments("2024-03-05", XsdDateTimeKind.Date, 2024L, 3, 5, 0, 0, "0", null, "2024-03-05")]
    [Arguments("2024-03-05+14:00", XsdDateTimeKind.Date, 2024L, 3, 5, 0, 0, "0", 840, "2024-03-05+14:00")]
    [Arguments("07:08:09.25Z", XsdDateTimeKind.Time, 0L, 0, 0, 7, 8, "9.25", 0, "07:08:09.25Z")]
    [Arguments("07:08:09", XsdDateTimeKind.Time, 0L, 0, 0, 7, 8, "9", null, "07:08:09")]
    public async Task Parse_ValidLexical_MapsTheSevenPropertiesAndBackToTheCanonicalForm(
        string lexical,
        XsdDateTimeKind kind,
        long year,
        int month,
        int day,
        int hour,
        int minute,
        string second,
        int? offset,
        string canonical
    )
    {
        var value = P(lexical, kind);

        await Assert.That(value.Year).IsEqualTo(year);
        await Assert.That(value.Month).IsEqualTo(month);
        await Assert.That(value.Day).IsEqualTo(day);
        await Assert.That(value.Hour).IsEqualTo(hour);
        await Assert.That(value.Minute).IsEqualTo(minute);
        await Assert.That(value.Second.ToString()).IsEqualTo(second);
        await Assert.That(value.TimezoneOffsetMinutes).IsEqualTo(offset);
        await Assert.That(value.ToString()).IsEqualTo(canonical);
    }

    [Test]
    public async Task Parse_24Hours_MapsToTheStartOfTheNextDay()
    {
        await Assert
            .That(P("2024-02-28T24:00:00", XsdDateTimeKind.DateTime).ToString())
            .IsEqualTo("2024-02-29T00:00:00");
        await Assert
            .That(P("2024-12-31T24:00:00.000Z", XsdDateTimeKind.DateTime).ToString())
            .IsEqualTo("2025-01-01T00:00:00Z");
        await Assert.That(P("24:00:00", XsdDateTimeKind.Time).ToString()).IsEqualTo("00:00:00");
    }

    [Test]
    public async Task Parse_24HoursAtTheLastDayBeforeCommonEra_SkipsYearZeroInVersion10Only()
    {
        await Assert
            .That(P("-0001-12-31T24:00:00", XsdDateTimeKind.DateTime, XsdVersion.V10).ToString())
            .IsEqualTo("0001-01-01T00:00:00");
        await Assert
            .That(P("-0001-12-31T24:00:00", XsdDateTimeKind.DateTime, XsdVersion.V11).ToString())
            .IsEqualTo("0000-01-01T00:00:00");
    }

    [Test]
    [Arguments("2024-03-05T24:00:01")]
    [Arguments("2024-03-05T24:01:00")]
    [Arguments("2024-03-05T24:00:00.1")]
    [Arguments("2024-03-05T25:00:00")]
    [Arguments("2024-03-05T07:60:00")]
    [Arguments("2024-03-05T07:00:60")]
    [Arguments("2024-13-05T07:00:00")]
    [Arguments("2024-00-05T07:00:00")]
    [Arguments("2024-03-32T07:00:00")]
    [Arguments("2024-03-05T7:00:00")]
    [Arguments("2024-03-05T07:00:00.")]
    [Arguments("2024-03-05T07:00")]
    [Arguments("2024-03-05 07:00:00")]
    [Arguments("+2024-03-05T07:00:00")]
    [Arguments("024-03-05T07:00:00")]
    [Arguments("02024-03-05T07:00:00")]
    [Arguments("")]
    public async Task Parse_InvalidDateTime_IsRejected(string lexical)
    {
        var result = XsdDateTime.Parse(lexical.AsSpan(), XsdDateTimeKind.DateTime, XsdVersion.V11);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotEqualTo(DatatypeError.None);
    }

    [Test]
    [Arguments("2024-02-29", true)]
    [Arguments("2023-02-29", false)]
    [Arguments("1900-02-29", false)]
    [Arguments("2000-02-29", true)]
    [Arguments("2100-02-29", false)]
    [Arguments("2400-02-29", true)]
    [Arguments("-0004-02-29", true)]
    [Arguments("-0100-02-29", false)]
    [Arguments("2024-04-31", false)]
    [Arguments("2024-04-30", true)]
    [Arguments("2024-12-31", true)]
    public async Task Parse_Day29OfFebruary_IsAcceptedOnlyInLeapYears(string lexical, bool valid)
    {
        var result = XsdDateTime.Parse(lexical.AsSpan(), XsdDateTimeKind.Date, XsdVersion.V11);

        await Assert.That(result.Success).IsEqualTo(valid);
    }

    [Test]
    public async Task Parse_FractionBeyondTickPrecision_IsKeptWithoutRounding()
    {
        var value = P("2024-03-05T07:08:09.12345678901234567890123456789", XsdDateTimeKind.DateTime);

        await Assert.That(value.Second.ToString()).IsEqualTo("9.12345678901234567890123456789");
        await Assert.That(value.ToString()).IsEqualTo("2024-03-05T07:08:09.12345678901234567890123456789");
    }

    [Test]
    public async Task Parse_TrailingZerosOfTheFraction_AreDroppedInTheCanonicalForm()
    {
        await Assert.That(P("07:08:09.1200", XsdDateTimeKind.Time).ToString()).IsEqualTo("07:08:09.12");
        await Assert.That(P("07:08:00.000", XsdDateTimeKind.Time).ToString()).IsEqualTo("07:08:00");
    }

    [Test]
    public async Task Parse_YearZero_IsRejectedInVersion10AndAcceptedInVersion11()
    {
        var v10 = XsdDateTime.Parse("0000-01-01T00:00:00".AsSpan(), XsdDateTimeKind.DateTime, XsdVersion.V10);
        var v10Negative = XsdDateTime.Parse("-0000-01-01".AsSpan(), XsdDateTimeKind.Date, XsdVersion.V10);

        await Assert.That(v10.Success).IsFalse();
        await Assert.That(v10Negative.Success).IsFalse();
        await Assert.That(P("0000-01-01T00:00:00", XsdDateTimeKind.DateTime, XsdVersion.V11).Year).IsEqualTo(0L);
        await Assert.That(P("0000-12-31", XsdDateTimeKind.Date, XsdVersion.V11).ToString()).IsEqualTo("0000-12-31");
    }

    [Test]
    [Arguments("12345-01-01", 12345L, "12345-01-01")]
    [Arguments("-12345-01-01", -12345L, "-12345-01-01")]
    [Arguments("99999999999999999-01-01", 99999999999999999L, "99999999999999999-01-01")]
    [Arguments("0001-01-01", 1L, "0001-01-01")]
    public async Task Parse_YearsBeyondFourDigitsWithoutLeadingZeros_Parse(string lexical, long year, string canonical)
    {
        var value = P(lexical, XsdDateTimeKind.Date);

        await Assert.That(value.Year).IsEqualTo(year);
        await Assert.That(value.ToString()).IsEqualTo(canonical);
    }

    [Test]
    public async Task Parse_YearWithMoreThan17Digits_ReportsTheYearCap()
    {
        var result = XsdDateTime.Parse("100000000000000000-01-01".AsSpan(), XsdDateTimeKind.Date, XsdVersion.V11);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.YearOrComponentCap);
    }

    [Test]
    [Arguments("2024-03-05T07:00:00+14:00", true)]
    [Arguments("2024-03-05T07:00:00-14:00", true)]
    [Arguments("2024-03-05T07:00:00+14:01", false)]
    [Arguments("2024-03-05T07:00:00-14:01", false)]
    [Arguments("2024-03-05T07:00:00+15:00", false)]
    [Arguments("2024-03-05T07:00:00+13:60", false)]
    [Arguments("2024-03-05T07:00:00+13:59", true)]
    [Arguments("2024-03-05T07:00:00+1:00", false)]
    [Arguments("2024-03-05T07:00:00+0100", false)]
    [Arguments("2024-03-05T07:00:00z", false)]
    public async Task Parse_TimezoneOffset_IsLimitedToPlusOrMinus14Hours(string lexical, bool valid)
    {
        var result = XsdDateTime.Parse(lexical.AsSpan(), XsdDateTimeKind.DateTime, XsdVersion.V11);

        await Assert.That(result.Success).IsEqualTo(valid);
    }

    [Test]
    public async Task Parse_FractionOverTheDigitCap_ReportsTheDigitCap()
    {
        var lexical = "07:08:09." + new string('1', DecimalDigitCap);
        var result = XsdDateTime.Parse(lexical.AsSpan(), XsdDateTimeKind.Time, XsdVersion.V11);

        await Assert.That(result.Error).IsEqualTo(DatatypeError.DigitCap);
    }

    [Test]
    public async Task Equals_SameFieldsAndKind_IsTrueAndTimezonePresenceMatters()
    {
        var a = P("2024-03-05T07:08:09Z", XsdDateTimeKind.DateTime);

        await Assert.That(a.Equals(P("2024-03-05T07:08:09.0+00:00", XsdDateTimeKind.DateTime))).IsTrue();
        await Assert.That(a.Equals(P("2024-03-05T07:08:09", XsdDateTimeKind.DateTime))).IsFalse();
        await Assert
            .That(P("2024-03-05", XsdDateTimeKind.Date).Equals(P("2024-03-05T00:00:00", XsdDateTimeKind.DateTime)))
            .IsFalse();
    }

    [Test]
    [Arguments("dateTime", " \t2024-03-05T07:08:09Z\n ", true)]
    [Arguments("date", "2024-03-05", true)]
    [Arguments("time", "07:08:09", true)]
    [Arguments("date", "2024-03-05T07:08:09", false)]
    [Arguments("time", "2024-03-05", false)]
    [Arguments("dateTime", "2024-03-05", false)]
    public async Task BuiltInDatatypes_RegistersThePrimitivesForBothVersions(string name, string lexical, bool valid)
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            var descriptor = (IDatatypeDescriptor<XsdDateTime>)BuiltInDatatypes.For(version).Get(name);

            await Assert.That(descriptor.Parse(lexical.AsSpan()).Success).IsEqualTo(valid);
        }
    }

    [Test]
    public async Task BuiltInDatatypes_YearZeroFollowsTheVersionOfTheRegistry()
    {
        var v10 = (IDatatypeDescriptor<XsdDateTime>)BuiltInDatatypes.For(XsdVersion.V10).Get("date");
        var v11 = (IDatatypeDescriptor<XsdDateTime>)BuiltInDatatypes.For(XsdVersion.V11).Get("date");

        await Assert.That(v10.Parse("0000-01-01".AsSpan()).Success).IsFalse();
        await Assert.That(v11.Parse("0000-01-01".AsSpan()).Success).IsTrue();
    }
}
