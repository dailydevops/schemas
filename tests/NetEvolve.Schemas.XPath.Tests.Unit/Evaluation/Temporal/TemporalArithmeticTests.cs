namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using static TemporalFixture;

public class TemporalArithmeticTests
{
    [Test]
    [Arguments("xs:yearMonthDuration('P1Y2M') + xs:yearMonthDuration('P3M')", "YearMonthDuration:P1Y5M")]
    [Arguments("xs:dayTimeDuration('P1DT2H') + xs:dayTimeDuration('PT23H')", "DayTimeDuration:P2DT1H")]
    [Arguments("xs:yearMonthDuration('P1Y') - xs:yearMonthDuration('P18M')", "YearMonthDuration:-P6M")]
    [Arguments("xs:dayTimeDuration('PT1S') - xs:dayTimeDuration('PT2S')", "DayTimeDuration:-PT1S")]
    [Arguments("xs:dayTimeDuration('PT0.5S') + xs:dayTimeDuration('PT0.25S')", "DayTimeDuration:PT0.75S")]
    public async Task Durations_AddAndSubtract_KeepTheirType(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:dateTime('2000-01-31T10:00:00') + xs:yearMonthDuration('P1M')", "DateTime:2000-02-29T10:00:00")]
    [Arguments("xs:yearMonthDuration('P1Y') + xs:dateTime('2000-02-29T10:00:00Z')", "DateTime:2001-02-28T10:00:00Z")]
    [Arguments("xs:dateTime('2000-03-31T10:00:00') - xs:yearMonthDuration('P1M')", "DateTime:2000-02-29T10:00:00")]
    [Arguments("xs:dateTime('2000-12-31T23:59:59Z') + xs:dayTimeDuration('PT1S')", "DateTime:2001-01-01T00:00:00Z")]
    [Arguments("xs:dayTimeDuration('PT1S') + xs:dateTime('2000-12-31T23:59:59')", "DateTime:2001-01-01T00:00:00")]
    [Arguments(
        "xs:dateTime('2000-01-01T00:00:00+05:00') - xs:dayTimeDuration('PT1H')",
        "DateTime:1999-12-31T23:00:00+05:00"
    )]
    public async Task DateTime_PlusOrMinusADuration_KeepsTheTimezoneOrItsAbsence(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:date('2000-01-31') + xs:yearMonthDuration('P1M')", "Date:2000-02-29")]
    [Arguments("xs:yearMonthDuration('P1M') + xs:date('2000-01-31Z')", "Date:2000-02-29Z")]
    [Arguments("xs:date('2000-12-31') + xs:dayTimeDuration('PT23H')", "Date:2000-12-31")]
    [Arguments("xs:date('2000-12-31') + xs:dayTimeDuration('PT24H')", "Date:2001-01-01")]
    [Arguments("xs:dayTimeDuration('P1D') + xs:date('2000-12-31')", "Date:2001-01-01")]
    [Arguments("xs:date('2000-01-01Z') - xs:dayTimeDuration('PT1H')", "Date:1999-12-31Z")]
    [Arguments("xs:date('2000-03-31') - xs:yearMonthDuration('P1M')", "Date:2000-02-29")]
    public async Task Date_PlusOrMinusADuration_IsTheDatePartOfTheDateTimeResult(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:time('23:00:00') + xs:dayTimeDuration('PT2H')", "Time:01:00:00")]
    [Arguments("xs:time('01:00:00Z') - xs:dayTimeDuration('P1DT2H')", "Time:23:00:00Z")]
    [Arguments("xs:dayTimeDuration('PT30M') + xs:time('12:00:00+02:00')", "Time:12:30:00+02:00")]
    [Arguments("xs:time('12:00:00.5') + xs:dayTimeDuration('PT0.75S')", "Time:12:00:01.25")]
    public async Task Time_PlusOrMinusADayTimeDuration_WrapsAroundTheDay(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:dateTime('2000-01-02T00:00:00Z') - xs:dateTime('2000-01-01T00:00:00Z')", "DayTimeDuration:P1D")]
    [Arguments(
        "xs:dateTime('2000-01-01T12:00:00+02:00') - xs:dateTime('2000-01-01T10:00:00Z')",
        "DayTimeDuration:PT0S"
    )]
    [Arguments("xs:dateTime('2000-01-01T00:00:01.5Z') - xs:dateTime('2000-01-01T00:00:00Z')", "DayTimeDuration:PT1.5S")]
    [Arguments("xs:date('2000-03-01Z') - xs:date('2000-02-01Z')", "DayTimeDuration:P29D")]
    [Arguments("xs:date('1999-12-31Z') - xs:date('2000-01-01Z')", "DayTimeDuration:-P1D")]
    [Arguments("xs:time('12:00:00Z') - xs:time('13:30:00Z')", "DayTimeDuration:-PT1H30M")]
    [Arguments("xs:time('12:00:00+02:00') - xs:time('12:00:00Z')", "DayTimeDuration:-PT2H")]
    public async Task TheDifferenceOfTwoMoments_IsADayTimeDuration(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments(0, "DayTimeDuration:PT0S")]
    [Arguments(120, "DayTimeDuration:PT2H")]
    [Arguments(-300, "DayTimeDuration:-PT5H")]
    public async Task TheDifferenceOfAZonedAndAZonelessValue_UsesTheImplicitTimezoneForTheZonelessOne(
        int implicitTimezone,
        string expected
    ) =>
        await Assert
            .That(Value("xs:dateTime('2000-01-01T12:00:00Z') - xs:dateTime('2000-01-01T12:00:00')", implicitTimezone))
            .IsEqualTo(expected);

    [Test]
    [Arguments("xs:dayTimeDuration('PT1H') * 2.5", "DayTimeDuration:PT2H30M")]
    [Arguments("2 * xs:dayTimeDuration('PT1H')", "DayTimeDuration:PT2H")]
    [Arguments("xs:dayTimeDuration('PT2H10M') * 2.1", "DayTimeDuration:PT4H33M")]
    [Arguments("xs:dayTimeDuration('PT1S') * xs:double('0.1')", "DayTimeDuration:PT0.1S")]
    [Arguments("xs:dayTimeDuration('PT1S') * 0", "DayTimeDuration:PT0S")]
    [Arguments("xs:dayTimeDuration('PT1S') * -1", "DayTimeDuration:-PT1S")]
    [Arguments("2 * xs:yearMonthDuration('P1Y')", "YearMonthDuration:P2Y")]
    [Arguments("xs:yearMonthDuration('P1Y') * 1.5", "YearMonthDuration:P1Y6M")]
    [Arguments("xs:yearMonthDuration('P3M') * 0.5", "YearMonthDuration:P2M")]
    [Arguments("xs:yearMonthDuration('P3M') * -0.5", "YearMonthDuration:-P1M")]
    [Arguments("xs:yearMonthDuration('P1M') * xs:float('2')", "YearMonthDuration:P2M")]
    public async Task ADurationTimesANumber_IsRoundedToTheTypeOfTheDuration(string expression, string expected) =>
        await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:yearMonthDuration('P1Y') div 4", "YearMonthDuration:P3M")]
    [Arguments("xs:yearMonthDuration('P1M') div 2", "YearMonthDuration:P1M")]
    [Arguments("xs:yearMonthDuration('-P1M') div 2", "YearMonthDuration:P0M")]
    [Arguments("xs:dayTimeDuration('PT1H') div 4", "DayTimeDuration:PT15M")]
    [Arguments("xs:dayTimeDuration('PT1S') div 3", "DayTimeDuration:PT0.333333333333333333S")]
    [Arguments("xs:dayTimeDuration('PT1S') div xs:double('INF')", "DayTimeDuration:PT0S")]
    [Arguments("xs:yearMonthDuration('P1Y') div xs:yearMonthDuration('P3M')", "XsDecimal:4")]
    [Arguments("xs:dayTimeDuration('P1D') div xs:dayTimeDuration('PT12H')", "XsDecimal:2")]
    [Arguments("xs:dayTimeDuration('PT1S') div xs:dayTimeDuration('PT4S')", "XsDecimal:0.25")]
    [Arguments("xs:yearMonthDuration('-P1Y') div xs:yearMonthDuration('P4M')", "XsDecimal:-3")]
    public async Task ADurationDividedByANumberOrADuration_HasTheResultTypeOfItsOperator(
        string expression,
        string expected
    ) => await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:date('0001-01-01Z') - xs:dayTimeDuration('P1D')", "Date:-0001-12-31Z")]
    [Arguments("xs:date('-0001-12-31Z') + xs:dayTimeDuration('P1D')", "Date:0001-01-01Z")]
    [Arguments("xs:date('0001-01-01Z') - xs:date('-0001-01-01Z')", "DayTimeDuration:P366D")]
    [Arguments("xs:dateTime('0001-01-01T00:00:00') - xs:yearMonthDuration('P1Y')", "DateTime:-0001-01-01T00:00:00")]
    public async Task AValueAcrossTheYearOne_FollowsTheYearNumberingOfXmlSchemaOnePointZero(
        string expression,
        string expected
    ) => await Assert.That(Value(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:date('2000-01-01') + ()")]
    [Arguments("() + xs:dayTimeDuration('PT1S')")]
    [Arguments("() * xs:dayTimeDuration('PT1S')")]
    [Arguments("xs:yearMonthDuration('P1Y') div ()")]
    public async Task AnEmptyOperand_GivesTheEmptySequence(string expression) =>
        await Assert.That(Value(expression)).IsEqualTo(string.Empty);

    [Test]
    public async Task AnUntypedAtomicOperand_IsCastToDouble_SoItIsNotADuration() =>
        await Assert.That(Code("xs:dayTimeDuration('PT1S') + xs:untypedAtomic('1')")).IsEqualTo("XPTY0004");
}
