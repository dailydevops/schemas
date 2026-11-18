namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System.Threading.Tasks;

/// <summary>
/// The component extraction functions (Functions and Operators 1.0, 10.5): each one for a value with a timezone, without one, and
/// for the empty sequence; the components are those of the local value, and a duration gives truncated, signed components.
/// </summary>
public class ComponentExtractionTests
{
    private const string WithZone = "dateTime:2002-03-07T10:30:12.345-07:00";
    private const string WithoutZone = "dateTime:2002-03-07T10:30:12.345";

    [Test]
    [Arguments("year-from-dateTime", "2002")]
    [Arguments("month-from-dateTime", "3")]
    [Arguments("day-from-dateTime", "7")]
    [Arguments("hours-from-dateTime", "10")]
    [Arguments("minutes-from-dateTime", "30")]
    [Arguments("seconds-from-dateTime", "12.345")]
    [Arguments("timezone-from-dateTime", "-PT7H")]
    public async Task DateTimeComponent_WithTimezone_ReadsTheLocalValue(string function, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, WithZone)).IsEqualTo(expected);

    [Test]
    [Arguments("year-from-dateTime", "2002")]
    [Arguments("month-from-dateTime", "3")]
    [Arguments("day-from-dateTime", "7")]
    [Arguments("hours-from-dateTime", "10")]
    [Arguments("minutes-from-dateTime", "30")]
    [Arguments("seconds-from-dateTime", "12.345")]
    [Arguments("timezone-from-dateTime", "")]
    public async Task DateTimeComponent_WithoutTimezone_TheTimezoneIsEmpty(string function, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, WithoutZone)).IsEqualTo(expected);

    [Test]
    [Arguments("year-from-dateTime")]
    [Arguments("month-from-dateTime")]
    [Arguments("day-from-dateTime")]
    [Arguments("hours-from-dateTime")]
    [Arguments("minutes-from-dateTime")]
    [Arguments("seconds-from-dateTime")]
    [Arguments("timezone-from-dateTime")]
    [Arguments("year-from-date")]
    [Arguments("month-from-date")]
    [Arguments("day-from-date")]
    [Arguments("timezone-from-date")]
    [Arguments("hours-from-time")]
    [Arguments("minutes-from-time")]
    [Arguments("seconds-from-time")]
    [Arguments("timezone-from-time")]
    [Arguments("years-from-duration")]
    [Arguments("months-from-duration")]
    [Arguments("days-from-duration")]
    [Arguments("hours-from-duration")]
    [Arguments("minutes-from-duration")]
    [Arguments("seconds-from-duration")]
    public async Task Component_EmptySequence_ReturnsTheEmptySequence(string function) =>
        await Assert.That(DateTimeHarness.Run(function, "()")).IsEqualTo(string.Empty);

    [Test]
    [Arguments("year-from-date", "date:2002-03-07-07:00", "2002")]
    [Arguments("month-from-date", "date:2002-03-07-07:00", "3")]
    [Arguments("day-from-date", "date:2002-03-07-07:00", "7")]
    [Arguments("timezone-from-date", "date:2002-03-07-07:00", "-PT7H")]
    [Arguments("year-from-date", "date:2002-03-07", "2002")]
    [Arguments("month-from-date", "date:2002-03-07", "3")]
    [Arguments("day-from-date", "date:2002-03-07", "7")]
    [Arguments("timezone-from-date", "date:2002-03-07", "")]
    [Arguments("timezone-from-date", "date:1999-12-31+14:00", "PT14H")]
    [Arguments("hours-from-time", "time:10:30:12.345-07:00", "10")]
    [Arguments("minutes-from-time", "time:10:30:12.345-07:00", "30")]
    [Arguments("seconds-from-time", "time:10:30:12.345-07:00", "12.345")]
    [Arguments("timezone-from-time", "time:10:30:12.345-07:00", "-PT7H")]
    [Arguments("hours-from-time", "time:10:30:12.345", "10")]
    [Arguments("minutes-from-time", "time:10:30:12.345", "30")]
    [Arguments("seconds-from-time", "time:10:30:12.345", "12.345")]
    [Arguments("timezone-from-time", "time:10:30:12.345", "")]
    public async Task DateAndTimeComponent_ReadsTheLocalValue(string function, string argument, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("year-from-dateTime", "dateTime:-0044-03-15T00:00:00Z", "-44")]
    [Arguments("timezone-from-dateTime", "dateTime:-0044-03-15T00:00:00Z", "PT0S")]
    [Arguments("hours-from-dateTime", "dateTime:2002-12-31T24:00:00", "0")]
    [Arguments("day-from-dateTime", "dateTime:2002-12-31T24:00:00", "1")]
    [Arguments("year-from-dateTime", "dateTime:2002-12-31T24:00:00", "2003")]
    [Arguments("seconds-from-dateTime", "dateTime:2002-03-07T10:30:00", "0")]
    [Arguments("seconds-from-dateTime", "dateTime:2002-03-07T10:30:59.9999999999", "59.9999999999")]
    [Arguments("timezone-from-dateTime", "dateTime:2002-03-07T10:30:00+05:30", "PT5H30M")]
    [Arguments("timezone-from-dateTime", "dateTime:2002-03-07T10:30:00-14:00", "-PT14H")]
    public async Task DateTimeComponent_EdgeValues(string function, string argument, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("years-from-duration", "duration:P1Y2M3DT4H5M6.5S", "1")]
    [Arguments("months-from-duration", "duration:P1Y2M3DT4H5M6.5S", "2")]
    [Arguments("days-from-duration", "duration:P1Y2M3DT4H5M6.5S", "3")]
    [Arguments("hours-from-duration", "duration:P1Y2M3DT4H5M6.5S", "4")]
    [Arguments("minutes-from-duration", "duration:P1Y2M3DT4H5M6.5S", "5")]
    [Arguments("seconds-from-duration", "duration:P1Y2M3DT4H5M6.5S", "6.5")]
    public async Task DurationComponent_Positive_ReadsTheCanonicalComponents(
        string function,
        string argument,
        string expected
    ) => await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("years-from-duration", "-1")]
    [Arguments("months-from-duration", "-2")]
    [Arguments("days-from-duration", "-3")]
    [Arguments("hours-from-duration", "-4")]
    [Arguments("minutes-from-duration", "-5")]
    [Arguments("seconds-from-duration", "-6.5")]
    public async Task DurationComponent_Negative_EveryComponentIsNegative(string function, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, "duration:-P1Y2M3DT4H5M6.5S")).IsEqualTo(expected);

    [Test]
    [Arguments("years-from-duration", "duration:P14M", "1")]
    [Arguments("months-from-duration", "duration:P14M", "2")]
    [Arguments("years-from-duration", "duration:-P14M", "-1")]
    [Arguments("months-from-duration", "duration:-P14M", "-2")]
    [Arguments("days-from-duration", "duration:PT36H", "1")]
    [Arguments("hours-from-duration", "duration:PT36H", "12")]
    [Arguments("hours-from-duration", "duration:-PT36H", "-12")]
    [Arguments("days-from-duration", "duration:-PT36H", "-1")]
    [Arguments("hours-from-duration", "duration:PT90M", "1")]
    [Arguments("minutes-from-duration", "duration:PT90M", "30")]
    [Arguments("minutes-from-duration", "duration:PT3600S", "0")]
    [Arguments("hours-from-duration", "duration:PT3600S", "1")]
    [Arguments("seconds-from-duration", "duration:PT61.25S", "1.25")]
    [Arguments("minutes-from-duration", "duration:PT61.25S", "1")]
    public async Task DurationComponent_Overflowing_IsNormalizedAndTruncated(
        string function,
        string argument,
        string expected
    ) => await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("seconds-from-duration", "duration:PT0.001S", "0.001")]
    [Arguments("seconds-from-duration", "duration:-PT0.001S", "-0.001")]
    [Arguments("seconds-from-duration", "duration:PT0.123456789012345678S", "0.123456789012345678")]
    [Arguments("seconds-from-duration", "duration:-PT59.5S", "-59.5")]
    [Arguments("seconds-from-duration", "duration:PT60S", "0")]
    [Arguments("seconds-from-dateTime", "dateTime:2002-03-07T10:30:00.000001", "0.000001")]
    [Arguments("seconds-from-time", "time:23:59:59.9", "59.9")]
    public async Task Seconds_WithFractionalDigits_AreExact(string function, string argument, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("years-from-duration", "0")]
    [Arguments("months-from-duration", "0")]
    [Arguments("days-from-duration", "0")]
    [Arguments("hours-from-duration", "0")]
    [Arguments("minutes-from-duration", "0")]
    [Arguments("seconds-from-duration", "0")]
    public async Task DurationComponent_ZeroDuration_IsZero(string function, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, "duration:PT0S")).IsEqualTo(expected);

    [Test]
    [Arguments("years-from-duration", "yearMonthDuration:-P1Y6M", "-1")]
    [Arguments("months-from-duration", "yearMonthDuration:-P1Y6M", "-6")]
    [Arguments("days-from-duration", "yearMonthDuration:-P1Y6M", "0")]
    [Arguments("hours-from-duration", "yearMonthDuration:-P1Y6M", "0")]
    [Arguments("minutes-from-duration", "yearMonthDuration:-P1Y6M", "0")]
    [Arguments("seconds-from-duration", "yearMonthDuration:-P1Y6M", "0")]
    [Arguments("years-from-duration", "dayTimeDuration:-P1DT1H", "0")]
    [Arguments("months-from-duration", "dayTimeDuration:-P1DT1H", "0")]
    [Arguments("days-from-duration", "dayTimeDuration:-P1DT1H", "-1")]
    [Arguments("hours-from-duration", "dayTimeDuration:-P1DT1H", "-1")]
    [Arguments("minutes-from-duration", "dayTimeDuration:-P1DT1H", "0")]
    [Arguments("seconds-from-duration", "dayTimeDuration:-P1DT1H", "0")]
    public async Task DurationComponent_DerivedDurationTypes_AreAccepted(
        string function,
        string argument,
        string expected
    ) => await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("year-from-dateTime", "untypedAtomic:2002-03-07T10:30:12-07:00", "2002")]
    [Arguments("seconds-from-time", "untypedAtomic:10:30:12.5", "12.5")]
    [Arguments("days-from-duration", "untypedAtomic:P3D", "3")]
    public async Task Component_UntypedAtomicArgument_IsCastToTheParameterType(
        string function,
        string argument,
        string expected
    ) => await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("year-from-dateTime", "date:2002-03-07")]
    [Arguments("year-from-date", "dateTime:2002-03-07T10:30:12")]
    [Arguments("hours-from-time", "dateTime:2002-03-07T10:30:12")]
    [Arguments("years-from-duration", "dateTime:2002-03-07T10:30:12")]
    [Arguments("year-from-dateTime", "string:2002-03-07T10:30:12")]
    [Arguments("years-from-duration", "integer:5")]
    public async Task Component_ArgumentOfAnotherType_RaisesXPTY0004(string function, string argument) =>
        await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("year-from-dateTime", "untypedAtomic:not a dateTime")]
    [Arguments("days-from-duration", "untypedAtomic:3 days")]
    public async Task Component_UntypedAtomicThatIsNotLexicallyValid_RaisesFORG0001(string function, string argument) =>
        await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo("error:FORG0001");
}
