namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Gregorian;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;

public class GregorianDatatypeTests
{
    private static IDatatypeDescriptor<XsdDateTime> Get(string name, XsdVersion version = XsdVersion.V11) =>
        (IDatatypeDescriptor<XsdDateTime>)BuiltInDatatypes.For(version).Get(name);

    [Test]
    [Arguments("gYear", "2024", XsdDateTimeKind.GYear, 2024L, 0, 0, null, "2024")]
    [Arguments("gYear", "0999Z", XsdDateTimeKind.GYear, 999L, 0, 0, 0, "0999Z")]
    [Arguments("gYear", "-0044+05:30", XsdDateTimeKind.GYear, -44L, 0, 0, 330, "-0044+05:30")]
    [Arguments("gYear", "12345-14:00", XsdDateTimeKind.GYear, 12345L, 0, 0, -840, "12345-14:00")]
    [Arguments("gYearMonth", "2024-03", XsdDateTimeKind.GYearMonth, 2024L, 3, 0, null, "2024-03")]
    [Arguments("gYearMonth", "2024-03+00:00", XsdDateTimeKind.GYearMonth, 2024L, 3, 0, 0, "2024-03Z")]
    [Arguments("gYearMonth", "-0001-12-05:00", XsdDateTimeKind.GYearMonth, -1L, 12, 0, -300, "-0001-12-05:00")]
    [Arguments("gMonth", "--05", XsdDateTimeKind.GMonth, 0L, 5, 0, null, "--05")]
    [Arguments("gMonth", "--12Z", XsdDateTimeKind.GMonth, 0L, 12, 0, 0, "--12Z")]
    [Arguments("gMonth", "--05-05:00", XsdDateTimeKind.GMonth, 0L, 5, 0, -300, "--05-05:00")]
    [Arguments("gMonthDay", "--02-29", XsdDateTimeKind.GMonthDay, 0L, 2, 29, null, "--02-29")]
    [Arguments("gMonthDay", "--12-31+01:00", XsdDateTimeKind.GMonthDay, 0L, 12, 31, 60, "--12-31+01:00")]
    [Arguments("gMonthDay", "--04-30Z", XsdDateTimeKind.GMonthDay, 0L, 4, 30, 0, "--04-30Z")]
    [Arguments("gDay", "---01", XsdDateTimeKind.GDay, 0L, 0, 1, null, "---01")]
    [Arguments("gDay", "---31Z", XsdDateTimeKind.GDay, 0L, 0, 31, 0, "---31Z")]
    [Arguments("gDay", "---15-08:00", XsdDateTimeKind.GDay, 0L, 0, 15, -480, "---15-08:00")]
    public async Task Parse_ValidLexical_MapsTheProperties_AndBackToTheCanonicalForm(
        string type,
        string lexical,
        XsdDateTimeKind kind,
        long year,
        int month,
        int day,
        int? offset,
        string canonical
    )
    {
        var result = Get(type).Parse(lexical);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Kind).IsEqualTo(kind);
        await Assert.That(result.Value.Year).IsEqualTo(year);
        await Assert.That(result.Value.Month).IsEqualTo(month);
        await Assert.That(result.Value.Day).IsEqualTo(day);
        await Assert.That(result.Value.Hour).IsEqualTo(0);
        await Assert.That(result.Value.Minute).IsEqualTo(0);
        await Assert.That(result.Value.Second.Sign).IsEqualTo(0);
        await Assert.That(result.Value.TimezoneOffsetMinutes).IsEqualTo(offset);
        await Assert.That(result.Value.ToString()).IsEqualTo(canonical);
    }

    [Test]
    [Arguments("gYear", " 2024 ")]
    [Arguments("gMonth", "\t--05\n")]
    public async Task Parse_SurroundingWhitespace_IsCollapsed(string type, string lexical) =>
        await Assert.That(Get(type).Parse(lexical).Success).IsTrue();

    [Test]
    [Arguments("--02-29", true)]
    [Arguments("--02-30", false)]
    [Arguments("--04-31", false)]
    [Arguments("--06-31", false)]
    [Arguments("--01-31", true)]
    [Arguments("--01-00", false)]
    [Arguments("--13-01", false)]
    public async Task GMonthDay_DayInMonth_IsChecked(string lexical, bool valid) =>
        await Assert.That(Get("gMonthDay").Validate(lexical).IsValid).IsEqualTo(valid);

    [Test]
    [Arguments("gYear", "0000", XsdVersion.V10, false)]
    [Arguments("gYear", "0000", XsdVersion.V11, true)]
    [Arguments("gYearMonth", "0000-06", XsdVersion.V10, false)]
    [Arguments("gYearMonth", "0000-06", XsdVersion.V11, true)]
    [Arguments("gYear", "-0000", XsdVersion.V10, false)]
    [Arguments("gYear", "-0000", XsdVersion.V11, true)]
    public async Task YearZero_IsRejectedInVersion10_AndAcceptedInVersion11(
        string type,
        string lexical,
        XsdVersion version,
        bool valid
    ) => await Assert.That(Get(type, version).Validate(lexical).IsValid).IsEqualTo(valid);

    [Test]
    [Arguments("gMonth", "05")]
    [Arguments("gMonth", "-05")]
    [Arguments("gMonth", "--5")]
    [Arguments("gMonth", "--13")]
    [Arguments("gMonth", "--00")]
    [Arguments("gMonth", "--05--")]
    [Arguments("gDay", "--15")]
    [Arguments("gDay", "15")]
    [Arguments("gDay", "---1")]
    [Arguments("gDay", "---32")]
    [Arguments("gDay", "---00")]
    [Arguments("gYearMonth", "2024-3")]
    [Arguments("gYearMonth", "2024-13")]
    [Arguments("gYearMonth", "2024-00")]
    [Arguments("gYearMonth", "2024")]
    [Arguments("gYear", "")]
    [Arguments("gYear", "24")]
    [Arguments("gYear", "02024")]
    [Arguments("gYear", "+2024")]
    [Arguments("gYear", "2024-03")]
    [Arguments("gYear", "2024+15:00")]
    [Arguments("gYear", "2024+05:60")]
    [Arguments("gYear", "2024+5:00")]
    [Arguments("gYear", "2024z")]
    [Arguments("gMonthDay", "--0229")]
    [Arguments("gMonthDay", "--2-29")]
    [Arguments("gMonthDay", "-02-29")]
    [Arguments("gMonthDay", "--02-29-")]
    public async Task Parse_MalformedLexical_IsRejected(string type, string lexical) =>
        await Assert.That(Get(type).Validate(lexical).IsValid).IsFalse();

    [Test]
    public async Task GYear_MoreThanSeventeenDigits_ReportsTheCap()
    {
        var result = Get("gYear").Parse(new string('1', 18));

        await Assert.That(result.Error).IsEqualTo(DatatypeError.YearOrComponentCap);
    }

    [Test]
    [Arguments("2024-03-05T07:08:09Z", true, "2024-03-05T07:08:09Z")]
    [Arguments("2024-03-05T07:08:09+05:30", true, "2024-03-05T07:08:09+05:30")]
    [Arguments("2024-03-05T07:08:09.50-00:00", true, "2024-03-05T07:08:09.5Z")]
    [Arguments("2024-03-05T07:08:09", false, null)]
    [Arguments("2024-03-05T24:00:00Z", true, "2024-03-06T00:00:00Z")]
    [Arguments("2024-03-05T07:08:09+15:00", false, null)]
    [Arguments("2024-03-05", false, null)]
    public async Task DateTimeStamp_RequiresATimezone(string lexical, bool valid, string? canonical)
    {
        var result = Get("dateTimeStamp").Parse(lexical);

        await Assert.That(result.Success).IsEqualTo(valid);
        if (valid)
        {
            await Assert.That(result.Value.Kind).IsEqualTo(XsdDateTimeKind.DateTime);
            await Assert.That(result.Value.ToString()).IsEqualTo(canonical);
        }
    }

    [Test]
    public async Task DateTimeStamp_WithoutTimezone_ReportsTheOffsetAtTheEnd()
    {
        var result = Get("dateTimeStamp").Parse("2024-03-05T07:08:09");

        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(result.ErrorOffset).IsEqualTo(19);
    }

    [Test]
    public async Task DateTimeStamp_IsNotRegistered_InVersion10()
    {
        var found = BuiltInDatatypes.For(XsdVersion.V10).TryGet("dateTimeStamp", out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task DateTimeStamp_IsRegistered_InVersion11()
    {
        var descriptor = BuiltInDatatypes.For(XsdVersion.V11).Get("dateTimeStamp");

        await Assert.That(descriptor.Name).IsEqualTo("dateTimeStamp");
        await Assert.That(descriptor.LexicalMode).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    [Arguments("gYear")]
    [Arguments("gYearMonth")]
    [Arguments("gMonth")]
    [Arguments("gMonthDay")]
    [Arguments("gDay")]
    public async Task Registry_ContainsThePartialTypes_InBothVersions(string name)
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            var descriptor = BuiltInDatatypes.For(version).Get(name);

            await Assert.That(descriptor.Name).IsEqualTo(name);
            await Assert.That(descriptor.LexicalMode).IsEqualTo(version);
        }
    }

    [Test]
    public async Task Identity_SameLexicalValue_IsIdentical_AndDifferentKindsAreNot()
    {
        var month = Get("gMonth");
        var day = Get("gDay");
        var a = month.Parse("--05Z").Value;
        var b = month.Parse("--05+00:00").Value;
        var c = day.Parse("---05Z").Value;

        await Assert.That(month.IsIdentical(a, b)).IsTrue();
        await Assert.That(month.IsIdentical(a, c)).IsFalse();
        await Assert.That(month.Compare(a, b)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(month.IsOrdered).IsFalse();
    }
}
