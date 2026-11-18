namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;

using static CastShow;

/// <summary>The value of every allowed cast among the date, time, and Gregorian types; the expected text is the kind of the item and its canonical form.</summary>
public class TemporalCastValueTests
{
    // dateTime to date and time: the timezone travels with the value, and a value without one stays without one.
    [Test]
    [Arguments("xs:dateTime('2004-10-13T12:30:15.5Z') cast as xs:date", "Date:2004-10-13Z")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15') cast as xs:date", "Date:2004-10-13")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15+05:30') cast as xs:date", "Date:2004-10-13+05:30")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15-08:00') cast as xs:date", "Date:2004-10-13-08:00")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15.5Z') cast as xs:time", "Time:12:30:15.5Z")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15') cast as xs:time", "Time:12:30:15")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15+05:30') cast as xs:time", "Time:12:30:15+05:30")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15-08:00') cast as xs:time", "Time:12:30:15-08:00")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15.250') cast as xs:time", "Time:12:30:15.25")]
    [Arguments("xs:dateTime('-0044-03-15T00:00:00') cast as xs:date", "Date:-0044-03-15")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15.5Z') cast as xs:dateTime", "DateTime:2004-10-13T12:30:15.5Z")]
    [Arguments("xs:dateTime('2004-10-13T24:00:00') cast as xs:dateTime", "DateTime:2004-10-14T00:00:00")]
    public async Task FromDateTime_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:dateTime('2004-10-13T12:30:15Z') cast as xs:gYearMonth", "GYearMonth:2004-10Z")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15') cast as xs:gYearMonth", "GYearMonth:2004-10")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15+01:00') cast as xs:gYear", "GYear:2004+01:00")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15') cast as xs:gYear", "GYear:2004")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15Z') cast as xs:gMonthDay", "GMonthDay:--10-13Z")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15') cast as xs:gDay", "GDay:---13")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15-05:00') cast as xs:gMonth", "GMonth:--10-05:00")]
    public async Task FromDateTime_ToAGregorianType_KeepsTheComponentsAndTheTimezone(
        string expression,
        string expected
    ) => await Assert.That(Show(expression)).IsEqualTo(expected);

    // date to dateTime: the time of day is midnight; date to the Gregorian types keeps the components.
    [Test]
    [Arguments("xs:date('2004-10-13') cast as xs:dateTime", "DateTime:2004-10-13T00:00:00")]
    [Arguments("xs:date('2004-10-13Z') cast as xs:dateTime", "DateTime:2004-10-13T00:00:00Z")]
    [Arguments("xs:date('2004-10-13-08:00') cast as xs:dateTime", "DateTime:2004-10-13T00:00:00-08:00")]
    [Arguments("xs:date('2004-10-13Z') cast as xs:date", "Date:2004-10-13Z")]
    [Arguments("xs:date('2004-10-13') cast as xs:gYearMonth", "GYearMonth:2004-10")]
    [Arguments("xs:date('2004-10-13Z') cast as xs:gYear", "GYear:2004Z")]
    [Arguments("xs:date('2004-02-29') cast as xs:gMonthDay", "GMonthDay:--02-29")]
    [Arguments("xs:date('2004-10-13+14:00') cast as xs:gDay", "GDay:---13+14:00")]
    [Arguments("xs:date('2004-10-13') cast as xs:gMonth", "GMonth:--10")]
    public async Task FromDate_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:time('12:30:15.5-05:00') cast as xs:time", "Time:12:30:15.5-05:00")]
    [Arguments("xs:time('12:30:15') cast as xs:time", "Time:12:30:15")]
    [Arguments("xs:gYearMonth('2004-10') cast as xs:gYearMonth", "GYearMonth:2004-10")]
    [Arguments("xs:gYear('-2004Z') cast as xs:gYear", "GYear:-2004Z")]
    [Arguments("xs:gMonthDay('--10-13') cast as xs:gMonthDay", "GMonthDay:--10-13")]
    [Arguments("xs:gDay('---13+01:00') cast as xs:gDay", "GDay:---13+01:00")]
    [Arguments("xs:gMonth('--10') cast as xs:gMonth", "GMonth:--10")]
    public async Task FromTimeAndGregorian_ToTheSameType_KeepsTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    // xs:string source: the lexical form goes through the datatype of the target, whitespace collapses, and a zero offset is Z.
    [Test]
    [Arguments("' 2004-10-13 ' cast as xs:date", "Date:2004-10-13")]
    [Arguments("'2004-10-13T24:00:00' cast as xs:dateTime", "DateTime:2004-10-14T00:00:00")]
    [Arguments("'24:00:00' cast as xs:time", "Time:00:00:00")]
    [Arguments("'12:30:00+00:00' cast as xs:time", "Time:12:30:00Z")]
    [Arguments("'12:30:00-00:00' cast as xs:time", "Time:12:30:00Z")]
    [Arguments("'2004-10-13+14:00' cast as xs:date", "Date:2004-10-13+14:00")]
    [Arguments("'--02-29' cast as xs:gMonthDay", "GMonthDay:--02-29")]
    [Arguments("'2004-02-29' cast as xs:date", "Date:2004-02-29")]
    [Arguments("'12345-01-01' cast as xs:date", "Date:12345-01-01")]
    [Arguments("'2004-10' cast as xs:gYearMonth", "GYearMonth:2004-10")]
    [Arguments("'2004' cast as xs:gYear", "GYear:2004")]
    [Arguments("'---31' cast as xs:gDay", "GDay:---31")]
    [Arguments("'--12' cast as xs:gMonth", "GMonth:--12")]
    public async Task FromString_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:untypedAtomic(' 2004-10-13Z ') cast as xs:date", "Date:2004-10-13Z")]
    [Arguments("xs:untypedAtomic('12:30:00') cast as xs:time", "Time:12:30:00")]
    [Arguments("xs:untypedAtomic('2004') cast as xs:gYear", "GYear:2004")]
    [Arguments("xs:untypedAtomic('2004-10-13T12:30:00') cast as xs:dateTime", "DateTime:2004-10-13T12:30:00")]
    public async Task FromUntypedAtomic_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    // Every temporal source to xs:string and xs:untypedAtomic: the canonical lexical form.
    [Test]
    [Arguments("xs:dateTime('2004-10-13T12:30:15.50Z') cast as xs:string", "XsString:2004-10-13T12:30:15.5Z")]
    [Arguments("xs:date('2004-10-13-00:00') cast as xs:string", "XsString:2004-10-13Z")]
    [Arguments("xs:time('01:02:03.000') cast as xs:string", "XsString:01:02:03")]
    [Arguments("xs:gYearMonth('2004-10+05:30') cast as xs:string", "XsString:2004-10+05:30")]
    [Arguments("xs:gYear('0099') cast as xs:string", "XsString:0099")]
    [Arguments("xs:gMonthDay('--10-13') cast as xs:string", "XsString:--10-13")]
    [Arguments("xs:gDay('---03') cast as xs:string", "XsString:---03")]
    [Arguments("xs:gMonth('--03Z') cast as xs:string", "XsString:--03Z")]
    [Arguments("xs:dateTime('2004-10-13T12:30:15') cast as xs:untypedAtomic", "UntypedAtomic:2004-10-13T12:30:15")]
    [Arguments("xs:date('2004-10-13Z') cast as xs:untypedAtomic", "UntypedAtomic:2004-10-13Z")]
    [Arguments("xs:time('12:30:15') cast as xs:untypedAtomic", "UntypedAtomic:12:30:15")]
    [Arguments("xs:gYear('2004') cast as xs:untypedAtomic", "UntypedAtomic:2004")]
    public async Task ToStringTypes_GivesTheCanonicalForm(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);
}
