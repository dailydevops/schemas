namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:dateTime</c> (Functions and Operators 1.0, 10.4.1): the date and the time are combined, an empty argument gives the empty
/// sequence, one timezone carries over, equal timezones are kept, and different ones raise FORG0008.
/// </summary>
public class DateTimeConstructorTests
{
    [Test]
    [Arguments("date:1999-12-31", "time:12:00:00", "1999-12-31T12:00:00")]
    [Arguments("date:1999-12-31", "time:24:00:00", "1999-12-31T00:00:00")]
    [Arguments("date:1999-12-31Z", "time:12:00:00", "1999-12-31T12:00:00Z")]
    [Arguments("date:1999-12-31", "time:12:00:00+05:30", "1999-12-31T12:00:00+05:30")]
    [Arguments("date:1999-12-31-05:00", "time:12:00:00-05:00", "1999-12-31T12:00:00-05:00")]
    [Arguments("date:1999-12-31Z", "time:12:00:00+00:00", "1999-12-31T12:00:00Z")]
    [Arguments("date:1999-12-31", "time:12:00:00.25", "1999-12-31T12:00:00.25")]
    [Arguments("()", "time:12:00:00", "")]
    [Arguments("date:1999-12-31", "()", "")]
    [Arguments("()", "()", "")]
    [Arguments("date:1999-12-31Z", "time:12:00:00+01:00", "error:FORG0008")]
    public async Task DateTime_CombinesDateAndTime(string date, string time, string expected) =>
        await Assert.That(DateTimeHarness.Run("dateTime", date, time)).IsEqualTo(expected);
}
