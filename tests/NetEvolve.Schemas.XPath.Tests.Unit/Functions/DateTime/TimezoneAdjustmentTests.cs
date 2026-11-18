namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System.Threading.Tasks;

/// <summary>
/// The timezone adjustment functions (Functions and Operators 1.0, 10.7) in both arities: a value without a timezone only gains
/// one, a value with a timezone keeps its instant, an empty timezone removes it, and a timezone outside -PT14H to PT14H or not
/// a whole number of minutes raises FODT0003.
/// </summary>
public class TimezoneAdjustmentTests
{
    private const int MinusFiveHours = -300;

    private static string OneArgument(string function, string argument) =>
        DateTimeHarness.RunWith(context => context.ImplicitTimezoneMinutes = MinusFiveHours, function, argument);

    [Test]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00", "2002-03-07T10:00:00-05:00")]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00-07:00", "2002-03-07T12:00:00-05:00")]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00-05:00", "2002-03-07T10:00:00-05:00")]
    [Arguments("adjust-dateTime-to-timezone", "()", "")]
    [Arguments("adjust-date-to-timezone", "date:2002-03-07", "2002-03-07-05:00")]
    [Arguments("adjust-date-to-timezone", "date:2002-03-07-07:00", "2002-03-07-05:00")]
    [Arguments("adjust-date-to-timezone", "()", "")]
    [Arguments("adjust-time-to-timezone", "time:10:00:00", "10:00:00-05:00")]
    [Arguments("adjust-time-to-timezone", "time:10:00:00-07:00", "12:00:00-05:00")]
    [Arguments("adjust-time-to-timezone", "()", "")]
    public async Task Adjust_OneArgument_UsesTheImplicitTimezone(string function, string argument, string expected) =>
        await Assert.That(OneArgument(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00Z", "2002-03-07T10:00:00Z")]
    [Arguments("adjust-date-to-timezone", "date:2002-03-07", "2002-03-07Z")]
    [Arguments("adjust-time-to-timezone", "time:10:00:00", "10:00:00Z")]
    [Arguments("adjust-time-to-timezone", "time:10:00:00+02:00", "08:00:00Z")]
    public async Task Adjust_OneArgument_DefaultImplicitTimezoneIsUtc(
        string function,
        string argument,
        string expected
    ) => await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    public async Task Adjust_OneArgument_FollowsAChangedImplicitTimezone() =>
        await Assert
            .That(
                DateTimeHarness.RunWith(
                    context => context.ImplicitTimezoneMinutes = 330,
                    "adjust-dateTime-to-timezone",
                    "dateTime:2002-03-07T10:00:00Z"
                )
            )
            .IsEqualTo("2002-03-07T15:30:00+05:30");

    [Test]
    [Arguments("dateTime:2002-03-07T10:00:00-07:00", "dayTimeDuration:PT10H", "2002-03-08T03:00:00+10:00")]
    [Arguments("dateTime:2002-03-07T00:00:00+01:00", "dayTimeDuration:-PT8H", "2002-03-06T15:00:00-08:00")]
    [Arguments("dateTime:2002-03-07T10:00:00", "()", "2002-03-07T10:00:00")]
    [Arguments("dateTime:2002-03-07T10:00:00-07:00", "()", "2002-03-07T10:00:00")]
    [Arguments("dateTime:2002-03-07T10:00:00", "dayTimeDuration:-PT10H", "2002-03-07T10:00:00-10:00")]
    [Arguments("()", "dayTimeDuration:PT1H", "")]
    [Arguments("dateTime:2002-03-07T10:00:00.5-07:00", "dayTimeDuration:PT0S", "2002-03-07T17:00:00.5Z")]
    [Arguments("dateTime:2002-12-31T23:00:00-05:00", "dayTimeDuration:PT0S", "2003-01-01T04:00:00Z")]
    [Arguments("dateTime:2000-02-29T23:00:00-05:00", "dayTimeDuration:PT0S", "2000-03-01T04:00:00Z")]
    [Arguments("dateTime:1900-02-28T23:00:00-05:00", "dayTimeDuration:PT0S", "1900-03-01T04:00:00Z")]
    [Arguments("dateTime:2002-03-07T10:00:00Z", "dayTimeDuration:PT14H", "2002-03-08T00:00:00+14:00")]
    [Arguments("dateTime:2002-03-07T10:00:00Z", "dayTimeDuration:-PT14H", "2002-03-06T20:00:00-14:00")]
    [Arguments("dateTime:2002-03-07T10:00:00Z", "dayTimeDuration:PT5H30M", "2002-03-07T15:30:00+05:30")]
    [Arguments("dateTime:2002-03-07T10:00:00+05:30", "dayTimeDuration:-PT3H45M", "2002-03-07T00:45:00-03:45")]
    public async Task AdjustDateTime_TwoArguments(string argument, string timezone, string expected) =>
        await Assert.That(DateTimeHarness.Run("adjust-dateTime-to-timezone", argument, timezone)).IsEqualTo(expected);

    [Test]
    [Arguments("date:2002-03-07-07:00", "dayTimeDuration:-PT10H", "2002-03-06-10:00")]
    [Arguments("date:2002-03-07", "()", "2002-03-07")]
    [Arguments("date:2002-03-07-07:00", "()", "2002-03-07")]
    [Arguments("date:2002-03-07", "dayTimeDuration:-PT10H", "2002-03-07-10:00")]
    [Arguments("()", "dayTimeDuration:PT1H", "")]
    [Arguments("date:2002-03-07+14:00", "dayTimeDuration:-PT14H", "2002-03-05-14:00")]
    [Arguments("date:2002-03-07Z", "dayTimeDuration:PT14H", "2002-03-07+14:00")]
    [Arguments("date:2002-12-31-05:00", "dayTimeDuration:PT0S", "2002-12-31Z")]
    [Arguments("date:2000-03-01+01:00", "dayTimeDuration:-PT1H", "2000-02-29-01:00")]
    public async Task AdjustDate_TwoArguments(string argument, string timezone, string expected) =>
        await Assert.That(DateTimeHarness.Run("adjust-date-to-timezone", argument, timezone)).IsEqualTo(expected);

    [Test]
    [Arguments("time:10:00:00-07:00", "dayTimeDuration:PT10H", "03:00:00+10:00")]
    [Arguments("time:10:00:00", "()", "10:00:00")]
    [Arguments("time:10:00:00-07:00", "()", "10:00:00")]
    [Arguments("time:10:00:00", "dayTimeDuration:PT2H", "10:00:00+02:00")]
    [Arguments("()", "dayTimeDuration:PT1H", "")]
    [Arguments("time:23:30:00Z", "dayTimeDuration:PT5H", "04:30:00+05:00")]
    [Arguments("time:00:30:00+05:00", "dayTimeDuration:PT0S", "19:30:00Z")]
    [Arguments("time:10:00:00.25-07:00", "dayTimeDuration:PT0S", "17:00:00.25Z")]
    [Arguments("time:12:00:00Z", "dayTimeDuration:-PT14H", "22:00:00-14:00")]
    public async Task AdjustTime_TwoArguments(string argument, string timezone, string expected) =>
        await Assert.That(DateTimeHarness.Run("adjust-time-to-timezone", argument, timezone)).IsEqualTo(expected);

    [Test]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00Z")]
    [Arguments("adjust-date-to-timezone", "date:2002-03-07Z")]
    [Arguments("adjust-time-to-timezone", "time:10:00:00Z")]
    public async Task Adjust_TimezoneOutsideFourteenHours_RaisesFODT0003(string function, string argument)
    {
        await Assert.That(DateTimeHarness.Run(function, argument, "dayTimeDuration:PT15H")).IsEqualTo("error:FODT0003");
        await Assert
            .That(DateTimeHarness.Run(function, argument, "dayTimeDuration:-PT15H"))
            .IsEqualTo("error:FODT0003");
        await Assert
            .That(DateTimeHarness.Run(function, argument, "dayTimeDuration:PT14H1M"))
            .IsEqualTo("error:FODT0003");
        await Assert
            .That(DateTimeHarness.Run(function, argument, "dayTimeDuration:-PT14H1M"))
            .IsEqualTo("error:FODT0003");
        await Assert.That(DateTimeHarness.Run(function, argument, "dayTimeDuration:P2D")).IsEqualTo("error:FODT0003");
    }

    [Test]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00Z")]
    [Arguments("adjust-date-to-timezone", "date:2002-03-07Z")]
    [Arguments("adjust-time-to-timezone", "time:10:00:00Z")]
    public async Task Adjust_TimezoneNotAWholeNumberOfMinutes_RaisesFODT0003(string function, string argument)
    {
        await Assert.That(DateTimeHarness.Run(function, argument, "dayTimeDuration:PT30S")).IsEqualTo("error:FODT0003");
        await Assert
            .That(DateTimeHarness.Run(function, argument, "dayTimeDuration:PT1H0M0.5S"))
            .IsEqualTo("error:FODT0003");
        await Assert
            .That(DateTimeHarness.Run(function, argument, "dayTimeDuration:-PT1M30S"))
            .IsEqualTo("error:FODT0003");
    }

    [Test]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00", "2002-03-07T10:00:00+14:00")]
    [Arguments("adjust-date-to-timezone", "date:2002-03-07", "2002-03-07+14:00")]
    [Arguments("adjust-time-to-timezone", "time:10:00:00", "10:00:00+14:00")]
    public async Task Adjust_TimezoneOfExactlyFourteenHours_IsAccepted(
        string function,
        string argument,
        string expected
    ) => await Assert.That(DateTimeHarness.Run(function, argument, "dayTimeDuration:PT14H")).IsEqualTo(expected);

    [Test]
    [Arguments("adjust-dateTime-to-timezone")]
    [Arguments("adjust-date-to-timezone")]
    [Arguments("adjust-time-to-timezone")]
    public async Task Adjust_EmptyValueWithInvalidTimezone_ReturnsTheEmptySequenceWithoutValidatingTheTimezone(
        string function
    ) => await Assert.That(DateTimeHarness.Run(function, "()", "dayTimeDuration:PT15H")).IsEqualTo(string.Empty);

    [Test]
    [Arguments("adjust-dateTime-to-timezone", "date:2002-03-07", "dayTimeDuration:PT1H")]
    [Arguments("adjust-date-to-timezone", "dateTime:2002-03-07T10:00:00", "dayTimeDuration:PT1H")]
    [Arguments("adjust-time-to-timezone", "dateTime:2002-03-07T10:00:00", "dayTimeDuration:PT1H")]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00", "duration:PT1H")]
    [Arguments("adjust-dateTime-to-timezone", "dateTime:2002-03-07T10:00:00", "string:PT1H")]
    public async Task Adjust_ArgumentOfAnotherType_RaisesXPTY0004(string function, string argument, string timezone) =>
        await Assert.That(DateTimeHarness.Run(function, argument, timezone)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Adjust_UntypedAtomicTimezone_IsCastToDayTimeDuration() =>
        await Assert
            .That(
                DateTimeHarness.Run(
                    "adjust-dateTime-to-timezone",
                    "dateTime:2002-03-07T10:00:00Z",
                    "untypedAtomic:-PT8H"
                )
            )
            .IsEqualTo("2002-03-07T02:00:00-08:00");
}
