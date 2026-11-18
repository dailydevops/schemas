namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using static CastShow;

/// <summary>The dynamic errors of the date, time, Gregorian, and duration casts, and the guarantee that <c>castable as</c> returns false for each.</summary>
public class TemporalCastErrorTests
{
    // FORG0001: the lexical form is not in the lexical space of the target (XML Schema 1.0, which XPath 2.0 refers to).
    private static (string, string)[] InvalidLexicalForms() =>
        [
            ("'2004-13-01' cast as xs:date", "FORG0001"),
            ("'2004-02-30' cast as xs:date", "FORG0001"),
            ("'2003-02-29' cast as xs:date", "FORG0001"),
            ("'2004-10-13' cast as xs:dateTime", "FORG0001"),
            ("'2004-10-13T12:30' cast as xs:dateTime", "FORG0001"),
            ("'2004-10-13T25:00:00' cast as xs:dateTime", "FORG0001"),
            ("'2004-10-13T12:30:00+15:00' cast as xs:dateTime", "FORG0001"),
            ("'2004-10-13T12:30:00+14:01' cast as xs:dateTime", "FORG0001"),
            ("'0000-01-01' cast as xs:date", "FORG0001"),
            ("'12:30' cast as xs:time", "FORG0001"),
            ("'25:00:00' cast as xs:time", "FORG0001"),
            ("'12:60:00' cast as xs:time", "FORG0001"),
            ("'12:30:60' cast as xs:time", "FORG0001"),
            ("'' cast as xs:date", "FORG0001"),
            ("'04' cast as xs:gYear", "FORG0001"),
            ("'2004-13' cast as xs:gYearMonth", "FORG0001"),
            ("'--13' cast as xs:gMonth", "FORG0001"),
            ("'--02-30' cast as xs:gMonthDay", "FORG0001"),
            ("'---32' cast as xs:gDay", "FORG0001"),
            ("'---00' cast as xs:gDay", "FORG0001"),
            ("'P' cast as xs:duration", "FORG0001"),
            ("'PT' cast as xs:duration", "FORG0001"),
            ("'P1YT' cast as xs:duration", "FORG0001"),
            ("'1Y' cast as xs:duration", "FORG0001"),
            ("'P-1Y' cast as xs:duration", "FORG0001"),
            ("'P1D' cast as xs:yearMonthDuration", "FORG0001"),
            ("'PT1S' cast as xs:yearMonthDuration", "FORG0001"),
            ("'P1Y' cast as xs:dayTimeDuration", "FORG0001"),
            ("'P1M' cast as xs:dayTimeDuration", "FORG0001"),
            ("xs:untypedAtomic('x') cast as xs:dateTime", "FORG0001"),
            ("xs:untypedAtomic('x') cast as xs:duration", "FORG0001"),
            ("xs:untypedAtomic('') cast as xs:gYear", "FORG0001"),
        ];

    // The form is valid and only the implementation cannot hold it: a year or a duration component beyond 17 digits.
    private static (string, string)[] ImplementationLimits() =>
        [
            ("'123456789012345678-01-01' cast as xs:date", "FODT0001"),
            ("'123456789012345678-01-01T00:00:00' cast as xs:dateTime", "FODT0001"),
            ("'123456789012345678' cast as xs:gYear", "FODT0001"),
            ("'P123456789012345678Y' cast as xs:duration", "FODT0002"),
            ("'P123456789012345678D' cast as xs:dayTimeDuration", "FODT0002"),
            ("'P123456789012345678M' cast as xs:yearMonthDuration", "FODT0002"),
        ];

    // XPTY0004: a pair of types without a row, and the wrong cardinality.
    private static (string, string)[] NotAllowedCases() =>
        [
            ("xs:integer('1') cast as xs:date", "XPTY0004"),
            ("xs:double('1') cast as xs:dateTime", "XPTY0004"),
            ("xs:decimal('1') cast as xs:time", "XPTY0004"),
            ("xs:float('1') cast as xs:duration", "XPTY0004"),
            ("xs:boolean('true') cast as xs:gYear", "XPTY0004"),
            ("xs:date('2004-10-13') cast as xs:time", "XPTY0004"),
            ("xs:time('12:30:00') cast as xs:date", "XPTY0004"),
            ("xs:time('12:30:00') cast as xs:dateTime", "XPTY0004"),
            ("xs:gYear('2004') cast as xs:date", "XPTY0004"),
            ("xs:gMonth('--10') cast as xs:gDay", "XPTY0004"),
            ("xs:dateTime('2004-10-13T12:30:00') cast as xs:duration", "XPTY0004"),
            ("xs:duration('P1Y') cast as xs:dateTime", "XPTY0004"),
            ("xs:duration('P1Y') cast as xs:integer", "XPTY0004"),
            ("xs:dateTime('2004-10-13T12:30:00') cast as xs:double", "XPTY0004"),
            ("xs:date('2004-10-13') cast as xs:boolean", "XPTY0004"),
            ("() cast as xs:date", "XPTY0004"),
            ("('2004-10-13', '2004-10-14') cast as xs:date", "XPTY0004"),
        ];

    public static IEnumerable<Func<(string Expression, string Code)>> Failures()
    {
        foreach (var group in (Func<(string, string)[]>[])[InvalidLexicalForms, ImplementationLimits, NotAllowedCases])
        {
            foreach (var entry in group())
            {
                var captured = entry;
                yield return () => captured;
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Failures))]
    public async Task Cast_Failure_RaisesItsCode(string expression, string code) =>
        await Assert.That(CastFixture.Code(expression)).IsEqualTo(code);

    // castable as never throws: it is false for every failing cast with a single operand (the empty sequence and a sequence are exercised by the cardinality rule).
    [Test]
    [MethodDataSource(nameof(Failures))]
    public async Task Castable_EveryFailingCast_ReturnsFalseAndRaisesNothing(string expression, string code)
    {
        _ = code;
        var castable = CastFixture.AsCastable(expression);

        await Assert.That(CastFixture.Code(castable)).IsEqualTo(string.Empty);
        await Assert.That(KindOf(castable)).IsEqualTo(NetEvolve.Schemas.XPath.Xdm.Values.XdmItemKind.Boolean);
        var result = CastFixture.Evaluate(castable);
        _ = result[0].TryGetBoolean(out var value);
        await Assert.That(value).IsFalse();
    }

    [Test]
    public async Task OptionalMarker_MapsTheEmptySequenceToItself()
    {
        await Assert.That(CastFixture.Evaluate("() cast as xs:date?").Count).IsEqualTo(0);
        await Assert.That(Castable("()", "date?")).IsTrue();
    }

    [Test]
    [Arguments("xs:date(())")]
    [Arguments("xs:dateTime(())")]
    [Arguments("xs:duration(())")]
    [Arguments("xs:gYear(())")]
    public async Task Constructor_OfTheEmptySequence_IsTheEmptySequence(string expression) =>
        await Assert.That(CastFixture.Evaluate(expression).Count).IsEqualTo(0);

    [Test]
    [Arguments("xs:date('2004-13-01')", "FORG0001")]
    [Arguments("xs:time('25:00:00')", "FORG0001")]
    [Arguments("xs:yearMonthDuration('P1D')", "FORG0001")]
    [Arguments("xs:date(1)", "XPTY0004")]
    [Arguments("xs:date(('2004-10-13', '2004-10-14'))", "XPTY0004")]
    [Arguments("xs:date()", "XPST0017")]
    [Arguments("xs:date('2004-10-13', 'x')", "XPST0017")]
    public async Task Constructor_Error_RaisesItsCode(string expression, string code) =>
        await Assert.That(CastFixture.Code(expression)).IsEqualTo(code);

    [Test]
    [Arguments("xs:dateTime('2004-10-13T12:30:00Z')", "DateTime")]
    [Arguments("xs:date('2004-10-13Z')", "Date")]
    [Arguments("xs:time('12:30:00Z')", "Time")]
    [Arguments("xs:gYearMonth('2004-10')", "GYearMonth")]
    [Arguments("xs:gYear('2004')", "GYear")]
    [Arguments("xs:gMonthDay('--10-13')", "GMonthDay")]
    [Arguments("xs:gDay('---13')", "GDay")]
    [Arguments("xs:gMonth('--10')", "GMonth")]
    [Arguments("xs:duration('P1Y')", "Duration")]
    [Arguments("xs:yearMonthDuration('P1Y')", "YearMonthDuration")]
    [Arguments("xs:dayTimeDuration('P1D')", "DayTimeDuration")]
    public async Task Constructor_ProducesAnItemOfItsType(string expression, string kind) =>
        await Assert.That(KindOf(expression).ToString()).IsEqualTo(kind);
}
