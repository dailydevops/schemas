namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System;
using System.Globalization;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:distinct-values</c>, <c>fn:index-of</c>, and <c>fn:deep-equal</c> compare the date, time, duration, and Gregorian types by
/// value (Functions and Operators 1.0, 15.1.6, 15.1.3, and 15.3.1): a value without a timezone is read in the implicit timezone, and the
/// hash agrees with <c>eq</c> across timezones. Items are written <c>type:lexical</c>.
/// </summary>
public class TemporalEqualityTests
{
    private static XdmItem Item(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        var type = text.Substring(0, colon);
        var lexical = text.Substring(colon + 1);
        return type switch
        {
            "duration" => XdmItem.FromDuration(XsdDuration.Parse(lexical, DurationKind.Duration).Value),
            "yearMonthDuration" => XdmItem.FromDuration(
                XsdDuration.Parse(lexical, DurationKind.YearMonth).Value,
                DurationKind.YearMonth
            ),
            "dayTimeDuration" => XdmItem.FromDuration(
                XsdDuration.Parse(lexical, DurationKind.DayTime).Value,
                DurationKind.DayTime
            ),
            "integer" => XdmItem.FromInteger(long.Parse(lexical, CultureInfo.InvariantCulture)),
            _ => XdmItem.FromTemporal(XsdDateTime.Parse(lexical, KindOf(type), XsdVersion.V10).Value),
        };
    }

    private static XsdDateTimeKind KindOf(string type) =>
        type switch
        {
            "dateTime" => XsdDateTimeKind.DateTime,
            "date" => XsdDateTimeKind.Date,
            "time" => XsdDateTimeKind.Time,
            "gYear" => XsdDateTimeKind.GYear,
            "gYearMonth" => XsdDateTimeKind.GYearMonth,
            "gMonth" => XsdDateTimeKind.GMonth,
            "gMonthDay" => XsdDateTimeKind.GMonthDay,
            _ => XsdDateTimeKind.GDay,
        };

    private static XdmSequence Sequence(string items)
    {
        var parts = items.Split('|');
        var result = new XdmItem[parts.Length];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Item(parts[i]);
        }

        return XdmSequence.Create(result);
    }

    private static string Run(string function, int timezone, params string[] arguments)
    {
        using var context = new XPathEvaluationContext { ImplicitTimezoneMinutes = timezone };
        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = Sequence(arguments[i]);
        }

        var result = SequencesHarness.Resolve(function, sequences.Length).Call(context, sequences);
        return SequencesHarness.Show(result);
    }

    private static int Count(string shown) => shown.Length == 0 ? 0 : shown.Split(',').Length;

    [Test]
    public async Task DistinctValues_DatesWithTheSameInstantInTwoSpellings_ReturnsOneValue() =>
        await Assert.That(Count(Run("distinct-values", 0, "date:2000-01-01Z|date:2000-01-01+00:00"))).IsEqualTo(1);

    [Test]
    public async Task IndexOf_DateTimesWithTheSameInstant_FindsThePosition() =>
        await Assert
            .That(Run("index-of", 0, "dateTime:2000-01-01T12:00:00+02:00", "dateTime:2000-01-01T10:00:00Z"))
            .IsEqualTo("integer:1");

    [Test]
    public async Task DeepEqual_DurationsOfOneDay_AreEqual() =>
        await Assert.That(Run("deep-equal", 0, "duration:P1D", "duration:PT24H")).IsEqualTo("boolean:true");

    [Test]
    [Arguments(0, "boolean:true")]
    [Arguments(60, "boolean:false")]
    [Arguments(-120, "boolean:false")]
    public async Task DeepEqual_DateTimeWithoutTimezoneAgainstZ_DependsOnTheImplicitTimezone(
        int timezone,
        string expected
    ) =>
        await Assert
            .That(Run("deep-equal", timezone, "dateTime:2000-01-01T10:00:00", "dateTime:2000-01-01T10:00:00Z"))
            .IsEqualTo(expected);

    [Test]
    [Arguments(120, "dateTime:2000-01-01T10:00:00", "dateTime:2000-01-01T08:00:00Z", "integer:1")]
    [Arguments(-300, "dateTime:2000-01-01T10:00:00", "dateTime:2000-01-01T15:00:00Z", "integer:1")]
    [Arguments(0, "dateTime:2000-01-01T10:00:00", "dateTime:2000-01-01T08:00:00Z", "")]
    [Arguments(120, "time:10:00:00", "time:08:00:00Z", "integer:1")]
    [Arguments(120, "date:2000-01-01", "date:2000-01-01+02:00", "integer:1")]
    [Arguments(0, "date:2000-01-01", "date:2000-01-01+02:00", "")]
    public async Task IndexOf_ValueWithAndWithoutTimezone_FollowsTheImplicitTimezone(
        int timezone,
        string withoutZone,
        string withZone,
        string expected
    ) => await Assert.That(Run("index-of", timezone, withoutZone, withZone)).IsEqualTo(expected);

    [Test]
    [Arguments(120, "dateTime:2000-01-01T10:00:00|dateTime:2000-01-01T08:00:00Z", 1)]
    [Arguments(0, "dateTime:2000-01-01T10:00:00|dateTime:2000-01-01T08:00:00Z", 2)]
    [Arguments(0, "dateTime:2000-01-01T10:00:00|dateTime:2000-01-01T10:00:00Z", 1)]
    [Arguments(-60, "dateTime:2000-01-01T10:00:00|dateTime:2000-01-01T11:00:00Z|dateTime:2000-01-01T12:00:00+01:00", 2)]
    [Arguments(0, "gYear:2000|gYear:2000Z", 1)]
    [Arguments(0, "gYear:2000|gYear:2000+01:00", 2)]
    [Arguments(60, "gYear:2000|gYear:2000+01:00", 1)]
    [Arguments(0, "gYearMonth:2000-01Z|gYearMonth:2000-01+00:00", 1)]
    [Arguments(0, "gMonth:--01Z|gMonth:--01+00:00", 1)]
    [Arguments(0, "gMonthDay:--01-02Z|gMonthDay:--01-02+00:00", 1)]
    [Arguments(0, "gDay:---02Z|gDay:---02+00:00", 1)]
    [Arguments(0, "gDay:---02Z|gDay:---03Z", 2)]
    [Arguments(0, "time:10:00:00Z|time:12:00:00+02:00", 1)]
    [Arguments(0, "time:10:00:00Z|time:10:00:01Z", 2)]
    public async Task DistinctValues_TemporalValues_AgreeWithEqAcrossTimezones(
        int timezone,
        string items,
        int expectedCount
    ) => await Assert.That(Count(Run("distinct-values", timezone, items))).IsEqualTo(expectedCount);

    [Test]
    public async Task DistinctValues_KeepsTheFirstOfEqualValues() =>
        await Assert
            .That(Run("distinct-values", 0, "dateTime:2000-01-01T12:00:00+02:00|dateTime:2000-01-01T10:00:00Z"))
            .IsEqualTo("date:2000-01-01T12:00:00+02:00");

    [Test]
    [Arguments("yearMonthDuration:P0M", "dayTimeDuration:PT0S", true)]
    [Arguments("yearMonthDuration:P12M", "duration:P1Y", true)]
    [Arguments("dayTimeDuration:PT1H", "duration:PT60M", true)]
    [Arguments("yearMonthDuration:P1M", "dayTimeDuration:PT0S", false)]
    [Arguments("duration:P1M", "duration:P30D", false)]
    public async Task AreEqual_Durations_CompareByMonthsAndSeconds(string first, string second, bool expected)
    {
        var left = Item(first);
        var right = Item(second);
        using var context = new XPathEvaluationContext();

        await Assert.That(AtomicEquality.AreEqual(in left, in right, nanEqual: true, context)).IsEqualTo(expected);
        if (expected)
        {
            await Assert.That(AtomicEquality.HashCode(in left)).IsEqualTo(AtomicEquality.HashCode(in right));
        }
    }

    [Test]
    [Arguments("date:2000-01-01Z", "dateTime:2000-01-01T00:00:00Z")]
    [Arguments("date:2000-01-01Z", "gYear:2000Z")]
    [Arguments("time:00:00:00Z", "dateTime:1972-12-31T00:00:00Z")]
    [Arguments("duration:P0M", "dateTime:2000-01-01T00:00:00Z")]
    [Arguments("gYear:2000Z", "integer:2000")]
    public async Task AreEqual_ValuesOfDifferentTypes_AreDifferentAndNotAnError(string first, string second)
    {
        var left = Item(first);
        var right = Item(second);
        using var context = new XPathEvaluationContext();

        await Assert.That(AtomicEquality.AreEqual(in left, in right, nanEqual: true, context)).IsFalse();
        await Assert.That(AtomicEquality.AreEqual(in right, in left, nanEqual: true, context)).IsFalse();
    }

    [Test]
    public async Task IndexOf_DateAgainstDateTime_FindsNothingWithoutAnError() =>
        await Assert.That(Run("index-of", 0, "date:2000-01-01Z", "dateTime:2000-01-01T00:00:00Z")).IsEqualTo("");

    [Test]
    public async Task DeepEqual_DateAgainstDateTime_IsFalseWithoutAnError() =>
        await Assert
            .That(Run("deep-equal", 0, "date:2000-01-01Z", "dateTime:2000-01-01T00:00:00Z"))
            .IsEqualTo("boolean:false");

    [Test]
    public async Task AreEqual_TemporalValuesWithoutAContext_FallBackToTheRepresentation()
    {
        var left = Item("date:2000-01-01Z");
        var same = Item("date:2000-01-01Z");
        var other = Item("date:2000-01-01+00:00");

        await Assert.That(AtomicEquality.AreEqual(in left, in same, nanEqual: true)).IsTrue();
        await Assert.That(AtomicEquality.AreEqual(in left, in other, nanEqual: true)).IsFalse();
    }

    [Test]
    [Arguments(0, "dateTime:2000-01-01T10:00:00", "dateTime:2000-01-01T10:00:00Z")]
    [Arguments(120, "dateTime:2000-01-01T10:00:00", "dateTime:2000-01-01T08:00:00Z")]
    [Arguments(-840, "date:2000-01-01", "date:2000-01-01-14:00")]
    [Arguments(0, "time:23:30:00-02:00", "time:01:30:00Z")]
    public async Task HashCode_AgreesWithEqualityForTheImplicitTimezone(int timezone, string first, string second)
    {
        var left = Item(first);
        var right = Item(second);
        using var context = new XPathEvaluationContext { ImplicitTimezoneMinutes = timezone };
        var equal = AtomicEquality.AreEqual(in left, in right, nanEqual: true, context);

        await Assert.That(equal).IsTrue();
        await Assert
            .That(AtomicEquality.HashCode(in left, timezone))
            .IsEqualTo(AtomicEquality.HashCode(in right, timezone));
    }
}
