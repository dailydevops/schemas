namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using System;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static TemporalFixture;

public class TemporalAggregateTests
{
    [Test]
    [Arguments("fn:max((xs:date('2000-01-02'), xs:date('2000-01-01'), xs:date('1999-12-31')))", "Date:2000-01-02")]
    [Arguments("fn:min((xs:date('2000-01-02'), xs:date('2000-01-01'), xs:date('1999-12-31')))", "Date:1999-12-31")]
    [Arguments(
        "fn:max((xs:dateTime('2000-01-01T10:00:00Z'), xs:dateTime('2000-01-01T10:00:00-05:00')))",
        "DateTime:2000-01-01T10:00:00-05:00"
    )]
    [Arguments(
        "fn:min((xs:dateTime('2000-01-01T10:00:00Z'), xs:dateTime('2000-01-01T10:00:00-05:00')))",
        "DateTime:2000-01-01T10:00:00Z"
    )]
    [Arguments("fn:max((xs:time('08:00:00'), xs:time('20:30:00'), xs:time('12:00:00')))", "Time:20:30:00")]
    [Arguments("fn:min((xs:time('08:00:00'), xs:time('20:30:00'), xs:time('12:00:00')))", "Time:08:00:00")]
    [Arguments(
        "fn:max((xs:yearMonthDuration('P1Y'), xs:yearMonthDuration('P13M'), xs:yearMonthDuration('P2M')))",
        "YearMonthDuration:P1Y1M"
    )]
    [Arguments(
        "fn:min((xs:dayTimeDuration('P1D'), xs:dayTimeDuration('PT1H'), xs:dayTimeDuration('PT90M')))",
        "DayTimeDuration:PT1H"
    )]
    [Arguments("fn:max(xs:dayTimeDuration('P1D'))", "DayTimeDuration:P1D")]
    public async Task MinAndMax_OrderTheTemporalTypesThroughTheComparisonEntries(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments(
        0,
        "fn:max((xs:dateTime('2000-01-01T10:00:00'), xs:dateTime('2000-01-01T10:00:00Z')))",
        "DateTime:2000-01-01T10:00:00"
    )]
    [Arguments(
        120,
        "fn:max((xs:dateTime('2000-01-01T10:00:00'), xs:dateTime('2000-01-01T10:00:00Z')))",
        "DateTime:2000-01-01T10:00:00Z"
    )]
    [Arguments(
        -300,
        "fn:max((xs:dateTime('2000-01-01T10:00:00'), xs:dateTime('2000-01-01T10:00:00Z')))",
        "DateTime:2000-01-01T10:00:00"
    )]
    [Arguments(
        120,
        "fn:min((xs:dateTime('2000-01-01T10:00:00'), xs:dateTime('2000-01-01T10:00:00Z')))",
        "DateTime:2000-01-01T10:00:00"
    )]
    [Arguments(
        -300,
        "fn:min((xs:dateTime('2000-01-01T10:00:00'), xs:dateTime('2000-01-01T10:00:00Z')))",
        "DateTime:2000-01-01T10:00:00Z"
    )]
    public async Task MinAndMax_UseTheImplicitTimezoneOfTheDynamicContext(
        int implicitTimezone,
        string expression,
        string expected
    ) =>
        // Equal values: the first of them is the result; otherwise the one that is later (max) or earlier (min) on the timeline.
        await Assert.That(Value(expression, implicitTimezone)).IsEqualTo(expected);

    [Test]
    [Arguments("fn:sum((xs:dayTimeDuration('P1D'), xs:dayTimeDuration('PT1H')))", "DayTimeDuration:P1DT1H")]
    [Arguments("fn:sum((xs:yearMonthDuration('P1Y'), xs:yearMonthDuration('P3M')))", "YearMonthDuration:P1Y3M")]
    [Arguments("fn:sum(xs:dayTimeDuration('PT5S'))", "DayTimeDuration:PT5S")]
    [Arguments("fn:sum((), xs:dayTimeDuration('PT0S'))", "DayTimeDuration:PT0S")]
    [Arguments("fn:avg((xs:dayTimeDuration('PT1H'), xs:dayTimeDuration('PT3H')))", "DayTimeDuration:PT2H")]
    [Arguments("fn:avg((xs:yearMonthDuration('P1Y'), xs:yearMonthDuration('P3Y')))", "YearMonthDuration:P2Y")]
    [Arguments("fn:avg(xs:yearMonthDuration('P1M'))", "YearMonthDuration:P1M")]
    public async Task SumAndAvg_AddAndDivideTheDurationsThroughTheArithmeticEntries(
        string expression,
        string expected
    ) => await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("fn:min((xs:yearMonthDuration('P1Y'), xs:dayTimeDuration('P1D')))")]
    [Arguments("fn:max((xs:duration('P1Y'), xs:duration('P2Y')))")]
    [Arguments("fn:min((xs:date('2000-01-01'), xs:dateTime('2000-01-01T00:00:00')))")]
    [Arguments("fn:max((xs:date('2000-01-01'), 1))")]
    [Arguments("fn:max((xs:gYear('2000'), xs:gYear('2001')))")]
    [Arguments("fn:sum((xs:date('2000-01-01'), xs:date('2000-01-02')))")]
    [Arguments("fn:sum((xs:dayTimeDuration('P1D'), xs:yearMonthDuration('P1M')))")]
    [Arguments("fn:sum((xs:dayTimeDuration('P1D'), 1))")]
    [Arguments("fn:sum(xs:duration('P1D'))")]
    [Arguments("fn:avg((xs:dateTime('2000-01-01T00:00:00'), xs:dateTime('2000-01-02T00:00:00')))")]
    [Arguments("fn:avg((xs:duration('P1D'), xs:duration('P3D')))")]
    public async Task AValueThatCannotBeOrderedOrAddedTogether_RaisesForg0006(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FORG0006");

    [Test]
    public async Task Sum_OfNumbers_WithoutAContext_RaisesArgumentNullException()
    {
        var numbers = Run("(1, 2)").Result;

        await Assert.That(() => AggregateInput.Sum(numbers, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Sum_OfDurations_WithoutAContext_RaisesArgumentNullException()
    {
        var durations = Run("(xs:dayTimeDuration('PT1H'), xs:dayTimeDuration('PT2H'))").Result;

        await Assert.That(() => AggregateInput.Sum(durations, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DivideDuration_WithoutAContext_RaisesArgumentNullException()
    {
        var total = Run("xs:dayTimeDuration('PT3H')").Result[0];

        await Assert.That(() => AggregateInput.DivideDuration(total, 3, null!)).Throws<ArgumentNullException>();
    }
}
