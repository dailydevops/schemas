namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using static TemporalFixture;

public class TemporalComparisonTests
{
    [Test]
    [Arguments("xs:dateTime('2000-01-01T12:00:00+02:00') eq xs:dateTime('2000-01-01T10:00:00Z')", "Boolean:true")]
    [Arguments("xs:dateTime('2000-01-01T12:00:00+02:00') ne xs:dateTime('2000-01-01T10:00:00Z')", "Boolean:false")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00Z') lt xs:dateTime('2000-01-01T00:00:01Z')", "Boolean:true")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00Z') le xs:dateTime('2000-01-01T00:00:00Z')", "Boolean:true")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00Z') gt xs:dateTime('1999-12-31T23:59:59Z')", "Boolean:true")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00Z') ge xs:dateTime('2000-01-01T00:00:01Z')", "Boolean:false")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00.5Z') gt xs:dateTime('2000-01-01T00:00:00.25Z')", "Boolean:true")]
    [Arguments("xs:date('2000-01-01Z') lt xs:date('2000-01-02Z')", "Boolean:true")]
    [Arguments("xs:date('2000-01-01+14:00') lt xs:date('2000-01-01-14:00')", "Boolean:true")]
    [Arguments("xs:time('10:00:00Z') eq xs:time('12:00:00+02:00')", "Boolean:true")]
    [Arguments("xs:time('23:00:00-05:00') gt xs:time('01:00:00Z')", "Boolean:true")]
    [Arguments("-5 lt 3", "Boolean:true")]
    public async Task TwoValuesOfTheSameTypeWithTimezones_AreOrderedOnTheTimeline(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments(0, "xs:dateTime('2000-01-01T10:00:00') eq xs:dateTime('2000-01-01T10:00:00Z')", "Boolean:true")]
    [Arguments(120, "xs:dateTime('2000-01-01T10:00:00') eq xs:dateTime('2000-01-01T10:00:00Z')", "Boolean:false")]
    [Arguments(120, "xs:dateTime('2000-01-01T10:00:00') lt xs:dateTime('2000-01-01T10:00:00Z')", "Boolean:true")]
    [Arguments(-300, "xs:dateTime('2000-01-01T10:00:00') lt xs:dateTime('2000-01-01T10:00:00Z')", "Boolean:false")]
    [Arguments(-300, "xs:dateTime('2000-01-01T10:00:00') gt xs:dateTime('2000-01-01T10:00:00Z')", "Boolean:true")]
    [Arguments(120, "xs:dateTime('2000-01-01T10:00:00') eq xs:dateTime('2000-01-01T10:00:00+02:00')", "Boolean:true")]
    [Arguments(-300, "xs:dateTime('2000-01-01T10:00:00') eq xs:dateTime('2000-01-01T10:00:00+02:00')", "Boolean:false")]
    [Arguments(0, "xs:date('2000-01-01') eq xs:date('2000-01-01Z')", "Boolean:true")]
    [Arguments(840, "xs:date('2000-01-01') eq xs:date('2000-01-01Z')", "Boolean:false")]
    [Arguments(840, "xs:date('2000-01-01') lt xs:date('2000-01-01Z')", "Boolean:true")]
    [Arguments(-840, "xs:date('2000-01-01') gt xs:date('2000-01-01Z')", "Boolean:true")]
    [Arguments(0, "xs:time('10:00:00') eq xs:time('10:00:00Z')", "Boolean:true")]
    [Arguments(120, "xs:time('10:00:00') eq xs:time('10:00:00Z')", "Boolean:false")]
    [Arguments(120, "xs:time('10:00:00') lt xs:time('10:00:00Z')", "Boolean:true")]
    [Arguments(-300, "xs:time('10:00:00') gt xs:time('10:00:00Z')", "Boolean:true")]
    public async Task AValueWithATimezoneAgainstOneWithout_UsesTheImplicitTimezoneForTheOneWithout(
        int implicitTimezone,
        string expression,
        string expected
    ) => await Assert.That(Value(expression, implicitTimezone)).IsEqualTo(expected);

    [Test]
    [Arguments(0)]
    [Arguments(120)]
    [Arguments(-300)]
    public async Task TwoValuesWithoutATimezone_AreComparedAsWrittenForEveryImplicitTimezone(int implicitTimezone)
    {
        await Assert
            .That(Value("xs:dateTime('2000-01-01T10:00:00') lt xs:dateTime('2000-01-01T10:00:01')", implicitTimezone))
            .IsEqualTo("Boolean:true");
        await Assert
            .That(Value("xs:date('2000-01-01') eq xs:date('2000-01-01')", implicitTimezone))
            .IsEqualTo("Boolean:true");
        await Assert
            .That(Value("xs:time('23:59:59') gt xs:time('00:00:00')", implicitTimezone))
            .IsEqualTo("Boolean:true");
    }

    [Test]
    [Arguments("xs:duration('P1Y') eq xs:duration('P12M')", "Boolean:true")]
    [Arguments("xs:duration('P1D') eq xs:duration('PT24H')", "Boolean:true")]
    [Arguments("xs:duration('P1M') eq xs:duration('P30D')", "Boolean:false")]
    [Arguments("xs:duration('P1M') ne xs:duration('P30D')", "Boolean:true")]
    [Arguments("xs:yearMonthDuration('P0M') eq xs:dayTimeDuration('PT0S')", "Boolean:true")]
    [Arguments("xs:yearMonthDuration('P1Y') eq xs:dayTimeDuration('PT0S')", "Boolean:false")]
    [Arguments("xs:yearMonthDuration('P1Y') eq xs:duration('P12M')", "Boolean:true")]
    [Arguments("xs:yearMonthDuration('P1Y') lt xs:yearMonthDuration('P13M')", "Boolean:true")]
    [Arguments("xs:yearMonthDuration('-P1Y') gt xs:yearMonthDuration('-P13M')", "Boolean:true")]
    [Arguments("xs:yearMonthDuration('P1Y') le xs:yearMonthDuration('P12M')", "Boolean:true")]
    [Arguments("xs:yearMonthDuration('P1Y') ge xs:yearMonthDuration('P13M')", "Boolean:false")]
    [Arguments("xs:dayTimeDuration('P1D') lt xs:dayTimeDuration('P1DT1S')", "Boolean:true")]
    [Arguments("xs:dayTimeDuration('-PT1S') lt xs:dayTimeDuration('PT0S')", "Boolean:true")]
    [Arguments("xs:dayTimeDuration('PT36H') gt xs:dayTimeDuration('P1D')", "Boolean:true")]
    [Arguments("xs:dayTimeDuration('PT1.5S') ge xs:dayTimeDuration('PT1.5S')", "Boolean:true")]
    public async Task Durations_AreEqualByTheirComponents_AndOrderedWhenTheyHaveOneKindOfComponent(
        string expression,
        string expected
    ) => await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments(0, "xs:gYear('2000Z') eq xs:gYear('2000')", "Boolean:true")]
    [Arguments(120, "xs:gYear('2000Z') eq xs:gYear('2000')", "Boolean:false")]
    [Arguments(0, "xs:gYear('2000') ne xs:gYear('2001')", "Boolean:true")]
    [Arguments(0, "xs:gYearMonth('2000-02') eq xs:gYearMonth('2000-02')", "Boolean:true")]
    [Arguments(0, "xs:gYearMonth('2000-02Z') eq xs:gYearMonth('2000-02+01:00')", "Boolean:false")]
    [Arguments(0, "xs:gMonth('--02') eq xs:gMonth('--02Z')", "Boolean:true")]
    [Arguments(0, "xs:gMonthDay('--02-29') eq xs:gMonthDay('--02-29Z')", "Boolean:true")]
    [Arguments(120, "xs:gMonthDay('--02-29') eq xs:gMonthDay('--02-29Z')", "Boolean:false")]
    [Arguments(0, "xs:gDay('---15') eq xs:gDay('---15Z')", "Boolean:true")]
    [Arguments(-300, "xs:gDay('---15') ne xs:gDay('---15Z')", "Boolean:true")]
    public async Task TheGregorianTypes_AreComparedForEqualityOnly_WithTheImplicitTimezone(
        int implicitTimezone,
        string expression,
        string expected
    ) => await Assert.That(Value(expression, implicitTimezone)).IsEqualTo(expected);

    [Test]
    [Arguments("() eq xs:date('2000-01-01')")]
    [Arguments("xs:dayTimeDuration('PT1S') lt ()")]
    public async Task AnEmptyOperand_GivesTheEmptySequence(string expression) =>
        await Assert.That(Value(expression)).IsEqualTo(string.Empty);

    [Test]
    [Arguments("xs:duration('P1D') lt xs:duration('P2D')")]
    [Arguments("xs:duration('P1D') ge xs:duration('P2D')")]
    [Arguments("xs:yearMonthDuration('P1M') lt xs:dayTimeDuration('P1D')")]
    [Arguments("xs:yearMonthDuration('P1M') gt xs:duration('P1D')")]
    [Arguments("xs:date('2000-01-01') eq xs:dateTime('2000-01-01T00:00:00')")]
    [Arguments("xs:time('10:00:00') lt xs:dateTime('2000-01-01T10:00:00')")]
    [Arguments("xs:gYear('2000') lt xs:gYear('2001')")]
    [Arguments("xs:gYear('2000') eq xs:gYearMonth('2000-01')")]
    [Arguments("xs:date('2000-01-01') eq 1")]
    [Arguments("xs:dayTimeDuration('PT1S') eq 'PT1S'")]
    public async Task APairWithoutAnOperator_RaisesXpty0004(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");

    [Test]
    public async Task AnOperandOfSeveralItems_RaisesXpty0004() =>
        await Assert
            .That(Code("(xs:date('2000-01-01'), xs:date('2000-01-02')) eq xs:date('2000-01-01')"))
            .IsEqualTo("XPTY0004");

    [Test]
    [Arguments(0, "xs:date('2000-01-01') = xs:date('2000-01-01Z')", "Boolean:true")]
    [Arguments(840, "xs:date('2000-01-01') = xs:date('2000-01-01Z')", "Boolean:false")]
    [Arguments(0, "(xs:date('2000-01-01'), xs:date('2001-01-01')) < xs:date('2000-06-01')", "Boolean:true")]
    [Arguments(0, "xs:dayTimeDuration('PT1S') != xs:dayTimeDuration('PT2S')", "Boolean:true")]
    [Arguments(0, "xs:gYear('2000') = xs:gYear('2000Z')", "Boolean:true")]
    public async Task TheGeneralComparisons_UseTheSameEntriesAndTheSameImplicitTimezone(
        int implicitTimezone,
        string expression,
        string expected
    ) => await Assert.That(Value(expression, implicitTimezone)).IsEqualTo(expected);

    [Test]
    public async Task AGeneralComparisonOfTwoDurationsWithoutAnOrder_RaisesXpty0004() =>
        await Assert.That(Code("xs:duration('P1D') < xs:duration('P2D')")).IsEqualTo("XPTY0004");
}
