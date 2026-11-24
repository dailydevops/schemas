namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Dates;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>format-date</c>, <c>format-dateTime</c>, and <c>format-time</c> (Functions and Operators 3.1, 9.8): the picture string, the
/// components with their presentation and width modifiers, the English names, the time zone forms, the errors, and the fallbacks.
/// </summary>
public class FormatDateTimeTests
{
    private const string Sample = "2003-09-07T09:15:06.456";
    private const string Early = "0985-03-01T09:15:06.456";

    // The examples of the specification (9.8.5) and of the XSLT 2.0 specification (16.5.5).
    [Test]
    [Arguments("2002-12-31T12:58:35.5", "[M01]/[D01]/[Y0001] at [H01]:[m01]:[s01]", "12/31/2002 at 12:58:35")]
    [Arguments("2002-12-31T12:58:35.5", "[FNn], [D1o] [MNn], [Y]", "Tuesday, 31st December, 2002")]
    [Arguments("2002-12-31T12:58:35.5", "[h]:[m01] [PN]", "12:58 PM")]
    [Arguments("2002-12-31T12:58:35.5", "[Y0001]-[M01]-[D01]T[H01]:[m01]:[s01].[f001]", "2002-12-31T12:58:35.500")]
    [Arguments("2002-12-31T12:58:35.5", "[[[Y]]]", "[2002]")]
    public async Task Examples_OfTheSpecification_FormatDateTime(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-dateTime", value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("2003-09-07", "[Y]-[M01]-[D]", "2003-09-7")]
    [Arguments("2003-09-07", "[D1] [MI] [Y]", "7 IX 2003")]
    [Arguments("2003-09-07", "[[[Y0001]-[M01]-[D01]]]", "[2003-09-07]")]
    [Arguments("2003-09-07", "([Y01]-[M01]-[D01])", "(03-09-07)")]
    [Arguments("2003-09-07", "[ D 01 ] [M 0 1] [ Y 0 0 0 1 ]", "07 09 2003")]
    [Arguments("2003-09-07", "[Y๐๐๐๑]-[M๐๑]-[D๑]", "๒๐๐๓-๐๙-๗")]
    [Arguments("2012-05-18", "[Y9;999]", "2;012")]
    [Arguments("2012-05-18", "[Y9,999,*]", "2,012")]
    [Arguments("2012-05-18", "[Y9999,25]", "0000000000000000000002012")]
    [Arguments("2016-02-01", "[d]", "32")]
    [Arguments("2003-09-07", "no markers at all", "no markers at all")]
    [Arguments("2003-09-07", "", "")]
    public async Task Examples_OfTheSpecification_FormatDate(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-date", value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("09:15:06.456", "[H01]:[m01]", "09:15")]
    [Arguments("09:15:06.456", "[H]:[m]:[s]", "9:15:06")]
    [Arguments("09:15:06.456", "[H]:[m]:[s1]", "9:15:6")]
    [Arguments("09:15:06.456", "[H]:[m]:[s01]:[f001]", "9:15:06:456")]
    [Arguments("09:15:06.456", "[H]:[m]:[s].[f,1-1]", "9:15:06.4")]
    [Arguments("09:15:06.456", "[H]:[m]:[s].[f01]", "9:15:06.45")]
    [Arguments("09:15:06.456", "[f,4-4]", "4560")]
    [Arguments("09:15:06.456", "[f,*-2]", "45")]
    [Arguments("09:15:06.456", "[f]", "456")]
    [Arguments("09:15:06", "[f]", "0")]
    [Arguments("09:15:06.456", "[f๐๐๐]", "๔๕๖")]
    [Arguments("09:15:06.456", "[H٠]:[m٠]:[s٠٠]:[f٠٠٠]", "٩:١٥:٠٦:٤٥٦")]
    [Arguments("15:15:06.456", "[h].[m]", "3.15")]
    [Arguments("00:15:06", "[h]", "12")]
    [Arguments("12:15:06", "[h]", "12")]
    [Arguments("09:15:06", "[Pn]/[PNn]/[PN]", "am/Am/AM")]
    [Arguments("15:15:06", "[Pn]/[PNn]/[PN]", "pm/Pm/PM")]
    [Arguments("09:15:06.456", "[m,3]", "015")]
    public async Task Examples_OfTheSpecification_FormatTime(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-time", value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("[Y,4-4]", "0985")]
    [Arguments("[Y,3-4]", "985")]
    [Arguments("[Y,2-5]", "985")]
    [Arguments("[Y,2-2]", "85")]
    [Arguments("[Y,2-*]", "985")]
    [Arguments("[Y,*-4]", "985")]
    [Arguments("[Y,3]", "985")]
    [Arguments("[M,4-4]", "0003")]
    [Arguments("[M,1-4]", "3")]
    [Arguments("[M,2-2]", "03")]
    [Arguments("[M,*-2]", "3")]
    [Arguments("[M,3]", "003")]
    [Arguments("[Yi,4-4]", "cmlxxxv")]
    [Arguments("[f,4-4]", "4560")]
    [Arguments("[f,1-4]", "456")]
    [Arguments("[f,2-2]", "45")]
    [Arguments("[f,3]", "456")]
    public async Task WidthModifiers_MinimumAndMaximum(string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-dateTime", Early, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("[Y]", "2003")]
    [Arguments("[M]", "9")]
    [Arguments("[D]", "7")]
    [Arguments("[d]", "250")]
    [Arguments("[F]", "Sunday")]
    [Arguments("[W]", "36")]
    [Arguments("[w]", "1")]
    [Arguments("[H]", "9")]
    [Arguments("[h]", "9")]
    [Arguments("[P]", "am")]
    [Arguments("[m]", "15")]
    [Arguments("[s]", "06")]
    [Arguments("[f]", "456")]
    [Arguments("[E]", "ad")]
    [Arguments("[C]", "ad")]
    public async Task DefaultPresentation_OfEveryComponent(string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-dateTime", Sample, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("[MN]", "SEPTEMBER")]
    [Arguments("[Mn]", "september")]
    [Arguments("[MNn]", "September")]
    [Arguments("[MN,3-3]", "SEP")]
    [Arguments("[MNn,3-3]", "Sep")]
    [Arguments("[MNn,3-5]", "Sep")]
    [Arguments("[MNn,2-2]", "Se")]
    [Arguments("[MNn,3-9]", "September")]
    [Arguments("[FN]", "SUNDAY")]
    [Arguments("[Fn]", "sunday")]
    [Arguments("[FNn]", "Sunday")]
    [Arguments("[FNn,3-3]", "Sun")]
    [Arguments("[F01]", "07")]
    [Arguments("[YN]", "2003")]
    [Arguments("[Y][EN]", "2003AD")]
    [Arguments("[Y][En]", "2003ad")]
    [Arguments("[CN]", "AD")]
    public async Task NamePresentation_Case_AndAbbreviation(string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-dateTime", Sample, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("[D1o]", "7th")]
    [Arguments("[Do]", "7th")]
    [Arguments("[Y1o]", "2003rd")]
    [Arguments("[Y0001o]", "2003rd")]
    [Arguments("[DW]", "SEVEN")]
    [Arguments("[Dw]", "seven")]
    [Arguments("[DWw]", "Seven")]
    [Arguments("[Dwo]", "seventh")]
    [Arguments("[DWwo]", "Seventh")]
    [Arguments("[YWwo]", "Two Thousand And Third")]
    [Arguments("[YI]", "MMIII")]
    [Arguments("[Yi]", "mmiii")]
    [Arguments("[mA].[sa]", "O.f")]
    public async Task OrdinalWordsRomanAndAlphabetic_ReuseTheIntegerFormatting(string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-dateTime", Sample, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("2003-09-07T09:15:06-10:00", "[Z]", "-10:00")]
    [Arguments("2003-09-07T09:15:06-05:00", "[Z]", "-05:00")]
    [Arguments("2003-09-07T09:15:06Z", "[Z]", "+00:00")]
    [Arguments("2003-09-07T09:15:06+05:30", "[Z]", "+05:30")]
    [Arguments("2003-09-07T09:15:06+13:00", "[Z]", "+13:00")]
    [Arguments("2003-09-07T09:15:06-05:00", "[Z0]", "-5")]
    [Arguments("2003-09-07T09:15:06+05:30", "[Z0]", "+5:30")]
    [Arguments("2003-09-07T09:15:06-05:00", "[Z0:00]", "-5:00")]
    [Arguments("2003-09-07T09:15:06-05:00", "[Z00:00]", "-05:00")]
    [Arguments("2003-09-07T09:15:06-05:00", "[Z0000]", "-0500")]
    [Arguments("2003-09-07T09:15:06-09:30", "[Z999]", "-930")]
    [Arguments("2003-09-07T09:15:06-09:30", "[Z00~00]", "-09~30")]
    [Arguments("2003-09-07T09:15:06+05:30", "[Z٠٠:٠٠]", "+٠٥:٣٠")]
    [Arguments("2003-09-07T09:15:06Z", "[Z00:00t]", "Z")]
    [Arguments("2003-09-07T09:15:06-05:00", "[Z00:00t]", "-05:00")]
    [Arguments("2003-09-07T09:15:06-05:00", "[z]", "GMT-05:00")]
    [Arguments("2003-09-07T09:15:06-13:30", "[z0]", "GMT-13:30")]
    [Arguments("2003-09-07T09:15:06Z", "[z0]", "GMT+0")]
    [Arguments("2003-09-07T09:15:06-05:00", "[ZZ]", "R")]
    [Arguments("2003-09-07T09:15:06Z", "[ZZ]", "Z")]
    [Arguments("2003-09-07T09:15:06+01:00", "[ZZ]", "A")]
    [Arguments("2003-09-07T09:15:06+12:00", "[ZZ]", "M")]
    [Arguments("2003-09-07T09:15:06-12:00", "[ZZ]", "Y")]
    [Arguments("2003-09-07T09:15:06+05:30", "[ZZ]", "+05:30")]
    [Arguments("2003-09-07T09:15:06", "[ZZ]", "J")]
    [Arguments("2003-09-07T09:15:06", "[Z]", "")]
    [Arguments("2003-09-07T09:15:06", "[z]", "")]
    [Arguments("2003-09-07T09:15:06Z", "[ZN]", "GMT")]
    [Arguments("2003-09-07T09:15:06-05:00", "[ZN]", "GMT-05:00")]
    public async Task TimeZone_NumericAndNamedForms(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-dateTime", value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("2004-01-01", "[W]", "1")]
    [Arguments("2005-01-01", "[W]", "53")]
    [Arguments("2008-12-29", "[W]", "1")]
    [Arguments("2004-01-01", "[F01]", "04")]
    [Arguments("2004-03-01", "[F01]", "01")]
    [Arguments("2005-12-04", "[w]", "1")]
    [Arguments("2005-12-31", "[w]", "5")]
    [Arguments("2006-01-01", "[w]", "5")]
    [Arguments("2006-01-03", "[w]", "1")]
    [Arguments("2006-01-30", "[w]", "5")]
    [Arguments("2006-02-02", "[w]", "1")]
    [Arguments("2006-03-31", "[w]", "5")]
    public async Task WeekAndDayOfWeek_FollowTheIsoRules(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-date", value, picture, null, "ISO")).IsEqualTo(expected);

    [Test]
    [Arguments("-0055-03-01", "[Y][EN]", "55BC")]
    [Arguments("1990-03-01", "[Y][EN]", "1990AD")]
    public async Task Era_IsAdOrBc(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-date", value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("[bla]")]
    [Arguments("[yY]")]
    [Arguments("[]")]
    [Arguments("[Y")]
    [Arguments("Y]")]
    [Arguments("[Y[]")]
    [Arguments("[Y999#]")]
    [Arguments("[Y##9#]")]
    [Arguments("[Y#.0,4-3]")]
    [Arguments("[Y,0]")]
    [Arguments("[Y1o(]")]
    public async Task MalformedPicture_RaisesFOFD1340(string picture) =>
        await Assert
            .That(FormatDateTimeHarness.Format("format-date", "2003-09-07", picture))
            .IsEqualTo("error:FOFD1340");

    [Test]
    [Arguments("format-date", "[H]")]
    [Arguments("format-date", "[h]")]
    [Arguments("format-date", "[P]")]
    [Arguments("format-date", "[m]")]
    [Arguments("format-date", "[s]")]
    [Arguments("format-date", "[f]")]
    [Arguments("format-time", "[Y]")]
    [Arguments("format-time", "[M]")]
    [Arguments("format-time", "[D]")]
    [Arguments("format-time", "[d]")]
    [Arguments("format-time", "[F]")]
    [Arguments("format-time", "[W]")]
    [Arguments("format-time", "[w]")]
    [Arguments("format-time", "[C]")]
    [Arguments("format-time", "[E]")]
    public async Task ComponentNotAvailableForTheType_RaisesFOFD1350(string function, string picture)
    {
        var value = function == "format-date" ? "2003-09-07" : "09:15:06";

        await Assert.That(FormatDateTimeHarness.Format(function, value, picture)).IsEqualTo("error:FOFD1350");
    }

    [Test]
    public async Task MalformedPicture_IsReportedBeforeAnUnavailableComponent() =>
        await Assert
            .That(FormatDateTimeHarness.Format("format-date", "2003-09-07", "[H][bla]"))
            .IsEqualTo("error:FOFD1340");

    [Test]
    [Arguments("format-date", "[Y]")]
    [Arguments("format-dateTime", "[Y]")]
    [Arguments("format-time", "[H]")]
    public async Task EmptySequence_GivesTheEmptySequence(string function, string picture)
    {
        await Assert.That(FormatDateTimeHarness.Format(function, "()", picture)).IsEqualTo("()");
        await Assert.That(FormatDateTimeHarness.Format(function, "()", picture, "en", null)).IsEqualTo("()");
    }

    [Test]
    [Arguments("en", null, "September")]
    [Arguments("EN", null, "September")]
    [Arguments("en-GB", null, "September")]
    [Arguments(null, null, "September")]
    [Arguments("", null, "September")]
    [Arguments("en", "AD", "September")]
    [Arguments("en", "CE", "September")]
    [Arguments("en", "ISO", "September")]
    [Arguments("en", "Q{}AD", "September")]
    [Arguments("de", null, "[Language: en]September")]
    [Arguments("xib", null, "[Language: en]September")]
    [Arguments("not a language", null, "[Language: en]September")]
    [Arguments("en", "CB", "[Calendar: AD]September")]
    [Arguments("en", "Q{http://calendar.example.com/none}CB", "[Calendar: AD]September")]
    [Arguments("en", "cal:CB", "[Calendar: AD]September")]
    [Arguments("fr", "JE", "[Language: en][Calendar: AD]September")]
    public async Task LanguageAndCalendar_FallBackToEnglishAndGregorian(
        string? language,
        string? calendar,
        string expected
    ) =>
        await Assert
            .That(FormatDateTimeHarness.Format("format-date", "2003-09-07", "[MNn]", language, calendar))
            .IsEqualTo(expected);

    [Test]
    [Arguments("ZODIAC")]
    [Arguments(":w")]
    [Arguments("Q{}1")]
    [Arguments("Q{}ZODIAC")]
    [Arguments("1")]
    public async Task UnknownCalendarWithoutNamespace_RaisesFOFD1340(string calendar) =>
        await Assert
            .That(FormatDateTimeHarness.Format("format-date", "2003-09-07", "[M01]", "en", calendar))
            .IsEqualTo("error:FOFD1340");

    [Test]
    public async Task Place_IsAcceptedAndNotUsed() =>
        await Assert
            .That(
                FormatDateTimeHarness.Format(
                    "format-dateTime",
                    "2003-09-07T09:15:06Z",
                    "[H01]:[m01] [Z]",
                    "en",
                    null,
                    "America/New_York"
                )
            )
            .IsEqualTo("09:15 +00:00");

    [Test]
    [Arguments("format-date", "2003-09-07")]
    [Arguments("format-dateTime", "2003-09-07T09:15:06")]
    [Arguments("format-time", "09:15:06")]
    public async Task Availability_PlainXPath20Raises_XSLT20Host_And30Allow(string function, string value)
    {
        await Assert
            .That(FormatDateTimeHarness.Format(function, value, "", XPathVersion.V20, XPathHostRequirements.None))
            .IsEqualTo("error:not-registered");
        await Assert
            .That(FormatDateTimeHarness.Format(function, value, "", XPathVersion.V20, XPathHostRequirements.Xslt20Host))
            .IsEqualTo(string.Empty);
        await Assert
            .That(FormatDateTimeHarness.Format(function, value, "", XPathVersion.V30, XPathHostRequirements.None))
            .IsEqualTo(string.Empty);
        await Assert
            .That(FormatDateTimeHarness.Format(function, value, "", XPathVersion.V31, XPathHostRequirements.None))
            .IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("format-date", 1)]
    [Arguments("format-date", 3)]
    [Arguments("format-date", 4)]
    [Arguments("format-date", 6)]
    [Arguments("format-dateTime", 3)]
    [Arguments("format-time", 4)]
    public async Task OtherArities_AreNotRegistered(string function, int arity) =>
        await Assert
            .That(
                FormatDateTimeHarness.TryResolve(function, arity, XPathVersion.V31, XPathHostRequirements.None, out _)
            )
            .IsFalse();

    [Test]
    [Arguments("format-date", "2003-09-07", "[bla]")]
    [Arguments("format-dateTime", "2003-09-07T09:15:06", "[bla]")]
    [Arguments("format-time", "09:15:06", "[bla]")]
    public async Task Xslt20Host_UsesTheXTDECodes(string function, string value, string picture) =>
        await Assert
            .That(
                FormatDateTimeHarness.Format(
                    function,
                    value,
                    picture,
                    XPathVersion.V20,
                    XPathHostRequirements.Xslt20Host
                )
            )
            .IsEqualTo("error:XTDE1340");

    [Test]
    public async Task Xslt20Host_ComponentNotAvailable_RaisesXTDE1350() =>
        await Assert
            .That(
                FormatDateTimeHarness.Format(
                    "format-date",
                    "2003-09-07",
                    "[H]",
                    XPathVersion.V20,
                    XPathHostRequirements.Xslt20Host
                )
            )
            .IsEqualTo("error:XTDE1350");

    [Test]
    [Arguments("Q{abc")]
    [Arguments("a b:c")]
    [Arguments("1x:c")]
    public async Task MalformedCalendarName_RaisesFOFD1340(string calendar) =>
        await Assert
            .That(FormatDateTimeHarness.Format("format-date", "2003-09-07", "[M01]", "en", calendar))
            .IsEqualTo("error:FOFD1340");

    [Test]
    [Arguments("2003-09-07T09:15:06+14:00", "[ZZ]", "+14:00")]
    [Arguments("2003-09-07T09:15:06+10:00", "[ZZ]", "K")]
    [Arguments("2003-09-07T09:15:06+09:00", "[ZZ]", "I")]
    [Arguments("2003-09-07T09:15:06-01:00", "[ZZ]", "N")]
    [Arguments("2003-09-07T09:15:06-00:30", "[Z]", "-00:30")]
    [Arguments("2003-09-07T09:15:06-05:00", "[Zxyz]", "-05:00")]
    [Arguments("2003-09-07T09:15:06-05:00", "[ZNn]", "GMT-05:00")]
    [Arguments("2003-09-07T09:15:06", "[ZN]", "")]
    [Arguments("2003-09-07T09:15:06Z", "[z]", "GMT+00:00")]
    [Arguments("2003-09-07T09:15:06Z", "[Z0t]", "Z")]
    [Arguments("2003-09-07T09:15:06Z", "[z0t]", "GMT+0")]
    public async Task TimeZone_MilitaryAndUnusualPatterns(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-dateTime", value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("[fI]", "456")]
    [Arguments("[f001,1-1]", "4")]
    [Arguments("[f,2]", "456")]
    [Arguments("[Y,2-2]", "03")]
    [Arguments("[Yw,2-2]", "two thousand and three")]
    [Arguments("[P,1-1]", "a")]
    [Arguments("[D#1]", "7")]
    [Arguments("[H٠١]", "٠٩")]
    [Arguments("[Fn,2-2]", "su")]
    public async Task UnusualPresentationAndWidth(string picture, string expected) =>
        await Assert
            .That(FormatDateTimeHarness.Format("format-dateTime", "2003-09-07T09:15:06.456", picture))
            .IsEqualTo(expected);

    [Test]
    [Arguments("2008-02-29", "[d]", "60")]
    [Arguments("2008-12-31", "[d]", "366")]
    [Arguments("2009-01-04", "[w]", "1")]
    [Arguments("2009-01-01", "[F]", "Thursday")]
    [Arguments("2009-02-28", "[w]", "4")]
    [Arguments("1969-12-31", "[F]", "Wednesday")]
    [Arguments("-0001-12-31", "[F]", "Friday")]
    [Arguments("2010-01-03", "[W]", "53")]
    [Arguments("2012-01-01", "[W]", "52")]
    public async Task CalendarArithmetic_AcrossYearsAndLeapDays(string value, string picture, string expected) =>
        await Assert.That(FormatDateTimeHarness.Format("format-date", value, picture)).IsEqualTo(expected);
}
