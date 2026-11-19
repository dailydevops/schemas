namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using static TemporalFixture;

public class TemporalErrorTests
{
    [Test]
    [Arguments("xs:date('99999999999999999-12-31') + xs:dayTimeDuration('P1D')")]
    [Arguments("xs:date('-99999999999999999-01-01') - xs:dayTimeDuration('P1D')")]
    [Arguments("xs:dateTime('99999999999999999-12-31T23:59:59') + xs:dayTimeDuration('PT1S')")]
    [Arguments("xs:dateTime('99999999999999999-12-31T00:00:00') + xs:yearMonthDuration('P1M')")]
    [Arguments("xs:dateTime('-99999999999999999-01-01T00:00:00') - xs:yearMonthDuration('P1M')")]
    [Arguments("xs:date('99999999999999999-06-15') + xs:yearMonthDuration('P1Y')")]
    [Arguments("xs:dateTime('99999999999999999-01-01T00:00:00Z') - xs:dateTime('-99999999999999999-01-01T00:00:00Z')")]
    [Arguments("xs:date('99999999999999999-01-01Z') - xs:date('-99999999999999999-01-01Z')")]
    public async Task ADateOrTimeResultBeyondTheYearRange_RaisesFodt0001(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FODT0001");

    [Test]
    [Arguments("xs:yearMonthDuration('P99999999999999999Y') + xs:yearMonthDuration('P99999999999999999Y')")]
    [Arguments("xs:yearMonthDuration('-P99999999999999999Y') - xs:yearMonthDuration('P99999999999999999Y')")]
    [Arguments("xs:dayTimeDuration('P99999999999999999D') + xs:dayTimeDuration('P99999999999999999D')")]
    [Arguments("xs:dayTimeDuration('-P99999999999999999D') - xs:dayTimeDuration('P99999999999999999D')")]
    [Arguments("xs:yearMonthDuration('P99999999999999999Y') * 8")]
    [Arguments("xs:yearMonthDuration('P1Y') * 1e300")]
    [Arguments("xs:dayTimeDuration('P99999999999999999D') * 2")]
    [Arguments("xs:dayTimeDuration('PT1S') * xs:double('INF')")]
    [Arguments("xs:dayTimeDuration('PT1S') * xs:double('-INF')")]
    [Arguments("xs:yearMonthDuration('P1Y') * xs:double('INF')")]
    [Arguments("xs:yearMonthDuration('P1M') div 0")]
    [Arguments("xs:dayTimeDuration('PT1S') div 0")]
    [Arguments("xs:dayTimeDuration('PT1S') div xs:double('-0')")]
    [Arguments("xs:dayTimeDuration('PT0S') div 0")]
    public async Task ADurationResultBeyondTheRange_RaisesFodt0002(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FODT0002");

    [Test]
    [Arguments("xs:dayTimeDuration('PT1S') * xs:double('NaN')")]
    [Arguments("xs:double('NaN') * xs:dayTimeDuration('PT1S')")]
    [Arguments("xs:yearMonthDuration('P1Y') * xs:double('NaN')")]
    [Arguments("xs:double('NaN') * xs:yearMonthDuration('P1Y')")]
    [Arguments("xs:dayTimeDuration('PT0S') * xs:double('NaN')")]
    [Arguments("xs:yearMonthDuration('P1Y') div xs:double('NaN')")]
    [Arguments("xs:dayTimeDuration('PT1S') div xs:double('NaN')")]
    [Arguments("xs:dayTimeDuration('PT1S') * xs:float('NaN')")]
    public async Task ADurationAndNaN_RaisesFoca0005(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FOCA0005");

    [Test]
    [Arguments("xs:yearMonthDuration('P1Y') div xs:yearMonthDuration('P0M')")]
    [Arguments("xs:dayTimeDuration('PT1S') div xs:dayTimeDuration('PT0S')")]
    public async Task ADurationDividedByAZeroDuration_RaisesFoar0001(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FOAR0001");

    [Test]
    [Arguments("xs:date('2000-01-01') + xs:date('2000-01-01')")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00') + xs:dateTime('2000-01-01T00:00:00')")]
    [Arguments("xs:time('10:00:00') + xs:time('10:00:00')")]
    [Arguments("xs:date('2000-01-01') + 1")]
    [Arguments("xs:duration('P1D') + xs:duration('P1D')")]
    [Arguments("xs:duration('P1D') + xs:dayTimeDuration('P1D')")]
    [Arguments("xs:duration('P1D') * 2")]
    [Arguments("xs:duration('P1D') div 2")]
    [Arguments("xs:duration('P1D') div xs:duration('P1D')")]
    [Arguments("xs:yearMonthDuration('P1M') + xs:dayTimeDuration('P1D')")]
    [Arguments("xs:yearMonthDuration('P1M') - xs:dayTimeDuration('P1D')")]
    [Arguments("xs:yearMonthDuration('P1M') div xs:dayTimeDuration('P1D')")]
    [Arguments("xs:time('10:00:00') + xs:yearMonthDuration('P1M')")]
    [Arguments("xs:time('10:00:00') - xs:yearMonthDuration('P1M')")]
    [Arguments("xs:yearMonthDuration('P1M') - xs:date('2000-01-01')")]
    [Arguments("xs:dayTimeDuration('P1D') - xs:dateTime('2000-01-01T00:00:00')")]
    [Arguments("xs:date('2000-01-01') - xs:dateTime('2000-01-01T00:00:00')")]
    [Arguments("xs:date('2000-01-01') - xs:time('10:00:00')")]
    [Arguments("xs:dayTimeDuration('P1D') * xs:dayTimeDuration('P1D')")]
    [Arguments("xs:date('2000-01-01') * 2")]
    [Arguments("xs:dayTimeDuration('P1D') * 'a'")]
    [Arguments("xs:dayTimeDuration('P1D') idiv 2")]
    [Arguments("xs:dayTimeDuration('P1D') mod 2")]
    [Arguments("xs:dayTimeDuration('P1D') idiv xs:dayTimeDuration('P1D')")]
    [Arguments("-xs:dayTimeDuration('P1D')")]
    [Arguments("+xs:date('2000-01-01')")]
    [Arguments("xs:gYear('2000') + xs:yearMonthDuration('P1Y')")]
    [Arguments("xs:dayTimeDuration('PT1S') + xs:untypedAtomic('1')")]
    public async Task APairOfTypesWithoutAnOperator_RaisesXpty0004(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");

    [Test]
    public async Task AnOperandOfSeveralItems_RaisesXpty0004() =>
        await Assert
            .That(Code("xs:date('2000-01-01') + (xs:dayTimeDuration('P1D'), xs:dayTimeDuration('P2D'))"))
            .IsEqualTo("XPTY0004");

    [Test]
    public async Task AnEmptyOperand_WinsOverASequenceOfSeveralItemsOnTheOtherSide() =>
        await Assert.That(Value("() + (xs:dayTimeDuration('P1D'), xs:dayTimeDuration('P2D'))")).IsEqualTo(string.Empty);

    [Test]
    public async Task ADurationAtTheEdgeOfTheRange_IsStillAValue()
    {
        await Assert
            .That(Value("xs:yearMonthDuration('P99999999999999999Y') + xs:yearMonthDuration('P11M')"))
            .IsEqualTo("YearMonthDuration:P99999999999999999Y11M");
        await Assert
            .That(Value("xs:dayTimeDuration('P99999999999999999D') + xs:dayTimeDuration('PT23H59M59S')"))
            .IsEqualTo("DayTimeDuration:P99999999999999999DT23H59M59S");
    }

    [Test]
    public async Task ADateAtTheEdgeOfTheRange_IsStillAValue() =>
        await Assert
            .That(Value("xs:date('99999999999999999-12-30') + xs:dayTimeDuration('P1D')"))
            .IsEqualTo("Date:99999999999999999-12-31");

    [Test]
    public async Task ATimeNeverLeavesTheRange_BecauseItWrapsAroundTheDay() =>
        await Assert
            .That(Value("xs:time('12:00:00') + xs:dayTimeDuration('P99999999999999999D')"))
            .IsEqualTo("Time:12:00:00");
}
