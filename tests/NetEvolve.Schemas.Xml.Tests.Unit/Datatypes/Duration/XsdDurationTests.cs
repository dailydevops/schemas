namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Duration;

using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;

public class XsdDurationTests
{
    private static IDatatypeDescriptor<XsdDuration> Get(string name, XsdVersion version) =>
        (IDatatypeDescriptor<XsdDuration>)BuiltInDatatypes.For(version).Get(name);

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Registry_ContainsDuration_InBothVersions(XsdVersion version)
    {
        await Assert.That(Get("duration", version).Name).IsEqualTo("duration");
        await Assert.That(Get("duration", version).LexicalMode).IsEqualTo(version);
    }

    [Test]
    public async Task Registry_ContainsDerivedTypes_InVersion11Only()
    {
        var v11 = BuiltInDatatypes.For(XsdVersion.V11);
        await Assert.That(v11.Get("yearMonthDuration").BaseType!.Name).IsEqualTo("duration");
        await Assert.That(v11.Get("dayTimeDuration").BaseType!.Name).IsEqualTo("duration");
        await Assert.That(BuiltInDatatypes.For(XsdVersion.V10).TryGet("yearMonthDuration", out _)).IsFalse();
        await Assert.That(BuiltInDatatypes.For(XsdVersion.V10).TryGet("dayTimeDuration", out _)).IsFalse();
    }

    [Test]
    public async Task Parse_Full_MapsToMonthsAndSeconds()
    {
        var result = Get("duration", XsdVersion.V11).Parse("P1Y2M3DT4H5M6.7S");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Months).IsEqualTo(14L);
        // 3 d = 259200 s, 4 h = 14400 s, 5 min = 300 s, plus 6.7 s.
        await Assert.That(result.Value.Seconds).IsEqualTo(XsdDecimal.Parse("273906.7").Value);
        await Assert.That(result.Value.ToString()).IsEqualTo("P1Y2M3DT4H5M6.7S");
    }

    [Test]
    public async Task Parse_Negative_KeepsSignOnBothComponents()
    {
        var value = Get("duration", XsdVersion.V11).Parse("-P1Y2M3DT4H5M6.7S").Value;

        await Assert.That(value.Months).IsEqualTo(-14L);
        await Assert.That(value.Seconds).IsEqualTo(XsdDecimal.Parse("-273906.7").Value);
        await Assert.That(value.ToString()).IsEqualTo("-P1Y2M3DT4H5M6.7S");
    }

    [Test]
    [Arguments("P1Y", "P1Y")]
    [Arguments("P12M", "P1Y")]
    [Arguments("P0Y13M", "P1Y1M")]
    [Arguments("PT60S", "PT1M")]
    [Arguments("PT3600S", "PT1H")]
    [Arguments("PT86400S", "P1D")]
    [Arguments("P0D", "PT0S")]
    [Arguments("-P0Y", "PT0S")]
    [Arguments("PT0.500S", "PT0.5S")]
    [Arguments("P1DT0H0M1.25S", "P1DT1.25S")]
    public async Task Canonical_NormalizesComponents(string lexical, string expected)
    {
        await Assert.That(Get("duration", XsdVersion.V11).Parse(lexical).Value.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task Canonical_Zero_FollowsTheKind()
    {
        await Assert.That(default(XsdDuration).ToString(DurationKind.YearMonth)).IsEqualTo("P0M");
        await Assert.That(default(XsdDuration).ToString(DurationKind.DayTime)).IsEqualTo("PT0S");
        await Assert.That(default(XsdDuration).ToString()).IsEqualTo("PT0S");
    }

    [Test]
    [Arguments("P1Y", "P12M")]
    [Arguments("PT60S", "PT1M")]
    [Arguments("P1D", "PT24H")]
    [Arguments("PT1.0S", "PT1S")]
    public async Task Equality_IsByValue(string left, string right)
    {
        var duration = Get("duration", XsdVersion.V11);
        var a = duration.Parse(left).Value;
        var b = duration.Parse(right).Value;

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a == b).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
        await Assert.That(duration.IsIdentical(a, b)).IsTrue();
        await Assert.That(duration.Compare(a, b)).IsEqualTo(XsdOrder.Equal);
    }

    [Test]
    public async Task Equality_DifferentValues_AreNotEqual()
    {
        var duration = Get("duration", XsdVersion.V11);
        var a = duration.Parse("P1M").Value;
        var b = duration.Parse("P30D").Value;

        await Assert.That(a != b).IsTrue();
        await Assert.That(a.Equals((object)b)).IsFalse();
        await Assert.That(duration.IsIdentical(a, b)).IsFalse();
        await Assert.That(duration.Compare(a, b)).IsEqualTo(XsdOrder.Incomparable);
    }

    [Test]
    public async Task Compare_OneComponentDiffers_IsDecided()
    {
        var duration = Get("duration", XsdVersion.V11);

        await Assert
            .That(duration.Compare(duration.Parse("P1Y").Value, duration.Parse("P2Y").Value))
            .IsEqualTo(XsdOrder.Less);
        await Assert
            .That(duration.Compare(duration.Parse("PT2S").Value, duration.Parse("PT1S").Value))
            .IsEqualTo(XsdOrder.Greater);
    }

    [Test]
    [Arguments("P")]
    [Arguments("PT")]
    [Arguments("P1S")]
    [Arguments("P1Y2D3M")]
    [Arguments("P1Y1Y")]
    [Arguments("P-1Y")]
    [Arguments("PT-1S")]
    [Arguments("--P1Y")]
    [Arguments("+P1Y")]
    [Arguments("P1.5Y")]
    [Arguments("P1Y2.5M")]
    [Arguments("P1.5D")]
    [Arguments("PT1.5H")]
    [Arguments("PT1.5M")]
    [Arguments("PT.5S")]
    [Arguments("PT1.S")]
    [Arguments("P1")]
    [Arguments("P1YT")]
    [Arguments("P1Y T1H")]
    [Arguments("PTT1H")]
    [Arguments("P1DT")]
    [Arguments("PT1H1D")]
    [Arguments("p1y")]
    [Arguments("1Y")]
    [Arguments("")]
    public async Task Parse_InvalidForms_AreRejectedWithCode(string lexical)
    {
        var result = Get("duration", XsdVersion.V11).Parse(lexical);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotEqualTo(DatatypeError.None);
    }

    [Test]
    public async Task Parse_InvalidForm_ReportsOffset()
    {
        var result = Get("duration", XsdVersion.V11).Parse("P1Y2D3M");

        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(result.ErrorOffset).IsEqualTo(6);
    }

    [Test]
    public async Task Parse_InvalidCharacter_IsCoded()
    {
        await Assert.That(Get("duration", XsdVersion.V11).Parse("P1Y#").Error).IsEqualTo(DatatypeError.InvalidCharacter);
    }

    [Test]
    public async Task Parse_SurroundingWhitespace_IsCollapsed()
    {
        await Assert.That(Get("duration", XsdVersion.V11).Parse("  P1Y \n").Value.Months).IsEqualTo(12L);
    }

    [Test]
    public async Task Parse_ComponentCap_Is17Digits()
    {
        var duration = Get("duration", XsdVersion.V11);

        await Assert.That(duration.Parse("P99999999999999999Y").Success).IsTrue();
        await Assert.That(duration.Parse("P00099999999999999999Y").Success).IsTrue();
        await Assert.That(duration.Parse("P100000000000000000Y").Error).IsEqualTo(DatatypeError.YearOrComponentCap);
        await Assert.That(duration.Parse("PT100000000000000000S").Error).IsEqualTo(DatatypeError.YearOrComponentCap);
    }

    [Test]
    public async Task Parse_LargestComponents_DoNotOverflow()
    {
        var value = Get("duration", XsdVersion.V11)
            .Parse("P99999999999999999Y99999999999999999M99999999999999999D")
            .Value;

        await Assert.That(value.Months).IsEqualTo((12L * 99999999999999999L) + 99999999999999999L);
        await Assert.That(value.Seconds.Mantissa).IsEqualTo(new BigInteger(99999999999999999L) * 86400);
    }

    [Test]
    public async Task Parse_FractionBeyondDecimalCap_IsRejected()
    {
        var lexical = "PT0." + new string('1', XsdDecimal.DefaultMaxDigits + 1) + "S";

        await Assert.That(Get("duration", XsdVersion.V11).Parse(lexical).Error).IsEqualTo(DatatypeError.DigitCap);
    }

    [Test]
    public async Task Parse_LongFraction_IsExact()
    {
        var value = Get("duration", XsdVersion.V11).Parse("PT0.123456789012345678901234567890S").Value;

        await Assert.That(value.Seconds).IsEqualTo(XsdDecimal.Parse("0.123456789012345678901234567890").Value);
    }

    [Test]
    [Arguments("P1Y", 12L)]
    [Arguments("-P1Y2M", -14L)]
    [Arguments("P0M", 0L)]
    [Arguments("P12M", 12L)]
    public async Task YearMonthDuration_AcceptsYearAndMonth(string lexical, long months)
    {
        var result = Get("yearMonthDuration", XsdVersion.V11).Parse(lexical);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Months).IsEqualTo(months);
        await Assert.That(result.Value.Seconds).IsEqualTo(XsdDecimal.Zero);
    }

    [Test]
    [Arguments("P1D")]
    [Arguments("P1Y1D")]
    [Arguments("PT1H")]
    [Arguments("PT1M")]
    [Arguments("PT1S")]
    [Arguments("P1YT1S")]
    [Arguments("P1YT")]
    public async Task YearMonthDuration_RejectsDayAndTimeComponents(string lexical)
    {
        await Assert.That(Get("yearMonthDuration", XsdVersion.V11).Parse(lexical).Success).IsFalse();
        await Assert.That(Get("yearMonthDuration", XsdVersion.V11).Validate(lexical).IsValid).IsFalse();
    }

    [Test]
    [Arguments("P1D", 86400)]
    [Arguments("-PT1H", -3600)]
    [Arguments("PT1M", 60)]
    [Arguments("P1DT2H3M4S", 93784)]
    [Arguments("P1DT1H1M", 90060)]
    public async Task DayTimeDuration_AcceptsDayAndTime(string lexical, int seconds)
    {
        var result = Get("dayTimeDuration", XsdVersion.V11).Parse(lexical);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Months).IsEqualTo(0L);
        await Assert.That(result.Value.Seconds).IsEqualTo(new XsdDecimal(seconds, 0));
    }

    [Test]
    [Arguments("P1Y")]
    [Arguments("P1M")]
    [Arguments("P1Y1D")]
    [Arguments("P1M1D")]
    [Arguments("PT1H1Y")]
    public async Task DayTimeDuration_RejectsYearAndMonthComponents(string lexical)
    {
        await Assert.That(Get("dayTimeDuration", XsdVersion.V11).Parse(lexical).Success).IsFalse();
        await Assert.That(Get("dayTimeDuration", XsdVersion.V11).Validate(lexical).IsValid).IsFalse();
    }

    [Test]
    public async Task DerivedTypes_CompareTotally()
    {
        var yearMonth = Get("yearMonthDuration", XsdVersion.V11);
        var dayTime = Get("dayTimeDuration", XsdVersion.V11);

        await Assert
            .That(yearMonth.Compare(yearMonth.Parse("P1Y").Value, yearMonth.Parse("P13M").Value))
            .IsEqualTo(XsdOrder.Less);
        await Assert
            .That(dayTime.Compare(dayTime.Parse("P1D").Value, dayTime.Parse("PT23H").Value))
            .IsEqualTo(XsdOrder.Greater);
    }

    [Test]
    public async Task Parse_Standalone_UsesTheKind()
    {
        await Assert.That(XsdDuration.Parse("P1D", DurationKind.YearMonth).Success).IsFalse();
        await Assert.That(XsdDuration.Parse("P1D", DurationKind.Duration).Success).IsTrue();
        await Assert.That(XsdDuration.Parse("P1D", DurationKind.DayTime).Success).IsTrue();
    }
}
