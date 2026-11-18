namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;

/// <summary>
/// The context functions (Functions and Operators 1.0, 16): the current dateTime is read from the dynamic context, fixed once per
/// evaluation, and expressed in the implicit timezone; a caller sets the clock and the implicit timezone for deterministic results.
/// </summary>
public class CurrentDateTimeTests
{
    private static readonly DateTimeOffset Instant = new DateTimeOffset(
        2024,
        5,
        17,
        13,
        45,
        30,
        TimeSpan.FromHours(2)
    ).AddTicks(1234567);

    private static string Run(DateTimeOffset clock, int implicitMinutes, string function)
    {
        using var context = new XPathEvaluationContext
        {
            ImplicitTimezoneMinutes = implicitMinutes,
            Clock = () => clock,
        };
        return DateTimeHarness.RunIn(context, function);
    }

    [Test]
    public async Task CurrentDateTime_TwoCallsInOneEvaluation_ReturnTheSameValue()
    {
        var reads = 0;
        using var context = new XPathEvaluationContext
        {
            Clock = () => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(reads++),
        };

        var first = DateTimeHarness.RunIn(context, "current-dateTime");
        var second = DateTimeHarness.RunIn(context, "current-dateTime");

        await Assert.That(first).IsEqualTo("2024-01-01T00:00:00Z");
        await Assert.That(second).IsEqualTo(first);
        await Assert.That(reads).IsEqualTo(1);
    }

    [Test]
    public async Task CurrentFunctions_InOneEvaluation_ShareOneClockReading()
    {
        var reads = 0;
        using var context = new XPathEvaluationContext
        {
            Clock = () => new DateTimeOffset(2024, 12, 31, 23, 59, 58, TimeSpan.Zero).AddSeconds(reads++),
        };

        var dateTime = DateTimeHarness.RunIn(context, "current-dateTime");
        var date = DateTimeHarness.RunIn(context, "current-date");
        var time = DateTimeHarness.RunIn(context, "current-time");
        var zone = DateTimeHarness.RunIn(context, "implicit-timezone");

        await Assert.That(dateTime).IsEqualTo("2024-12-31T23:59:58Z");
        await Assert.That(date).IsEqualTo("2024-12-31Z");
        await Assert.That(time).IsEqualTo("23:59:58Z");
        await Assert.That(zone).IsEqualTo("PT0S");
        await Assert.That(reads).IsEqualTo(1);
    }

    [Test]
    public async Task CurrentDateTime_AfterReset_ReadsTheClockAgain()
    {
        var reads = 0;
        using var context = new XPathEvaluationContext
        {
            Clock = () => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(reads++),
        };

        var first = DateTimeHarness.RunIn(context, "current-dateTime");
        context.Reset();
        var second = DateTimeHarness.RunIn(context, "current-dateTime");

        await Assert.That(first).IsEqualTo("2024-01-01T00:00:00Z");
        await Assert.That(second).IsEqualTo("2024-01-01T00:00:01Z");
    }

    [Test]
    [Arguments("current-dateTime", "2024-05-17T13:45:30.1234567+02:00")]
    [Arguments("current-date", "2024-05-17+02:00")]
    [Arguments("current-time", "13:45:30.1234567+02:00")]
    [Arguments("implicit-timezone", "PT2H")]
    public async Task Current_InjectedClockInTheImplicitTimezone_IsReturnedUnchanged(
        string function,
        string expected
    ) => await Assert.That(Run(Instant, 120, function)).IsEqualTo(expected);

    [Test]
    [Arguments("current-dateTime", "2024-05-18T01:30:00+02:00")]
    [Arguments("current-date", "2024-05-18+02:00")]
    [Arguments("current-time", "01:30:00+02:00")]
    public async Task Current_InjectedClockInAnotherTimezone_KeepsTheInstantInTheImplicitTimezone(
        string function,
        string expected
    ) =>
        await Assert
            .That(Run(new DateTimeOffset(2024, 5, 17, 23, 30, 0, TimeSpan.Zero), 120, function))
            .IsEqualTo(expected);

    [Test]
    [Arguments(0, "PT0S")]
    [Arguments(120, "PT2H")]
    [Arguments(-300, "-PT5H")]
    [Arguments(330, "PT5H30M")]
    [Arguments(840, "PT14H")]
    [Arguments(-840, "-PT14H")]
    public async Task ImplicitTimezone_ReturnsTheDayTimeDurationOfTheContext(int minutes, string expected)
    {
        using var context = new XPathEvaluationContext { ImplicitTimezoneMinutes = minutes };

        await Assert.That(DateTimeHarness.RunIn(context, "implicit-timezone")).IsEqualTo(expected);
    }

    [Test]
    public async Task Current_WithTheSystemClock_IsExpressedInUtcByDefault()
    {
        var before = DateTimeOffset.UtcNow;

        var result = DateTimeHarness.Run("current-dateTime");

        var parsed = DateTimeOffset.Parse(result, System.Globalization.CultureInfo.InvariantCulture);
        await Assert.That(result).EndsWith("Z");
        await Assert.That(parsed >= before.AddSeconds(-1)).IsTrue();
        await Assert.That(parsed <= DateTimeOffset.UtcNow.AddSeconds(1)).IsTrue();
    }

    [Test]
    [Arguments("current-dateTime")]
    [Arguments("current-date")]
    [Arguments("current-time")]
    [Arguments("implicit-timezone")]
    public async Task Current_WithAnArgument_IsNotRegistered(string function) =>
        await Assert.That(DateTimeHarness.Run(function, "string:x")).IsEqualTo("error:not-registered");
}
