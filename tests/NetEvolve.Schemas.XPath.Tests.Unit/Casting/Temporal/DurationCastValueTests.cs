namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;

using static CastShow;

/// <summary>The value of every allowed cast among the three duration types: the components of the other subtype are dropped (Functions and Operators, 17.1.2).</summary>
public class DurationCastValueTests
{
    // duration to both subtypes: the year-month part or the day-time part, with the sign kept.
    [Test]
    [Arguments("xs:duration('P1Y2M3DT10H30M') cast as xs:yearMonthDuration", "YearMonthDuration:P1Y2M")]
    [Arguments("xs:duration('P1Y2M3DT10H30M') cast as xs:dayTimeDuration", "DayTimeDuration:P3DT10H30M")]
    [Arguments("xs:duration('-P1Y2M3DT10H30M') cast as xs:yearMonthDuration", "YearMonthDuration:-P1Y2M")]
    [Arguments("xs:duration('-P1Y2M3DT10H30M') cast as xs:dayTimeDuration", "DayTimeDuration:-P3DT10H30M")]
    [Arguments("xs:duration('P1Y2M3DT10H30M') cast as xs:duration", "Duration:P1Y2M3DT10H30M")]
    [Arguments("xs:duration('P14M') cast as xs:yearMonthDuration", "YearMonthDuration:P1Y2M")]
    [Arguments("xs:duration('PT36H') cast as xs:dayTimeDuration", "DayTimeDuration:P1DT12H")]
    [Arguments("xs:duration('P1Y') cast as xs:dayTimeDuration", "DayTimeDuration:PT0S")]
    [Arguments("xs:duration('-P1Y') cast as xs:dayTimeDuration", "DayTimeDuration:PT0S")]
    [Arguments("xs:duration('P1D') cast as xs:yearMonthDuration", "YearMonthDuration:P0M")]
    [Arguments("xs:duration('-PT1S') cast as xs:yearMonthDuration", "YearMonthDuration:P0M")]
    [Arguments("xs:duration('PT1.50S') cast as xs:dayTimeDuration", "DayTimeDuration:PT1.5S")]
    public async Task FromDuration_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    // A subtype to the other subtype is the zero of the target; a subtype to duration keeps the value.
    [Test]
    [Arguments("xs:yearMonthDuration('P1Y2M') cast as xs:dayTimeDuration", "DayTimeDuration:PT0S")]
    [Arguments("xs:dayTimeDuration('P3DT10H') cast as xs:yearMonthDuration", "YearMonthDuration:P0M")]
    [Arguments("xs:yearMonthDuration('P1Y2M') cast as xs:duration", "Duration:P1Y2M")]
    [Arguments("xs:yearMonthDuration('-P1Y2M') cast as xs:duration", "Duration:-P1Y2M")]
    [Arguments("xs:dayTimeDuration('P3DT10H') cast as xs:duration", "Duration:P3DT10H")]
    [Arguments("xs:dayTimeDuration('P3DT10H') cast as xs:dayTimeDuration", "DayTimeDuration:P3DT10H")]
    [Arguments("xs:yearMonthDuration('P1Y2M') cast as xs:yearMonthDuration", "YearMonthDuration:P1Y2M")]
    public async Task FromSubtype_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("' P1Y2M3D ' cast as xs:duration", "Duration:P1Y2M3D")]
    [Arguments("'P1Y2M' cast as xs:yearMonthDuration", "YearMonthDuration:P1Y2M")]
    [Arguments("'P3DT10H' cast as xs:dayTimeDuration", "DayTimeDuration:P3DT10H")]
    [Arguments("'-P0Y' cast as xs:duration", "Duration:PT0S")]
    [Arguments("xs:untypedAtomic('PT0S') cast as xs:dayTimeDuration", "DayTimeDuration:PT0S")]
    [Arguments("xs:untypedAtomic('P0M') cast as xs:yearMonthDuration", "YearMonthDuration:P0M")]
    public async Task FromStringTypes_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    // The canonical lexical form: days instead of hours beyond 24, years instead of months beyond 12, the zero of each type.
    [Test]
    [Arguments("xs:duration('P1Y14M') cast as xs:string", "XsString:P2Y2M")]
    [Arguments("xs:duration('PT36H') cast as xs:string", "XsString:P1DT12H")]
    [Arguments("xs:duration('PT0S') cast as xs:string", "XsString:PT0S")]
    [Arguments("xs:duration('P0Y') cast as xs:untypedAtomic", "UntypedAtomic:PT0S")]
    [Arguments("xs:yearMonthDuration('P0M') cast as xs:string", "XsString:P0M")]
    [Arguments("xs:dayTimeDuration('PT0S') cast as xs:string", "XsString:PT0S")]
    [Arguments("xs:dayTimeDuration('PT90M') cast as xs:string", "XsString:PT1H30M")]
    [Arguments("xs:dayTimeDuration('-PT0.5S') cast as xs:untypedAtomic", "UntypedAtomic:-PT0.5S")]
    public async Task ToStringTypes_GivesTheCanonicalForm(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);
}
