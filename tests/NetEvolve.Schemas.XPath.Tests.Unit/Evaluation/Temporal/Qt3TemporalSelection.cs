namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

/// <summary>
/// The selection of the QT3 test sets for the operators of dates, times, and durations: <c>op-add-*</c>, <c>op-subtract-*</c>,
/// <c>op-multiply-dayTimeDuration</c>, <c>op-multiply-yearMonthDuration</c>, <c>op-divide-*</c> of the durations, the equality, less-than,
/// and greater-than sets of <c>xs:dateTime</c>, <c>xs:date</c>, <c>xs:time</c>, <c>xs:yearMonthDuration</c>, and <c>xs:dayTimeDuration</c>,
/// <c>op-duration-equal</c>, and the five Gregorian equality sets. The cases are taken from the pinned commit of the suite
/// (tests/conformance/suites/qt3.json); the expectation is a tab-separated list of alternatives: <c>T</c> or <c>F</c> for a boolean, <c>Z</c>
/// for the empty sequence, <c>S:</c> and <c>N:</c> for a string value (<c>N:</c> with normalized space), <c>E:</c> for a value equal to an
/// expression, and <c>X:</c> for an error code.
/// </summary>
/// <remarks>
/// <para>
/// Limitation: the QT3 runner of the integration tests still has no evaluator behind it (#295), so these cases run through the lowerings of
/// <see cref="TemporalFixture"/> and not through the runner; the end-to-end run stays open until #295, as for the casting rows (#265, #301).
/// <see cref="NotXPath20"/> lists the cases that depend on XQuery 1.0 or 3.0 and are never selected for XPath 2.0 (the dependency rule of the QT3
/// selector of the integration tests). <see cref="OutOfShape"/> lists every other case of the sets with the reason: the assertion is an
/// XPath expression over <c>$result</c>, or the expected value needs the comparison of durations by value in <c>fn:distinct-values</c> (#732). The
/// functions of the boolean, numeric, and sequence groups (<c>fn:not</c>, <c>fn:boolean</c>, <c>fn:number</c>, <c>fn:true</c>, <c>fn:false</c>,
/// <c>fn:round-half-to-even</c>, and the like) have landed, so the cases that use them run, with the expression of the suite unchanged. The cases that read
/// <c>fn:implicit-timezone</c> in a conditional run with the implicit timezone of the default context, which is UTC, so they take the branch of
/// that timezone; the hand-written tests cover the other implicit timezones. The lists are taken mechanically from the pinned suite.
/// </para>
/// </remarks>
internal static class Qt3TemporalSelection
{
    public const int Total = 1783;

    public static readonly (string Id, string Expression, string Expectation)[] Cases =
    [
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date2args-1",
            "xs:date(\"1970-01-01Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1970-01-01Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date2args-2",
            "xs:date(\"1983-11-17Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1983-11-17Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date2args-3",
            "xs:date(\"2030-12-31Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:2030-12-31Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date2args-4",
            "xs:date(\"1970-01-01Z\") + xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:1970-01-16Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date2args-5",
            "xs:date(\"1970-01-01Z\") + xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:1970-02-01Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-1",
            "xs:date(\"2004-10-30Z\") + xs:dayTimeDuration(\"P2DT2H30M0S\")",
            "S:2004-11-01Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-datealt-2",
            "fn:string((xs:date(\"2000-12-12Z\") + xs:dayTimeDuration(\"P19DT13H10M\"))) and fn:false()",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-3",
            "fn:string((xs:date(\"1999-10-23Z\") + xs:dayTimeDuration(\"P19DT13H10M\"))) or fn:false()",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-4",
            "fn:not(fn:string(xs:date(\"1998-09-12Z\") + xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-5",
            "fn:boolean(fn:string(xs:date(\"1962-03-12Z\") + xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-6",
            "fn:number(xs:date(\"1988-01-28Z\") + xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-7",
            "fn:string(xs:date(\"1989-07-05Z\") + xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:1989-07-06Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-8",
            "(xs:date(\"0001-01-01Z\") + xs:dayTimeDuration(\"-P11DT02H02M\"))",
            "S:-0001-12-20Z\tS:0000-12-20Z"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-9",
            "fn:string((xs:date(\"1993-12-09Z\") + xs:dayTimeDuration(\"P03DT01H04M\"))) and fn:string((xs:date(\"1993-12-09Z\") + xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-10",
            "fn:string((xs:date(\"1985-07-05Z\") + xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:date(\"1985-07-05Z\") + xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-12",
            "fn:string((xs:date(\"1980-03-02Z\") + xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-13",
            "(xs:date(\"1980-05-05Z\") + xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:date(\"1980-05-05Z\")",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-14",
            "(xs:date(\"1979-12-12Z\") + xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:date(\"1979-12-12Z\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-15",
            "(xs:date(\"1978-12-12Z\") + xs:dayTimeDuration(\"P17DT10H02M\")) le xs:date(\"1978-12-12Z\")",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-date/op-add-dayTimeDuration-to-date-16",
            "(xs:date(\"1977-12-12Z\") + xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:date(\"1977-12-12Z\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/K-DateAddDTD-1",
            "xs:date(\"1999-08-12\") + xs:dayTimeDuration(\"P23DT09H32M59S\") eq xs:date(\"1999-09-04\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-date/K-DateAddDTD-2",
            "xs:dayTimeDuration(\"P23DT09H32M59S\") + xs:date(\"1999-08-12\") eq xs:date(\"1999-09-04\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime2args-1",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1970-01-01T00:00:00Z"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime2args-2",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1996-04-07T01:40:52Z"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime2args-3",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:2030-12-31T23:59:59Z"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime2args-4",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") + xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:1970-01-16T11:59:59Z"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime2args-5",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") + xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:1970-02-01T23:59:59Z"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-1",
            "xs:dateTime(\"2000-10-30T11:12:00\") + xs:dayTimeDuration(\"P3DT1H15M\")",
            "S:2000-11-02T12:27:00"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-2",
            "fn:string((xs:dateTime(\"2000-12-12T11:10:03Z\") + xs:dayTimeDuration(\"P12DT10H07M\"))) and fn:false()",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-3",
            "fn:string((xs:dateTime(\"1999-10-23T03:12:23Z\") + xs:dayTimeDuration(\"P19DT13H10M\"))) or fn:false()",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-4",
            "fn:not(fn:string(xs:dateTime(\"1998-09-12T13:23:23Z\") + xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-5",
            "fn:boolean(fn:string(xs:dateTime(\"1962-03-12T12:34:09Z\") + xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-6",
            "fn:number(xs:dateTime(\"1988-01-28T12:34:12Z\") + xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-7",
            "fn:string(xs:dateTime(\"1989-07-05T10:10:10Z\") + xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:1989-07-06T19:12:10Z"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-8",
            "(xs:dateTime(\"0001-01-01T11:11:11Z\") + xs:dayTimeDuration(\"-P11DT02H02M\"))",
            "S:-0001-12-21T09:09:11Z\tS:0000-12-21T09:09:11Z"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-9",
            "fn:string((xs:dateTime(\"1993-12-09T13:13:13Z\") + xs:dayTimeDuration(\"P03DT01H04M\"))) and fn:string((xs:dateTime(\"1993-12-09T13:13:13Z\") + xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-10",
            "fn:string((xs:dateTime(\"1985-07-05T14:14:14Z\") + xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:dateTime(\"1985-07-05T15:15:15Z\") + xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-12",
            "fn:string((xs:dateTime(\"1980-03-02T16:12:10Z\") + xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-13",
            "(xs:dateTime(\"1980-05-05T17:17:17Z\") + xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:dateTime(\"1980-05-05T17:17:17Z\")",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-14",
            "(xs:dateTime(\"1979-12-12T18:18:18Z\") + xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:dateTime(\"1979-12-12T16:15:14Z\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-15",
            "(xs:dateTime(\"1978-12-12T12:45:12Z\") + xs:dayTimeDuration(\"P17DT10H02M\")) le xs:dateTime(\"1978-12-12T16:34:23Z\")",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/op-add-dayTimeDuration-to-dateTime-16",
            "(xs:dateTime(\"1977-12-12T13:12:15Z\") + xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:dateTime(\"1977-12-12T15:56:10Z\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/K-DateTimeAddDTD-1",
            "xs:date(\"1999-10-12\") + xs:dateTime(\"1999-10-12T08:12:12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/K-DateTimeAddDTD-2",
            "xs:dateTime(\"1999-10-12T08:12:12\") + xs:date(\"1999-10-12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/K-DateTimeAddDTD-3",
            "xs:time(\"08:12:12\") + xs:dateTime(\"1999-10-12T08:12:12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-dayTimeDuration-to-dateTime/K-DateTimeAddDTD-4",
            "xs:dateTime(\"1999-10-12T08:12:12\") + xs:time(\"08:12:12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time2args-1",
            "xs:time(\"00:00:00Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:00:00:00Z"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time2args-2",
            "xs:time(\"08:03:35Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:08:03:35Z"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time2args-3",
            "xs:time(\"23:59:59Z\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:23:59:59Z"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time2args-4",
            "xs:time(\"00:00:00Z\") + xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:11:59:59Z"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time2args-5",
            "xs:time(\"00:00:00Z\") + xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:23:59:59Z"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-1",
            "xs:time(\"11:12:00\") + xs:dayTimeDuration(\"P3DT1H15M\")",
            "S:12:27:00"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-2",
            "xs:time(\"23:12:00+03:00\") + xs:dayTimeDuration(\"P1DT3H15M\")",
            "S:02:27:00+03:00"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-3",
            "fn:string((xs:time(\"12:12:01Z\") + xs:dayTimeDuration(\"P19DT13H10M\"))) or fn:false()",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-4",
            "fn:not(fn:string(xs:time(\"13:12:00Z\") + xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-5",
            "fn:boolean(fn:string(xs:time(\"02:02:02Z\") + xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-6",
            "fn:number(xs:time(\"01:01:01Z\") + xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-7",
            "fn:string(xs:time(\"10:02:03Z\") + xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:19:04:03Z"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-8",
            "(xs:time(\"08:02:06\") + xs:dayTimeDuration(\"-P11DT02H02M\"))",
            "S:06:00:06"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-9",
            "fn:string((xs:time(\"10:10:10Z\") + xs:dayTimeDuration(\"P02DT09H02M\"))) and fn:string((xs:time(\"09:02:02Z\") + xs:dayTimeDuration(\"P04DT04H04M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-10",
            "fn:string((xs:time(\"02:03:01Z\") + xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:time(\"02:03:01Z\") + xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-timealt-12",
            "fn:string((xs:time(\"02:02:02Z\") + xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-13",
            "(xs:time(\"01:03:03Z\") + xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:time(\"04:03:05Z\")",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-14",
            "(xs:time(\"04:04:05Z\") + xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:time(\"05:08:02Z\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-15",
            "(xs:time(\"08:09:09Z\") + xs:dayTimeDuration(\"P17DT10H02M\")) le xs:time(\"09:08:10Z\")",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-16",
            "(xs:time(\"09:06:07Z\") + xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:time(\"01:01:01Z\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/op-add-dayTimeDuration-to-time-17",
            "fn:string(xs:time(\"12:07:08Z\") + xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-add-dayTimeDuration-to-time/K-TimeAddDTD-1",
            "xs:time(\"08:12:32\") + xs:dayTimeDuration(\"P23DT09H32M59S\") eq xs:time(\"17:45:31\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/K-TimeAddDTD-2",
            "xs:dayTimeDuration(\"P23DT09H32M59S\") + xs:time(\"08:12:32\") eq xs:time(\"17:45:31\")",
            "T"
        ),
        (
            "op-add-dayTimeDuration-to-time/K2-TimeAddDTD-1",
            "xs:time(\"10:10:10\") + xs:time(\"23:10:10\")",
            "X:XPTY0004"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:PT0S"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations2args-2",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:P15DT11H59M59S"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations2args-3",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") + xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:P31DT23H59M59S"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") + xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:P15DT11H59M59S"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") + xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:P31DT23H59M59S"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-1",
            "xs:dayTimeDuration(\"P2DT12H5M\") + xs:dayTimeDuration(\"P5DT12H\")",
            "S:P8DT5M"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-2",
            "fn:string(xs:dayTimeDuration(\"P10DT10H11M\") + xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-3",
            "fn:string(xs:dayTimeDuration(\"P20DT20H10M\") + xs:dayTimeDuration(\"P19DT13H10M\")) or fn:false()",
            "T"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-4",
            "fn:not(fn:string(xs:dayTimeDuration(\"P11DT12H04M\") + xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-5",
            "fn:boolean(fn:string(xs:dayTimeDuration(\"P05DT09H08M\") + xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-6",
            "fn:number(xs:dayTimeDuration(\"P02DT06H09M\") + xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-7",
            "fn:string(xs:dayTimeDuration(\"P03DT04H08M\") + xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:P4DT13H10M"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-8",
            "(xs:dayTimeDuration(\"P10DT01H01M\") + xs:dayTimeDuration(\"-P11DT02H02M\"))",
            "S:-P1DT1H1M"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-9",
            "fn:string((xs:dayTimeDuration(\"P01DT02H01M\") + xs:dayTimeDuration(\"P02DT09H02M\"))) and fn:string((xs:dayTimeDuration(\"P02DT03H03M\") + xs:dayTimeDuration(\"P04DT04H04M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-10",
            "fn:string((xs:dayTimeDuration(\"P05DT09H02M\") + xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:dayTimeDuration(\"P05DT05H03M\") + xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-11",
            "(xs:dayTimeDuration(\"P42DT10H10M\") + xs:dayTimeDuration(\"P10DT10H10M\")) div (xs:dayTimeDuration(\"P42DT10H10M\") + xs:dayTimeDuration(\"P10DT10H10M\"))",
            "S:1"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-12",
            "fn:string((xs:dayTimeDuration(\"P10DT08H11M\") + xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-13",
            "(xs:dayTimeDuration(\"P23DT11H11M\") + xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:dayTimeDuration(\"P23DT11H11M\")",
            "F"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-14",
            "(xs:dayTimeDuration(\"P21DT08H12M\") + xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:dayTimeDuration(\"P08DT08H05M\")",
            "T"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-15",
            "(xs:dayTimeDuration(\"P10DT10H01M\") + xs:dayTimeDuration(\"P17DT10H02M\")) le xs:dayTimeDuration(\"P17DT10H02M\")",
            "F"
        ),
        (
            "op-add-dayTimeDurations/op-add-dayTimeDurations-16",
            "(xs:dayTimeDuration(\"P13DT09H09M\") + xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:dayTimeDuration(\"P18DT02H02M\")",
            "T"
        ),
        (
            "op-add-dayTimeDurations/K-DayTimeDurationAdd-1",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") + xs:dayTimeDuration(\"PT0S\") eq xs:dayTimeDuration(\"P3DT4H3M3.100S\")",
            "T"
        ),
        (
            "op-add-dayTimeDurations/K-DayTimeDurationAdd-2",
            "xs:dayTimeDuration(\"PT0S\") + xs:dayTimeDuration(\"P3DT4H3M3.100S\") eq xs:dayTimeDuration(\"P3DT4H3M3.100S\")",
            "T"
        ),
        (
            "op-add-dayTimeDurations/K-DayTimeDurationAdd-3",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") + xs:dayTimeDuration(\"P3DT12H31M56.303S\") eq xs:dayTimeDuration(\"P6DT16H34M59.403S\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date2args-1",
            "xs:date(\"1970-01-01Z\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:1970-01-01Z"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date2args-2",
            "xs:date(\"1983-11-17Z\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:1983-11-17Z"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date2args-3",
            "xs:date(\"2030-12-31Z\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:2030-12-31Z"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date2args-4",
            "xs:date(\"1970-01-01Z\") + xs:yearMonthDuration(\"P1000Y6M\")",
            "S:2970-07-01Z"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date2args-5",
            "xs:date(\"1970-01-01Z\") + xs:yearMonthDuration(\"P2030Y12M\")",
            "S:4001-01-01Z"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-1",
            "xs:date(\"2000-10-30\") + xs:yearMonthDuration(\"P1Y2M\")",
            "S:2001-12-30"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-2",
            "fn:string(xs:date(\"2000-12-12Z\") + xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-3",
            "fn:string((xs:date(\"1999-10-23Z\") + xs:yearMonthDuration(\"P19Y12M\"))) or fn:false()",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-4",
            "fn:not(fn:string(xs:date(\"1998-09-12Z\") + xs:yearMonthDuration(\"P20Y03M\")))",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-5",
            "fn:boolean(fn:string(xs:date(\"1962-03-12Z\") + xs:yearMonthDuration(\"P10Y01M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-6",
            "fn:number(xs:date(\"1988-01-28Z\") + xs:yearMonthDuration(\"P09Y02M\"))",
            "S:NaN"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-7",
            "fn:string(xs:date(\"1989-07-05Z\") + xs:yearMonthDuration(\"P08Y04M\"))",
            "S:1997-11-05Z"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-8",
            "(xs:date(\"0001-01-01Z\") + xs:yearMonthDuration(\"-P20Y07M\"))",
            "S:-0021-06-01Z\tS:-0020-06-01Z"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-9",
            "fn:string((xs:date(\"1993-12-09Z\") + xs:yearMonthDuration(\"P03Y03M\"))) and fn:string((xs:date(\"1993-12-09Z\") + xs:yearMonthDuration(\"P03Y03M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-10",
            "fn:string((xs:date(\"1985-07-05Z\") + xs:yearMonthDuration(\"P02Y02M\"))) or fn:string((xs:date(\"1985-07-05Z\") + xs:yearMonthDuration(\"P02Y02M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-12",
            "fn:string((xs:date(\"1980-03-02Z\") + xs:yearMonthDuration(\"P05Y05M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-13",
            "(xs:date(\"1980-05-05Z\") + xs:yearMonthDuration(\"P23Y11M\")) eq xs:date(\"1980-05-05Z\")",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-14",
            "(xs:date(\"1979-12-12Z\") + xs:yearMonthDuration(\"P08Y08M\")) ne xs:date(\"1979-12-12Z\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-15",
            "(xs:date(\"1978-12-12Z\") + xs:yearMonthDuration(\"P17Y12M\")) le xs:date(\"1978-12-12Z\")",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-date/op-add-yearMonthDuration-to-date-16",
            "(xs:date(\"1977-12-12Z\") + xs:yearMonthDuration(\"P18Y02M\")) ge xs:date(\"1977-12-12Z\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/K-DateAddYMD-1",
            "xs:date(\"1999-08-12\") + xs:yearMonthDuration(\"P3Y7M\") eq xs:date(\"2003-03-12\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/K-DateAddYMD-2",
            "xs:yearMonthDuration(\"P3Y7M\") + xs:date(\"1999-08-12\") eq xs:date(\"2003-03-12\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-date/K-DateAddYMD-3",
            "xs:yearMonthDuration(\"P1Y\") - xs:date(\"1999-08-12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-yearMonthDuration-to-date/K-DateAddYMD-4",
            "xs:yearMonthDuration(\"P1Y\") - xs:dateTime(\"1999-08-12T08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime2args-1",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:1970-01-01T00:00:00Z"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime2args-2",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:1996-04-07T01:40:52Z"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime2args-3",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:2030-12-31T23:59:59Z"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime2args-4",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") + xs:yearMonthDuration(\"P1000Y6M\")",
            "S:2970-07-01T00:00:00Z"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime2args-5",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") + xs:yearMonthDuration(\"P2030Y12M\")",
            "S:4001-01-01T00:00:00Z"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-1",
            "xs:dateTime(\"2000-10-30T11:12:00\") + xs:yearMonthDuration(\"P1Y2M\")",
            "S:2001-12-30T11:12:00"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-2",
            "fn:string(xs:dateTime(\"2000-12-12T12:12:12Z\") + xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-3",
            "fn:string((xs:dateTime(\"1999-10-23T13:45:45Z\") + xs:yearMonthDuration(\"P19Y12M\"))) or fn:false()",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-4",
            "fn:not(fn:string(xs:dateTime(\"1998-09-12T13:56:12Z\") + xs:yearMonthDuration(\"P20Y03M\")))",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTimealt-5",
            "fn:boolean(fn:string(xs:dateTime(\"1962-03-12T10:12:34Z\") + xs:yearMonthDuration(\"P10Y01M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-6",
            "fn:number(xs:dateTime(\"1988-01-28T13:45:23Z\") + xs:yearMonthDuration(\"P09Y02M\"))",
            "S:NaN"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-7",
            "fn:string(xs:dateTime(\"1989-07-05T14:34:36Z\") + xs:yearMonthDuration(\"P08Y04M\"))",
            "S:1997-11-05T14:34:36Z"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-8",
            "(xs:dateTime(\"0001-01-01T01:01:01Z\") + xs:yearMonthDuration(\"-P20Y07M\"))",
            "S:-0021-06-01T01:01:01Z\tS:-0020-06-01T01:01:01Z"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-9",
            "fn:string((xs:dateTime(\"1993-12-09T10:10:10Z\") + xs:yearMonthDuration(\"P03Y03M\"))) and fn:string((xs:dateTime(\"1993-12-09T10:10:10Z\") + xs:yearMonthDuration(\"P03Y03M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-10",
            "fn:string((xs:dateTime(\"1985-07-05T09:09:09Z\") + xs:yearMonthDuration(\"P02Y02M\"))) or fn:string((xs:dateTime(\"1985-07-05T09:09:09Z\") + xs:yearMonthDuration(\"P02Y02M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-12",
            "fn:string((xs:dateTime(\"1980-03-02T02:02:02Z\") + xs:yearMonthDuration(\"P05Y05M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-13",
            "(xs:dateTime(\"1980-05-05T05:05:05Z\") + xs:yearMonthDuration(\"P23Y11M\")) eq xs:dateTime(\"1980-05-05T05:05:05Z\")",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-14",
            "(xs:dateTime(\"1979-12-12T09:09:09Z\") + xs:yearMonthDuration(\"P08Y08M\")) ne xs:dateTime(\"1979-12-12T09:09:09Z\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-15",
            "(xs:dateTime(\"1978-12-12T07:07:07Z\") + xs:yearMonthDuration(\"P17Y12M\")) le xs:dateTime(\"1978-12-12T07:07:07Z\")",
            "F"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/op-add-yearMonthDuration-to-dateTime-16",
            "(xs:dateTime(\"1977-12-12T03:03:03Z\") + xs:yearMonthDuration(\"P18Y02M\")) ge xs:dateTime(\"1977-12-12T03:03:03Z\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/K-YearMonthDurationAddDT-1",
            "xs:dateTime(\"1999-07-19T08:23:01.765\") + xs:yearMonthDuration(\"P3Y35M\") eq xs:dateTime(\"2005-06-19T08:23:01.765\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/K-YearMonthDurationAddDT-2",
            "xs:yearMonthDuration(\"P3Y35M\") + xs:dateTime(\"1999-07-19T08:23:01.765\") eq xs:dateTime(\"2005-06-19T08:23:01.765\")",
            "T"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/K-YearMonthDurationAddDT-3",
            "xs:date(\"1999-10-12\") + xs:time(\"08:12:12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/K-YearMonthDurationAddDT-4",
            "xs:time(\"08:12:12\") + xs:date(\"1999-10-12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/K-YearMonthDurationAddDT-5",
            "xs:date(\"1999-10-12\") + xs:time(\"08:12:12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-yearMonthDuration-to-dateTime/K-YearMonthDurationAddDT-6",
            "xs:time(\"08:12:12\") + xs:date(\"1999-10-12\")",
            "X:XPTY0004"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:P0M"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:P1000Y6M"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") + xs:yearMonthDuration(\"P0Y0M\")",
            "S:P2031Y"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") + xs:yearMonthDuration(\"P1000Y6M\")",
            "S:P1000Y6M"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") + xs:yearMonthDuration(\"P2030Y12M\")",
            "S:P2031Y"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-1",
            "xs:yearMonthDuration(\"P2Y11M\") + xs:yearMonthDuration(\"P3Y3M\")",
            "S:P6Y2M"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-2",
            "fn:string(xs:yearMonthDuration(\"P10Y11M\") + xs:yearMonthDuration(\"P12Y07M\")) and fn:false()",
            "F"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-3",
            "fn:string(xs:yearMonthDuration(\"P20Y10M\") + xs:yearMonthDuration(\"P19Y10M\")) or fn:false()",
            "T"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-4",
            "fn:not(fn:string(xs:yearMonthDuration(\"P11Y04M\") + xs:yearMonthDuration(\"P02Y11M\")))",
            "F"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-5",
            "fn:boolean(fn:string(xs:yearMonthDuration(\"P05Y08M\") + xs:yearMonthDuration(\"P03Y06M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-6",
            "fn:number(xs:yearMonthDuration(\"P02Y09M\") + xs:yearMonthDuration(\"P10Y01M\"))",
            "S:NaN"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-7",
            "fn:string(xs:yearMonthDuration(\"P03Y08M\") + xs:yearMonthDuration(\"P01Y02M\"))",
            "S:P4Y10M"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-8",
            "(xs:yearMonthDuration(\"P10Y01M\") + xs:yearMonthDuration(\"-P11Y02M\"))",
            "S:-P1Y1M"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-9",
            "fn:string((xs:yearMonthDuration(\"P01Y01M\") + xs:yearMonthDuration(\"P02Y02M\"))) and fn:string((xs:yearMonthDuration(\"P02Y03M\") + xs:yearMonthDuration(\"P04Y04M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-10",
            "fn:string((xs:yearMonthDuration(\"P05Y02M\") + xs:yearMonthDuration(\"P03Y04M\"))) or fn:string((xs:yearMonthDuration(\"P05Y03M\") + xs:yearMonthDuration(\"P01Y03M\")))",
            "T"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-11",
            "(xs:yearMonthDuration(\"P42Y10M\") + xs:yearMonthDuration(\"P28Y10M\")) div (xs:yearMonthDuration(\"P10Y10M\") + xs:yearMonthDuration(\"P60Y10M\"))",
            "S:1"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-12",
            "fn:string((xs:yearMonthDuration(\"P10Y11M\") + xs:yearMonthDuration(\"P05Y07M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-13",
            "(xs:yearMonthDuration(\"P23Y11M\") + xs:yearMonthDuration(\"P23Y11M\")) eq xs:yearMonthDuration(\"P23Y11M\")",
            "F"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-14",
            "(xs:yearMonthDuration(\"P21Y12M\") + xs:yearMonthDuration(\"P08Y05M\")) ne xs:yearMonthDuration(\"P08Y05M\")",
            "T"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-15",
            "(xs:yearMonthDuration(\"P10Y01M\") + xs:yearMonthDuration(\"P17Y02M\")) le xs:yearMonthDuration(\"P17Y02M\")",
            "F"
        ),
        (
            "op-add-yearMonthDurations/op-add-yearMonthDurations-16",
            "(xs:yearMonthDuration(\"P13Y09M\") + xs:yearMonthDuration(\"P18Y02M\")) ge xs:yearMonthDuration(\"P18Y02M\")",
            "T"
        ),
        (
            "op-add-yearMonthDurations/K-YearMonthDurationAdd-1",
            "xs:yearMonthDuration(\"P3Y3M\") + xs:yearMonthDuration(\"P0M\") eq xs:yearMonthDuration(\"P3Y3M\")",
            "T"
        ),
        (
            "op-add-yearMonthDurations/K-YearMonthDurationAdd-2",
            "xs:yearMonthDuration(\"P0M\") + xs:yearMonthDuration(\"P3Y3M\") eq xs:yearMonthDuration(\"P3Y3M\")",
            "T"
        ),
        (
            "op-add-yearMonthDurations/K-YearMonthDurationAdd-3",
            "xs:yearMonthDuration(\"P32Y43M\") + xs:yearMonthDuration(\"P12Y13M\") eq xs:yearMonthDuration(\"P48Y8M\")",
            "T"
        ),
        ("op-date-equal/op-date-equal2args-1", "xs:date(\"1970-01-01Z\") eq xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-equal/op-date-equal2args-2", "xs:date(\"1983-11-17Z\") eq xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-equal/op-date-equal2args-3", "xs:date(\"2030-12-31Z\") eq xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-equal/op-date-equal2args-4", "xs:date(\"1970-01-01Z\") eq xs:date(\"1983-11-17Z\")", "F"),
        ("op-date-equal/op-date-equal2args-5", "xs:date(\"1970-01-01Z\") eq xs:date(\"2030-12-31Z\")", "F"),
        ("op-date-equal/op-date-equal2args-6", "xs:date(\"1970-01-01Z\") ne xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-equal/op-date-equal2args-7", "xs:date(\"1983-11-17Z\") ne xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-equal/op-date-equal2args-8", "xs:date(\"2030-12-31Z\") ne xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-equal/op-date-equal2args-9", "xs:date(\"1970-01-01Z\") ne xs:date(\"1983-11-17Z\")", "T"),
        ("op-date-equal/op-date-equal2args-10", "xs:date(\"1970-01-01Z\") ne xs:date(\"2030-12-31Z\")", "T"),
        ("op-date-equal/op-date-equal2args-11", "xs:date(\"1970-01-01Z\") le xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-equal/op-date-equal2args-12", "xs:date(\"1983-11-17Z\") le xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-equal/op-date-equal2args-13", "xs:date(\"2030-12-31Z\") le xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-equal/op-date-equal2args-14", "xs:date(\"1970-01-01Z\") le xs:date(\"1983-11-17Z\")", "T"),
        ("op-date-equal/op-date-equal2args-15", "xs:date(\"1970-01-01Z\") le xs:date(\"2030-12-31Z\")", "T"),
        ("op-date-equal/op-date-equal2args-16", "xs:date(\"1970-01-01Z\") ge xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-equal/op-date-equal2args-17", "xs:date(\"1983-11-17Z\") ge xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-equal/op-date-equal2args-18", "xs:date(\"2030-12-31Z\") ge xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-equal/op-date-equal2args-19", "xs:date(\"1970-01-01Z\") ge xs:date(\"1983-11-17Z\")", "F"),
        ("op-date-equal/op-date-equal2args-20", "xs:date(\"1970-01-01Z\") ge xs:date(\"2030-12-31Z\")", "F"),
        ("op-date-equal/K-DateEQ-1", "xs:date(\"2004-08-12\") eq xs:date(\"2004-08-12\")", "T"),
        ("op-date-equal/K-DateEQ-2", "not(xs:date(\"2004-08-12\") eq xs:date(\"2003-08-12\"))", "T"),
        ("op-date-equal/K-DateEQ-3", "xs:date(\"2004-08-12\") ne xs:date(\"2004-07-12\")", "T"),
        ("op-date-equal/K-DateEQ-4", "not(xs:date(\"2004-07-12\") ne xs:date(\"2004-07-12\"))", "T"),
        ("op-date-equal/K-DateEQ-5", "xs:date(\"1999-12-04-00:00\") eq xs:date(\"1999-12-04Z\")", "T"),
        ("op-date-equal/K-DateEQ-6", "xs:date(\"1999-12-04+00:00\") eq xs:date(\"1999-12-04Z\")", "T"),
        ("op-date-equal/K-DateEQ-7", "xs:date(\"1999-12-04Z\") eq xs:date(\"1999-12-04Z\")", "T"),
        ("op-date-equal/K-DateEQ-8", "xs:date(\"1999-12-04-00:00\") eq xs:date(\"1999-12-04+00:00\")", "T"),
        ("op-date-equal/K2-DateEQ-1", "xs:time(\"01:01:01-03:00\") ne xs:time(\"01:01:01+03:00\")", "T"),
        (
            "op-date-equal/cbcl-date-eq-001",
            "xs:date(\"-25252734927766555-06-07+02:00\") = xs:date(\"25252734927766555-07-28\")",
            "X:FODT0001\tF"
        ),
        (
            "op-date-equal/cbcl-date-ne-001",
            "xs:date(\"25252734927766555-07-28\") != xs:date(\"-25252734927766555-06-07+02:00\")",
            "X:FODT0001\tT"
        ),
        ("op-date-equal/cbcl-date-equal-002", "xs:date(\"2008-01-30\") eq xs:date(\"2008-01-31+09:00\")", "F"),
        ("op-date-equal/cbcl-date-equal-003", "xs:date(\"2008-01-31+09:00\") eq xs:date(\"2008-01-30\")", "F"),
        ("op-date-equal/cbcl-date-equal-004", "xs:date(\"2008-01-31\") eq xs:date(\"2008-01-31+09:00\")", "F"),
        ("op-date-equal/cbcl-date-equal-005", "xs:date(\"2008-01-31+09:00\") eq xs:date(\"2008-01-31\")", "F"),
        ("op-date-equal/cbcl-date-equal-007", "xs:date(\"2008-01-30\") ne xs:date(\"2008-01-31+09:00\")", "T"),
        ("op-date-equal/cbcl-date-equal-008", "xs:date(\"2008-01-31+09:00\") ne xs:date(\"2008-01-30\")", "T"),
        ("op-date-equal/cbcl-date-equal-009", "xs:date(\"2008-01-31\") ne xs:date(\"2008-01-31+09:00\")", "T"),
        ("op-date-equal/cbcl-date-equal-010", "xs:date(\"2008-01-31+09:00\") ne xs:date(\"2008-01-31\")", "T"),
        (
            "op-date-greater-than/op-date-greater-than2args-1",
            "xs:date(\"1970-01-01Z\") gt xs:date(\"1970-01-01Z\")",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-2",
            "xs:date(\"1983-11-17Z\") gt xs:date(\"1970-01-01Z\")",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-3",
            "xs:date(\"2030-12-31Z\") gt xs:date(\"1970-01-01Z\")",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-4",
            "xs:date(\"1970-01-01Z\") gt xs:date(\"1983-11-17Z\")",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-5",
            "xs:date(\"1970-01-01Z\") gt xs:date(\"2030-12-31Z\")",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-6",
            "xs:date(\"1970-01-01Z\") le xs:date(\"1970-01-01Z\")",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-7",
            "xs:date(\"1983-11-17Z\") le xs:date(\"1970-01-01Z\")",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-8",
            "xs:date(\"2030-12-31Z\") le xs:date(\"1970-01-01Z\")",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-9",
            "xs:date(\"1970-01-01Z\") le xs:date(\"1983-11-17Z\")",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than2args-10",
            "xs:date(\"1970-01-01Z\") le xs:date(\"2030-12-31Z\")",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than-1",
            "(xs:date(\"2004-12-25Z\") gt xs:date(\"2004-12-25+07:00\"))",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than-2",
            "(xs:date(\"2004-12-25-12:00\") gt xs:date(\"2004-12-26+12:00\"))",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than-3",
            "fn:not((xs:date(\"2005-12-26Z\") gt xs:date(\"2005-12-25Z\")))",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than-4",
            "fn:not(xs:date(\"2005-04-02Z\") ge xs:date(\"2005-04-02Z\"))",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than-5",
            "fn:not(xs:date(\"2000-11-09Z\") gt xs:date(\"2000-11-10Z\"))",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than-6",
            "fn:not(xs:date(\"2005-10-23Z\") ge xs:date(\"2005-10-25Z\"))",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than-7",
            "(xs:date(\"2000-01-01Z\") gt xs:date(\"2000-01-01Z\")) and (xs:date(\"2001-02-02Z\") gt xs:date(\"2001-03-02Z\"))",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than-8",
            "(xs:date(\"2000-01-25Z\") ge xs:date(\"2000-10-26Z\")) and (xs:date(\"1975-10-26Z\") ge xs:date(\"1975-10-28Z\"))",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than-9",
            "(xs:date(\"2000-10-26Z\") gt xs:date(\"2000-10-28Z\")) or (xs:date(\"1976-10-28Z\") gt xs:date(\"1976-10-28Z\"))",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than-10",
            "(xs:date(\"1976-10-25Z\") ge xs:date(\"1976-10-28Z\")) or (xs:date(\"1980-08-11Z\") ge xs:date(\"1980-08-10Z\"))",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than-11",
            "(xs:date(\"1980-05-18Z\") gt xs:date(\"1980-05-17Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than-12",
            "(xs:date(\"2000-10-25Z\") ge xs:date(\"2000-10-26Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-date-greater-than/op-date-greater-than-13",
            "(xs:date(\"1980-01-01Z\") gt xs:date(\"1980-10-01Z\")) or (fn:false())",
            "F"
        ),
        (
            "op-date-greater-than/op-date-greater-than-14",
            "(xs:date(\"1980-10-25Z\") ge xs:date(\"1980-10-26Z\")) or (fn:false())",
            "F"
        ),
        ("op-date-greater-than/K-DateGT-1", "xs:date(\"2004-07-13\") gt xs:date(\"2004-07-12\")", "T"),
        ("op-date-greater-than/K-DateGT-2", "not(xs:date(\"2004-07-12\") gt xs:date(\"2004-07-12\"))", "T"),
        ("op-date-greater-than/K-DateGT-3", "not(xs:date(\"2004-07-12\") gt xs:date(\"2004-07-13\"))", "T"),
        ("op-date-greater-than/K-DateGT-4", "xs:date(\"2004-07-12\") ge xs:date(\"2004-07-12\")", "T"),
        ("op-date-greater-than/K-DateGT-5", "xs:date(\"2004-07-13\") ge xs:date(\"2004-07-12\")", "T"),
        ("op-date-greater-than/K-DateGT-6", "not(xs:date(\"2004-07-11\") ge xs:date(\"2004-07-12\"))", "T"),
        (
            "op-date-greater-than/cbcl-date-gt-001",
            "xs:date(\"25252734927766555-07-28\") > xs:date(\"-25252734927766555-06-07+02:00\")",
            "X:FODT0001\tT"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-002",
            "xs:date(\"2008-01-30\") lt xs:date(\"2008-01-31+09:00\")",
            "T"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-003",
            "xs:date(\"2008-01-31+09:00\") lt xs:date(\"2008-01-30\")",
            "F"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-004",
            "xs:date(\"2008-01-31\") lt xs:date(\"2008-01-31+09:00\")",
            "F"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-005",
            "xs:date(\"2008-01-31+09:00\") lt xs:date(\"2008-01-31\")",
            "T"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-007",
            "xs:date(\"2008-01-30\") ge xs:date(\"2008-01-31+09:00\")",
            "F"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-008",
            "xs:date(\"2008-01-31+09:00\") ge xs:date(\"2008-01-30\")",
            "T"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-009",
            "xs:date(\"2008-01-31\") ge xs:date(\"2008-01-31+09:00\")",
            "T"
        ),
        (
            "op-date-greater-than/cbcl-date-greater-equal-010",
            "xs:date(\"2008-01-31+09:00\") ge xs:date(\"2008-01-31\")",
            "F"
        ),
        ("op-date-less-than/op-date-less-than2args-1", "xs:date(\"1970-01-01Z\") lt xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-less-than/op-date-less-than2args-2", "xs:date(\"1983-11-17Z\") lt xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-less-than/op-date-less-than2args-3", "xs:date(\"2030-12-31Z\") lt xs:date(\"1970-01-01Z\")", "F"),
        ("op-date-less-than/op-date-less-than2args-4", "xs:date(\"1970-01-01Z\") lt xs:date(\"1983-11-17Z\")", "T"),
        ("op-date-less-than/op-date-less-than2args-5", "xs:date(\"1970-01-01Z\") lt xs:date(\"2030-12-31Z\")", "T"),
        ("op-date-less-than/op-date-less-than2args-6", "xs:date(\"1970-01-01Z\") ge xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-less-than/op-date-less-than2args-7", "xs:date(\"1983-11-17Z\") ge xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-less-than/op-date-less-than2args-8", "xs:date(\"2030-12-31Z\") ge xs:date(\"1970-01-01Z\")", "T"),
        ("op-date-less-than/op-date-less-than2args-9", "xs:date(\"1970-01-01Z\") ge xs:date(\"1983-11-17Z\")", "F"),
        ("op-date-less-than/op-date-less-than2args-10", "xs:date(\"1970-01-01Z\") ge xs:date(\"2030-12-31Z\")", "F"),
        ("op-date-less-than/op-date-less-than-1", "(xs:date(\"2004-12-25Z\") lt xs:date(\"2004-12-25-05:00\"))", "T"),
        (
            "op-date-less-than/op-date-less-than-2",
            "(xs:date(\"2004-12-25-12:00\") le xs:date(\"2004-12-26+12:00\"))",
            "T"
        ),
        (
            "op-date-less-than/op-date-less-than-3",
            "fn:not((xs:date(\"2005-12-25Z\") lt xs:date(\"2005-12-26Z\")))",
            "F"
        ),
        ("op-date-less-than/op-date-less-than-4", "fn:not(xs:date(\"2005-04-02Z\") le xs:date(\"2005-04-02Z\"))", "F"),
        ("op-date-less-than/op-date-less-than-5", "fn:not(xs:date(\"2000-12-25Z\") lt xs:date(\"2000-11-25Z\"))", "T"),
        ("op-date-less-than/op-date-less-than-6", "fn:not(xs:date(\"2005-10-25Z\") le xs:date(\"2005-10-23Z\"))", "T"),
        (
            "op-date-less-than/op-date-less-than-7",
            "(xs:date(\"2000-01-01Z\") lt xs:date(\"2000-01-01Z\")) and (xs:date(\"2001-02-02Z\") lt xs:date(\"2001-03-02Z\"))",
            "F"
        ),
        (
            "op-date-less-than/op-date-less-than-8",
            "(xs:date(\"2000-01-25Z\") le xs:date(\"2000-10-26Z\")) and (xs:date(\"1975-10-26Z\") le xs:date(\"1975-10-28Z\"))",
            "T"
        ),
        (
            "op-date-less-than/op-date-less-than-9",
            "(xs:date(\"2000-10-26Z\") lt xs:date(\"2000-10-28Z\")) or (xs:date(\"1976-10-28Z\") lt xs:date(\"1976-10-28Z\"))",
            "T"
        ),
        (
            "op-date-less-than/op-date-less-than-10",
            "(xs:date(\"1976-10-25Z\") le xs:date(\"1976-10-28Z\")) or (xs:date(\"1980-08-11Z\") le xs:date(\"1980-08-10Z\"))",
            "T"
        ),
        (
            "op-date-less-than/op-date-less-than-11",
            "(xs:date(\"1980-05-18Z\") lt xs:date(\"1980-05-17Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-date-less-than/op-date-less-than-12",
            "(xs:date(\"2000-10-25Z\") le xs:date(\"2000-10-26Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-date-less-than/op-date-less-than-13",
            "(xs:date(\"1980-01-01Z\") lt xs:date(\"1980-10-01Z\")) or (fn:false())",
            "T"
        ),
        (
            "op-date-less-than/op-date-less-than-14",
            "(xs:date(\"1980-10-25Z\") le xs:date(\"1980-10-26Z\")) or (fn:false())",
            "T"
        ),
        ("op-date-less-than/K-DateLT-1", "xs:date(\"2004-07-12\") lt xs:date(\"2004-07-13\")", "T"),
        ("op-date-less-than/K-DateLT-2", "not(xs:date(\"2004-07-13\") lt xs:date(\"2004-07-12\"))", "T"),
        ("op-date-less-than/K-DateLT-3", "not(xs:date(\"2004-07-13\") lt xs:date(\"2004-07-13\"))", "T"),
        ("op-date-less-than/K-DateLT-4", "xs:date(\"2004-07-12\") le xs:date(\"2004-07-12\")", "T"),
        ("op-date-less-than/K-DateLT-5", "xs:date(\"2004-07-12\") le xs:date(\"2004-07-12\")", "T"),
        ("op-date-less-than/K-DateLT-6", "not(xs:date(\"2004-07-13\") le xs:date(\"2004-07-12\"))", "T"),
        (
            "op-date-less-than/cbcl-date-ge-001",
            "xs:date(\"25252734927766555-07-28\") >= xs:date(\"-25252734927766555-06-07+02:00\")",
            "X:FODT0001\tT"
        ),
        ("op-date-less-than/cbcl-date-less-than-002", "xs:date(\"2008-01-30\") lt xs:date(\"2008-01-31+09:00\")", "T"),
        ("op-date-less-than/cbcl-date-less-than-003", "xs:date(\"2008-01-31+09:00\") lt xs:date(\"2008-01-30\")", "F"),
        ("op-date-less-than/cbcl-date-less-than-004", "xs:date(\"2008-01-31\") lt xs:date(\"2008-01-31+09:00\")", "F"),
        ("op-date-less-than/cbcl-date-less-than-005", "xs:date(\"2008-01-31+09:00\") lt xs:date(\"2008-01-31\")", "T"),
        ("op-date-less-than/cbcl-date-less-than-007", "xs:date(\"2008-01-30\") le xs:date(\"2008-01-31+09:00\")", "T"),
        ("op-date-less-than/cbcl-date-less-than-008", "xs:date(\"2008-01-31+09:00\") le xs:date(\"2008-01-30\")", "F"),
        ("op-date-less-than/cbcl-date-less-than-009", "xs:date(\"2008-01-31\") le xs:date(\"2008-01-31+09:00\")", "F"),
        ("op-date-less-than/cbcl-date-less-than-010", "xs:date(\"2008-01-31+09:00\") le xs:date(\"2008-01-31\")", "T"),
        (
            "op-dateTime-equal/op-dateTime-equal2args-1",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") eq xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-2",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") eq xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-3",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") eq xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-4",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") eq xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-5",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") eq xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-6",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ne xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-7",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") ne xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-8",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") ne xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-9",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ne xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-10",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ne xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-11",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") le xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-12",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") le xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-13",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") le xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-14",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") le xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-15",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") le xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-16",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ge xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-17",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") ge xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-18",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") ge xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-19",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ge xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/op-dateTime-equal2args-20",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ge xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "F"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-1",
            "xs:dateTime(\"2004-08-12T23:01:04.12\") eq xs:dateTime(\"2004-08-12T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-2",
            "not(xs:dateTime(\"2004-08-12T23:01:04.12\") eq xs:dateTime(\"2004-08-12T23:01:04.13\"))",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-3",
            "xs:dateTime(\"2004-08-12T23:01:05.12\") ne xs:dateTime(\"2004-08-12T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-4",
            "not(xs:dateTime(\"2004-07-12T23:01:04.12\") ne xs:dateTime(\"2004-07-12T23:01:04.12\"))",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-5",
            "xs:dateTime(\"1999-12-04T16:00:12.345-00:00\") eq xs:dateTime(\"1999-12-04T16:00:12.345Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-6",
            "xs:dateTime(\"1999-12-04T16:00:12.345+00:00\") eq xs:dateTime(\"1999-12-04T16:00:12.345Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-7",
            "xs:dateTime(\"1999-12-04T16:00:12.345Z\") eq xs:dateTime(\"1999-12-04T16:00:12.345Z\")",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-8",
            "xs:dateTime(\"1999-12-04T16:00:12.345-00:00\") eq xs:dateTime(\"1999-12-04T16:00:12.345+00:00\")",
            "T"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-9",
            "xs:dateTime(\"1999-12-04T12:12:23\") eq xs:date(\"1999-12-04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-10",
            "xs:dateTime(\"1999-12-04T12:12:23\") ne xs:date(\"1999-12-04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-11",
            "xs:dateTime(\"1999-12-04T12:12:23\") le xs:date(\"1999-12-04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-12",
            "xs:dateTime(\"1999-12-04T12:12:23\") lt xs:date(\"1999-12-04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-13",
            "xs:dateTime(\"1999-12-04T12:12:23\") ge xs:date(\"1999-12-04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-14",
            "xs:dateTime(\"1999-12-04T12:12:23\") gt xs:date(\"1999-12-04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-15",
            "xs:date(\"1999-12-04\") eq xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-16",
            "xs:date(\"1999-12-04\") ne xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-17",
            "xs:date(\"1999-12-04\") le xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-18",
            "xs:date(\"1999-12-04\") lt xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-19",
            "xs:date(\"1999-12-04\") ge xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-20",
            "xs:date(\"1999-12-04\") gt xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-21",
            "xs:dateTime(\"1999-12-04T12:12:23\") eq xs:time(\"22:12:04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-22",
            "xs:dateTime(\"1999-12-04T12:12:23\") ne xs:time(\"22:12:04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-23",
            "xs:dateTime(\"1999-12-04T12:12:23\") le xs:time(\"22:12:04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-24",
            "xs:dateTime(\"1999-12-04T12:12:23\") lt xs:time(\"22:12:04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-25",
            "xs:dateTime(\"1999-12-04T12:12:23\") ge xs:time(\"22:12:04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-26",
            "xs:dateTime(\"1999-12-04T12:12:23\") gt xs:time(\"22:12:04\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-27",
            "xs:time(\"22:12:04\") eq xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-28",
            "xs:time(\"22:12:04\") ne xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-29",
            "xs:time(\"22:12:04\") le xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-30",
            "xs:time(\"22:12:04\") lt xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-31",
            "xs:time(\"22:12:04\") ge xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K-DateTimeEQ-32",
            "xs:time(\"22:12:04\") gt xs:dateTime(\"1999-12-04T12:12:23\")",
            "X:XPTY0004"
        ),
        (
            "op-dateTime-equal/K2-DateTimeEQ-1",
            "xs:dateTime(\"2002-03-06T00:00:00Z\") ne xs:dateTime(\"2002-03-05T00:00:00+03:00\")",
            "T"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-002",
            "xs:dateTime(\"2008-01-30T00:01:00\") eq xs:dateTime(\"2008-01-31T01:00:00+09:00\")",
            "F"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-003",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") eq xs:dateTime(\"2008-01-30T00:01:00\")",
            "F"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-004",
            "xs:dateTime(\"2008-01-31T00:01:00\") eq xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "F"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-005",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") eq xs:dateTime(\"2008-01-31T00:01:00\")",
            "F"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-007",
            "xs:dateTime(\"2008-01-30T00:01:00\") ne xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "T"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-008",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") ne xs:dateTime(\"2008-01-30T00:01:00\")",
            "T"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-009",
            "xs:dateTime(\"2008-01-31T00:01:00\") ne xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "T"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-010",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") ne xs:dateTime(\"2008-01-31T00:01:00\")",
            "T"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-017",
            "xs:dateTime(\"1972-12-15T00:00:00+12:00\") eq xs:dateTime(\"1972-12-15T00:00:01\")",
            "F"
        ),
        (
            "op-dateTime-equal/cbcl-dateTime-equal-018",
            "xs:dateTime(\"1972-12-15T00:00:00+12:00\") ne xs:dateTime(\"1972-12-15T00:00:01\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-1",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") gt xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-2",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") gt xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-3",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") gt xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-4",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") gt xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-5",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") gt xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-6",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") le xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-7",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") le xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-8",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") le xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-9",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") le xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than2args-10",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") le xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-3",
            "fn:not((xs:dateTime(\"2004-04-02T12:00:00Z\") gt xs:dateTime(\"2003-04-02T12:00:00Z\")))",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-4",
            "fn:not(xs:dateTime(\"2002-04-02T12:00:00Z\") ge xs:dateTime(\"2002-04-02T12:00:00Z\"))",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-5",
            "fn:not(xs:dateTime(\"2002-04-02T12:00:00Z\") gt xs:dateTime(\"2002-05-02T12:00:00Z\"))",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-6",
            "fn:not(xs:dateTime(\"2002-04-02T12:00:00Z\") ge xs:dateTime(\"2008-04-02T12:00:00Z\"))",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-7",
            "(xs:dateTime(\"2002-04-02T12:00:00Z\") gt xs:dateTime(\"2002-04-02T12:01:00Z\")) and (xs:dateTime(\"2003-04-02T12:00:00Z\") gt xs:dateTime(\"2002-04-02T12:00:00Z\"))",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-8",
            "(xs:dateTime(\"2002-04-02T12:00:00Z\") ge xs:dateTime(\"2005-04-02T12:00:20Z\")) and (xs:dateTime(\"2002-04-02T12:10:00Z\") ge xs:dateTime(\"2002-04-03T12:00:00Z\"))",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-9",
            "(xs:dateTime(\"2002-06-02T12:00:10Z\") gt xs:dateTime(\"2000-04-04T12:00:00Z\")) or (xs:dateTime(\"2002-04-02T13:00:10Z\") gt xs:dateTime(\"2001-04-02T10:00:00Z\"))",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-10",
            "(xs:dateTime(\"2002-04-03T12:00:10Z\") ge xs:dateTime(\"1990-04-02T12:10:00Z\")) or (xs:dateTime(\"1975-04-03T12:10:00Z\") ge xs:dateTime(\"2000-02-02T12:00:09Z\"))",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-11",
            "(xs:dateTime(\"1990-04-02T12:00:10Z\") gt xs:dateTime(\"2006-06-02T12:10:00Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-12",
            "(xs:dateTime(\"1970-04-02T12:00:20Z\") ge xs:dateTime(\"1980-04-02T12:00:20Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-13",
            "(xs:dateTime(\"1981-04-02T12:00:00Z\") gt xs:dateTime(\"2003-04-02T12:10:00Z\")) or (fn:false())",
            "F"
        ),
        (
            "op-dateTime-greater-than/op-dateTime-greater-than-14",
            "(xs:dateTime(\"1976-04-03T12:00:00Z\") ge xs:dateTime(\"2002-07-02T12:00:30Z\")) or (fn:false())",
            "F"
        ),
        (
            "op-dateTime-greater-than/K-DateTimeGT-1",
            "xs:dateTime(\"2004-07-13T23:01:04.12\") gt xs:dateTime(\"2004-07-12T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/K-DateTimeGT-2",
            "not(xs:dateTime(\"2004-07-12T23:01:04.12\") gt xs:dateTime(\"2004-07-12T23:01:04.12\"))",
            "T"
        ),
        (
            "op-dateTime-greater-than/K-DateTimeGT-3",
            "not(xs:dateTime(\"2004-07-12T23:01:04.12\") gt xs:dateTime(\"2004-07-13T23:01:04.12\"))",
            "T"
        ),
        (
            "op-dateTime-greater-than/K-DateTimeGT-4",
            "xs:dateTime(\"2004-07-12T23:01:04.12\") ge xs:dateTime(\"2004-07-12T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/K-DateTimeGT-5",
            "xs:dateTime(\"2004-07-13T23:01:04.12\") ge xs:dateTime(\"2004-07-12T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/K-DateTimeGT-6",
            "not(xs:dateTime(\"2004-07-11T23:01:04.12\") ge xs:dateTime(\"2004-07-12T23:01:04.12\"))",
            "T"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-002",
            "xs:dateTime(\"2008-01-30T00:01:00\") gt xs:dateTime(\"2008-01-31T01:00:00+09:00\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-003",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") gt xs:dateTime(\"2008-01-30T00:01:00\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-004",
            "xs:dateTime(\"2008-01-31T00:01:00\") gt xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-005",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") gt xs:dateTime(\"2008-01-31T00:01:00\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-007",
            "xs:dateTime(\"2008-01-30T00:01:00\") ge xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "F"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-008",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") ge xs:dateTime(\"2008-01-30T00:01:00\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-009",
            "xs:dateTime(\"2008-01-31T00:01:00\") ge xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "T"
        ),
        (
            "op-dateTime-greater-than/cbcl-dateTime-greater-than-010",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") ge xs:dateTime(\"2008-01-31T00:01:00\")",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-1",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") lt xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-2",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") lt xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-3",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") lt xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-4",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") lt xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-5",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") lt xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-6",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ge xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-7",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") ge xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-8",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") ge xs:dateTime(\"1970-01-01T00:00:00Z\")",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-9",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ge xs:dateTime(\"1996-04-07T01:40:52Z\")",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than2args-10",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") ge xs:dateTime(\"2030-12-31T23:59:59Z\")",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-3",
            "fn:not((xs:dateTime(\"2002-04-02T12:00:00Z\") lt xs:dateTime(\"2003-04-02T12:00:00Z\")))",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-4",
            "fn:not(xs:dateTime(\"2002-04-02T12:00:00Z\") le xs:dateTime(\"2002-04-02T12:00:00Z\"))",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-5",
            "fn:not(xs:dateTime(\"2002-05-02T12:00:00Z\") lt xs:dateTime(\"2002-04-02T12:00:00Z\"))",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-6",
            "fn:not(xs:dateTime(\"2004-04-02T12:00:00Z\") le xs:dateTime(\"2002-04-02T12:00:00Z\"))",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-7",
            "(xs:dateTime(\"2002-04-02T12:00:00Z\") lt xs:dateTime(\"2002-04-02T12:01:00Z\")) and (xs:dateTime(\"2003-04-02T12:00:00Z\") lt xs:dateTime(\"2002-04-02T12:00:00Z\"))",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-8",
            "(xs:dateTime(\"2002-04-02T12:00:00Z\") le xs:dateTime(\"2005-04-02T12:00:20Z\")) and (xs:dateTime(\"2002-04-02T12:10:00Z\") le xs:dateTime(\"2002-04-03T12:00:00Z\"))",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-9",
            "(xs:dateTime(\"2002-06-02T12:00:10Z\") lt xs:dateTime(\"2000-04-04T12:00:00Z\")) or (xs:dateTime(\"2002-04-02T13:00:10Z\") lt xs:dateTime(\"2001-04-02T10:00:00Z\"))",
            "F"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-10",
            "(xs:dateTime(\"2002-04-03T12:00:10Z\") le xs:dateTime(\"1990-04-02T12:10:00Z\")) or (xs:dateTime(\"1975-04-03T12:10:00Z\") le xs:dateTime(\"2000-02-02T12:00:09Z\"))",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-11",
            "(xs:dateTime(\"1990-04-02T12:00:10Z\") lt xs:dateTime(\"2006-06-02T12:10:00Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-12",
            "(xs:dateTime(\"1970-04-02T12:00:20Z\") le xs:dateTime(\"1980-04-02T12:00:20Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-13",
            "(xs:dateTime(\"1981-04-02T12:00:00Z\") lt xs:dateTime(\"2003-04-02T12:10:00Z\")) or (fn:false())",
            "T"
        ),
        (
            "op-dateTime-less-than/op-dateTime-less-than-14",
            "(xs:dateTime(\"1976-04-03T12:00:00Z\") le xs:dateTime(\"2002-07-02T12:00:30Z\")) or (fn:false())",
            "T"
        ),
        (
            "op-dateTime-less-than/K-DateTimeLT-1",
            "xs:dateTime(\"2004-07-12T23:01:04.12\") lt xs:dateTime(\"2004-07-13T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-less-than/K-DateTimeLT-2",
            "not(xs:dateTime(\"2004-07-13T23:01:04.12\") lt xs:dateTime(\"2004-07-12T23:01:04.12\"))",
            "T"
        ),
        (
            "op-dateTime-less-than/K-DateTimeLT-3",
            "not(xs:dateTime(\"2004-07-13T23:01:04.12\") lt xs:dateTime(\"2004-07-13T23:01:04.12\"))",
            "T"
        ),
        (
            "op-dateTime-less-than/K-DateTimeLT-4",
            "xs:dateTime(\"2004-07-12T23:01:04.12\") le xs:dateTime(\"2004-07-12T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-less-than/K-DateTimeLT-5",
            "xs:dateTime(\"2004-07-12T23:01:04.12\") le xs:dateTime(\"2004-07-12T23:01:04.12\")",
            "T"
        ),
        (
            "op-dateTime-less-than/K-DateTimeLT-6",
            "not(xs:dateTime(\"2004-07-13T23:01:04.12\") le xs:dateTime(\"2004-07-12T23:01:04.12\"))",
            "T"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-002",
            "xs:dateTime(\"2008-01-30T00:01:00\") lt xs:dateTime(\"2008-01-31T01:00:00+09:00\")",
            "T"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-003",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") lt xs:dateTime(\"2008-01-30T00:01:00\")",
            "F"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-004",
            "xs:dateTime(\"2008-01-31T00:01:00\") lt xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "F"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-005",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") lt xs:dateTime(\"2008-01-31T00:01:00\")",
            "T"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-007",
            "xs:dateTime(\"2008-01-30T00:01:00\") le xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "T"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-008",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") le xs:dateTime(\"2008-01-30T00:01:00\")",
            "F"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-009",
            "xs:dateTime(\"2008-01-31T00:01:00\") le xs:dateTime(\"2008-01-31T00:01:00+09:00\")",
            "F"
        ),
        (
            "op-dateTime-less-than/cbcl-dateTime-less-than-010",
            "xs:dateTime(\"2008-01-31T00:01:00+09:00\") le xs:dateTime(\"2008-01-31T00:01:00\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") gt xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-2",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") gt xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-3",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") gt xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") gt xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") gt xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-6",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") le xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-7",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") le xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-8",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") le xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-9",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") le xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than2args-10",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") le xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-3",
            "fn:not((xs:dayTimeDuration(\"P15DT12H\") gt xs:dayTimeDuration(\"P14DT11H\")))",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-4",
            "fn:not(xs:dayTimeDuration(\"P10DT11H\") ge xs:dayTimeDuration(\"P10DT10H\"))",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-5",
            "fn:not(xs:dayTimeDuration(\"P08DT10H\") gt xs:dayTimeDuration(\"P9DT09H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-6",
            "fn:not(xs:dayTimeDuration(\"P07DT09H\") ge xs:dayTimeDuration(\"P09DT09H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-7",
            "(xs:dayTimeDuration(\"P10DT09H\") gt xs:dayTimeDuration(\"P09DT10H\")) and (xs:dayTimeDuration(\"P10DT01H\") gt xs:dayTimeDuration(\"P08DT06H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-8",
            "(xs:dayTimeDuration(\"P10DT09H\") ge xs:dayTimeDuration(\"P10DT01H\")) and (xs:dayTimeDuration(\"P02DT04H\") ge xs:dayTimeDuration(\"P09DT07H\"))",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-9",
            "(xs:dayTimeDuration(\"P10DT08H\") gt xs:dayTimeDuration(\"P10DT07H\")) or (xs:dayTimeDuration(\"P10DT09H\") gt xs:dayTimeDuration(\"P10DT09H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-10",
            "(xs:dayTimeDuration(\"P10DT01H\") ge xs:dayTimeDuration(\"P09DT06H\")) or (xs:dayTimeDuration(\"P15DT01H\") ge xs:dayTimeDuration(\"P02DT04H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-11",
            "(xs:dayTimeDuration(\"P10DT02H\") gt xs:dayTimeDuration(\"P01DT10H\")) or (fn:true())",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-12",
            "(xs:dayTimeDuration(\"P10DT01H\") ge xs:dayTimeDuration(\"P09DT05H\")) or (fn:true())",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-13",
            "(xs:dayTimeDuration(\"P30DT10H\") gt xs:dayTimeDuration(\"P01DT02H\")) or (fn:false())",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/op-dayTimeDuration-greater-than-14",
            "(xs:dayTimeDuration(\"P10DT05H\") ge xs:dayTimeDuration(\"P20DT10H\")) or (fn:false())",
            "F"
        ),
        (
            "op-dayTimeDuration-greater-than/K-DayTimeDurationGT-1",
            "xs:dayTimeDuration(\"P3DT08H34M12.144S\") gt xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/K-DayTimeDurationGT-2",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.144S\") gt xs:dayTimeDuration(\"P3DT08H34M12.144S\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/K-DayTimeDurationGT-3",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.144S\") gt xs:dayTimeDuration(\"P3DT08H34M12.145S\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/K-DayTimeDurationGT-4",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") ge xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/K-DayTimeDurationGT-5",
            "xs:dayTimeDuration(\"P3DT08H34M12.144S\") ge xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-greater-than/K-DayTimeDurationGT-6",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.143S\") ge xs:dayTimeDuration(\"P3DT08H34M12.144S\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") lt xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-2",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") lt xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-3",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") lt xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") lt xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") lt xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-6",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ge xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-7",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") ge xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-8",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") ge xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-9",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ge xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than2args-10",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ge xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-3",
            "fn:not((xs:dayTimeDuration(\"P13DT12H\") lt xs:dayTimeDuration(\"P14DT11H\")))",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-4",
            "fn:not(xs:dayTimeDuration(\"P10DT110H\") le xs:dayTimeDuration(\"P10DT11H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-5",
            "fn:not(xs:dayTimeDuration(\"P10DT10H\") lt xs:dayTimeDuration(\"P9DT09H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-6",
            "fn:not(xs:dayTimeDuration(\"P10DT09H\") le xs:dayTimeDuration(\"P09DT09H\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-7",
            "(xs:dayTimeDuration(\"P10DT09H\") lt xs:dayTimeDuration(\"P09DT10H\")) and (xs:dayTimeDuration(\"P10DT01H\") lt xs:dayTimeDuration(\"P08DT06H\"))",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-8",
            "(xs:dayTimeDuration(\"P10DT09H\") le xs:dayTimeDuration(\"P10DT01H\")) and (xs:dayTimeDuration(\"P02DT04H\") le xs:dayTimeDuration(\"P09DT07H\"))",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-9",
            "(xs:dayTimeDuration(\"P10DT08H\") lt xs:dayTimeDuration(\"P10DT07H\")) or (xs:dayTimeDuration(\"P10DT09H\") lt xs:dayTimeDuration(\"P10DT09H\"))",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-10",
            "(xs:dayTimeDuration(\"P10DT01H\") le xs:dayTimeDuration(\"P09DT06H\")) or (xs:dayTimeDuration(\"P15DT01H\") le xs:dayTimeDuration(\"P02DT04H\"))",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-11",
            "(xs:dayTimeDuration(\"P10DT02H\") lt xs:dayTimeDuration(\"P01DT10H\")) or (fn:true())",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-12",
            "(xs:dayTimeDuration(\"P10DT01H\") le xs:dayTimeDuration(\"P09DT05H\")) or (fn:true())",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-13",
            "(xs:dayTimeDuration(\"P30DT10H\") lt xs:dayTimeDuration(\"P01DT02H\")) or (fn:false())",
            "F"
        ),
        (
            "op-dayTimeDuration-less-than/op-dayTimeDuration-less-than-14",
            "(xs:dayTimeDuration(\"P10DT05H\") le xs:dayTimeDuration(\"P20DT10H\")) or (fn:false())",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/K-DayTimeDurationLT-1",
            "xs:dayTimeDuration(\"P3DT08H34M12.142S\") lt xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/K-DayTimeDurationLT-2",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.143S\") lt xs:dayTimeDuration(\"P3DT08H34M12.143S\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/K-DayTimeDurationLT-3",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.144S\") lt xs:dayTimeDuration(\"P3DT08H34M12.143S\"))",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/K-DayTimeDurationLT-4",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") le xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/K-DayTimeDurationLT-5",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") le xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-dayTimeDuration-less-than/K-DayTimeDurationLT-6",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.143S\") le xs:dayTimeDuration(\"P3DT08H34M12.142S\"))",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dayTimeDuration2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") div xs:dayTimeDuration(\"P0DT0H0M01S\")",
            "E:0"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dayTimeDuration2args-2",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") div xs:dayTimeDuration(\"P0DT0H0M01S\")",
            "E:1339199"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dayTimeDuration2args-3",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") div xs:dayTimeDuration(\"P0DT0H0M01S\")",
            "E:2764799"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dayTimeDuration2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") div xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "E:0"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dayTimeDuration2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") div xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "E:0"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-1",
            "fn:round-half-to-even((xs:dayTimeDuration(\"P2DT53M11S\") div xs:dayTimeDuration(\"P1DT10H\")),15)",
            "E:1.437834967320261\tE:1.4378349673"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-2",
            "xs:dayTimeDuration(\"P10DT10H11M\") div xs:dayTimeDuration(\"P12DT10H07M\") and fn:false()",
            "F"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-3",
            "xs:dayTimeDuration(\"P20DT20H10M\") div xs:dayTimeDuration(\"P19DT13H10M\") or fn:false()",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-4",
            "fn:not(xs:dayTimeDuration(\"P11DT12H04M\") div xs:dayTimeDuration(\"P02DT07H01M\"))",
            "F"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-5",
            "fn:boolean(xs:dayTimeDuration(\"P05DT09H08M\") div xs:dayTimeDuration(\"P03DT08H06M\"))",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-6",
            "fn:number(xs:dayTimeDuration(\"P02DT06H09M\") div xs:dayTimeDuration(\"P02DT06H09M\"))",
            "E:1"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-7",
            "fn:string(xs:dayTimeDuration(\"P08DT06H08M\") div xs:dayTimeDuration(\"P08DT06H08M\"))",
            "E:\"1\""
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-8",
            "(xs:dayTimeDuration(\"P10DT01H01M\") div xs:dayTimeDuration(\"-P10DT01H01M\"))",
            "E:-1"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-9",
            "(xs:dayTimeDuration(\"P01DT02H01M\") div xs:dayTimeDuration(\"P02DT09H02M\")) and (xs:dayTimeDuration(\"P01DT02H01M\") div xs:dayTimeDuration(\"P02DT09H02M\"))",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-10",
            "(xs:dayTimeDuration(\"P05DT09H02M\") div xs:dayTimeDuration(\"P03DT01H04M\")) or (xs:dayTimeDuration(\"P05DT05H03M\") div xs:dayTimeDuration(\"P01DT01H03M\"))",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-11",
            "fn:round-half-to-even( (xs:dayTimeDuration(\"P42DT10H10M\") div xs:dayTimeDuration(\"P10DT10H10M\")) div (xs:dayTimeDuration(\"P20DT10H10M\") div xs:dayTimeDuration(\"P18DT10H10M\")) ,15)",
            "E:3.671399617754547"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-12",
            "(xs:dayTimeDuration(\"P10DT08H11M\") div xs:dayTimeDuration(\"P05DT08H11M\")) and (fn:true())",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-13",
            "(xs:dayTimeDuration(\"P23DT11H11M\") div xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:decimal(2.0)",
            "F"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-14",
            "(xs:dayTimeDuration(\"P21DT08H12M\") div xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:decimal(2.0)",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-15",
            "(xs:dayTimeDuration(\"P10DT10H01M\") div xs:dayTimeDuration(\"P17DT10H02M\")) le xs:decimal(2.0)",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/op-divide-dayTimeDuration-by-dTD-16",
            "(xs:dayTimeDuration(\"P13DT09H09M\") div xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:decimal(2.0)",
            "F"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/K-DayTimeDurationDivideDTD-1",
            "(xs:dayTimeDuration(\"PT8M\") div xs:dayTimeDuration(\"PT2M\")) eq 4",
            "T"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/cbcl-divide-dayTimeDuration-by-dayTimeDuration-001",
            "xs:dayTimeDuration(\"P9223372036854775807D\") div xs:dayTimeDuration(\"P0D\")",
            "X:FOAR0001\tX:FODT0002"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/cbcl-divide-dayTimeDuration-by-dayTimeDuration-002",
            "xs:dayTimeDuration(\"P9223372036854775807D\") div xs:dayTimeDuration(\"P0DT0H0M0.000000001S\")",
            "X:FOAR0002\tX:FODT0002\tE:796899343984252629724800000000000"
        ),
        (
            "op-divide-dayTimeDuration-by-dayTimeDuration/cbcl-divide-dayTimeDuration-by-dayTimeDuration-003",
            "xs:dayTimeDuration(\"P9223372036854775806D\") div xs:dayTimeDuration(\"P4611686018427387903D\")",
            "E:2\tX:FODT0002"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") div xs:double(\"-1.7976931348623157E308\")",
            "S:PT0S"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") div xs:double(\"0.1\")",
            "S:PT0S"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") div xs:double(\"1.7976931348623157E308\")",
            "S:PT0S"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-2",
            "fn:string((xs:dayTimeDuration(\"P10DT10H11M\")) div 2.0) and fn:false()",
            "F"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-3",
            "fn:string((xs:dayTimeDuration(\"P20DT20H10M\") div 2.0)) or fn:false()",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-4",
            "fn:not(fn:string(xs:dayTimeDuration(\"P11DT12H04M\") div 2.0))",
            "F"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-5",
            "fn:boolean(fn:string(xs:dayTimeDuration(\"P05DT09H08M\") div 2.0))",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-6",
            "fn:number(xs:dayTimeDuration(\"P02DT06H09M\") div 2.0)",
            "S:NaN"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-7",
            "fn:string(xs:dayTimeDuration(\"P03DT04H08M\") div 2.0)",
            "S:P1DT14H4M"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-8",
            "(xs:dayTimeDuration(\"P10DT01H01M\") div -2.0)",
            "S:-P5DT30M30S"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-9",
            "fn:string((xs:dayTimeDuration(\"P01DT02H01M\") div 2.0)) and fn:string((xs:dayTimeDuration(\"P02DT03H03M\") div 2.0 ))",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-10",
            "fn:string((xs:dayTimeDuration(\"P05DT09H02M\") div 2.0)) or fn:string((xs:dayTimeDuration(\"P05DT05H03M\") div 2.0))",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-11",
            "(xs:dayTimeDuration(\"P42DT10H10M\") div 2.0) div (xs:dayTimeDuration(\"P42DT10H10M\") div 2.0)",
            "S:1"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-12",
            "fn:string((xs:dayTimeDuration(\"P10DT08H11M\") div 2.0)) and (fn:true())",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-13",
            "(xs:dayTimeDuration(\"P23DT11H11M\") div 2.0) eq xs:dayTimeDuration(\"P23DT11H11M\")",
            "F"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-14",
            "(xs:dayTimeDuration(\"P21DT08H12M\") div 2.0) ne xs:dayTimeDuration(\"P08DT08H05M\")",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-15",
            "(xs:dayTimeDuration(\"P10DT10H01M\") div 2.0) le xs:dayTimeDuration(\"P17DT10H02M\")",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration-16",
            "(xs:dayTimeDuration(\"P13DT09H09M\") div 2.0) ge xs:dayTimeDuration(\"P18DT02H02M\")",
            "F"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-1",
            "xs:dayTimeDuration(\"P8DT4H4M4.400S\") div 4 eq xs:dayTimeDuration(\"P2DT1H1M1.1S\")",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-2",
            "xs:dayTimeDuration(\"P3D\") div xs:double(\"-INF\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-3",
            "xs:dayTimeDuration(\"P3D\") div xs:double(\"INF\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-4",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") div 0",
            "X:FODT0002"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-5",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") div xs:double(\"NaN\")",
            "X:FOCA0005"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-6",
            "xs:dayTimeDuration(\"P3D\") div xs:double(\"-0\")",
            "X:FODT0002"
        ),
        ("op-divide-dayTimeDuration/K-DayTimeDurationDivide-7", "xs:duration(\"P1Y3M\") div 3", "X:XPTY0004"),
        ("op-divide-dayTimeDuration/K-DayTimeDurationDivide-8", "3 div xs:duration(\"P1Y3M\")", "X:XPTY0004"),
        ("op-divide-dayTimeDuration/K-DayTimeDurationDivide-9", "3 div xs:yearMonthDuration(\"P1Y3M\")", "X:XPTY0004"),
        ("op-divide-dayTimeDuration/K-DayTimeDurationDivide-10", "3 div xs:dayTimeDuration(\"P3D\")", "X:XPTY0004"),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-11",
            "xs:dayTimeDuration(\"P3D\") div xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-12",
            "xs:yearMonthDuration(\"P3Y3M\") div xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-13",
            "xs:duration(\"P3D\") div xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-14",
            "xs:yearMonthDuration(\"P3Y3M\") div xs:duration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-15",
            "xs:dayTimeDuration(\"P3D\") div xs:duration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-divide-dayTimeDuration/K-DayTimeDurationDivide-16",
            "xs:duration(\"P3Y3M\") div xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-divide-dayTimeDuration/cbcl-divide-dayTimeDuration-003",
            "xs:dayTimeDuration(\"P9223372036854775807D\") div 0.5",
            "E:xs:dayTimeDuration(\"P18446744073709551614D\")\tX:FODT0002"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yearMonthDuration2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") div xs:yearMonthDuration(\"P0Y1M\")",
            "E:0"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yearMonthDuration2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") div xs:yearMonthDuration(\"P0Y1M\")",
            "E:12006"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yearMonthDuration2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") div xs:yearMonthDuration(\"P0Y1M\")",
            "E:24372"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yearMonthDuration2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") div xs:yearMonthDuration(\"P1000Y6M\")",
            "E:0"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yearMonthDuration2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") div xs:yearMonthDuration(\"P2030Y12M\")",
            "E:0"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-1",
            "xs:yearMonthDuration(\"P3Y4M\") div xs:yearMonthDuration(\"-P1Y4M\")",
            "E:-2.5"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-2",
            "xs:yearMonthDuration(\"P10Y11M\") div xs:yearMonthDuration(\"P12Y07M\") and fn:false()",
            "F"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-3",
            "xs:yearMonthDuration(\"P20Y10M\") div xs:yearMonthDuration(\"P19Y10M\") or fn:false()",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-4",
            "fn:not(xs:yearMonthDuration(\"P11Y04M\") div xs:yearMonthDuration(\"P02Y11M\"))",
            "F"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-5",
            "fn:boolean(xs:yearMonthDuration(\"P05Y08M\") div xs:yearMonthDuration(\"P03Y06M\"))",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-6",
            "fn:number(xs:yearMonthDuration(\"P02Y09M\") div xs:yearMonthDuration(\"P02Y09M\"))",
            "E:1"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-7",
            "fn:string(xs:yearMonthDuration(\"P03Y08M\") div xs:yearMonthDuration(\"P03Y08M\"))",
            "E:\"1\""
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-8",
            "(xs:yearMonthDuration(\"P10Y01M\") div xs:yearMonthDuration(\"-P10Y01M\"))",
            "E:-1"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-9",
            "(xs:yearMonthDuration(\"P01Y01M\") div xs:yearMonthDuration(\"P02Y02M\")) and (xs:yearMonthDuration(\"P02Y03M\") div xs:yearMonthDuration(\"P04Y04M\"))",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-10",
            "(xs:yearMonthDuration(\"P05Y02M\") div xs:yearMonthDuration(\"P03Y04M\")) or (xs:yearMonthDuration(\"P05Y03M\") div xs:yearMonthDuration(\"P01Y03M\"))",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-11",
            "round-half-to-even( (xs:yearMonthDuration(\"P42Y10M\") div xs:yearMonthDuration(\"P20Y10M\")) div (xs:yearMonthDuration(\"P20Y11M\") div xs:yearMonthDuration(\"P18Y11M\")), 15)",
            "E:1.859410358565737"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-12",
            "(xs:yearMonthDuration(\"P10Y11M\") div xs:yearMonthDuration(\"P05Y07M\")) and (fn:true())",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-13",
            "(xs:yearMonthDuration(\"P23Y11M\") div xs:yearMonthDuration(\"P23Y11M\")) eq xs:decimal(2.0)",
            "F"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-14",
            "(xs:yearMonthDuration(\"P21Y12M\") div xs:yearMonthDuration(\"P08Y05M\")) ne xs:decimal(2.0)",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-15",
            "(xs:yearMonthDuration(\"P10Y01M\") div xs:yearMonthDuration(\"P17Y02M\")) le xs:decimal(2.0)",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/op-divide-yearMonthDuration-by-yMD-16",
            "(xs:yearMonthDuration(\"P13Y09M\") div xs:yearMonthDuration(\"P18Y02M\")) ge xs:decimal(2.0)",
            "F"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/K-YearMonthDurationDivideYMD-1",
            "xs:yearMonthDuration(\"P3Y36M\") div xs:yearMonthDuration(\"P60Y\") eq 0.1",
            "T"
        ),
        (
            "op-divide-yearMonthDuration-by-yearMonthDuration/cbcl-divide-yearMonthDuration-by-yearMonthDuration-001",
            "xs:yearMonthDuration(\"P2Y\") div xs:yearMonthDuration(\"P0Y\")",
            "X:FOAR0001"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") div xs:double(\"-1.7976931348623157E308\")",
            "S:P0M"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") div xs:double(\"-1.7976931348623157E308\")",
            "S:P0M"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") div xs:double(\"-1.7976931348623157E308\")",
            "S:P0M"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") div xs:double(\"0.1\")",
            "S:P0M"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") div xs:double(\"1.7976931348623157E308\")",
            "S:P0M"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-1",
            "(xs:yearMonthDuration(\"P2Y11M\") div 1.5)",
            "S:P1Y11M"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-2",
            "fn:string((xs:yearMonthDuration(\"P10Y11M\") div 2.0)) and fn:false()",
            "F"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-3",
            "fn:string((xs:yearMonthDuration(\"P20Y10M\") div 2.0)) or fn:false()",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-4",
            "fn:not(fn:string(xs:yearMonthDuration(\"P11Y04M\") div 2.0))",
            "F"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-5",
            "fn:boolean(fn:string(xs:yearMonthDuration(\"P05Y08M\") div 2.0))",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-6",
            "fn:number(xs:yearMonthDuration(\"P02Y09M\") div 2.0)",
            "S:NaN"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-7",
            "fn:string(xs:yearMonthDuration(\"P03Y08M\") div 2.0)",
            "S:P1Y10M"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-8",
            "(xs:yearMonthDuration(\"P10Y01M\") div -2.0)",
            "S:-P5Y"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-9",
            "fn:string((xs:yearMonthDuration(\"P01Y01M\") div 2.0)) and fn:string((xs:yearMonthDuration(\"P02Y03M\") div 2.0))",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-10",
            "fn:string((xs:yearMonthDuration(\"P05Y02M\") div 2.0)) or fn:string((xs:yearMonthDuration(\"P05Y03M\") div 2.0))",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-11",
            "(xs:yearMonthDuration(\"P20Y11M\") div 2.0) div (xs:yearMonthDuration(\"P20Y11M\") div 2.0)",
            "S:1"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-12",
            "fn:string((xs:yearMonthDuration(\"P10Y11M\") div 2.0)) and (fn:true())",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-13",
            "(xs:yearMonthDuration(\"P23Y11M\") div 2.0) eq xs:yearMonthDuration(\"P23Y11M\")",
            "F"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-14",
            "(xs:yearMonthDuration(\"P21Y12M\") div 2.0) ne xs:yearMonthDuration(\"P08Y05M\")",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-15",
            "(xs:yearMonthDuration(\"P10Y01M\") div 2.0) le xs:yearMonthDuration(\"P17Y02M\")",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-16",
            "(xs:yearMonthDuration(\"P13Y09M\") div 2.0) ge xs:yearMonthDuration(\"P18Y02M\")",
            "F"
        ),
        (
            "op-divide-yearMonthDuration/op-divide-yearMonthDuration-17",
            "for $i in (-2, -4, -10, -50, +50, +10, +4, +2) return (xs:yearMonthDuration(\"P5M\") div $i)",
            "S:-P2M -P1M P0M P0M P0M P1M P1M P3M"
        ),
        (
            "op-divide-yearMonthDuration/K-YearMonthDurationDivide-1",
            "xs:yearMonthDuration(\"P3Y36M\") div 3 eq xs:yearMonthDuration(\"P2Y\")",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/K-YearMonthDurationDivide-2",
            "xs:yearMonthDuration(\"P3Y36M\") div xs:double(\"-INF\") eq xs:yearMonthDuration(\"P0M\")",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/K-YearMonthDurationDivide-3",
            "xs:yearMonthDuration(\"P3Y36M\") div xs:double(\"INF\") eq xs:yearMonthDuration(\"P0M\")",
            "T"
        ),
        (
            "op-divide-yearMonthDuration/K-YearMonthDurationDivide-4",
            "xs:yearMonthDuration(\"P3Y36M\") div 0",
            "X:FODT0002"
        ),
        (
            "op-divide-yearMonthDuration/K-YearMonthDurationDivide-5",
            "xs:yearMonthDuration(\"P3Y36M\") div xs:double(\"NaN\")",
            "X:FOCA0005"
        ),
        (
            "op-divide-yearMonthDuration/K-YearMonthDurationDivide-6",
            "xs:yearMonthDuration(\"P3Y36M\") div xs:double(\"-0\")",
            "X:FODT0002"
        ),
        ("op-duration-equal/op-duration-equal-1", "xs:duration(\"P1Y\") eq xs:duration(\"P12M\")", "T"),
        ("op-duration-equal/op-duration-equal-2", "xs:duration(\"PT24H\") eq xs:duration(\"P1D\")", "T"),
        ("op-duration-equal/op-duration-equal-3", "xs:duration(\"P1Y\") eq xs:duration(\"P365D\")", "F"),
        ("op-duration-equal/op-duration-equal-4", "xs:duration(\"P1Y\") eq xs:duration(\"P1Y\")", "T"),
        ("op-duration-equal/op-duration-equal-5", "xs:duration(\"P1Y\") ne xs:duration(\"P1Y\")", "F"),
        ("op-duration-equal/op-duration-equal-6", "xs:duration(\"PT24H\") eq xs:duration(\"PT24H\")", "T"),
        ("op-duration-equal/op-duration-equal-7", "xs:duration(\"PT24H\") ne xs:duration(\"PT24H\")", "F"),
        ("op-duration-equal/op-duration-equal-8", "xs:duration(\"P12M\") eq xs:duration(\"P12M\")", "T"),
        ("op-duration-equal/op-duration-equal-9", "xs:duration(\"P12M\") ne xs:duration(\"P12M\")", "F"),
        ("op-duration-equal/op-duration-equal-10", "xs:duration(\"P365D\") eq xs:duration(\"P365D\")", "T"),
        ("op-duration-equal/op-duration-equal-11", "xs:duration(\"P365D\") ne xs:duration(\"P365D\")", "F"),
        ("op-duration-equal/op-duration-equal-12", "xs:duration(\"P36D\") eq xs:duration(\"P39D\")", "F"),
        ("op-duration-equal/op-duration-equal-13", "xs:duration(\"P36D\") ne xs:duration(\"P39D\")", "T"),
        ("op-duration-equal/op-duration-equal-14", "fn:not(xs:duration(\"P36D\") eq xs:duration(\"P36D\"))", "F"),
        ("op-duration-equal/op-duration-equal-15", "fn:not(xs:duration(\"P36D\") ne xs:duration(\"P36D\"))", "T"),
        ("op-duration-equal/op-duration-equal-16", "xs:boolean(xs:duration(\"P36D\") eq xs:duration(\"P36D\"))", "T"),
        ("op-duration-equal/op-duration-equal-17", "xs:boolean(xs:duration(\"P36D\") ne xs:duration(\"P36D\"))", "F"),
        (
            "op-duration-equal/op-duration-equal-18",
            "(xs:duration(\"P36D\") eq xs:duration(\"P36D\")) and fn:true()",
            "T"
        ),
        (
            "op-duration-equal/op-duration-equal-19",
            "(xs:duration(\"P36D\") ne xs:duration(\"P36D\")) and fn:true()",
            "F"
        ),
        (
            "op-duration-equal/op-duration-equal-20",
            "(xs:duration(\"P36D\") eq xs:duration(\"P36D\")) or fn:true()",
            "T"
        ),
        (
            "op-duration-equal/op-duration-equal-21",
            "(xs:duration(\"P36D\") ne xs:duration(\"P36D\")) or fn:true()",
            "T"
        ),
        (
            "op-duration-equal/op-duration-equal-22",
            "(xs:duration(\"P36D\") eq xs:duration(\"P36D\")) and fn:false()",
            "F"
        ),
        (
            "op-duration-equal/op-duration-equal-23",
            "(xs:duration(\"P36D\") ne xs:duration(\"P36D\")) and fn:false()",
            "F"
        ),
        (
            "op-duration-equal/op-duration-equal-24",
            "(xs:duration(\"P36D\") eq xs:duration(\"P36D\")) or fn:false()",
            "T"
        ),
        (
            "op-duration-equal/op-duration-equal-25",
            "(xs:duration(\"P36D\") ne xs:duration(\"P36D\")) or fn:false()",
            "F"
        ),
        ("op-duration-equal/op-duration-equal-26", "xs:yearMonthDuration('P0Y') eq xs:dayTimeDuration('P0D')", "T"),
        ("op-duration-equal/op-duration-equal-27", "xs:yearMonthDuration('P1Y') eq xs:dayTimeDuration('P365D')", "F"),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") eq xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") eq xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") eq xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") eq xs:yearMonthDuration(\"P1000Y6M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") eq xs:yearMonthDuration(\"P2030Y12M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-6",
            "xs:yearMonthDuration(\"P0Y0M\") ne xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-7",
            "xs:yearMonthDuration(\"P1000Y6M\") ne xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-8",
            "xs:yearMonthDuration(\"P2030Y12M\") ne xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-9",
            "xs:yearMonthDuration(\"P0Y0M\") ne xs:yearMonthDuration(\"P1000Y6M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-10",
            "xs:yearMonthDuration(\"P0Y0M\") ne xs:yearMonthDuration(\"P2030Y12M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-11",
            "xs:yearMonthDuration(\"P0Y0M\") le xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-12",
            "xs:yearMonthDuration(\"P1000Y6M\") le xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-13",
            "xs:yearMonthDuration(\"P2030Y12M\") le xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-14",
            "xs:yearMonthDuration(\"P0Y0M\") le xs:yearMonthDuration(\"P1000Y6M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-15",
            "xs:yearMonthDuration(\"P0Y0M\") le xs:yearMonthDuration(\"P2030Y12M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-16",
            "xs:yearMonthDuration(\"P0Y0M\") ge xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-17",
            "xs:yearMonthDuration(\"P1000Y6M\") ge xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-18",
            "xs:yearMonthDuration(\"P2030Y12M\") ge xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-19",
            "xs:yearMonthDuration(\"P0Y0M\") ge xs:yearMonthDuration(\"P1000Y6M\")",
            "F"
        ),
        (
            "op-duration-equal/op-yearMonthDuration-equal2args-20",
            "xs:yearMonthDuration(\"P0Y0M\") ge xs:yearMonthDuration(\"P2030Y12M\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") eq xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-2",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") eq xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-3",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") eq xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") eq xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") eq xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-6",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ne xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-7",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") ne xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-8",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") ne xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-9",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ne xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-10",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ne xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-11",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") le xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-12",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") le xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-13",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") le xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-14",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") le xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-15",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") le xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-16",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ge xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-17",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") ge xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-18",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") ge xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "T"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-19",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ge xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "F"
        ),
        (
            "op-duration-equal/op-dayTimeDuration-equal2args-20",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") ge xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "F"
        ),
        (
            "op-duration-equal/distinct-duration-equal-2",
            "fn:distinct-values((xs:yearMonthDuration('P1Y'), xs:dayTimeDuration('P365D')))",
            "S:P1Y P365D\tS:P365D P1Y"
        ),
        (
            "op-duration-equal/K-DurationEQ-1",
            "xs:duration(\"P1999Y10M3DT08H34M12.143S\") eq xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "T"
        ),
        ("op-duration-equal/K-DurationEQ-2", "xs:duration(\"PT0S\") eq xs:duration(\"PT0S\")", "T"),
        (
            "op-duration-equal/K-DurationEQ-3",
            "not(xs:duration(\"P1999Y10M3DT08H34M12.043S\") eq xs:duration(\"P1999Y10M3DT08H34M12.143S\"))",
            "T"
        ),
        (
            "op-duration-equal/K-DurationEQ-4",
            "xs:duration(\"P1999Y01M3DT08H34M12.143S\") ne xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-duration-equal/K-DurationEQ-5",
            "not(xs:duration(\"P1999Y10M3DT08H34M12.143S\") ne xs:duration(\"P1999Y10M3DT08H34M12.143S\"))",
            "T"
        ),
        ("op-duration-equal/K-DurationEQ-6", "xs:duration(\"P1Y\") ne xs:duration(\"P365D\")", "T"),
        ("op-duration-equal/K-DurationEQ-7", "xs:duration(\"P1Y\") eq xs:duration(\"P12M\")", "T"),
        ("op-duration-equal/K-DurationEQ-8", "xs:duration(\"P1M\") ne xs:duration(\"P31D\")", "T"),
        ("op-duration-equal/K-DurationEQ-9", "xs:duration(\"-PT0S\") eq xs:duration(\"PT0S\")", "T"),
        (
            "op-duration-equal/K-DurationEQ-10",
            "xs:duration(\"-P1Y3M4DT08H4M33S\") ne xs:duration(\"P1Y3M4DT08H4M33S\")",
            "T"
        ),
        (
            "op-duration-equal/K-DurationEQ-11",
            "xs:duration(\"P1999Y10M3DT08H34M12.142S\") lt xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-12",
            "xs:duration(\"P1999Y10M3DT08H34M12.143S\") le xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-13",
            "xs:duration(\"P1999Y10M3DT08H34M12.144S\") gt xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-14",
            "xs:duration(\"P1999Y10M3DT08H34M12.143S\") ge xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        ("op-duration-equal/K-DurationEQ-15", "xs:duration(\"PT0S\") eq xs:dayTimeDuration(\"PT0S\")", "T"),
        ("op-duration-equal/K-DurationEQ-16", "xs:dayTimeDuration(\"PT0S\") eq xs:duration(\"PT0S\")", "T"),
        ("op-duration-equal/K-DurationEQ-17", "xs:yearMonthDuration(\"P0M\") eq xs:duration(\"PT0S\")", "T"),
        ("op-duration-equal/K-DurationEQ-18", "xs:duration(\"PT0S\") eq xs:yearMonthDuration(\"P0M\")", "T"),
        ("op-duration-equal/K-DurationEQ-19", "xs:yearMonthDuration(\"P0M\") eq xs:dayTimeDuration(\"PT0S\")", "T"),
        ("op-duration-equal/K-DurationEQ-20", "xs:dayTimeDuration(\"PT0S\") eq xs:yearMonthDuration(\"P0M\")", "T"),
        ("op-duration-equal/K-DurationEQ-21", "xs:duration(\"P1DT2H\") eq xs:dayTimeDuration(\"P1DT2H\")", "T"),
        ("op-duration-equal/K-DurationEQ-22", "xs:dayTimeDuration(\"P1DT2H\") eq xs:duration(\"P1DT2H\")", "T"),
        ("op-duration-equal/K-DurationEQ-23", "xs:duration(\"P1M1DT2H\") ne xs:dayTimeDuration(\"P1DT2H\")", "T"),
        ("op-duration-equal/K-DurationEQ-24", "xs:dayTimeDuration(\"P1DT2H\") ne xs:duration(\"P1M1DT2H\")", "T"),
        ("op-duration-equal/K-DurationEQ-25", "xs:yearMonthDuration(\"P0M\") eq xs:dayTimeDuration(\"PT0S\")", "T"),
        ("op-duration-equal/K-DurationEQ-26", "xs:dayTimeDuration(\"PT0S\") eq xs:yearMonthDuration(\"P0M\")", "T"),
        ("op-duration-equal/K-DurationEQ-27", "xs:yearMonthDuration(\"P1Y12M\") eq xs:duration(\"P1Y12M0D\")", "T"),
        ("op-duration-equal/K-DurationEQ-28", "xs:duration(\"P1Y12M0D\") eq xs:yearMonthDuration(\"P1Y12M\")", "T"),
        ("op-duration-equal/K-DurationEQ-29", "xs:yearMonthDuration(\"P1M\") ne xs:duration(\"P31D\")", "T"),
        ("op-duration-equal/K-DurationEQ-30", "xs:duration(\"P31D\") ne xs:yearMonthDuration(\"P1M\")", "T"),
        ("op-duration-equal/K-DurationEQ-31", "xs:yearMonthDuration(\"P1M\") ne xs:dayTimeDuration(\"P31D\")", "T"),
        ("op-duration-equal/K-DurationEQ-32", "xs:dayTimeDuration(\"P31D\") ne xs:yearMonthDuration(\"P1M\")", "T"),
        (
            "op-duration-equal/K-DurationEQ-33",
            "xs:duration(\"P1999Y10M3DT08H34M12.143S\") ge xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-34",
            "xs:duration(\"P1999Y10M3DT08H34M12.143S\") gt xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-35",
            "xs:duration(\"P1999Y10M3DT08H34M12.143S\") lt xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-36",
            "xs:duration(\"P1999Y10M3DT08H34M12.143S\") le xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-37",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") ge xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-38",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") gt xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-39",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") lt xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-40",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") le xs:duration(\"P1999Y10M3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-41",
            "xs:yearMonthDuration(\"P1999Y10M\") ge xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-42",
            "xs:yearMonthDuration(\"P1999Y10M\") gt xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-43",
            "xs:yearMonthDuration(\"P1999Y10M\") lt xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-44",
            "xs:yearMonthDuration(\"P1999Y10M\") le xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-45",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") ge xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-46",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") gt xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-47",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") lt xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-48",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") le xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-49",
            "xs:yearMonthDuration(\"P1999Y10M\") ge xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-50",
            "xs:yearMonthDuration(\"P1999Y10M\") gt xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-51",
            "xs:yearMonthDuration(\"P1999Y10M\") lt xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-52",
            "xs:yearMonthDuration(\"P1999Y10M\") le xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-53",
            "xs:yearMonthDuration(\"P1999Y10M\") ge xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-54",
            "xs:yearMonthDuration(\"P1999Y10M\") gt xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-55",
            "xs:yearMonthDuration(\"P1999Y10M\") lt xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-56",
            "xs:yearMonthDuration(\"P1999Y10M\") le xs:duration(\"P3DT08H34M12.143S\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-57",
            "xs:duration(\"P3DT08H34M12.143S\") ge xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-58",
            "xs:duration(\"P3DT08H34M12.143S\") gt xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-59",
            "xs:duration(\"P3DT08H34M12.143S\") lt xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DurationEQ-60",
            "xs:duration(\"P3DT08H34M12.143S\") le xs:yearMonthDuration(\"P1999Y10M\")",
            "X:XPTY0004"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-1",
            "xs:dayTimeDuration(\"P3DT08H34M12.143S\") eq xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-2",
            "xs:dayTimeDuration(\"PT0S\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-3",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.043S\") eq xs:dayTimeDuration(\"P3DT08H34M12.143S\"))",
            "T"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-4",
            "xs:dayTimeDuration(\"PT08H34M12.143S\") ne xs:dayTimeDuration(\"P3DT08H34M12.143S\")",
            "T"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-5",
            "not(xs:dayTimeDuration(\"P3DT08H34M12.143S\") ne xs:dayTimeDuration(\"P3DT08H34M12.143S\"))",
            "T"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-6",
            "xs:dayTimeDuration(\"-PT0S\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-7",
            "xs:dayTimeDuration(\"-P2DT5H\") ne xs:dayTimeDuration(\"P2DT5H\")",
            "T"
        ),
        (
            "op-duration-equal/K-DayTimeDurationEQ-8",
            "xs:dayTimeDuration(\"P6DT16H34M59.613999S\") eq xs:dayTimeDuration(\"P6DT16H34M59.613999S\")",
            "T"
        ),
        (
            "op-duration-equal/K-YearMonthDurationEQ-1",
            "xs:yearMonthDuration(\"P1999Y10M\") eq xs:yearMonthDuration(\"P1999Y10M\")",
            "T"
        ),
        (
            "op-duration-equal/K-YearMonthDurationEQ-2",
            "xs:yearMonthDuration(\"P0M\") eq xs:yearMonthDuration(\"P0M\")",
            "T"
        ),
        (
            "op-duration-equal/K-YearMonthDurationEQ-3",
            "not(xs:yearMonthDuration(\"P1999Y\") eq xs:yearMonthDuration(\"P1999Y10M\"))",
            "T"
        ),
        (
            "op-duration-equal/K-YearMonthDurationEQ-4",
            "xs:yearMonthDuration(\"P1999Y01M\") ne xs:yearMonthDuration(\"P1999Y10M\")",
            "T"
        ),
        (
            "op-duration-equal/K-YearMonthDurationEQ-5",
            "not(xs:yearMonthDuration(\"P1999Y10M\") ne xs:yearMonthDuration(\"P1999Y10M\"))",
            "T"
        ),
        (
            "op-duration-equal/K-YearMonthDurationEQ-6",
            "xs:yearMonthDuration(\"-P3Y8M\") ne xs:yearMonthDuration(\"P3Y8M\")",
            "T"
        ),
        (
            "op-duration-equal/K-YearMonthDurationEQ-7",
            "xs:yearMonthDuration(\"-P3Y8M\") ne xs:yearMonthDuration(\"P3Y8M\")",
            "T"
        ),
        ("op-gDay-equal/op-gDay-equal2args-1", "xs:gDay(\"---01Z\") eq xs:gDay(\"---01Z\")", "T"),
        ("op-gDay-equal/op-gDay-equal2args-2", "xs:gDay(\"---14Z\") eq xs:gDay(\"---01Z\")", "F"),
        ("op-gDay-equal/op-gDay-equal2args-3", "xs:gDay(\"---31Z\") eq xs:gDay(\"---01Z\")", "F"),
        ("op-gDay-equal/op-gDay-equal2args-4", "xs:gDay(\"---01Z\") eq xs:gDay(\"---14Z\")", "F"),
        ("op-gDay-equal/op-gDay-equal2args-5", "xs:gDay(\"---01Z\") eq xs:gDay(\"---31Z\")", "F"),
        ("op-gDay-equal/op-gDay-equal2args-6", "xs:gDay(\"---01Z\") ne xs:gDay(\"---01Z\")", "F"),
        ("op-gDay-equal/op-gDay-equal2args-7", "xs:gDay(\"---14Z\") ne xs:gDay(\"---01Z\")", "T"),
        ("op-gDay-equal/op-gDay-equal2args-8", "xs:gDay(\"---31Z\") ne xs:gDay(\"---01Z\")", "T"),
        ("op-gDay-equal/op-gDay-equal2args-9", "xs:gDay(\"---01Z\") ne xs:gDay(\"---14Z\")", "T"),
        ("op-gDay-equal/op-gDay-equal2args-10", "xs:gDay(\"---01Z\") ne xs:gDay(\"---31Z\")", "T"),
        ("op-gDay-equal/op-gDay-equal-2", "(xs:gDay(\"---12-05:00\") eq xs:gDay(\"---12Z\"))", "F"),
        ("op-gDay-equal/op-gDay-equal-3", "fn:not((xs:gDay(\"---12Z\") eq xs:gDay(\"---12Z\")))", "F"),
        ("op-gDay-equal/op-gDay-equal-4", "fn:not(xs:gDay(\"---05Z\") ne xs:gDay(\"---06Z\"))", "F"),
        ("op-gDay-equal/op-gDay-equal-5", "fn:not(xs:gDay(\"---11Z\") eq xs:gDay(\"---10Z\"))", "T"),
        ("op-gDay-equal/op-gDay-equal-6", "fn:not(xs:gDay(\"---05Z\") ne xs:gDay(\"---05Z\"))", "T"),
        (
            "op-gDay-equal/op-gDay-equal-7",
            "(xs:gDay(\"---04Z\") eq xs:gDay(\"---02Z\")) and (xs:gDay(\"---01Z\") eq xs:gDay(\"---12Z\"))",
            "F"
        ),
        (
            "op-gDay-equal/op-gDay-equal-8",
            "(xs:gDay(\"---12Z\") ne xs:gDay(\"---03Z\")) and (xs:gDay(\"---05Z\") ne xs:gDay(\"---08Z\"))",
            "T"
        ),
        (
            "op-gDay-equal/op-gDay-equal-9",
            "(xs:gDay(\"---02Z\") eq xs:gDay(\"---02Z\")) or (xs:gDay(\"---06Z\") eq xs:gDay(\"---06Z\"))",
            "T"
        ),
        (
            "op-gDay-equal/op-gDay-equal-10",
            "(xs:gDay(\"---06Z\") ne xs:gDay(\"---06Z\")) or (xs:gDay(\"---08Z\") ne xs:gDay(\"---09Z\"))",
            "T"
        ),
        ("op-gDay-equal/op-gDay-equal-11", "(xs:gDay(\"---03Z\") eq xs:gDay(\"---01Z\")) or (fn:true())", "T"),
        ("op-gDay-equal/op-gDay-equal-12", "(xs:gDay(\"---08Z\") ne xs:gDay(\"---07Z\")) or (fn:true())", "T"),
        ("op-gDay-equal/op-gDay-equal-13", "(xs:gDay(\"---05Z\") eq xs:gDay(\"---05Z\")) or (fn:false())", "T"),
        ("op-gDay-equal/op-gDay-equal-14", "(xs:gDay(\"---09Z\") ne xs:gDay(\"---09Z\")) or (fn:false())", "F"),
        ("op-gDay-equal/K-gDayEQ-1", "xs:gDay(\" ---31 \") eq xs:gDay(\"---31\")", "T"),
        ("op-gDay-equal/K-gDayEQ-2", "not(xs:gDay(\"---31\") eq xs:gDay(\"---01\"))", "T"),
        ("op-gDay-equal/K-gDayEQ-3", "xs:gDay(\"---01\") ne xs:gDay(\"---10\")", "T"),
        ("op-gDay-equal/K-gDayEQ-4", "not(xs:gDay(\"---01\") ne xs:gDay(\"---01\"))", "T"),
        ("op-gDay-equal/K-gDayEQ-5", "xs:gDay(\"---01-00:00\") eq xs:gDay(\"---01Z\")", "T"),
        ("op-gDay-equal/K-gDayEQ-6", "xs:gDay(\"---01+00:00\") eq xs:gDay(\"---01Z\")", "T"),
        ("op-gDay-equal/K-gDayEQ-7", "xs:gDay(\"---01Z\") eq xs:gDay(\"---01Z\")", "T"),
        ("op-gDay-equal/K-gDayEQ-8", "xs:gDay(\"---01-00:00\") eq xs:gDay(\"---01+00:00\")", "T"),
        (
            "op-gDay-equal/cbcl-gDay-equal-002",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gDay(\"---30\") eq xs:gDay(\"---31+09:01\") else xs:gDay(\"---30\") eq xs:gDay(\"---31+09:00\")",
            "F"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-003",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gDay(\"---31+09:01\") eq xs:gDay(\"---30\") else xs:gDay(\"---31+09:00\") eq xs:gDay(\"---30\")",
            "F"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-004",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gDay(\"---30\") eq xs:gDay(\"---31-09:01\") else xs:gDay(\"---30\") eq xs:gDay(\"---31-09:00\")",
            "F"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-005",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gDay(\"---31-09:01\") eq xs:gDay(\"---30\") else xs:gDay(\"---31-09:00\") eq xs:gDay(\"---30\")",
            "F"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-007",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gDay(\"---30\") ne xs:gDay(\"---31+09:01\") else xs:gDay(\"---30\") ne xs:gDay(\"---31+09:00\")",
            "T"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-008",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gDay(\"---31+09:01\") ne xs:gDay(\"---30\") else xs:gDay(\"---31+09:00\") ne xs:gDay(\"---30\")",
            "T"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-009",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gDay(\"---30\") ne xs:gDay(\"---31-09:01\") else xs:gDay(\"---30\") ne xs:gDay(\"---31-09:00\")",
            "T"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-010",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gDay(\"---31-09:01\") ne xs:gDay(\"---30\") else xs:gDay(\"---31-09:00\") ne xs:gDay(\"---30\")",
            "T"
        ),
        ("op-gDay-equal/cbcl-gDay-equal-015", "xs:gDay(\"---30-12:00\") eq xs:gDay(\"---31+12:00\")", "T"),
        ("op-gDay-equal/cbcl-gDay-equal-016", "xs:gDay(\"---31+12:00\") eq xs:gDay(\"---30-12:00\")", "T"),
        ("op-gDay-equal/cbcl-gDay-equal-017", "xs:gDay(\"---30-12:00\") ne xs:gDay(\"---31+12:00\")", "F"),
        ("op-gDay-equal/cbcl-gDay-equal-018", "xs:gDay(\"---31+12:00\") ne xs:gDay(\"---30-12:00\")", "F"),
        (
            "op-gDay-equal/cbcl-gDay-equal-019",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT1H')) then xs:gDay(\"---31+01:01\") eq xs:gDay(\"---31\") else xs:gDay(\"---31+01:00\") eq xs:gDay(\"---31\")",
            "F"
        ),
        (
            "op-gDay-equal/cbcl-gDay-equal-020",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT1H')) then xs:gDay(\"---31\") eq xs:gDay(\"---31+01:01\") else xs:gDay(\"---31\") eq xs:gDay(\"---31+01:00\")",
            "F"
        ),
        ("op-gMonth-equal/op-gMonth-equal2args-1", "xs:gMonth(\"--01Z\") eq xs:gMonth(\"--01Z\")", "T"),
        ("op-gMonth-equal/op-gMonth-equal2args-2", "xs:gMonth(\"--07Z\") eq xs:gMonth(\"--01Z\")", "F"),
        ("op-gMonth-equal/op-gMonth-equal2args-3", "xs:gMonth(\"--12Z\") eq xs:gMonth(\"--01Z\")", "F"),
        ("op-gMonth-equal/op-gMonth-equal2args-4", "xs:gMonth(\"--01Z\") eq xs:gMonth(\"--07Z\")", "F"),
        ("op-gMonth-equal/op-gMonth-equal2args-5", "xs:gMonth(\"--01Z\") eq xs:gMonth(\"--12Z\")", "F"),
        ("op-gMonth-equal/op-gMonth-equal2args-6", "xs:gMonth(\"--01Z\") ne xs:gMonth(\"--01Z\")", "F"),
        ("op-gMonth-equal/op-gMonth-equal2args-7", "xs:gMonth(\"--07Z\") ne xs:gMonth(\"--01Z\")", "T"),
        ("op-gMonth-equal/op-gMonth-equal2args-8", "xs:gMonth(\"--12Z\") ne xs:gMonth(\"--01Z\")", "T"),
        ("op-gMonth-equal/op-gMonth-equal2args-9", "xs:gMonth(\"--01Z\") ne xs:gMonth(\"--07Z\")", "T"),
        ("op-gMonth-equal/op-gMonth-equal2args-10", "xs:gMonth(\"--01Z\") ne xs:gMonth(\"--12Z\")", "T"),
        ("op-gMonth-equal/op-gMonth-equal-2", "(xs:gMonth(\"--12-05:00\") eq xs:gMonth(\"--12Z\"))", "F"),
        ("op-gMonth-equal/op-gMonth-equal-3", "fn:not((xs:gMonth(\"--12Z\") eq xs:gMonth(\"--12Z\")))", "F"),
        ("op-gMonth-equal/op-gMonth-equal-4", "fn:not(xs:gMonth(\"--05Z\") ne xs:gMonth(\"--06Z\"))", "F"),
        ("op-gMonth-equal/op-gMonth-equal-5", "fn:not(xs:gMonth(\"--11Z\") eq xs:gMonth(\"--10Z\"))", "T"),
        ("op-gMonth-equal/op-gMonth-equal-6", "fn:not(xs:gMonth(\"--05Z\") ne xs:gMonth(\"--05Z\"))", "T"),
        (
            "op-gMonth-equal/op-gMonth-equal-7",
            "(xs:gMonth(\"--04Z\") eq xs:gMonth(\"--02Z\")) and (xs:gMonth(\"--01Z\") eq xs:gMonth(\"--12Z\"))",
            "F"
        ),
        (
            "op-gMonth-equal/op-gMonth-equal-8",
            "(xs:gMonth(\"--12Z\") ne xs:gMonth(\"--03Z\")) and (xs:gMonth(\"--05Z\") ne xs:gMonth(\"--08Z\"))",
            "T"
        ),
        (
            "op-gMonth-equal/op-gMonth-equal-9",
            "(xs:gMonth(\"--02Z\") eq xs:gMonth(\"--02Z\")) or (xs:gMonth(\"--06Z\") eq xs:gMonth(\"--06Z\"))",
            "T"
        ),
        (
            "op-gMonth-equal/op-gMonth-equal-10",
            "(xs:gMonth(\"--06Z\") ne xs:gMonth(\"--06Z\")) or (xs:gMonth(\"--08Z\") ne xs:gMonth(\"--09Z\"))",
            "T"
        ),
        ("op-gMonth-equal/op-gMonth-equal-11", "(xs:gMonth(\"--03Z\") eq xs:gMonth(\"--01Z\")) or (fn:true())", "T"),
        ("op-gMonth-equal/op-gMonth-equal-12", "(xs:gMonth(\"--08Z\") ne xs:gMonth(\"--07Z\")) or (fn:true())", "T"),
        ("op-gMonth-equal/op-gMonth-equal-13", "(xs:gMonth(\"--05Z\") eq xs:gMonth(\"--05Z\")) or (fn:false())", "T"),
        ("op-gMonth-equal/op-gMonth-equal-14", "(xs:gMonth(\"--09Z\") ne xs:gMonth(\"--09Z\")) or (fn:false())", "F"),
        ("op-gMonth-equal/K-gMonthEQ-1", "xs:gMonth(\"--11 \") eq xs:gMonth(\"--11\")", "T"),
        ("op-gMonth-equal/K-gMonthEQ-2", "not(xs:gMonth(\"--11\") eq xs:gMonth(\"--01\"))", "T"),
        ("op-gMonth-equal/K-gMonthEQ-3", "xs:gMonth(\"--12\") ne xs:gMonth(\"--10\")", "T"),
        ("op-gMonth-equal/K-gMonthEQ-4", "not(xs:gMonth(\"--03\") ne xs:gMonth(\"--03\"))", "T"),
        ("op-gMonth-equal/K-gMonthEQ-5", "xs:gMonth(\"--01-00:00\") eq xs:gMonth(\"--01Z\")", "T"),
        ("op-gMonth-equal/K-gMonthEQ-6", "xs:gMonth(\"--01+00:00\") eq xs:gMonth(\"--01Z\")", "T"),
        ("op-gMonth-equal/K-gMonthEQ-7", "xs:gMonth(\"--01Z\") eq xs:gMonth(\"--01Z\")", "T"),
        ("op-gMonth-equal/K-gMonthEQ-8", "xs:gMonth(\"--01-00:00\") eq xs:gMonth(\"--01+00:00\")", "T"),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-002",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonth(\"--06\") eq xs:gMonth(\"--06+09:01\") else xs:gMonth(\"--06\") eq xs:gMonth(\"--06+09:00\")",
            "F"
        ),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-003",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonth(\"--06+09:01\") eq xs:gMonth(\"--06\") else xs:gMonth(\"--06+09:00\") eq xs:gMonth(\"--06\")",
            "F"
        ),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-004",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonth(\"--06\") eq xs:gMonth(\"--06-09:01\") else xs:gMonth(\"--06\") eq xs:gMonth(\"--06-09:00\")",
            "F"
        ),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-005",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonth(\"--06-09:01\") eq xs:gMonth(\"--06\") else xs:gMonth(\"--06-09:00\") eq xs:gMonth(\"--06\")",
            "F"
        ),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-007",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonth(\"--06\") ne xs:gMonth(\"--06+09:01\") else xs:gMonth(\"--06\") ne xs:gMonth(\"--06+09:00\")",
            "T"
        ),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-008",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonth(\"--06+09:01\") ne xs:gMonth(\"--06\") else xs:gMonth(\"--06+09:00\") ne xs:gMonth(\"--06\")",
            "T"
        ),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-009",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonth(\"--06\") ne xs:gMonth(\"--06-09:01\") else xs:gMonth(\"--06\") ne xs:gMonth(\"--06-09:00\")",
            "T"
        ),
        (
            "op-gMonth-equal/cbcl-gMonth-equal-010",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonth(\"--06-09:01\") ne xs:gMonth(\"--06\") else xs:gMonth(\"--06-09:00\") ne xs:gMonth(\"--06\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-1",
            "xs:gMonthDay(\"--01-01Z\") eq xs:gMonthDay(\"--01-01Z\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-2",
            "xs:gMonthDay(\"--04-10Z\") eq xs:gMonthDay(\"--01-01Z\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-3",
            "xs:gMonthDay(\"--12-31Z\") eq xs:gMonthDay(\"--01-01Z\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-4",
            "xs:gMonthDay(\"--01-01Z\") eq xs:gMonthDay(\"--04-10Z\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-5",
            "xs:gMonthDay(\"--01-01Z\") eq xs:gMonthDay(\"--12-31Z\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-6",
            "xs:gMonthDay(\"--01-01Z\") ne xs:gMonthDay(\"--01-01Z\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-7",
            "xs:gMonthDay(\"--04-10Z\") ne xs:gMonthDay(\"--01-01Z\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-8",
            "xs:gMonthDay(\"--12-31Z\") ne xs:gMonthDay(\"--01-01Z\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-9",
            "xs:gMonthDay(\"--01-01Z\") ne xs:gMonthDay(\"--04-10Z\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal2args-10",
            "xs:gMonthDay(\"--01-01Z\") ne xs:gMonthDay(\"--12-31Z\")",
            "T"
        ),
        ("op-gMonthDay-equal/op-gMonthDay-equal-2", "(xs:gMonthDay(\"--12-25Z\") eq xs:gMonthDay(\"--12-26Z\"))", "F"),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-3",
            "fn:not((xs:gMonthDay(\"--12-10Z\") eq xs:gMonthDay(\"--12-10Z\")))",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-4",
            "fn:not(xs:gMonthDay(\"--05-01Z\") ne xs:gMonthDay(\"--06-12Z\"))",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-5",
            "fn:not(xs:gMonthDay(\"--11-10Z\") eq xs:gMonthDay(\"--10-02Z\"))",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-6",
            "fn:not(xs:gMonthDay(\"--05-10Z\") ne xs:gMonthDay(\"--05-10Z\"))",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-7",
            "(xs:gMonthDay(\"--04-02Z\") eq xs:gMonthDay(\"--02-03Z\")) and (xs:gMonthDay(\"--01-07Z\") eq xs:gMonthDay(\"--12-10Z\"))",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-8",
            "(xs:gMonthDay(\"--12-10Z\") ne xs:gMonthDay(\"--03-11Z\")) and (xs:gMonthDay(\"--05-12Z\") ne xs:gMonthDay(\"--08-08Z\"))",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-9",
            "(xs:gMonthDay(\"--02-01Z\") eq xs:gMonthDay(\"--02-10Z\")) or (xs:gMonthDay(\"--06-02Z\") eq xs:gMonthDay(\"--06-03Z\"))",
            "F"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-10",
            "(xs:gMonthDay(\"--06-02Z\") ne xs:gMonthDay(\"--06-09Z\")) or (xs:gMonthDay(\"--08-06Z\") ne xs:gMonthDay(\"--09-01Z\"))",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-11",
            "(xs:gMonthDay(\"--03-04Z\") eq xs:gMonthDay(\"--01-07Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-12",
            "(xs:gMonthDay(\"--08-04Z\") ne xs:gMonthDay(\"--07-12Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-13",
            "(xs:gMonthDay(\"--05-05Z\") eq xs:gMonthDay(\"--05-05Z\")) or (fn:false())",
            "T"
        ),
        (
            "op-gMonthDay-equal/op-gMonthDay-equal-14",
            "(xs:gMonthDay(\"--09-09Z\") ne xs:gMonthDay(\"--09-10Z\")) or (fn:false())",
            "T"
        ),
        ("op-gMonthDay-equal/K-gMonthDayEQ-1", "xs:gMonthDay(\"--04-01 \") eq xs:gMonthDay(\"--04-01\")", "T"),
        ("op-gMonthDay-equal/K-gMonthDayEQ-2", "not(xs:gMonthDay(\"--03-03\") eq xs:gMonthDay(\"--04-03\"))", "T"),
        ("op-gMonthDay-equal/K-gMonthDayEQ-3", "xs:gMonthDay(\"--04-12\") ne xs:gMonthDay(\"--04-11\")", "T"),
        ("op-gMonthDay-equal/K-gMonthDayEQ-4", "not(xs:gMonthDay(\"--04-11\") ne xs:gMonthDay(\"--04-11\"))", "T"),
        ("op-gMonthDay-equal/K-gMonthDayEQ-5", "xs:gMonthDay(\"--03-01-00:00\") eq xs:gMonthDay(\"--03-01Z\")", "T"),
        ("op-gMonthDay-equal/K-gMonthDayEQ-6", "xs:gMonthDay(\"--03-01+00:00\") eq xs:gMonthDay(\"--03-01Z\")", "T"),
        ("op-gMonthDay-equal/K-gMonthDayEQ-7", "xs:gMonthDay(\"--03-01Z\") eq xs:gMonthDay(\"--03-01Z\")", "T"),
        (
            "op-gMonthDay-equal/K-gMonthDayEQ-8",
            "xs:gMonthDay(\"--03-01-00:00\") eq xs:gMonthDay(\"--03-01+00:00\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-002",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonthDay(\"--12-30\") eq xs:gMonthDay(\"--12-31+09:01\") else xs:gMonthDay(\"--12-30\") eq xs:gMonthDay(\"--12-31+09:00\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-003",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonthDay(\"--12-31+09:01\") eq xs:gMonthDay(\"--12-30\") else xs:gMonthDay(\"--12-31+09:00\") eq xs:gMonthDay(\"--12-30\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-004",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonthDay(\"--12-30\") eq xs:gMonthDay(\"--12-31-09:01\") else xs:gMonthDay(\"--12-30\") eq xs:gMonthDay(\"--12-31-09:00\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-005",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonthDay(\"--12-31-09:01\") eq xs:gMonthDay(\"--12-30\") else xs:gMonthDay(\"--12-31-09:00\") eq xs:gMonthDay(\"--12-30\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-007",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonthDay(\"--12-30\") ne xs:gMonthDay(\"--12-31+09:01\") else xs:gMonthDay(\"--12-30\") ne xs:gMonthDay(\"--12-31+09:00\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-008",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gMonthDay(\"--12-31+09:01\") ne xs:gMonthDay(\"--12-30\") else xs:gMonthDay(\"--12-31+09:00\") ne xs:gMonthDay(\"--12-30\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-009",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonthDay(\"--12-30\") ne xs:gMonthDay(\"--12-31-09:01\") else xs:gMonthDay(\"--12-30\") ne xs:gMonthDay(\"--12-31-09:00\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-010",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gMonthDay(\"--12-31-09:01\") ne xs:gMonthDay(\"--12-30\") else xs:gMonthDay(\"--12-31-09:00\") ne xs:gMonthDay(\"--12-30\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-015",
            "xs:gMonthDay(\"--01-30-12:00\") eq xs:gMonthDay(\"--01-31+12:00\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-016",
            "xs:gMonthDay(\"--01-31+12:00\") eq xs:gMonthDay(\"--01-30-12:00\")",
            "T"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-017",
            "xs:gMonthDay(\"--01-30-12:00\") ne xs:gMonthDay(\"--01-31+12:00\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-018",
            "xs:gMonthDay(\"--01-31+12:00\") ne xs:gMonthDay(\"--01-30-12:00\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-019",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT1M')) then xs:gMonthDay(\"--12-30\") eq xs:gMonthDay(\"--12-31+01:01\") else xs:gMonthDay(\"--12-30\") eq xs:gMonthDay(\"--12-31+01:00\")",
            "F"
        ),
        (
            "op-gMonthDay-equal/cbcl-gMonthDay-equal-020",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT1M')) then xs:gMonthDay(\"--12-31+01:01\") eq xs:gMonthDay(\"--12-30\") else xs:gMonthDay(\"--12-31+01:00\") eq xs:gMonthDay(\"--12-30\")",
            "F"
        ),
        ("op-gYear-equal/op-gYear-equal2args-1", "xs:gYear(\"1970Z\") eq xs:gYear(\"1970Z\")", "T"),
        ("op-gYear-equal/op-gYear-equal2args-2", "xs:gYear(\"2012Z\") eq xs:gYear(\"1970Z\")", "F"),
        ("op-gYear-equal/op-gYear-equal2args-3", "xs:gYear(\"2030Z\") eq xs:gYear(\"1970Z\")", "F"),
        ("op-gYear-equal/op-gYear-equal2args-4", "xs:gYear(\"1970Z\") eq xs:gYear(\"2012Z\")", "F"),
        ("op-gYear-equal/op-gYear-equal2args-5", "xs:gYear(\"1970Z\") eq xs:gYear(\"2030Z\")", "F"),
        ("op-gYear-equal/op-gYear-equal2args-6", "xs:gYear(\"1970Z\") ne xs:gYear(\"1970Z\")", "F"),
        ("op-gYear-equal/op-gYear-equal2args-7", "xs:gYear(\"2012Z\") ne xs:gYear(\"1970Z\")", "T"),
        ("op-gYear-equal/op-gYear-equal2args-8", "xs:gYear(\"2030Z\") ne xs:gYear(\"1970Z\")", "T"),
        ("op-gYear-equal/op-gYear-equal2args-9", "xs:gYear(\"1970Z\") ne xs:gYear(\"2012Z\")", "T"),
        ("op-gYear-equal/op-gYear-equal2args-10", "xs:gYear(\"1970Z\") ne xs:gYear(\"2030Z\")", "T"),
        ("op-gYear-equal/op-gYear-equal-1", "(xs:gYear(\"2005-12:00\") eq xs:gYear(\"2005+12:00\"))", "F"),
        ("op-gYear-equal/op-gYear-equal-2", "(xs:gYear(\"1976-05:00\") eq xs:gYear(\"1976-05:00\"))", "T"),
        ("op-gYear-equal/op-gYear-equalNew-3", "fn:not((xs:gYear(\"1995Z\") eq xs:gYear(\"1995Z\")))", "F"),
        ("op-gYear-equal/op-gYear-equalNew-4", "fn:not(xs:gYear(\"2005Z\") ne xs:gYear(\"2006Z\"))", "F"),
        ("op-gYear-equal/op-gYear-equal-5", "fn:not(xs:gYear(\"2000Z\") eq xs:gYear(\"2001Z\"))", "T"),
        ("op-gYear-equal/op-gYear-equalNew-6", "fn:not(xs:gYear(\"2005Z\") ne xs:gYear(\"2005Z\"))", "T"),
        (
            "op-gYear-equal/op-gYear-equalNew-7",
            "(xs:gYear(\"2000Z\") eq xs:gYear(\"2000Z\")) and (xs:gYear(\"2001Z\") eq xs:gYear(\"2001Z\"))",
            "T"
        ),
        (
            "op-gYear-equal/op-gYear-equal-8",
            "(xs:gYear(\"2000Z\") ne xs:gYear(\"2000Z\")) and (xs:gYear(\"1975Z\") ne xs:gYear(\"1975Z\"))",
            "F"
        ),
        (
            "op-gYear-equal/op-gYear-equalNew-9",
            "(xs:gYear(\"2000Z\") eq xs:gYear(\"2000Z\")) or (xs:gYear(\"1976Z\") eq xs:gYear(\"1976Z\"))",
            "T"
        ),
        (
            "op-gYear-equal/op-gYear-equal-10",
            "(xs:gYear(\"1976Z\") ne xs:gYear(\"1976Z\")) or (xs:gYear(\"1980Z\") ne xs:gYear(\"1980Z\"))",
            "F"
        ),
        ("op-gYear-equal/op-gYear-equal-11", "(xs:gYear(\"1980Z\") eq xs:gYear(\"1980Z\")) or (fn:true())", "T"),
        ("op-gYear-equal/op-gYear-equalNew-12", "(xs:gYear(\"2000Z\") ne xs:gYear(\"2000Z\")) or (fn:true())", "T"),
        ("op-gYear-equal/op-gYear-equal-13", "(xs:gYear(\"1980Z\") eq xs:gYear(\"1980Z\")) or (fn:false())", "T"),
        ("op-gYear-equal/op-gYear-equal-14", "(xs:gYear(\"1980Z\") ne xs:gYear(\"1980Z\")) or (fn:false())", "F"),
        ("op-gYear-equal/K-gYearEQ-1", "xs:gYear(\" 1956 \") eq xs:gYear(\"1956\")", "T"),
        ("op-gYear-equal/K-gYearEQ-2", "not(xs:gYear(\"1956\") eq xs:gYear(\"1958\"))", "T"),
        ("op-gYear-equal/K-gYearEQ-3", "xs:gYear(\"1956\") ne xs:gYear(\"1958\")", "T"),
        ("op-gYear-equal/K-gYearEQ-4", "not(xs:gYear(\"1956\") ne xs:gYear(\"1956\"))", "T"),
        ("op-gYear-equal/K-gYearEQ-5", "xs:gYear(\"1956-00:00\") eq xs:gYear(\"1956Z\")", "T"),
        ("op-gYear-equal/K-gYearEQ-6", "xs:gYear(\"1956+00:00\") eq xs:gYear(\"1956Z\")", "T"),
        ("op-gYear-equal/K-gYearEQ-7", "xs:gYear(\"1956Z\") eq xs:gYear(\"1956Z\")", "T"),
        ("op-gYear-equal/K-gYearEQ-8", "xs:gYear(\"1956-00:00\") eq xs:gYear(\"1956+00:00\")", "T"),
        (
            "op-gYear-equal/cbcl-gYear-equal-002",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYear(\"2008\") eq xs:gYear(\"2008+09:01\") else xs:gYear(\"2008\") eq xs:gYear(\"2008+09:00\")",
            "F"
        ),
        (
            "op-gYear-equal/cbcl-gYear-equal-003",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYear(\"2008+09:01\") eq xs:gYear(\"2008\") else xs:gYear(\"2008+09:00\") eq xs:gYear(\"2008\")",
            "F"
        ),
        (
            "op-gYear-equal/cbcl-gYear-equal-004",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYear(\"2008\") eq xs:gYear(\"2008-09:01\") else xs:gYear(\"2008\") eq xs:gYear(\"2008-09:00\")",
            "F"
        ),
        (
            "op-gYear-equal/cbcl-gYear-equal-005",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYear(\"2008-09:01\") eq xs:gYear(\"2008\") else xs:gYear(\"2008-09:00\") eq xs:gYear(\"2008\")",
            "F"
        ),
        (
            "op-gYear-equal/cbcl-gYear-equal-007",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYear(\"2008\") ne xs:gYear(\"2008+09:01\") else xs:gYear(\"2008\") ne xs:gYear(\"2008+09:00\")",
            "T"
        ),
        (
            "op-gYear-equal/cbcl-gYear-equal-008",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYear(\"2008+09:01\") ne xs:gYear(\"2008\") else xs:gYear(\"2008+09:00\") ne xs:gYear(\"2008\")",
            "T"
        ),
        (
            "op-gYear-equal/cbcl-gYear-equal-009",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYear(\"2008\") ne xs:gYear(\"2008-09:01\") else xs:gYear(\"2008\") ne xs:gYear(\"2008-09:00\")",
            "T"
        ),
        (
            "op-gYear-equal/cbcl-gYear-equal-010",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYear(\"2008-09:01\") ne xs:gYear(\"2008\") else xs:gYear(\"2008-09:00\") ne xs:gYear(\"2008\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-1",
            "xs:gYearMonth(\"1970-01Z\") eq xs:gYearMonth(\"1970-01Z\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-2",
            "xs:gYearMonth(\"1984-12Z\") eq xs:gYearMonth(\"1970-01Z\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-3",
            "xs:gYearMonth(\"2030-12Z\") eq xs:gYearMonth(\"1970-01Z\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-4",
            "xs:gYearMonth(\"1970-01Z\") eq xs:gYearMonth(\"1984-12Z\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-5",
            "xs:gYearMonth(\"1970-01Z\") eq xs:gYearMonth(\"2030-12Z\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-6",
            "xs:gYearMonth(\"1970-01Z\") ne xs:gYearMonth(\"1970-01Z\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-7",
            "xs:gYearMonth(\"1984-12Z\") ne xs:gYearMonth(\"1970-01Z\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-8",
            "xs:gYearMonth(\"2030-12Z\") ne xs:gYearMonth(\"1970-01Z\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-9",
            "xs:gYearMonth(\"1970-01Z\") ne xs:gYearMonth(\"1984-12Z\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equal2args-10",
            "xs:gYearMonth(\"1970-01Z\") ne xs:gYearMonth(\"2030-12Z\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-1",
            "(xs:gYearMonth(\"1976-02-05:00\") eq xs:gYearMonth(\"1976-03Z\"))",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-2",
            "(xs:gYearMonth(\"1976-03-05:00\") eq xs:gYearMonth(\"1976-03Z\"))",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-3",
            "fn:not((xs:gYearMonth(\"1995-02Z\") eq xs:gYearMonth(\"1995-02Z\")))",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-4",
            "fn:not(xs:gYearMonth(\"2005-02Z\") ne xs:gYearMonth(\"2006-03Z\"))",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-5",
            "fn:not(xs:gYearMonth(\"2000-01Z\") eq xs:gYearMonth(\"2001-04Z\"))",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-6",
            "fn:not(xs:gYearMonth(\"2005-01Z\") ne xs:gYearMonth(\"2005-01Z\"))",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-7",
            "(xs:gYearMonth(\"2000-02Z\") eq xs:gYearMonth(\"2000-03Z\")) and (xs:gYearMonth(\"2001-01Z\") eq xs:gYearMonth(\"2001-01Z\"))",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-8",
            "(xs:gYearMonth(\"2000-01Z\") ne xs:gYearMonth(\"2000-01Z\")) and (xs:gYearMonth(\"1975-01Z\") ne xs:gYearMonth(\"1975-03Z\"))",
            "F"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-9",
            "(xs:gYearMonth(\"2000-01Z\") eq xs:gYearMonth(\"2000-03Z\")) or (xs:gYearMonth(\"1976-06Z\") eq xs:gYearMonth(\"1976-06Z\"))",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-10",
            "(xs:gYearMonth(\"1976-01Z\") ne xs:gYearMonth(\"1976-02Z\")) or (xs:gYearMonth(\"1980-03Z\") ne xs:gYearMonth(\"1980-04Z\"))",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-11",
            "(xs:gYearMonth(\"1980-05Z\") eq xs:gYearMonth(\"1980-05Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-12",
            "(xs:gYearMonth(\"2000-06Z\") ne xs:gYearMonth(\"2000-07Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-13",
            "(xs:gYearMonth(\"1980-09Z\") eq xs:gYearMonth(\"1980-09Z\")) or (fn:false())",
            "T"
        ),
        (
            "op-gYearMonth-equal/op-gYearMonth-equalNew-14",
            "(xs:gYearMonth(\"1980-03Z\") ne xs:gYearMonth(\"1980-03Z\")) or (fn:false())",
            "F"
        ),
        ("op-gYearMonth-equal/K-gYearMonthEQ-1", "xs:gYearMonth(\"2001-01 \") eq xs:gYearMonth(\"2001-01\")", "T"),
        ("op-gYearMonth-equal/K-gYearMonthEQ-2", "not(xs:gYearMonth(\"2001-03\") eq xs:gYearMonth(\"2000-03\"))", "T"),
        ("op-gYearMonth-equal/K-gYearMonthEQ-3", "xs:gYearMonth(\"2001-12\") ne xs:gYearMonth(\"2001-11\")", "T"),
        ("op-gYearMonth-equal/K-gYearMonthEQ-4", "not(xs:gYearMonth(\"1995-11\") ne xs:gYearMonth(\"1995-11\"))", "T"),
        (
            "op-gYearMonth-equal/K-gYearMonthEQ-5",
            "xs:gYearMonth(\"1999-01-00:00\") eq xs:gYearMonth(\"1999-01Z\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/K-gYearMonthEQ-6",
            "xs:gYearMonth(\"1999-01+00:00\") eq xs:gYearMonth(\"1999-01Z\")",
            "T"
        ),
        ("op-gYearMonth-equal/K-gYearMonthEQ-7", "xs:gYearMonth(\"1999-01Z\") eq xs:gYearMonth(\"1999-01Z\")", "T"),
        (
            "op-gYearMonth-equal/K-gYearMonthEQ-8",
            "xs:gYearMonth(\"1999-01-00:00\") eq xs:gYearMonth(\"1999-01+00:00\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-002",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYearMonth(\"2008-01\") eq xs:gYearMonth(\"2008-01+09:01\") else xs:gYearMonth(\"2008-01\") eq xs:gYearMonth(\"2008-01+09:00\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-003",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYearMonth(\"2008-01+09:01\") eq xs:gYearMonth(\"2008-01\") else xs:gYearMonth(\"2008-01+09:00\") eq xs:gYearMonth(\"2008-01\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-004",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYearMonth(\"2008-01\") eq xs:gYearMonth(\"2008-01-09:01\") else xs:gYearMonth(\"2008-01\") eq xs:gYearMonth(\"2008-01-09:00\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-005",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYearMonth(\"2008-01-09:01\") eq xs:gYearMonth(\"2008-01\") else xs:gYearMonth(\"2008-01-09:00\") eq xs:gYearMonth(\"2008-01\")",
            "F"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-007",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYearMonth(\"2008-01\") ne xs:gYearMonth(\"2008-01+09:01\") else xs:gYearMonth(\"2008-01\") ne xs:gYearMonth(\"2008-01+09:00\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-008",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT9M')) then xs:gYearMonth(\"2008-01+09:01\") ne xs:gYearMonth(\"2008-01\") else xs:gYearMonth(\"2008-01+09:00\") ne xs:gYearMonth(\"2008-01\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-009",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYearMonth(\"2008-01\") ne xs:gYearMonth(\"2008-01-09:01\") else xs:gYearMonth(\"2008-01\") ne xs:gYearMonth(\"2008-01-09:00\")",
            "T"
        ),
        (
            "op-gYearMonth-equal/cbcl-gYearMonth-equal-010",
            "if (implicit-timezone() eq xs:dayTimeDuration('-PT9M')) then xs:gYearMonth(\"2008-01-09:01\") ne xs:gYearMonth(\"2008-01\") else xs:gYearMonth(\"2008-01-09:00\") ne xs:gYearMonth(\"2008-01\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") * xs:double(\"-1.7976931348623157E308\")",
            "S:PT0S"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration2args-2",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") * xs:double(\"0\")",
            "S:PT0S"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration2args-3",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") * xs:double(\"0\")",
            "S:PT0S"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") * xs:double(\"0\")",
            "S:PT0S"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") * xs:double(\"1.7976931348623157E308\")",
            "S:PT0S"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-1",
            "xs:dayTimeDuration(\"PT2H10M\") * 2.1",
            "S:PT4H33M"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-2",
            "fn:string((xs:dayTimeDuration(\"P10DT10H11M\")) * 2.0) and fn:false()",
            "F"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-3",
            "fn:string((xs:dayTimeDuration(\"P20DT20H10M\") * 2.0)) or fn:false()",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-4",
            "fn:not(fn:string(xs:dayTimeDuration(\"P11DT12H04M\") * 2.0))",
            "F"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-5",
            "fn:boolean(fn:string(xs:dayTimeDuration(\"P05DT09H08M\") *2.0))",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-6",
            "fn:number(xs:dayTimeDuration(\"P02DT06H09M\") *2.0)",
            "S:NaN"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-7",
            "fn:string(xs:dayTimeDuration(\"P03DT04H08M\") * 2.0)",
            "S:P6DT8H16M"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-8",
            "(xs:dayTimeDuration(\"P10DT01H01M\") * -2.0)",
            "S:-P20DT2H2M"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-9",
            "fn:string((xs:dayTimeDuration(\"P01DT02H01M\") * 2.0)) and fn:string((xs:dayTimeDuration(\"P02DT03H03M\") * 2.0 ))",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-10",
            "fn:string((xs:dayTimeDuration(\"P05DT09H02M\") * 2.0)) or fn:string((xs:dayTimeDuration(\"P05DT05H03M\") * 2.0))",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-11",
            "(xs:dayTimeDuration(\"P42DT10H10M\") * 2.0) div (xs:dayTimeDuration(\"P42DT10H10M\") * 2.0)",
            "S:1"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-12",
            "fn:string((xs:dayTimeDuration(\"P10DT08H11M\") * 2.0)) and (fn:true())",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-13",
            "(xs:dayTimeDuration(\"P23DT11H11M\") * 2.0) eq xs:dayTimeDuration(\"P23DT11H11M\")",
            "F"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-14",
            "(xs:dayTimeDuration(\"P21DT08H12M\") * 2.0) ne xs:dayTimeDuration(\"P08DT08H05M\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-15",
            "(xs:dayTimeDuration(\"P10DT10H01M\") * 2.0) le xs:dayTimeDuration(\"P17DT10H02M\")",
            "F"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-16",
            "(xs:dayTimeDuration(\"P13DT09H09M\") * 2.0) ge xs:dayTimeDuration(\"P18DT02H02M\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-17",
            "fn:count(xs:dayTimeDuration(\"P13DT09H09M\") * -0)",
            "S:1"
        ),
        (
            "op-multiply-dayTimeDuration/op-multiply-dayTimeDuration-18",
            "fn:count(xs:dayTimeDuration(\"P13DT09H09M\") * +0)",
            "S:1"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-1",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") * 3 eq xs:dayTimeDuration(\"P9DT12H9M9.3S\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-2",
            "3 * xs:dayTimeDuration(\"P3DT4H3M3.100S\") eq xs:dayTimeDuration(\"P9DT12H9M9.3S\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-3",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") * 0 eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-4",
            "0 * xs:dayTimeDuration(\"P3DT4H3M3.100S\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-5",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") * xs:double(\"-0\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-6",
            "xs:double(\"-0\") * xs:dayTimeDuration(\"P3DT4H3M3.100S\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-7",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") * xs:double(\"INF\")",
            "X:FODT0002"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-8",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") * xs:double(\"-INF\")",
            "X:FODT0002"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-9",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") * xs:double(\"NaN\")",
            "X:FOCA0005"
        ),
        (
            "op-multiply-dayTimeDuration/K-DayTimeDurationMultiply-10",
            "xs:dayTimeDuration('PT0.001S') * 60000 * 60",
            "S:PT1H"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") * xs:double(\"-1.7976931348623157E308\")",
            "S:P0M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") * xs:double(\"-0\")",
            "S:P0M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") * xs:double(\"-0\")",
            "S:P0M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") * xs:double(\"0\")",
            "S:P0M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") * xs:double(\"1.7976931348623157E308\")",
            "S:P0M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-1",
            "xs:yearMonthDuration(\"P2Y11M\") * 2.3",
            "S:P6Y9M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-2",
            "fn:string((xs:yearMonthDuration(\"P10Y11M\")) * 2.0) and fn:false()",
            "F"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-3",
            "fn:string((xs:yearMonthDuration(\"P20Y10M\") * 2.0)) or fn:false()",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-4",
            "fn:not(fn:string(xs:yearMonthDuration(\"P11Y04M\") * 2.0))",
            "F"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-5",
            "fn:boolean(fn:string(xs:yearMonthDuration(\"P05Y08M\") * 2.0))",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-6",
            "fn:number(xs:yearMonthDuration(\"P02Y09M\") * 2.0)",
            "S:NaN"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-7",
            "fn:string(xs:yearMonthDuration(\"P03Y08M\") * 2.0)",
            "S:P7Y4M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-8",
            "(xs:yearMonthDuration(\"P10Y01M\") * -2.0)",
            "S:-P20Y2M"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-9",
            "fn:string((xs:yearMonthDuration(\"P01Y01M\") * 2.0)) and fn:string((xs:yearMonthDuration(\"P02Y03M\") * 2.0))",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-10",
            "fn:string((xs:yearMonthDuration(\"P05Y02M\") * 2.0)) or fn:string((xs:yearMonthDuration(\"P05Y03M\") * 2.0))",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-11",
            "(xs:yearMonthDuration(\"P20Y11M\") * 2.0) div (xs:yearMonthDuration(\"P20Y11M\") * 2.0)",
            "S:1"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-12",
            "fn:string((xs:yearMonthDuration(\"P10Y11M\") * 2.0)) and (fn:true())",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-13",
            "(xs:yearMonthDuration(\"P23Y11M\") * 2.0) eq xs:yearMonthDuration(\"P23Y11M\")",
            "F"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-14",
            "(xs:yearMonthDuration(\"P21Y12M\") * 2.0) ne xs:yearMonthDuration(\"P08Y05M\")",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-15",
            "(xs:yearMonthDuration(\"P10Y01M\") * 2.0) le xs:yearMonthDuration(\"P17Y02M\")",
            "F"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-16",
            "(xs:yearMonthDuration(\"P13Y09M\") - xs:yearMonthDuration(\"P18Y02M\")) ge xs:yearMonthDuration(\"P18Y02M\")",
            "F"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-17",
            "fn:count((xs:yearMonthDuration(\"P13Y09M\") *+0))",
            "S:1"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-18",
            "fn:count((xs:yearMonthDuration(\"P13Y09M\") *-0))",
            "S:1"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-19",
            "xs:yearMonthDuration(\"P13Y09M\") * fn:number(())",
            "X:FOCA0005"
        ),
        (
            "op-multiply-yearMonthDuration/op-multiply-yearMonthDuration-20",
            "for $i in (-3.9, -3.5, -3.1, -0.9, -0.5, -0.1, +0.1, +0.5, +0.9, +3.1, +3.5, +3.9) return xs:yearMonthDuration(\"P1M\") * $i",
            "S:-P4M -P3M -P3M -P1M P0M P0M P0M P1M P1M P3M P4M P4M"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-1",
            "xs:yearMonthDuration(\"P3Y36M\") * 3 eq xs:yearMonthDuration(\"P18Y\")",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-2",
            "3 * xs:yearMonthDuration(\"P3Y36M\") eq xs:yearMonthDuration(\"P18Y\")",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-3",
            "0 * xs:yearMonthDuration(\"P3Y36M\") eq xs:yearMonthDuration(\"P0M\")",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-4",
            "xs:yearMonthDuration(\"P3Y36M\") * 0 eq xs:yearMonthDuration(\"P0M\")",
            "T"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-5",
            "xs:yearMonthDuration(\"P3Y3M\") * xs:double(\"INF\")",
            "X:FODT0002"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-6",
            "xs:yearMonthDuration(\"P3Y3M\") * xs:double(\"-INF\")",
            "X:FODT0002"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-7",
            "xs:yearMonthDuration(\"P3Y3M\") * xs:double(\"NaN\")",
            "X:FOCA0005"
        ),
        ("op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-8", "xs:duration(\"P1Y3M\") * 3", "X:XPTY0004"),
        ("op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-9", "3 * xs:duration(\"P1Y3M\")", "X:XPTY0004"),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-10",
            "xs:dayTimeDuration(\"P3D\") * xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-11",
            "xs:yearMonthDuration(\"P3Y3M\") * xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-12",
            "xs:yearMonthDuration(\"P3Y3M\") * xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-multiply-yearMonthDuration/K-YearMonthDurationMultiply-13",
            "xs:dayTimeDuration(\"P3D\") * xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-multiply-yearMonthDuration/cbcl-multiply-yearMonthDuration-003",
            "xs:yearMonthDuration(\"P2Y\") * xs:double('NaN')",
            "X:FOCA0005"
        ),
        (
            "op-multiply-yearMonthDuration/cbcl-multiply-yearMonthDuration-006",
            "xs:double('NaN') * xs:yearMonthDuration(\"P2Y\")",
            "X:FOCA0005"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-1",
            "xs:dateTime(\"2000-10-30T06:12:00-05:00\") - xs:dateTime(\"1999-11-28T09:00:00Z\")",
            "S:P337DT2H12M"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-2",
            "xs:dateTime(\"2000-12-12T09:08:07+05:00\") - xs:dateTime(\"1999-12-12T09:08:07+05:00\")",
            "S:P366D"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-3",
            "xs:dateTime(\"2000-02-03T02:09:07-06:00\") - xs:dateTime(\"1998-02-03T02:09:07-06:00\")",
            "S:P730D"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-4",
            "fn:not(fn:string(xs:dateTime(\"1998-09-12T11:12:12Z\") - xs:dateTime(\"1996-02-02T01:01:01Z\")))",
            "F"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-5",
            "fn:boolean(fn:string(xs:dateTime(\"1962-03-12T10:09:09Z\") - xs:dateTime(\"1961-02-01T20:10:10Z\")))",
            "T"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-6",
            "fn:number(xs:dateTime(\"1988-01-28T10:09:08Z\") - xs:dateTime(\"1987-01-01T01:01:02Z\"))",
            "S:NaN"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-7",
            "fn:string(xs:dateTime(\"1989-07-05T02:02:02Z\") - xs:dateTime(\"1988-01-28T03:03:03Z\"))",
            "S:P523DT22H58M59S"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-8",
            "xs:dateTime(\"0001-01-01T01:01:01Z\") - xs:dateTime(\"2005-07-06T12:12:12Z\")",
            "S:-P732132DT11H11M11S"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-9",
            "fn:string((xs:dateTime(\"1993-12-09T04:04:04Z\") - xs:dateTime(\"1992-12-09T05:05:05Z\"))) and fn:string((xs:dateTime(\"1993-12-09T01:01:01Z\") - xs:dateTime(\"1992-12-09T06:06:06Z\")))",
            "T"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-10",
            "fn:string((xs:dateTime(\"1985-07-05T07:07:07Z\") - xs:dateTime(\"1984-07-05T08:08:08Z\"))) or fn:string((xs:dateTime(\"1985-07-05T09:09:09Z\") - xs:dateTime(\"1984-07-05T10:10:10Z\")))",
            "T"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-11",
            "(xs:dateTime(\"1985-07-05T07:07:07Z\") - xs:dateTime(\"1985-07-05T07:07:07Z\")) div xs:dayTimeDuration(\"P05DT08H11M\")",
            "S:0"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-12",
            "fn:string((xs:dateTime(\"1980-03-02T11:11:11Z\") - xs:dateTime(\"1981-12-12T12:12:12Z\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-13",
            "fn:string((xs:dateTime(\"1980-05-05T13:13:13Z\") - xs:dateTime(\"1979-10-05T14:14:14Z\"))) eq xs:string(xs:dayTimeDuration(\"P17DT10H02M\"))",
            "F"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-14",
            "fn:string((xs:dateTime(\"1979-12-12T16:16:16Z\") - xs:dateTime(\"1978-12-12T17:17:17Z\"))) ne xs:string(xs:dayTimeDuration(\"P17DT10H02M\"))",
            "T"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-15",
            "fn:string((xs:dateTime(\"1978-12-12T10:09:08Z\") - xs:dateTime(\"1977-12-12T09:08:07Z\"))) le xs:string(xs:dayTimeDuration(\"P17DT10H02M\"))",
            "F"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-16",
            "fn:string((xs:dateTime(\"1977-12-12T01:02:02Z\") - xs:dateTime(\"1976-12-12T02:03:04Z\"))) ge xs:string(xs:dayTimeDuration(\"P18DT02H02M\"))",
            "T"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-17",
            "fn:string(xs:dateTime(\"2000-12-12T12:07:08Z\") - xs:dateTime(\"1999-12-12T13:08:09Z\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-18",
            "fn:string((xs:dateTime(\"1999-10-23T03:02:01Z\") - xs:dateTime(\"1998-09-09T04:04:05Z\"))) or fn:false()",
            "T"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-19",
            "(xs:dateTime(\"1999-10-23T01:01:01Z\") - xs:dateTime(\"1998-09-09T02:02:02Z\")) * xs:decimal(2.0)",
            "S:P817DT21H57M58S"
        ),
        (
            "op-subtract-dateTimes/op-subtract-dateTimes-yielding-DTD-20",
            "(xs:dateTime(\"1999-10-23T09:08:07Z\") - xs:dateTime(\"1998-09-09T04:03:02Z\")) + xs:dayTimeDuration(\"P17DT10H02M\")",
            "S:P426DT15H7M5S"
        ),
        (
            "op-subtract-dateTimes/K-dateTimesSubtract-1",
            "xs:dateTime(\"1999-07-19T08:23:12.765\") - xs:dateTime(\"1999-07-19T08:23:12.765\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        (
            "op-subtract-dateTimes/K-dateTimesSubtract-2",
            "xs:dateTime(\"1999-10-12T08:01:23\") + xs:dateTime(\"1999-10-12T08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dateTimes/K-dateTimesSubtract-3",
            "xs:dateTime(\"1999-10-12T08:01:23\") div xs:dateTime(\"1999-10-12T08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dateTimes/K-dateTimesSubtract-4",
            "xs:dateTime(\"1999-10-12T08:01:23\") * xs:dateTime(\"1999-10-12T08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dateTimes/K-dateTimesSubtract-5",
            "xs:dateTime(\"1999-10-12T08:01:23\") mod xs:dateTime(\"1999-10-12T08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dateTimes/K-dateTimesSubtract-6",
            "xs:dayTimeDuration(\"P3D\") - xs:dateTime(\"1999-08-12T08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dateTimes/cbcl-subtract-dateTimes-001",
            "xs:dateTime(\"-25252734927766554-12-31T12:00:00\") - xs:dateTime(\"25252734927766554-12-31T12:00:00\")",
            "X:FODT0001"
        ),
        (
            "op-subtract-dateTimes/cbcl-subtract-dateTimes-002",
            "xs:dateTime(\"-25252734927766554-12-31T12:00:00\") - xs:dateTime(\"25252734927766554-12-31T12:00:00+01:00\")",
            "X:FODT0001"
        ),
        (
            "op-subtract-dateTimes/cbcl-subtract-dateTimes-003",
            "xs:dateTime(\"2008-12-31T12:00:00\") - xs:dateTime(\"2002-12-31T12:00:00+01:00\") + implicit-timezone()",
            "S:P2192DT1H"
        ),
        (
            "op-subtract-dateTimes/cbcl-subtract-dateTimes-004",
            "xs:dateTime(\"2002-12-31T12:00:00+01:00\") - xs:dateTime(\"2008-12-31T12:00:00\") - implicit-timezone()",
            "S:-P2192DT1H"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-1",
            "xs:date(\"2000-10-30\") - xs:date(\"1999-11-28\")",
            "S:P337D"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-2",
            "xs:date(\"2000-10-30+05:00\") - xs:date(\"1999-11-28Z\")",
            "S:P336DT19H"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-3",
            "xs:date(\"2000-10-15-05:00\") - xs:date(\"2000-10-10+02:00\")",
            "S:P5DT7H"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-4",
            "fn:not(fn:string(xs:date(\"1998-09-12Z\") - xs:date(\"1998-09-21Z\")))",
            "F"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-5",
            "fn:boolean(fn:string(xs:date(\"1962-03-12Z\") - xs:date(\"1962-03-12Z\")))",
            "T"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-6",
            "fn:number(xs:date(\"1988-01-28Z\") - xs:date(\"2001-03-02\"))",
            "S:NaN"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-7",
            "fn:string(xs:date(\"1989-07-05Z\") - xs:date(\"1962-09-04Z\"))",
            "S:P9801D"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-8",
            "xs:date(\"0001-01-01Z\") - xs:date(\"2005-07-06Z\")",
            "S:-P732132D"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-9",
            "fn:string((xs:date(\"1993-12-09Z\") - xs:date(\"1992-10-02Z\"))) and fn:string((xs:date(\"1993-12-09Z\") - xs:date(\"1980-10-20Z\")))",
            "T"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-10",
            "fn:string((xs:date(\"1985-07-05Z\") - xs:date(\"1977-12-02Z\"))) or fn:string((xs:date(\"1985-07-05Z\") - xs:date(\"1960-11-07Z\")))",
            "T"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-11",
            "(xs:date(\"1978-12-12Z\") - xs:date(\"1978-12-12Z\")) div xs:dayTimeDuration(\"P17DT10H02M\")",
            "S:0"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-12",
            "fn:string((xs:date(\"1980-03-02Z\") - xs:date(\"2001-09-11Z\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-13",
            "fn:string((xs:date(\"1980-05-05Z\") - xs:date(\"1981-12-03Z\"))) eq xs:string(xs:dayTimeDuration(\"P17DT10H02M\"))",
            "F"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-14",
            "fn:string((xs:date(\"1979-12-12Z\") - xs:date(\"1979-11-11Z\"))) ne xs:string(xs:dayTimeDuration(\"P17DT10H02M\"))",
            "T"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-15",
            "fn:string((xs:date(\"1978-12-12Z\") - xs:date(\"1977-03-12Z\"))) le xs:string(xs:dayTimeDuration(\"P17DT10H02M\"))",
            "F"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-16",
            "fn:string((xs:date(\"1977-12-12Z\") - xs:date(\"1976-12-12Z\"))) ge xs:string(xs:dayTimeDuration(\"P17DT10H02M\"))",
            "T"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-17",
            "fn:string(xs:date(\"2000-12-12Z\") - xs:date(\"2000-11-11Z\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-18",
            "fn:string((xs:date(\"1999-10-23Z\") - xs:date(\"1998-09-09Z\"))) or fn:false()",
            "T"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-19",
            "(xs:date(\"1999-10-23Z\") - xs:date(\"1998-09-09Z\")) * xs:decimal(2.0)",
            "S:P818D"
        ),
        (
            "op-subtract-dates/op-subtract-dates-yielding-DTD-20",
            "(xs:date(\"1999-10-23Z\") - xs:date(\"1998-09-09Z\")) + xs:dayTimeDuration(\"P17DT10H02M\")",
            "S:P426DT10H2M"
        ),
        (
            "op-subtract-dates/K-DatesSubtract-1",
            "xs:date(\"1999-07-19\") - xs:date(\"1969-11-30\") eq xs:dayTimeDuration(\"P10823D\")",
            "T"
        ),
        (
            "op-subtract-dates/K-DatesSubtract-2",
            "xs:date(\"1999-07-19\") - xs:date(\"1999-07-19\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        ("op-subtract-dates/K-DatesSubtract-3", "xs:date(\"1999-10-12\") + xs:date(\"1999-10-12\")", "X:XPTY0004"),
        ("op-subtract-dates/K-DatesSubtract-4", "xs:date(\"1999-10-12\") div xs:date(\"1999-10-12\")", "X:XPTY0004"),
        ("op-subtract-dates/K-DatesSubtract-5", "xs:date(\"1999-10-12\") * xs:date(\"1999-10-12\")", "X:XPTY0004"),
        ("op-subtract-dates/K-DatesSubtract-6", "xs:date(\"1999-10-12\") mod xs:date(\"1999-10-12\")", "X:XPTY0004"),
        (
            "op-subtract-dates/cbcl-subtract-dates-001",
            "xs:date(\"-25252734927766554-12-31\") - xs:date(\"25252734927766554-12-31\")",
            "X:FODT0001"
        ),
        (
            "op-subtract-dates/cbcl-subtract-dates-002",
            "xs:date(\"-25252734927766554-12-31\") - xs:date(\"25252734927766554-12-31+01:00\")",
            "X:FODT0001"
        ),
        (
            "op-subtract-dates/cbcl-subtract-dates-003",
            "xs:date(\"2008-12-31\") - xs:date(\"2002-12-31+01:00\") + implicit-timezone()",
            "S:P2192DT1H"
        ),
        (
            "op-subtract-dates/cbcl-subtract-dates-004",
            "xs:date(\"2002-12-31+01:00\") - xs:date(\"2008-12-31\") - implicit-timezone()",
            "S:-P2192DT1H"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date2args-1",
            "xs:date(\"1970-01-01Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1970-01-01Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date2args-2",
            "xs:date(\"1983-11-17Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1983-11-17Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date2args-3",
            "xs:date(\"2030-12-31Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:2030-12-31Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date2args-4",
            "xs:date(\"1970-01-01Z\") - xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:1969-12-16Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date2args-5",
            "xs:date(\"1970-01-01Z\") - xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:1969-11-30Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-1",
            "xs:date(\"2000-10-30\") - xs:dayTimeDuration(\"P3DT1H15M\")",
            "S:2000-10-26"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-2",
            "fn:string(xs:date(\"2000-12-12Z\") - xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-3",
            "fn:string((xs:date(\"1999-10-23Z\") - xs:dayTimeDuration(\"P19DT13H10M\"))) or fn:false()",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-4",
            "fn:not(fn:string(xs:date(\"1998-09-12Z\") - xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-5",
            "fn:boolean(fn:string(xs:date(\"1962-03-12Z\") - xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-6",
            "fn:number(xs:date(\"1988-01-28Z\") - xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-7",
            "fn:string(xs:date(\"1989-07-05Z\") - xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:1989-07-03Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-8",
            "(xs:date(\"0001-01-01Z\") - xs:dayTimeDuration(\"P11DT02H02M\"))",
            "S:-0001-12-20Z\tS:0000-12-20Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-9",
            "fn:string((xs:date(\"1993-12-09Z\") - xs:dayTimeDuration(\"P03DT01H04M\"))) and fn:string((xs:date(\"1993-12-09Z\") - xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-10",
            "fn:string((xs:date(\"1985-07-05Z\") - xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:date(\"1985-07-05Z\") - xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-12",
            "fn:string((xs:date(\"1980-03-02Z\") - xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-13",
            "(xs:date(\"1980-05-05Z\") - xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:date(\"1980-05-05Z\")",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-14",
            "(xs:date(\"1979-12-12Z\") - xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:date(\"1979-12-12Z\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-15",
            "(xs:date(\"1978-12-12Z\") - xs:dayTimeDuration(\"P17DT10H02M\")) le xs:date(\"1978-12-12Z\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/op-subtract-dayTimeDuration-from-date-16",
            "(xs:date(\"1977-12-12Z\") - xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:date(\"1977-12-12Z\")",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-date/K-DateSubtractDTD-1",
            "xs:date(\"1999-08-12\") - xs:dayTimeDuration(\"P23DT09H32M59S\") eq xs:date(\"1999-07-19\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime2args-1",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1970-01-01T00:00:00Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime2args-2",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:1996-04-07T01:40:52Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime2args-3",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:2030-12-31T23:59:59Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime2args-4",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") - xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:1969-12-16T12:00:01Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime2args-5",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") - xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:1969-11-30T00:00:01Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-1",
            "xs:dateTime(\"2000-10-30T11:12:00\") - xs:dayTimeDuration(\"P3DT1H15M\")",
            "S:2000-10-27T09:57:00"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-2",
            "fn:string(xs:dateTime(\"2000-12-12T11:10:03Z\") - xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-3",
            "fn:string((xs:dateTime(\"1999-10-23T03:12:23Z\") - xs:dayTimeDuration(\"P19DT13H10M\"))) or fn:false()",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-4",
            "fn:not(fn:string(xs:dateTime(\"1998-09-12T13:23:23Z\") - xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-5",
            "fn:boolean(fn:string(xs:dateTime(\"1962-03-12T12:34:09Z\") - xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-6",
            "fn:number(xs:dateTime(\"1988-01-28T12:34:12Z\") - xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-7",
            "fn:string(xs:dateTime(\"1989-07-05T10:10:10Z\") - xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:1989-07-04T01:08:10Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-8",
            "(xs:dateTime(\"0001-01-01T11:11:11Z\") - xs:dayTimeDuration(\"-P11DT02H02M\"))",
            "S:0001-01-12T13:13:11Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-9",
            "fn:string((xs:dateTime(\"1993-12-09T13:13:13Z\") - xs:dayTimeDuration(\"P03DT01H04M\"))) and fn:string((xs:dateTime(\"1993-12-09T13:13:13Z\") - xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-10",
            "fn:string((xs:dateTime(\"1985-07-05T14:14:14Z\") - xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:dateTime(\"1985-07-05T15:15:15Z\") - xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-12",
            "fn:string((xs:dateTime(\"1980-03-02T16:12:10Z\") - xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-13",
            "(xs:dateTime(\"1980-05-05T17:17:17Z\") - xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:dateTime(\"1980-05-05T17:17:17Z\")",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-14",
            "(xs:dateTime(\"1979-12-12T18:18:18Z\") - xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:dateTime(\"1979-12-12T16:15:14Z\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-15",
            "(xs:dateTime(\"1978-12-12T12:45:12Z\") - xs:dayTimeDuration(\"P17DT10H02M\")) le xs:dateTime(\"1978-12-12T16:34:23Z\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-dateTime/op-subtract-dayTimeDuration-from-dateTime-16",
            "(xs:dateTime(\"1977-12-12T13:12:15Z\") - xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:dateTime(\"1977-12-12T15:56:10Z\")",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time2args-1",
            "xs:time(\"00:00:00Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:00:00:00Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time2args-2",
            "xs:time(\"08:03:35Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:08:03:35Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time2args-3",
            "xs:time(\"23:59:59Z\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:23:59:59Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time2args-4",
            "xs:time(\"00:00:00Z\") - xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:12:00:01Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time2args-5",
            "xs:time(\"00:00:00Z\") - xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:00:00:01Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-1",
            "xs:time(\"11:12:00\") - xs:dayTimeDuration(\"P3DT1H15M\")",
            "S:09:57:00"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-2",
            "xs:time(\"08:20:00-05:00\") - xs:dayTimeDuration(\"P23DT10H10M\")",
            "S:22:10:00-05:00"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-3",
            "fn:string((xs:time(\"12:12:01Z\") - xs:dayTimeDuration(\"P19DT13H10M\"))) or fn:false()",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-4",
            "fn:not(fn:string(xs:time(\"20:50:50Z\") - xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-5",
            "fn:boolean(fn:string(xs:time(\"23:55:55Z\") - xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-6",
            "fn:number(xs:time(\"10:11:45Z\") - xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-7",
            "fn:string(xs:time(\"19:45:55Z\") - xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:10:43:55Z"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-8",
            "(xs:time(\"01:01:01\") - xs:dayTimeDuration(\"-P11DT02H02M\"))",
            "S:03:03:01"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-9",
            "fn:string((xs:time(\"10:10:10Z\") - xs:dayTimeDuration(\"P02DT09H02M\"))) and fn:string((xs:time(\"09:02:02Z\") - xs:dayTimeDuration(\"P04DT04H04M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-10",
            "fn:string((xs:time(\"23:45:50Z\") - xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:time(\"23:45:50Z\") + xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-12",
            "fn:string((xs:time(\"02:02:02Z\") - xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-13",
            "(xs:time(\"01:03:03Z\") - xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:time(\"04:03:05Z\")",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-14",
            "(xs:time(\"04:04:05Z\") - xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:time(\"05:08:02Z\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-15",
            "fn:string((xs:time(\"08:09:09Z\") - xs:dayTimeDuration(\"P17DT10H02M\"))) le fn:string(xs:time(\"09:08:10Z\"))",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-16",
            "fn:string((xs:time(\"09:06:07Z\") - xs:dayTimeDuration(\"P18DT02H02M\"))) ge fn:string(xs:time(\"01:01:01Z\"))",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/op-subtract-dayTimeDuration-from-time-17",
            "fn:string(xs:time(\"12:07:08Z\") - xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/K-TimeSubtractDTD-1",
            "xs:time(\"08:12:32\") - xs:dayTimeDuration(\"P23DT09H32M59S\") eq xs:time(\"22:39:33\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/K-TimeSubtractDTD-2",
            "xs:yearMonthDuration(\"P1Y\") + xs:time(\"08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/K-TimeSubtractDTD-3",
            "xs:time(\"08:01:23\") + xs:yearMonthDuration(\"P1Y\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/K-TimeSubtractDTD-4",
            "xs:yearMonthDuration(\"P1Y\") - xs:time(\"08:01:23\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDuration-from-time/K-TimeSubtractDTD-5",
            "xs:time(\"08:01:23\") - xs:yearMonthDuration(\"P1Y\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations2args-1",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:PT0S"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations2args-2",
            "xs:dayTimeDuration(\"P15DT11H59M59S\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:P15DT11H59M59S"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations2args-3",
            "xs:dayTimeDuration(\"P31DT23H59M59S\") - xs:dayTimeDuration(\"P0DT0H0M0S\")",
            "S:P31DT23H59M59S"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations2args-4",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") - xs:dayTimeDuration(\"P15DT11H59M59S\")",
            "S:-P15DT11H59M59S"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations2args-5",
            "xs:dayTimeDuration(\"P0DT0H0M0S\") - xs:dayTimeDuration(\"P31DT23H59M59S\")",
            "S:-P31DT23H59M59S"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-1",
            "xs:dayTimeDuration(\"P2DT12H\") - xs:dayTimeDuration(\"P1DT10H30M\")",
            "S:P1DT1H30M"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-2",
            "fn:string(xs:dayTimeDuration(\"P10DT10H11M\") - xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-3",
            "fn:string(xs:dayTimeDuration(\"P20DT20H10M\") - xs:dayTimeDuration(\"P19DT13H10M\")) or fn:false()",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-4",
            "fn:not(fn:string(xs:dayTimeDuration(\"P11DT12H04M\") - xs:dayTimeDuration(\"P02DT07H01M\")))",
            "F"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-5",
            "fn:boolean(fn:string(xs:dayTimeDuration(\"P05DT09H08M\") - xs:dayTimeDuration(\"P03DT08H06M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-6",
            "fn:number(xs:dayTimeDuration(\"P02DT06H09M\") - xs:dayTimeDuration(\"P10DT08H01M\"))",
            "S:NaN"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-7",
            "fn:string(xs:dayTimeDuration(\"P03DT04H08M\") - xs:dayTimeDuration(\"P01DT09H02M\"))",
            "S:P1DT19H6M"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-8",
            "(xs:dayTimeDuration(\"P10DT01H01M\") - xs:dayTimeDuration(\"P11DT02H02M\"))",
            "S:-P1DT1H1M"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-9",
            "fn:string((xs:dayTimeDuration(\"P01DT02H01M\") - xs:dayTimeDuration(\"P02DT09H02M\"))) and fn:string((xs:dayTimeDuration(\"P02DT03H03M\") - xs:dayTimeDuration(\"P04DT04H04M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-10",
            "fn:string((xs:dayTimeDuration(\"P05DT09H02M\") - xs:dayTimeDuration(\"P03DT01H04M\"))) or fn:string((xs:dayTimeDuration(\"P05DT05H03M\") - xs:dayTimeDuration(\"P01DT01H03M\")))",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-11",
            "(xs:dayTimeDuration(\"P42DT10H10M\") - xs:dayTimeDuration(\"P10DT10H10M\")) div (xs:dayTimeDuration(\"P20DT10H10M\") - xs:dayTimeDuration(\"P18DT10H10M\"))",
            "S:16"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-12",
            "fn:string((xs:dayTimeDuration(\"P10DT08H11M\") - xs:dayTimeDuration(\"P05DT08H11M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-13",
            "(xs:dayTimeDuration(\"P23DT11H11M\") - xs:dayTimeDuration(\"P23DT11H11M\")) eq xs:dayTimeDuration(\"P23DT11H11M\")",
            "F"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-14",
            "(xs:dayTimeDuration(\"P21DT08H12M\") - xs:dayTimeDuration(\"P08DT08H05M\")) ne xs:dayTimeDuration(\"P08DT08H05M\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-15",
            "(xs:dayTimeDuration(\"P10DT10H01M\") - xs:dayTimeDuration(\"P17DT10H02M\")) le xs:dayTimeDuration(\"P17DT10H02M\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/op-subtract-dayTimeDurations-16",
            "(xs:dayTimeDuration(\"P13DT09H09M\") - xs:dayTimeDuration(\"P18DT02H02M\")) ge xs:dayTimeDuration(\"P18DT02H02M\")",
            "F"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-1",
            "xs:dayTimeDuration(\"P3DT4H3M3.100S\") - xs:dayTimeDuration(\"PT0S\") eq xs:dayTimeDuration(\"P3DT4H3M3.100S\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-2",
            "xs:dayTimeDuration(\"PT0S\") - xs:dayTimeDuration(\"P3DT4H3M3.100S\") eq xs:dayTimeDuration(\"-P3DT4H3M3.100S\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-3",
            "xs:dayTimeDuration(\"PT0S\") - xs:dayTimeDuration(\"P3DT4H3M3.100S\") eq xs:dayTimeDuration(\"-P3DT4H3M3.100S\")",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-4",
            "xs:dayTimeDuration(\"P3D\") - xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-5",
            "xs:yearMonthDuration(\"P3Y3M\") - xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-6",
            "xs:duration(\"P3D\") - xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-7",
            "xs:yearMonthDuration(\"P3Y3M\") - xs:duration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-8",
            "xs:dayTimeDuration(\"P3D\") - xs:duration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDurations/K-DayTimeDurationSubtract-9",
            "xs:duration(\"P3Y3M\") - xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-dayTimeDurations/K2-DayTimeDurationSubtract-1",
            "(current-time() - xs:dayTimeDuration(\"PT3H\")) ne current-time()",
            "T"
        ),
        (
            "op-subtract-dayTimeDurations/K2-DayTimeDurationSubtract-2",
            "(current-dateTime() - xs:dayTimeDuration(\"PT3H\")) lt current-dateTime()",
            "T"
        ),
        ("op-subtract-times/op-subtract-times2args-1", "xs:time(\"00:00:00Z\") - xs:time(\"00:00:00Z\")", "S:PT0S"),
        (
            "op-subtract-times/op-subtract-times2args-2",
            "xs:time(\"08:03:35Z\") - xs:time(\"00:00:00Z\")",
            "S:PT8H3M35S"
        ),
        (
            "op-subtract-times/op-subtract-times2args-3",
            "xs:time(\"23:59:59Z\") - xs:time(\"00:00:00Z\")",
            "S:PT23H59M59S"
        ),
        (
            "op-subtract-times/op-subtract-times2args-4",
            "xs:time(\"00:00:00Z\") - xs:time(\"08:03:35Z\")",
            "S:-PT8H3M35S"
        ),
        (
            "op-subtract-times/op-subtract-times2args-5",
            "xs:time(\"00:00:00Z\") - xs:time(\"23:59:59Z\")",
            "S:-PT23H59M59S"
        ),
        ("op-subtract-times/op-subtract-times-1", "xs:time(\"11:12:00Z\") - xs:time(\"04:00:00Z\")", "S:PT7H12M"),
        (
            "op-subtract-times/op-subtract-times-2",
            "xs:time(\"11:00:00-05:00\") - xs:time(\"21:30:00+05:30\")",
            "S:PT0S"
        ),
        ("op-subtract-times/op-subtract-times-3", "xs:time(\"17:00:00-06:00\") - xs:time(\"08:00:00+09:00\")", "S:P1D"),
        (
            "op-subtract-times/op-subtract-times-4",
            "fn:not(fn:string(xs:time(\"13:00:00Z\") - xs:time(\"14:00:00Z\")))",
            "F"
        ),
        (
            "op-subtract-times/op-subtract-times-5",
            "fn:boolean(fn:string(xs:time(\"13:00:00Z\") - xs:time(\"10:00:00Z\")))",
            "T"
        ),
        (
            "op-subtract-times/op-subtract-times-6",
            "fn:number(xs:time(\"13:00:00Z\") - xs:time(\"12:00:00Z\"))",
            "S:NaN"
        ),
        (
            "op-subtract-times/op-subtract-times-7",
            "fn:string(xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\"))",
            "S:-PT4H"
        ),
        ("op-subtract-times/op-subtract-times-8", "(xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\"))", "S:-PT4H"),
        (
            "op-subtract-times/op-subtract-times-9",
            "fn:string((xs:time(\"13:00:00Z\") - xs:time(\"12:00:00Z\"))) and fn:string((xs:time(\"13:00:00Z\") - xs:time(\"10:00:00Z\")))",
            "T"
        ),
        (
            "op-subtract-times/op-subtract-times-10",
            "fn:string((xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\"))) or fn:string((xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\")))",
            "T"
        ),
        (
            "op-subtract-times/op-subtract-times-11",
            "(xs:time(\"23:00:00Z\") - xs:time(\"17:00:00Z\")) div (xs:time(\"13:00:00Z\") - xs:time(\"10:00:00Z\"))",
            "S:2"
        ),
        (
            "op-subtract-times/op-subtract-times-12",
            "fn:string((xs:time(\"17:00:00Z\") - xs:time(\"13:00:00Z\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-times/op-subtract-times-13",
            "(xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\")) eq xs:dayTimeDuration(\"P20DT01H02M\")",
            "F"
        ),
        (
            "op-subtract-times/op-subtract-times-14",
            "(xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\")) ne xs:dayTimeDuration(\"P10DT01H01M\")",
            "T"
        ),
        (
            "op-subtract-times/op-subtract-times-15",
            "(xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\")) le xs:dayTimeDuration(\"P10DT02H10M\")",
            "T"
        ),
        (
            "op-subtract-times/op-subtract-times-16",
            "(xs:time(\"13:00:00Z\") - xs:time(\"17:00:00Z\")) ge xs:dayTimeDuration(\"P17DT10H02M\")",
            "F"
        ),
        (
            "op-subtract-times/K-TimeSubtract-1",
            "xs:time(\"08:12:32\") - xs:time(\"18:12:32\") eq xs:dayTimeDuration(\"-PT10H\")",
            "T"
        ),
        (
            "op-subtract-times/K-TimeSubtract-2",
            "xs:time(\"08:12:32\") - xs:time(\"08:12:32\") eq xs:dayTimeDuration(\"PT0S\")",
            "T"
        ),
        ("op-subtract-times/K-TimeSubtract-3", "xs:dayTimeDuration(\"P3D\") - xs:time(\"08:01:23\")", "X:XPTY0004"),
        ("op-subtract-times/K-TimeSubtract-4", "xs:dayTimeDuration(\"P3D\") - xs:date(\"1999-08-12\")", "X:XPTY0004"),
        ("op-subtract-times/K-TimeSubtract-5", "xs:time(\"08:01:23\") + xs:time(\"08:01:23\")", "X:XPTY0004"),
        ("op-subtract-times/K-TimeSubtract-6", "xs:time(\"08:01:23\") * xs:time(\"08:01:23\")", "X:XPTY0004"),
        ("op-subtract-times/K-TimeSubtract-7", "xs:time(\"08:01:23\") div xs:time(\"08:01:23\")", "X:XPTY0004"),
        ("op-subtract-times/K-TimeSubtract-8", "xs:time(\"08:01:23\") mod xs:time(\"08:01:23\")", "X:XPTY0004"),
        (
            "op-subtract-times/cbcl-subtract-times-001",
            "xs:time(\"12:00:00+01:00\") - xs:time(\"12:00:00\") - implicit-timezone()",
            "S:-PT1H"
        ),
        (
            "op-subtract-times/cbcl-subtract-times-002",
            "xs:time(\"12:00:00\") - xs:time(\"12:00:00+01:00\") + implicit-timezone()",
            "S:PT1H"
        ),
        (
            "op-subtract-times/cbcl-subtract-times-003",
            "fn:adjust-time-to-timezone(xs:time(\"12:00:00\")) - fn:adjust-time-to-timezone(xs:time(\"08:00:00+05:00\"), xs:dayTimeDuration(\"PT1H\")) + implicit-timezone()",
            "S:PT9H"
        ),
        (
            "op-subtract-times/cbcl-subtract-times-004",
            "fn:adjust-time-to-timezone(xs:time(\"08:00:00+05:00\"), xs:dayTimeDuration(\"PT1H\")) - fn:adjust-time-to-timezone(xs:time(\"12:00:00\")) - implicit-timezone()",
            "S:-PT9H"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date2args-1",
            "xs:date(\"1970-01-01Z\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:1970-01-01Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date2args-2",
            "xs:date(\"1983-11-17Z\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:1983-11-17Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date2args-3",
            "xs:date(\"2030-12-31Z\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:2030-12-31Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date2args-4",
            "xs:date(\"1970-01-01Z\") - xs:yearMonthDuration(\"P1000Y6M\")",
            "S:0969-07-01Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date2args-5",
            "xs:date(\"1970-01-01Z\") - xs:yearMonthDuration(\"P2030Y12M\")",
            "S:-0062-01-01Z\tS:-0061-01-01Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-1",
            "xs:date(\"2000-10-30\") - xs:yearMonthDuration(\"P1Y2M\")",
            "S:1999-08-30"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-2",
            "xs:date(\"2000-02-29Z\") - xs:yearMonthDuration(\"P1Y\")",
            "S:1999-02-28Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-3",
            "xs:date(\"2000-10-31-05:00\") - xs:yearMonthDuration(\"P1Y1M\")",
            "S:1999-09-30-05:00"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-4",
            "fn:not(fn:string(xs:date(\"1998-09-12Z\") - xs:yearMonthDuration(\"P20Y03M\")))",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-5",
            "fn:boolean(fn:string(xs:date(\"1962-03-12Z\") - xs:yearMonthDuration(\"P10Y01M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-6",
            "fn:number(xs:date(\"1988-01-28Z\") - xs:yearMonthDuration(\"P09Y02M\"))",
            "S:NaN"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-7",
            "fn:string(xs:date(\"1989-07-05Z\") - xs:yearMonthDuration(\"P08Y04M\"))",
            "S:1981-03-05Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-8",
            "(xs:date(\"0001-01-01Z\") - xs:yearMonthDuration(\"-P20Y07M\"))",
            "S:0021-08-01Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-9",
            "fn:string((xs:date(\"1993-12-09Z\") - xs:yearMonthDuration(\"P03Y03M\"))) and fn:string((xs:date(\"1993-12-09Z\") - xs:yearMonthDuration(\"P03Y03M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-10",
            "fn:string((xs:date(\"1985-07-05Z\") - xs:yearMonthDuration(\"P02Y02M\"))) or fn:string((xs:date(\"1985-07-05Z\") - xs:yearMonthDuration(\"P02Y02M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-12",
            "fn:string((xs:date(\"1980-03-02Z\") - xs:yearMonthDuration(\"P05Y05M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-13",
            "(xs:date(\"1980-05-05Z\") - xs:yearMonthDuration(\"P23Y11M\")) eq xs:date(\"1980-05-05Z\")",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-14",
            "(xs:date(\"1979-12-12Z\") - xs:yearMonthDuration(\"P08Y08M\")) ne xs:date(\"1979-12-12Z\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-15",
            "(xs:date(\"1978-12-12Z\") - xs:yearMonthDuration(\"P17Y12M\")) le xs:date(\"1978-12-12Z\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-16",
            "(xs:date(\"1977-12-12Z\") - xs:yearMonthDuration(\"P18Y02M\")) ge xs:date(\"1977-12-12Z\")",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-17",
            "fn:string(xs:date(\"2000-12-12Z\") - xs:yearMonthDuration(\"P18Y11M\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/op-subtract-yearMonthDuration-from-date-18",
            "fn:string((xs:date(\"1999-10-23Z\") - xs:yearMonthDuration(\"P19Y12M\"))) or fn:false()",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-date/K-DateSubtractYMD-1",
            "xs:date(\"1999-08-12\") - xs:yearMonthDuration(\"P3Y7M\") eq xs:date(\"1996-01-12\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime2args-1",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:1970-01-01T00:00:00Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime2args-2",
            "xs:dateTime(\"1996-04-07T01:40:52Z\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:1996-04-07T01:40:52Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime2args-3",
            "xs:dateTime(\"2030-12-31T23:59:59Z\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:2030-12-31T23:59:59Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime2args-4",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") - xs:yearMonthDuration(\"P1000Y6M\")",
            "S:0969-07-01T00:00:00Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime2args-5",
            "xs:dateTime(\"1970-01-01T00:00:00Z\") - xs:yearMonthDuration(\"P2030Y12M\")",
            "S:-0062-01-01T00:00:00Z\tS:-0061-01-01T00:00:00Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-1",
            "xs:dateTime(\"2000-10-30T11:12:00\") - xs:yearMonthDuration(\"P1Y2M\")",
            "S:1999-08-30T11:12:00"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-2",
            "fn:string(xs:dateTime(\"2000-12-12T12:12:12Z\") - xs:dayTimeDuration(\"P12DT10H07M\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-3",
            "fn:string((xs:dateTime(\"1999-10-23T13:45:45Z\") - xs:yearMonthDuration(\"P19Y12M\"))) or fn:false()",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-4",
            "fn:not(fn:string(xs:dateTime(\"1998-09-12T13:56:12Z\") - xs:yearMonthDuration(\"P20Y03M\")))",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-5",
            "fn:boolean(fn:string(xs:dateTime(\"1962-03-12T10:12:34Z\") - xs:yearMonthDuration(\"P10Y01M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-6",
            "fn:number(xs:dateTime(\"1988-01-28T13:45:23Z\") - xs:yearMonthDuration(\"P09Y02M\"))",
            "S:NaN"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-7",
            "fn:string(xs:dateTime(\"1989-07-05T14:34:36Z\") - xs:yearMonthDuration(\"P08Y04M\"))",
            "S:1981-03-05T14:34:36Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-8",
            "(xs:dateTime(\"0001-01-01T01:01:01Z\") - xs:yearMonthDuration(\"-P20Y07M\"))",
            "S:0021-08-01T01:01:01Z"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-9",
            "fn:string((xs:dateTime(\"1993-12-09T10:10:10Z\") - xs:yearMonthDuration(\"P03Y03M\"))) and fn:string((xs:dateTime(\"1993-12-09T10:10:10Z\") - xs:yearMonthDuration(\"P03Y03M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-10",
            "fn:string((xs:dateTime(\"1985-07-05T09:09:09Z\") - xs:yearMonthDuration(\"P02Y02M\"))) or fn:string((xs:dateTime(\"1985-07-05T09:09:09Z\") - xs:yearMonthDuration(\"P02Y02M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-12",
            "fn:string((xs:dateTime(\"1980-03-02T02:02:02Z\") - xs:yearMonthDuration(\"P05Y05M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-13",
            "(xs:dateTime(\"1980-05-05T05:05:05Z\") - xs:yearMonthDuration(\"P23Y11M\")) eq xs:dateTime(\"1980-05-05T05:05:05Z\")",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-14",
            "(xs:dateTime(\"1979-12-12T09:09:09Z\") - xs:yearMonthDuration(\"P08Y08M\")) ne xs:dateTime(\"1979-12-12T09:09:09Z\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-15",
            "(xs:dateTime(\"1978-12-12T07:07:07Z\") - xs:yearMonthDuration(\"P17Y12M\")) le xs:dateTime(\"1978-12-12T07:07:07Z\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/op-subtract-yearMonthDuration-from-dateTime-16",
            "(xs:dateTime(\"1977-12-12T03:03:03Z\") - xs:yearMonthDuration(\"P18Y02M\")) ge xs:dateTime(\"1977-12-12T03:03:03Z\")",
            "F"
        ),
        (
            "op-subtract-yearMonthDuration-from-dateTime/K-DateTimeSubtractYMD-1",
            "xs:dateTime(\"1999-07-19T08:23:01.765\") - xs:yearMonthDuration(\"P3Y35M\") eq xs:dateTime(\"1993-08-19T08:23:01.765\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:P0M"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:P1000Y6M"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") - xs:yearMonthDuration(\"P0Y0M\")",
            "S:P2031Y"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") - xs:yearMonthDuration(\"P1000Y6M\")",
            "S:-P1000Y6M"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") - xs:yearMonthDuration(\"P2030Y12M\")",
            "S:-P2031Y"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-1",
            "(xs:yearMonthDuration(\"P2Y11M\") - xs:yearMonthDuration(\"P3Y3M\"))",
            "S:-P4M"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-2",
            "fn:string(xs:yearMonthDuration(\"P10Y11M\") - xs:yearMonthDuration(\"P12Y07M\")) and fn:false()",
            "F"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-3",
            "fn:string(xs:yearMonthDuration(\"P20Y10M\") - xs:yearMonthDuration(\"P19Y10M\")) or fn:false()",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-4",
            "fn:not(fn:string(xs:yearMonthDuration(\"P11Y04M\") - xs:yearMonthDuration(\"P02Y11M\")))",
            "F"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-5",
            "fn:boolean(fn:string(xs:yearMonthDuration(\"P05Y08M\") - xs:yearMonthDuration(\"P03Y06M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-6",
            "fn:number(xs:yearMonthDuration(\"P02Y09M\") - xs:yearMonthDuration(\"P10Y01M\"))",
            "S:NaN"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-7",
            "fn:string(xs:yearMonthDuration(\"P03Y08M\") - xs:yearMonthDuration(\"P01Y02M\"))",
            "S:P2Y6M"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-8",
            "(xs:yearMonthDuration(\"P10Y01M\") - xs:yearMonthDuration(\"P11Y02M\"))",
            "S:-P1Y1M"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-9",
            "fn:string((xs:yearMonthDuration(\"P01Y01M\") - xs:yearMonthDuration(\"P02Y02M\"))) and fn:string((xs:yearMonthDuration(\"P02Y03M\") - xs:yearMonthDuration(\"P04Y04M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-10",
            "fn:string((xs:yearMonthDuration(\"P05Y02M\") - xs:yearMonthDuration(\"P03Y04M\"))) or fn:string((xs:yearMonthDuration(\"P05Y03M\") - xs:yearMonthDuration(\"P01Y03M\")))",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-11",
            "(xs:yearMonthDuration(\"P42Y10M\") - xs:yearMonthDuration(\"P20Y10M\")) div (xs:yearMonthDuration(\"P20Y11M\") - xs:yearMonthDuration(\"P18Y11M\"))",
            "S:11"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-12",
            "fn:string((xs:yearMonthDuration(\"P10Y11M\") - xs:yearMonthDuration(\"P05Y07M\"))) and (fn:true())",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-13",
            "(xs:yearMonthDuration(\"P23Y11M\") - xs:yearMonthDuration(\"P23Y11M\")) eq xs:yearMonthDuration(\"P23Y11M\")",
            "F"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-14",
            "(xs:yearMonthDuration(\"P21Y12M\") - xs:yearMonthDuration(\"P08Y05M\")) ne xs:yearMonthDuration(\"P08Y05M\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-15",
            "(xs:yearMonthDuration(\"P10Y01M\") - xs:yearMonthDuration(\"P17Y02M\")) le xs:yearMonthDuration(\"P17Y02M\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/op-subtract-yearMonthDurations-16",
            "(xs:yearMonthDuration(\"P13Y09M\") - xs:yearMonthDuration(\"P18Y02M\")) ge xs:yearMonthDuration(\"P18Y02M\")",
            "F"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-1",
            "xs:yearMonthDuration(\"P3Y3M\") - xs:yearMonthDuration(\"P0M\") eq xs:yearMonthDuration(\"P3Y3M\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-2",
            "xs:yearMonthDuration(\"P0M\") - xs:yearMonthDuration(\"P3Y3M\") eq xs:yearMonthDuration(\"-P3Y3M\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-3",
            "xs:yearMonthDuration(\"P32Y43M\") - xs:yearMonthDuration(\"P12Y13M\") eq xs:yearMonthDuration(\"P22Y6M\")",
            "T"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-4",
            "xs:dayTimeDuration(\"P3D\") + xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-5",
            "xs:yearMonthDuration(\"P3Y3M\") + xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-6",
            "xs:duration(\"P3D\") + xs:yearMonthDuration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-7",
            "xs:yearMonthDuration(\"P3Y3M\") + xs:duration(\"P3D\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-8",
            "xs:dayTimeDuration(\"P3D\") + xs:duration(\"P3Y3M\")",
            "X:XPTY0004"
        ),
        (
            "op-subtract-yearMonthDurations/K-YearMonthDurationSubtract-9",
            "xs:duration(\"P3Y3M\") + xs:dayTimeDuration(\"P3D\")",
            "X:XPTY0004"
        ),
        ("op-time-equal/op-time-equal2args-1", "xs:time(\"00:00:00Z\") eq xs:time(\"00:00:00Z\")", "T"),
        ("op-time-equal/op-time-equal2args-2", "xs:time(\"08:03:35Z\") eq xs:time(\"00:00:00Z\")", "F"),
        ("op-time-equal/op-time-equal2args-3", "xs:time(\"23:59:59Z\") eq xs:time(\"00:00:00Z\")", "F"),
        ("op-time-equal/op-time-equal2args-4", "xs:time(\"00:00:00Z\") eq xs:time(\"08:03:35Z\")", "F"),
        ("op-time-equal/op-time-equal2args-5", "xs:time(\"00:00:00Z\") eq xs:time(\"23:59:59Z\")", "F"),
        ("op-time-equal/op-time-equal2args-6", "xs:time(\"00:00:00Z\") ne xs:time(\"00:00:00Z\")", "F"),
        ("op-time-equal/op-time-equal2args-7", "xs:time(\"08:03:35Z\") ne xs:time(\"00:00:00Z\")", "T"),
        ("op-time-equal/op-time-equal2args-8", "xs:time(\"23:59:59Z\") ne xs:time(\"00:00:00Z\")", "T"),
        ("op-time-equal/op-time-equal2args-9", "xs:time(\"00:00:00Z\") ne xs:time(\"08:03:35Z\")", "T"),
        ("op-time-equal/op-time-equal2args-10", "xs:time(\"00:00:00Z\") ne xs:time(\"23:59:59Z\")", "T"),
        ("op-time-equal/op-time-equal2args-11", "xs:time(\"00:00:00Z\") le xs:time(\"00:00:00Z\")", "T"),
        ("op-time-equal/op-time-equal2args-12", "xs:time(\"08:03:35Z\") le xs:time(\"00:00:00Z\")", "F"),
        ("op-time-equal/op-time-equal2args-13", "xs:time(\"23:59:59Z\") le xs:time(\"00:00:00Z\")", "F"),
        ("op-time-equal/op-time-equal2args-14", "xs:time(\"00:00:00Z\") le xs:time(\"08:03:35Z\")", "T"),
        ("op-time-equal/op-time-equal2args-15", "xs:time(\"00:00:00Z\") le xs:time(\"23:59:59Z\")", "T"),
        ("op-time-equal/op-time-equal2args-16", "xs:time(\"00:00:00Z\") ge xs:time(\"00:00:00Z\")", "T"),
        ("op-time-equal/op-time-equal2args-17", "xs:time(\"08:03:35Z\") ge xs:time(\"00:00:00Z\")", "T"),
        ("op-time-equal/op-time-equal2args-18", "xs:time(\"23:59:59Z\") ge xs:time(\"00:00:00Z\")", "T"),
        ("op-time-equal/op-time-equal2args-19", "xs:time(\"00:00:00Z\") ge xs:time(\"08:03:35Z\")", "F"),
        ("op-time-equal/op-time-equal2args-20", "xs:time(\"00:00:00Z\") ge xs:time(\"23:59:59Z\")", "F"),
        ("op-time-equal/K-TimeEQ-1", "xs:time(\"23:01:04.12\") eq xs:time(\"23:01:04.12\")", "T"),
        ("op-time-equal/K-TimeEQ-2", "not(xs:time(\"23:01:04.12\") eq xs:time(\"23:01:04.13\"))", "T"),
        ("op-time-equal/K-TimeEQ-3", "xs:time(\"23:01:05.12\") ne xs:time(\"23:01:04.12\")", "T"),
        ("op-time-equal/K-TimeEQ-4", "not(xs:time(\"23:01:04.12\") ne xs:time(\"23:01:04.12\"))", "T"),
        ("op-time-equal/K-TimeEQ-5", "xs:time(\"16:00:12.345-00:00\") eq xs:time(\"16:00:12.345Z\")", "T"),
        ("op-time-equal/K-TimeEQ-6", "xs:time(\"16:00:12.345+00:00\") eq xs:time(\"16:00:12.345Z\")", "T"),
        ("op-time-equal/K-TimeEQ-7", "xs:time(\"16:00:12.345Z\") eq xs:time(\"16:00:12.345Z\")", "T"),
        ("op-time-equal/K-TimeEQ-8", "xs:time(\"16:00:12.345-00:00\") eq xs:time(\"16:00:12.345+00:00\")", "T"),
        ("op-time-equal/K-TimeEQ-9", "xs:time(\"12:12:23\") eq xs:date(\"1999-12-04\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-10", "xs:time(\"12:12:23\") ne xs:date(\"1999-12-04\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-11", "xs:time(\"12:12:23\") le xs:date(\"1999-12-04\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-12", "xs:time(\"12:12:23\") lt xs:date(\"1999-12-04\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-13", "xs:time(\"12:12:23\") ge xs:date(\"1999-12-04\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-14", "xs:time(\"12:12:23\") gt xs:date(\"1999-12-04\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-15", "xs:date(\"1999-12-04\") eq xs:time(\"12:12:23\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-16", "xs:date(\"1999-12-04\") ne xs:time(\"12:12:23\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-17", "xs:date(\"1999-12-04\") le xs:time(\"12:12:23\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-18", "xs:date(\"1999-12-04\") lt xs:time(\"12:12:23\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-19", "xs:date(\"1999-12-04\") ge xs:time(\"12:12:23\")", "X:XPTY0004"),
        ("op-time-equal/K-TimeEQ-20", "xs:date(\"1999-12-04\") gt xs:time(\"12:12:23\")", "X:XPTY0004"),
        ("op-time-equal/K2-TimeEQ-1", "xs:time(\"01:01:01-03:00\") ne xs:time(\"01:01:01+03:00\")", "T"),
        ("op-time-equal/cbcl-time-equal-002", "xs:time(\"14:00:00-12:00\") eq xs:time(\"02:00:00\")", "F"),
        ("op-time-equal/cbcl-time-equal-003", "xs:time(\"02:00:00\") eq xs:time(\"14:00:00-12:00\")", "F"),
        ("op-time-equal/cbcl-time-equal-004", "xs:time(\"00:00:01+01:00\") eq xs:time(\"00:00:00\")", "F"),
        ("op-time-equal/cbcl-time-equal-005", "xs:time(\"00:00:00\") eq xs:time(\"00:00:01+01:00\")", "F"),
        ("op-time-equal/cbcl-time-equal-007", "xs:time(\"14:00:00-12:00\") ne xs:time(\"02:00:00\")", "T"),
        ("op-time-equal/cbcl-time-equal-008", "xs:time(\"02:00:00\") ne xs:time(\"14:00:00-12:00\")", "T"),
        ("op-time-equal/cbcl-time-equal-009", "xs:time(\"00:00:01+01:00\") ne xs:time(\"00:00:00\")", "T"),
        ("op-time-equal/cbcl-time-equal-010", "xs:time(\"00:00:00\") ne xs:time(\"00:00:01+01:00\")", "T"),
        ("op-time-equal/cbcl-time-equal-017", "xs:time(\"00:00:00+12:00\") eq xs:time(\"00:00:01\")", "F"),
        ("op-time-equal/cbcl-time-equal-018", "xs:time(\"00:00:00+12:00\") ne xs:time(\"00:00:01\")", "T"),
        (
            "op-time-equal/cbcl-time-equal-019",
            "if (implicit-timezone() eq xs:dayTimeDuration('PT1H')) then xs:time(\"12:00:00+02:00\") eq xs:time(\"12:00:00\") else xs:time(\"12:00:00+01:00\") eq xs:time(\"12:00:00\")",
            "F"
        ),
        ("op-time-greater-than/op-time-greater-than2args-1", "xs:time(\"00:00:00Z\") gt xs:time(\"00:00:00Z\")", "F"),
        ("op-time-greater-than/op-time-greater-than2args-2", "xs:time(\"08:03:35Z\") gt xs:time(\"00:00:00Z\")", "T"),
        ("op-time-greater-than/op-time-greater-than2args-3", "xs:time(\"23:59:59Z\") gt xs:time(\"00:00:00Z\")", "T"),
        ("op-time-greater-than/op-time-greater-than2args-4", "xs:time(\"00:00:00Z\") gt xs:time(\"08:03:35Z\")", "F"),
        ("op-time-greater-than/op-time-greater-than2args-5", "xs:time(\"00:00:00Z\") gt xs:time(\"23:59:59Z\")", "F"),
        ("op-time-greater-than/op-time-greater-than2args-6", "xs:time(\"00:00:00Z\") le xs:time(\"00:00:00Z\")", "T"),
        ("op-time-greater-than/op-time-greater-than2args-7", "xs:time(\"08:03:35Z\") le xs:time(\"00:00:00Z\")", "F"),
        ("op-time-greater-than/op-time-greater-than2args-8", "xs:time(\"23:59:59Z\") le xs:time(\"00:00:00Z\")", "F"),
        ("op-time-greater-than/op-time-greater-than2args-9", "xs:time(\"00:00:00Z\") le xs:time(\"08:03:35Z\")", "T"),
        ("op-time-greater-than/op-time-greater-than2args-10", "xs:time(\"00:00:00Z\") le xs:time(\"23:59:59Z\")", "T"),
        (
            "op-time-greater-than/op-time-greater-than-1",
            "(xs:time(\"08:00:00+09:00\") gt xs:time(\"17:00:00-06:00\"))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-2",
            "(xs:time(\"08:00:00+09:00\") ge xs:time(\"17:00:00-06:00\"))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-3",
            "fn:not((xs:time(\"14:00:00Z\") gt xs:time(\"13:00:00Z\")))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-4",
            "fn:not(xs:time(\"13:00:00Z\") ge xs:time(\"10:00:00Z\"))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-5",
            "fn:not(xs:time(\"13:00:00Z\") gt xs:time(\"14:00:00Z\"))",
            "T"
        ),
        (
            "op-time-greater-than/op-time-greater-than-6",
            "fn:not(xs:time(\"13:00:00Z\") ge xs:time(\"17:00:00Z\"))",
            "T"
        ),
        (
            "op-time-greater-than/op-time-greater-than-7",
            "(xs:time(\"13:00:00Z\") gt xs:time(\"17:00:00Z\")) and (xs:time(\"13:00:00Z\") gt xs:time(\"17:00:00Z\"))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-8",
            "(xs:time(\"13:00:00Z\") ge xs:time(\"17:00:00Z\")) and (xs:time(\"13:00:00Z\") ge xs:time(\"17:00:00Z\"))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-9",
            "(xs:time(\"13:00:00Z\") gt xs:time(\"17:00:00Z\")) or (xs:time(\"13:00:00Z\") gt xs:time(\"17:00:00Z\"))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-10",
            "(xs:time(\"13:00:00Z\") ge xs:time(\"17:00:00Z\")) or (xs:time(\"13:00:00Z\") ge xs:time(\"17:00:00Z\"))",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-11",
            "(xs:time(\"13:00:00Z\") gt xs:time(\"17:00:00Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-time-greater-than/op-time-greater-than-12",
            "(xs:time(\"13:00:00Z\") ge xs:time(\"17:00:00Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-time-greater-than/op-time-greater-than-13",
            "(xs:time(\"13:00:00Z\") gt xs:time(\"17:00:00Z\")) or (fn:false())",
            "F"
        ),
        (
            "op-time-greater-than/op-time-greater-than-14",
            "(xs:time(\"13:00:00Z\") ge xs:time(\"17:00:00Z\")) or (fn:false())",
            "F"
        ),
        ("op-time-greater-than/K-TimeGT-1", "xs:time(\"23:01:03.12\") lt xs:time(\"23:01:04.12\")", "T"),
        ("op-time-greater-than/K-TimeGT-2", "not(xs:time(\"23:01:04.12\") lt xs:time(\"23:01:04.12\"))", "T"),
        ("op-time-greater-than/K-TimeGT-3", "not(xs:time(\"23:01:05.12\") lt xs:time(\"23:01:04.12\"))", "T"),
        ("op-time-greater-than/K-TimeGT-4", "xs:time(\"23:01:04.12\") le xs:time(\"23:01:04.12\")", "T"),
        ("op-time-greater-than/K-TimeGT-5", "xs:time(\"23:01:03.12\") le xs:time(\"23:01:04.12\")", "T"),
        ("op-time-greater-than/K-TimeGT-6", "not(xs:time(\"23:01:05.12\") le xs:time(\"23:01:04.12\"))", "T"),
        (
            "op-time-greater-than/cbcl-time-greater-than-002",
            "xs:time(\"14:00:00-12:00\") gt xs:time(\"02:00:00\")",
            "T"
        ),
        (
            "op-time-greater-than/cbcl-time-greater-than-003",
            "xs:time(\"02:00:00\") gt xs:time(\"14:00:00-12:00\")",
            "F"
        ),
        (
            "op-time-greater-than/cbcl-time-greater-than-004",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:00\") gt xs:time(\"00:00:00+01:00\") else xs:time(\"00:00:00+01:01\") gt xs:time(\"00:00:00\")",
            "F"
        ),
        (
            "op-time-greater-than/cbcl-time-greater-than-005",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:01+01:00\") gt xs:time(\"00:00:00\") else xs:time(\"00:00:00\") gt xs:time(\"00:00:00+01:01\")",
            "T"
        ),
        (
            "op-time-greater-than/cbcl-time-greater-than-007",
            "xs:time(\"14:00:00-12:00\") le xs:time(\"02:00:00\")",
            "F"
        ),
        (
            "op-time-greater-than/cbcl-time-greater-than-008",
            "xs:time(\"02:00:00\") le xs:time(\"14:00:00-12:00\")",
            "T"
        ),
        (
            "op-time-greater-than/cbcl-time-greater-than-009",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:00\") le xs:time(\"00:00:00+01:00\") else xs:time(\"00:00:00+01:01\") le xs:time(\"00:00:00\")",
            "T"
        ),
        (
            "op-time-greater-than/cbcl-time-greater-than-010",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:01+01:00\") le xs:time(\"00:00:00\") else xs:time(\"00:00:00\") le xs:time(\"00:00:00+01:01\")",
            "F"
        ),
        ("op-time-less-than/op-time-less-than2args-1", "xs:time(\"00:00:00Z\") lt xs:time(\"00:00:00Z\")", "F"),
        ("op-time-less-than/op-time-less-than2args-2", "xs:time(\"08:03:35Z\") lt xs:time(\"00:00:00Z\")", "F"),
        ("op-time-less-than/op-time-less-than2args-3", "xs:time(\"23:59:59Z\") lt xs:time(\"00:00:00Z\")", "F"),
        ("op-time-less-than/op-time-less-than2args-4", "xs:time(\"00:00:00Z\") lt xs:time(\"08:03:35Z\")", "T"),
        ("op-time-less-than/op-time-less-than2args-5", "xs:time(\"00:00:00Z\") lt xs:time(\"23:59:59Z\")", "T"),
        ("op-time-less-than/op-time-less-than2args-6", "xs:time(\"00:00:00Z\") ge xs:time(\"00:00:00Z\")", "T"),
        ("op-time-less-than/op-time-less-than2args-7", "xs:time(\"08:03:35Z\") ge xs:time(\"00:00:00Z\")", "T"),
        ("op-time-less-than/op-time-less-than2args-8", "xs:time(\"23:59:59Z\") ge xs:time(\"00:00:00Z\")", "T"),
        ("op-time-less-than/op-time-less-than2args-9", "xs:time(\"00:00:00Z\") ge xs:time(\"08:03:35Z\")", "F"),
        ("op-time-less-than/op-time-less-than2args-10", "xs:time(\"00:00:00Z\") ge xs:time(\"23:59:59Z\")", "F"),
        ("op-time-less-than/op-time-less-than-1", "(xs:time(\"12:00:00-05:00\") lt xs:time(\"23:00:00+06:00\"))", "F"),
        ("op-time-less-than/op-time-less-than-2", "xs:time(\"11:00:00-05:00\") lt xs:time(\"17:00:00Z\")", "T"),
        ("op-time-less-than/op-time-less-than-3", "fn:not((xs:time(\"13:00:00Z\") lt xs:time(\"14:00:00Z\")))", "F"),
        ("op-time-less-than/op-time-less-than-4", "fn:not(xs:time(\"13:00:00Z\") le xs:time(\"14:00:00Z\"))", "F"),
        ("op-time-less-than/op-time-less-than-5", "fn:not(xs:time(\"13:00:00Z\") lt xs:time(\"10:00:00Z\"))", "T"),
        ("op-time-less-than/op-time-less-than-6", "fn:not(xs:time(\"13:00:00Z\") le xs:time(\"12:00:00Z\"))", "T"),
        (
            "op-time-less-than/op-time-less-than-7",
            "(xs:time(\"13:00:00Z\") lt xs:time(\"17:00:00Z\")) and (xs:time(\"13:00:00Z\") lt xs:time(\"17:00:00Z\"))",
            "T"
        ),
        (
            "op-time-less-than/op-time-less-than-8",
            "(xs:time(\"13:00:00Z\") le xs:time(\"17:00:00Z\")) and (xs:time(\"13:00:00Z\") le xs:time(\"17:00:00Z\"))",
            "T"
        ),
        (
            "op-time-less-than/op-time-less-than-9",
            "(xs:time(\"13:00:00Z\") lt xs:time(\"17:00:00Z\")) or (xs:time(\"13:00:00Z\") lt xs:time(\"17:00:00Z\"))",
            "T"
        ),
        (
            "op-time-less-than/op-time-less-than-10",
            "(xs:time(\"13:00:00Z\") le xs:time(\"17:00:00Z\")) or (xs:time(\"13:00:00Z\") le xs:time(\"17:00:00Z\"))",
            "T"
        ),
        (
            "op-time-less-than/op-time-less-than-11",
            "(xs:time(\"13:00:00Z\") lt xs:time(\"17:00:00Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-time-less-than/op-time-less-than-12",
            "(xs:time(\"13:00:00Z\") le xs:time(\"17:00:00Z\")) or (fn:true())",
            "T"
        ),
        (
            "op-time-less-than/op-time-less-than-13",
            "(xs:time(\"13:00:00Z\") lt xs:time(\"17:00:00Z\")) or (fn:false())",
            "T"
        ),
        (
            "op-time-less-than/op-time-less-than-14",
            "(xs:time(\"13:00:00Z\") le xs:time(\"17:00:00Z\")) or (fn:false())",
            "T"
        ),
        ("op-time-less-than/K-TimeLT-1", "xs:time(\"23:01:05.12\") gt xs:time(\"23:01:04.12\")", "T"),
        ("op-time-less-than/K-TimeLT-2", "not(xs:time(\"23:01:03.12\") gt xs:time(\"23:01:04.12\"))", "T"),
        ("op-time-less-than/K-TimeLT-3", "not(xs:time(\"23:01:04.12\") gt xs:time(\"23:01:04.12\"))", "T"),
        ("op-time-less-than/K-TimeLT-4", "xs:time(\"23:01:04.12\") ge xs:time(\"23:01:04.12\")", "T"),
        ("op-time-less-than/K-TimeLT-5", "xs:time(\"23:01:05.12\") ge xs:time(\"23:01:04.12\")", "T"),
        ("op-time-less-than/K-TimeLT-6", "not(xs:time(\"23:01:03.12\") ge xs:time(\"23:01:04.12\"))", "T"),
        ("op-time-less-than/cbcl-time-less-than-002", "xs:time(\"14:00:00-12:00\") lt xs:time(\"02:00:00\")", "F"),
        ("op-time-less-than/cbcl-time-less-than-003", "xs:time(\"02:00:00\") lt xs:time(\"14:00:00-12:00\")", "T"),
        (
            "op-time-less-than/cbcl-time-less-than-004",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:00\") lt xs:time(\"00:00:00+01:00\") else xs:time(\"00:00:00+01:01\") lt xs:time(\"00:00:00\")",
            "T"
        ),
        (
            "op-time-less-than/cbcl-time-less-than-005",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:00+01:00\") lt xs:time(\"00:00:00\") else xs:time(\"00:00:00\") lt xs:time(\"00:00:00+01:01\")",
            "F"
        ),
        ("op-time-less-than/cbcl-time-less-than-007", "xs:time(\"14:00:00-12:00\") ge xs:time(\"02:00:00\")", "T"),
        ("op-time-less-than/cbcl-time-less-than-008", "xs:time(\"02:00:00\") ge xs:time(\"14:00:00-12:00\")", "F"),
        (
            "op-time-less-than/cbcl-time-less-than-009",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:00\") ge xs:time(\"00:00:01+01:00\") else xs:time(\"00:00:00+01:01\") ge xs:time(\"00:00:00\")",
            "F"
        ),
        (
            "op-time-less-than/cbcl-time-less-than-010",
            "if (implicit-timezone() gt xs:dayTimeDuration('PT1H')) then xs:time(\"00:00:01+01:00\") ge xs:time(\"00:00:00\") else xs:time(\"00:00:00\") ge xs:time(\"00:00:00+01:01\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") gt xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") gt xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") gt xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") gt xs:yearMonthDuration(\"P1000Y6M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") gt xs:yearMonthDuration(\"P2030Y12M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-6",
            "xs:yearMonthDuration(\"P0Y0M\") le xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-7",
            "xs:yearMonthDuration(\"P1000Y6M\") le xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-8",
            "xs:yearMonthDuration(\"P2030Y12M\") le xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-9",
            "xs:yearMonthDuration(\"P0Y0M\") le xs:yearMonthDuration(\"P1000Y6M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than2args-10",
            "xs:yearMonthDuration(\"P0Y0M\") le xs:yearMonthDuration(\"P2030Y12M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-3",
            "fn:not((xs:yearMonthDuration(\"P20Y123M\") gt xs:yearMonthDuration(\"P20Y11M\")))",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-4",
            "fn:not(xs:yearMonthDuration(\"P20Y10M\") ge xs:yearMonthDuration(\"P20Y10M\"))",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-5",
            "fn:not(xs:yearMonthDuration(\"P20Y09M\") gt xs:yearMonthDuration(\"P20Y10M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-6",
            "fn:not(xs:yearMonthDuration(\"P10Y07M\") ge xs:yearMonthDuration(\"P10Y09M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-7",
            "(xs:yearMonthDuration(\"P10Y09M\") gt xs:yearMonthDuration(\"P09Y10M\")) and (xs:yearMonthDuration(\"P10Y01M\") gt xs:yearMonthDuration(\"P08Y06M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-8",
            "(xs:yearMonthDuration(\"P10Y09M\") ge xs:yearMonthDuration(\"P10Y01M\")) and (xs:yearMonthDuration(\"P02Y04M\") ge xs:yearMonthDuration(\"P09Y07M\"))",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-9",
            "(xs:yearMonthDuration(\"P10Y08M\") gt xs:yearMonthDuration(\"P10Y07M\")) or (xs:yearMonthDuration(\"P10Y09M\") gt xs:yearMonthDuration(\"P10Y09M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-10",
            "(xs:yearMonthDuration(\"P10Y01M\") ge xs:yearMonthDuration(\"P09Y06M\")) or (xs:yearMonthDuration(\"P15Y01M\") ge xs:yearMonthDuration(\"P02Y04M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-11",
            "(xs:yearMonthDuration(\"P10Y02M\") gt xs:yearMonthDuration(\"P01Y10M\")) or (fn:true())",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-12",
            "(xs:yearMonthDuration(\"P10Y01M\") ge xs:yearMonthDuration(\"P09Y05M\")) or (fn:true())",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-13",
            "(xs:yearMonthDuration(\"P30Y10M\") gt xs:yearMonthDuration(\"P01Y02M\")) or (fn:false())",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/op-yearMonthDuration-greater-than-14",
            "(xs:yearMonthDuration(\"P10Y05M\") ge xs:yearMonthDuration(\"P20Y10M\")) or (fn:false())",
            "F"
        ),
        (
            "op-yearMonthDuration-greater-than/K-YearMonthDurationGT-1",
            "xs:yearMonthDuration(\"P1999Y10M\") gt xs:yearMonthDuration(\"P1999Y9M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/K-YearMonthDurationGT-2",
            "not(xs:yearMonthDuration(\"P1999Y10M\") gt xs:yearMonthDuration(\"P1999Y10M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/K-YearMonthDurationGT-3",
            "not(xs:yearMonthDuration(\"P1999Y10M\") gt xs:yearMonthDuration(\"P1999Y11M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/K-YearMonthDurationGT-4",
            "xs:yearMonthDuration(\"P1999Y11M\") ge xs:yearMonthDuration(\"P1999Y10M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/K-YearMonthDurationGT-5",
            "xs:yearMonthDuration(\"P1999Y10M\") ge xs:yearMonthDuration(\"P1999Y10M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-greater-than/K-YearMonthDurationGT-6",
            "not(xs:yearMonthDuration(\"P1999Y9M\") ge xs:yearMonthDuration(\"P1999Y10M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-1",
            "xs:yearMonthDuration(\"P0Y0M\") lt xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-2",
            "xs:yearMonthDuration(\"P1000Y6M\") lt xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-3",
            "xs:yearMonthDuration(\"P2030Y12M\") lt xs:yearMonthDuration(\"P0Y0M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-4",
            "xs:yearMonthDuration(\"P0Y0M\") lt xs:yearMonthDuration(\"P1000Y6M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-5",
            "xs:yearMonthDuration(\"P0Y0M\") lt xs:yearMonthDuration(\"P2030Y12M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-6",
            "xs:yearMonthDuration(\"P0Y0M\") ge xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2argsNew-7",
            "xs:yearMonthDuration(\"P1000Y6M\") ge xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-8",
            "xs:yearMonthDuration(\"P2030Y12M\") ge xs:yearMonthDuration(\"P0Y0M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-9",
            "xs:yearMonthDuration(\"P0Y0M\") ge xs:yearMonthDuration(\"P1000Y6M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than2args-10",
            "xs:yearMonthDuration(\"P0Y0M\") ge xs:yearMonthDuration(\"P2030Y12M\")",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-3",
            "fn:not((xs:yearMonthDuration(\"P20Y10M\") lt xs:yearMonthDuration(\"P20Y11M\")))",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-4",
            "fn:not(xs:yearMonthDuration(\"P20Y10M\") le xs:yearMonthDuration(\"P20Y10M\"))",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-5",
            "fn:not(xs:yearMonthDuration(\"P20Y10M\") lt xs:yearMonthDuration(\"P20Y09M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-6",
            "fn:not(xs:yearMonthDuration(\"P10Y09M\") le xs:yearMonthDuration(\"P10Y07M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-thanNew-7",
            "(xs:yearMonthDuration(\"P10Y09M\") lt xs:yearMonthDuration(\"P09Y10M\")) and (xs:yearMonthDuration(\"P10Y01M\") lt xs:yearMonthDuration(\"P08Y06M\"))",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-8",
            "(xs:yearMonthDuration(\"P10Y09M\") le xs:yearMonthDuration(\"P10Y01M\")) and (xs:yearMonthDuration(\"P02Y04M\") le xs:yearMonthDuration(\"P09Y07M\"))",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-9",
            "(xs:yearMonthDuration(\"P10Y08M\") lt xs:yearMonthDuration(\"P10Y07M\")) or (xs:yearMonthDuration(\"P10Y09M\") lt xs:yearMonthDuration(\"P10Y09M\"))",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-10",
            "(xs:yearMonthDuration(\"P10Y01M\") le xs:yearMonthDuration(\"P09Y06M\")) or (xs:yearMonthDuration(\"P15Y01M\") le xs:yearMonthDuration(\"P02Y04M\"))",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-11",
            "(xs:yearMonthDuration(\"P10Y02M\") lt xs:yearMonthDuration(\"P01Y10M\")) or (fn:true())",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-12",
            "(xs:yearMonthDuration(\"P10Y01M\") le xs:yearMonthDuration(\"P09Y05M\")) or (fn:true())",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-13",
            "(xs:yearMonthDuration(\"P30Y10M\") lt xs:yearMonthDuration(\"P01Y02M\")) or (fn:false())",
            "F"
        ),
        (
            "op-yearMonthDuration-less-than/op-yearMonthDuration-less-than-14",
            "(xs:yearMonthDuration(\"P10Y05M\") le xs:yearMonthDuration(\"P20Y10M\")) or (fn:false())",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/K-YearMonthDurationLT-1",
            "xs:yearMonthDuration(\"P1999Y9M\") lt xs:yearMonthDuration(\"P1999Y10M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/K-YearMonthDurationLT-2",
            "not(xs:yearMonthDuration(\"P1999Y10M\") lt xs:yearMonthDuration(\"P1999Y10M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/K-YearMonthDurationLT-3",
            "not(xs:yearMonthDuration(\"P1999Y10M\") lt xs:yearMonthDuration(\"P1999Y9M\"))",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/K-YearMonthDurationLT-4",
            "xs:yearMonthDuration(\"P1999Y10M\") le xs:yearMonthDuration(\"P1999Y10M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/K-YearMonthDurationLT-5",
            "xs:yearMonthDuration(\"P1999Y9M\") le xs:yearMonthDuration(\"P1999Y10M\")",
            "T"
        ),
        (
            "op-yearMonthDuration-less-than/K-YearMonthDurationLT-6",
            "not(xs:yearMonthDuration(\"P1999Y10M\") le xs:yearMonthDuration(\"P1999Y9M\"))",
            "T"
        ),
    ];

    public static readonly (string Id, string Reason)[] OutOfShape =
    [
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration2args-2",
            "The assertion is not one of the shapes of the selection."
        ),
        (
            "op-divide-dayTimeDuration/op-divide-dayTimeDuration2args-3",
            "The assertion is not one of the shapes of the selection."
        ),
        (
            "op-duration-equal/distinct-duration-equal-1",
            "fn:distinct-values compares two durations of different subtypes by representation, so it keeps both of two equal zero durations; the comparison by value is open in #732."
        ),
    ];

    public static readonly string[] NotXPath20 =
    [
        "op-add-dayTimeDuration-to-date/cbcl-add-dayTimeDuration-to-date-001",
        "op-add-dayTimeDuration-to-date/cbcl-add-dayTimeDuration-to-date-002",
        "op-add-dayTimeDuration-to-date/cbcl-add-dayTimeDuration-to-date-003",
        "op-add-dayTimeDuration-to-date/Add-UnionType-36",
        "op-add-dayTimeDuration-to-date/Add-UnionType-37",
        "op-add-dayTimeDuration-to-dateTime/cbcl-add-dayTimeDuration-to-dateTime-001",
        "op-add-dayTimeDuration-to-dateTime/cbcl-add-dayTimeDuration-to-dateTime-002",
        "op-add-dayTimeDuration-to-dateTime/cbcl-add-dayTimeDuration-to-dateTime-003",
        "op-add-dayTimeDuration-to-time/cbcl-add-dayTimeDuration-to-time-001",
        "op-add-dayTimeDuration-to-time/cbcl-add-dayTimeDuration-to-time-002",
        "op-add-dayTimeDurations/cbcl-add-dayTimeDurations-001",
        "op-add-dayTimeDurations/cbcl-add-dayTimeDurations-002",
        "op-add-dayTimeDurations/cbcl-add-dayTimeDurations-003",
        "op-add-dayTimeDurations/cbcl-plus-001",
        "op-add-dayTimeDurations/cbcl-plus-002",
        "op-add-dayTimeDurations/cbcl-plus-003",
        "op-add-dayTimeDurations/cbcl-plus-004",
        "op-add-dayTimeDurations/cbcl-plus-005",
        "op-add-dayTimeDurations/cbcl-plus-006",
        "op-add-dayTimeDurations/cbcl-plus-007",
        "op-add-dayTimeDurations/cbcl-plus-008",
        "op-add-dayTimeDurations/cbcl-plus-009",
        "op-add-dayTimeDurations/cbcl-plus-010",
        "op-add-dayTimeDurations/cbcl-plus-011",
        "op-add-dayTimeDurations/cbcl-plus-012",
        "op-add-dayTimeDurations/cbcl-plus-013",
        "op-add-dayTimeDurations/cbcl-plus-014",
        "op-add-dayTimeDurations/cbcl-plus-015",
        "op-add-dayTimeDurations/cbcl-plus-016",
        "op-add-dayTimeDurations/cbcl-plus-017",
        "op-add-dayTimeDurations/cbcl-plus-018",
        "op-add-dayTimeDurations/cbcl-plus-019",
        "op-add-dayTimeDurations/cbcl-plus-020",
        "op-add-dayTimeDurations/cbcl-plus-021",
        "op-add-dayTimeDurations/cbcl-plus-022",
        "op-add-dayTimeDurations/cbcl-plus-023",
        "op-add-dayTimeDurations/cbcl-plus-024",
        "op-add-dayTimeDurations/cbcl-plus-025",
        "op-add-dayTimeDurations/cbcl-plus-026",
        "op-add-dayTimeDurations/cbcl-plus-027",
        "op-add-dayTimeDurations/cbcl-plus-028",
        "op-add-dayTimeDurations/cbcl-plus-029",
        "op-add-dayTimeDurations/cbcl-plus-030",
        "op-add-dayTimeDurations/cbcl-plus-031",
        "op-add-dayTimeDurations/cbcl-plus-032",
        "op-add-dayTimeDurations/cbcl-plus-033",
        "op-add-dayTimeDurations/cbcl-plus-034",
        "op-add-yearMonthDuration-to-date/cbcl-add-yearMonthDuration-to-date-001",
        "op-add-yearMonthDuration-to-date/cbcl-add-yearMonthDuration-to-date-002",
        "op-add-yearMonthDuration-to-date/cbcl-add-yearMonthDuration-to-date-003",
        "op-add-yearMonthDuration-to-dateTime/cbcl-add-yearMonthDuration-to-dateTime-001",
        "op-add-yearMonthDuration-to-dateTime/cbcl-add-yearMonthDuration-to-dateTime-002",
        "op-add-yearMonthDuration-to-dateTime/cbcl-add-yearMonthDuration-to-dateTime-003",
        "op-add-yearMonthDurations/cbcl-add-yearMonthDurations-001",
        "op-add-yearMonthDurations/cbcl-add-yearMonthDurations-002",
        "op-add-yearMonthDurations/cbcl-add-yearMonthDurations-003",
        "op-date-equal/cbcl-date-equal-001",
        "op-date-equal/cbcl-date-equal-006",
        "op-date-equal/cbcl-date-equal-011",
        "op-date-equal/cbcl-date-equal-012",
        "op-date-equal/cbcl-date-equal-013",
        "op-date-equal/cbcl-date-equal-014",
        "op-date-equal/cbcl-date-equal-015",
        "op-date-equal/cbcl-date-equal-016",
        "op-date-greater-than/cbcl-date-le-001",
        "op-date-greater-than/cbcl-date-greater-equal-001",
        "op-date-greater-than/cbcl-date-greater-equal-006",
        "op-date-greater-than/cbcl-date-greater-than-011",
        "op-date-greater-than/cbcl-date-greater-than-012",
        "op-date-greater-than/cbcl-date-greater-than-013",
        "op-date-greater-than/cbcl-date-greater-than-014",
        "op-date-greater-than/cbcl-date-greater-than-015",
        "op-date-greater-than/cbcl-date-greater-than-016",
        "op-date-less-than/cbcl-date-lt-001",
        "op-date-less-than/cbcl-date-less-than-001",
        "op-date-less-than/cbcl-date-less-than-006",
        "op-date-less-than/cbcl-date-less-than-011",
        "op-date-less-than/cbcl-date-less-than-012",
        "op-date-less-than/cbcl-date-less-than-013",
        "op-date-less-than/cbcl-date-less-than-014",
        "op-date-less-than/cbcl-date-less-than-015",
        "op-date-less-than/cbcl-date-less-than-016",
        "op-dateTime-equal/cbcl-dateTime-equal-001",
        "op-dateTime-equal/cbcl-dateTime-equal-006",
        "op-dateTime-equal/cbcl-dateTime-equal-011",
        "op-dateTime-equal/cbcl-dateTime-equal-012",
        "op-dateTime-equal/cbcl-dateTime-equal-013",
        "op-dateTime-equal/cbcl-dateTime-equal-014",
        "op-dateTime-equal/cbcl-dateTime-equal-015",
        "op-dateTime-equal/cbcl-dateTime-equal-016",
        "op-dateTime-equal/UnionType-equal-01",
        "op-dateTime-equal/UnionType-equal-02",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-001",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-006",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-011",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-012",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-013",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-014",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-015",
        "op-dateTime-greater-than/cbcl-dateTime-greater-than-016",
        "op-dateTime-less-than/cbcl-dateTime-less-than-001",
        "op-dateTime-less-than/cbcl-dateTime-less-than-006",
        "op-dateTime-less-than/cbcl-dateTime-less-than-011",
        "op-dateTime-less-than/cbcl-dateTime-less-than-012",
        "op-dateTime-less-than/cbcl-dateTime-less-than-013",
        "op-dateTime-less-than/cbcl-dateTime-less-than-014",
        "op-dateTime-less-than/cbcl-dateTime-less-than-015",
        "op-dateTime-less-than/cbcl-dateTime-less-than-016",
        "op-dayTimeDuration-greater-than/cbcl-dayTimeDuration-greater-than-001",
        "op-dayTimeDuration-greater-than/cbcl-dayTimeDuration-greater-than-002",
        "op-dayTimeDuration-greater-than/cbcl-dayTimeDuration-greater-than-003",
        "op-dayTimeDuration-greater-than/cbcl-dayTimeDuration-greater-than-004",
        "op-dayTimeDuration-greater-than/cbcl-dayTimeDuration-greater-than-005",
        "op-dayTimeDuration-greater-than/cbcl-dayTimeDuration-greater-than-006",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-001",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-002",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-003",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-004",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-005",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-006",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-007",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-008",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-009",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-010",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-011",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-equal-012",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-001",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-002",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-003",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-004",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-005",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-006",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-007",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-008",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-009",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-010",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-011",
        "op-dayTimeDuration-greater-than/cbcl-value-greater-than-012",
        "op-dayTimeDuration-less-than/cbcl-dayTimeDuration-less-than-001",
        "op-dayTimeDuration-less-than/cbcl-dayTimeDuration-less-than-002",
        "op-dayTimeDuration-less-than/cbcl-dayTimeDuration-less-than-003",
        "op-dayTimeDuration-less-than/cbcl-dayTimeDuration-less-than-004",
        "op-dayTimeDuration-less-than/cbcl-dayTimeDuration-less-than-005",
        "op-dayTimeDuration-less-than/cbcl-dayTimeDuration-less-than-006",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-001",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-002",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-003",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-004",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-005",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-006",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-007",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-008",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-009",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-010",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-011",
        "op-dayTimeDuration-less-than/cbcl-value-less-equal-012",
        "op-divide-dayTimeDuration/cbcl-divide-dayTimeDuration-001",
        "op-divide-dayTimeDuration/cbcl-divide-dayTimeDuration-002",
        "op-divide-dayTimeDuration/cbcl-div-001",
        "op-divide-dayTimeDuration/cbcl-div-002",
        "op-divide-dayTimeDuration/cbcl-div-003",
        "op-divide-dayTimeDuration/cbcl-div-004",
        "op-divide-dayTimeDuration/cbcl-div-005",
        "op-divide-dayTimeDuration/cbcl-div-006",
        "op-divide-dayTimeDuration/cbcl-div-007",
        "op-divide-dayTimeDuration/cbcl-div-008",
        "op-divide-dayTimeDuration/cbcl-div-009",
        "op-divide-dayTimeDuration/cbcl-div-010",
        "op-divide-dayTimeDuration/cbcl-div-011",
        "op-divide-dayTimeDuration/cbcl-div-012",
        "op-divide-dayTimeDuration/cbcl-div-013",
        "op-divide-dayTimeDuration/cbcl-div-014",
        "op-divide-dayTimeDuration/cbcl-div-015",
        "op-divide-dayTimeDuration/cbcl-div-016",
        "op-divide-yearMonthDuration/cbcl-divide-yearMonthDuration-001",
        "op-divide-yearMonthDuration/cbcl-divide-yearMonthDuration-002",
        "op-divide-yearMonthDuration/cbcl-divide-yearMonthDuration-003",
        "op-duration-equal/cbcl-dayTimeDuration-equal-001",
        "op-duration-equal/cbcl-dayTimeDuration-equal-002",
        "op-duration-equal/cbcl-dayTimeDuration-equal-003",
        "op-duration-equal/cbcl-dayTimeDuration-equal-004",
        "op-duration-equal/cbcl-dayTimeDuration-equal-005",
        "op-duration-equal/cbcl-dayTimeDuration-equal-006",
        "op-duration-equal/cbcl-duration-equal-001",
        "op-duration-equal/cbcl-duration-equal-002",
        "op-duration-equal/cbcl-duration-equal-003",
        "op-duration-equal/cbcl-duration-equal-004",
        "op-duration-equal/cbcl-duration-equal-005",
        "op-duration-equal/cbcl-duration-equal-006",
        "op-duration-equal/cbcl-yearMonthDuration-equal-001",
        "op-duration-equal/cbcl-yearMonthDuration-equal-002",
        "op-gDay-equal/cbcl-gDay-equal-001",
        "op-gDay-equal/cbcl-gDay-equal-006",
        "op-gDay-equal/cbcl-gDay-equal-011",
        "op-gDay-equal/cbcl-gDay-equal-012",
        "op-gDay-equal/cbcl-gDay-equal-013",
        "op-gDay-equal/cbcl-gDay-equal-014",
        "op-gMonth-equal/cbcl-gMonth-equal-001",
        "op-gMonth-equal/cbcl-gMonth-equal-006",
        "op-gMonth-equal/cbcl-gMonth-equal-011",
        "op-gMonth-equal/cbcl-gMonth-equal-012",
        "op-gMonth-equal/cbcl-gMonth-equal-013",
        "op-gMonth-equal/cbcl-gMonth-equal-014",
        "op-gMonthDay-equal/cbcl-gMonthDay-equal-001",
        "op-gMonthDay-equal/cbcl-gMonthDay-equal-006",
        "op-gMonthDay-equal/cbcl-gMonthDay-equal-011",
        "op-gMonthDay-equal/cbcl-gMonthDay-equal-012",
        "op-gMonthDay-equal/cbcl-gMonthDay-equal-013",
        "op-gMonthDay-equal/cbcl-gMonthDay-equal-014",
        "op-gYear-equal/cbcl-gYear-equal-001",
        "op-gYear-equal/cbcl-gYear-equal-006",
        "op-gYear-equal/cbcl-gYear-equal-011",
        "op-gYear-equal/cbcl-gYear-equal-012",
        "op-gYear-equal/cbcl-gYear-equal-013",
        "op-gYear-equal/cbcl-gYear-equal-014",
        "op-gYearMonth-equal/cbcl-gYearMonth-equal-001",
        "op-gYearMonth-equal/cbcl-gYearMonth-equal-006",
        "op-gYearMonth-equal/cbcl-gYearMonth-equal-011",
        "op-gYearMonth-equal/cbcl-gYearMonth-equal-012",
        "op-gYearMonth-equal/cbcl-gYearMonth-equal-013",
        "op-gYearMonth-equal/cbcl-gYearMonth-equal-014",
        "op-multiply-dayTimeDuration/cbcl-multiply-dayTimeDuration-001",
        "op-multiply-dayTimeDuration/cbcl-multiply-dayTimeDuration-002",
        "op-multiply-dayTimeDuration/cbcl-multiply-dayTimeDuration-003",
        "op-multiply-dayTimeDuration/cbcl-multiply-dayTimeDuration-004",
        "op-multiply-dayTimeDuration/cbcl-multiply-dayTimeDuration-005",
        "op-multiply-dayTimeDuration/cbcl-multiply-dayTimeDuration-006",
        "op-multiply-dayTimeDuration/cbcl-times-001",
        "op-multiply-dayTimeDuration/cbcl-times-002",
        "op-multiply-dayTimeDuration/cbcl-times-003",
        "op-multiply-dayTimeDuration/cbcl-times-004",
        "op-multiply-dayTimeDuration/cbcl-times-005",
        "op-multiply-dayTimeDuration/cbcl-times-006",
        "op-multiply-dayTimeDuration/cbcl-times-007",
        "op-multiply-dayTimeDuration/cbcl-times-008",
        "op-multiply-yearMonthDuration/cbcl-multiply-yearMonthDuration-001",
        "op-multiply-yearMonthDuration/cbcl-multiply-yearMonthDuration-002",
        "op-multiply-yearMonthDuration/cbcl-multiply-yearMonthDuration-004",
        "op-multiply-yearMonthDuration/cbcl-multiply-yearMonthDuration-005",
        "op-subtract-dayTimeDuration-from-date/cbcl-subtract-dayTimeDuration-from-date-001",
        "op-subtract-dayTimeDuration-from-date/cbcl-subtract-dayTimeDuration-from-date-002",
        "op-subtract-dayTimeDuration-from-dateTime/cbcl-subtract-dayTimeDuration-from-dateTime-001",
        "op-subtract-dayTimeDuration-from-dateTime/cbcl-subtract-dayTimeDuration-from-dateTime-002",
        "op-subtract-dayTimeDuration-from-time/cbcl-subtract-dayTimeDuration-from-time-001",
        "op-subtract-dayTimeDurations/cbcl-subtract-dayTimeDurations-001",
        "op-subtract-dayTimeDurations/cbcl-subtract-dayTimeDurations-002",
        "op-subtract-dayTimeDurations/cbcl-subtract-dayTimeDurations-003",
        "op-subtract-dayTimeDurations/cbcl-minus-001",
        "op-subtract-dayTimeDurations/cbcl-minus-002",
        "op-subtract-dayTimeDurations/cbcl-minus-003",
        "op-subtract-dayTimeDurations/cbcl-minus-004",
        "op-subtract-dayTimeDurations/cbcl-minus-005",
        "op-subtract-dayTimeDurations/cbcl-minus-006",
        "op-subtract-dayTimeDurations/cbcl-minus-007",
        "op-subtract-dayTimeDurations/cbcl-minus-008",
        "op-subtract-dayTimeDurations/cbcl-minus-009",
        "op-subtract-dayTimeDurations/cbcl-minus-010",
        "op-subtract-dayTimeDurations/cbcl-minus-011",
        "op-subtract-dayTimeDurations/cbcl-minus-012",
        "op-subtract-dayTimeDurations/cbcl-minus-013",
        "op-subtract-dayTimeDurations/cbcl-minus-014",
        "op-subtract-dayTimeDurations/cbcl-minus-015",
        "op-subtract-dayTimeDurations/cbcl-minus-016",
        "op-subtract-dayTimeDurations/cbcl-minus-017",
        "op-subtract-dayTimeDurations/cbcl-minus-018",
        "op-subtract-dayTimeDurations/cbcl-minus-019",
        "op-subtract-dayTimeDurations/cbcl-minus-020",
        "op-subtract-dayTimeDurations/cbcl-minus-021",
        "op-subtract-dayTimeDurations/cbcl-minus-022",
        "op-subtract-dayTimeDurations/cbcl-minus-023",
        "op-subtract-dayTimeDurations/cbcl-minus-024",
        "op-subtract-dayTimeDurations/cbcl-minus-025",
        "op-subtract-dayTimeDurations/cbcl-minus-026",
        "op-subtract-dayTimeDurations/cbcl-minus-027",
        "op-subtract-dayTimeDurations/cbcl-minus-028",
        "op-subtract-dayTimeDurations/cbcl-minus-029",
        "op-subtract-dayTimeDurations/cbcl-minus-030",
        "op-subtract-dayTimeDurations/cbcl-minus-031",
        "op-subtract-dayTimeDurations/cbcl-minus-032",
        "op-subtract-dayTimeDurations/cbcl-minus-033",
        "op-subtract-dayTimeDurations/cbcl-minus-034",
        "op-subtract-yearMonthDuration-from-date/cbcl-subtract-yearMonthDuration-from-date-001",
        "op-subtract-yearMonthDuration-from-date/cbcl-subtract-yearMonthDuration-from-date-002",
        "op-subtract-yearMonthDuration-from-dateTime/cbcl-subtract-yearMonthDuration-from-dateTime-001",
        "op-subtract-yearMonthDuration-from-dateTime/cbcl-subtract-yearMonthDuration-from-dateTime-002",
        "op-subtract-yearMonthDurations/cbcl-subtract-yearMonthDurations-001",
        "op-subtract-yearMonthDurations/cbcl-subtract-yearMonthDurations-002",
        "op-subtract-yearMonthDurations/cbcl-subtract-yearMonthDurations-003",
        "op-time-equal/cbcl-time-equal-001",
        "op-time-equal/cbcl-time-equal-006",
        "op-time-equal/cbcl-time-equal-011",
        "op-time-equal/cbcl-time-equal-012",
        "op-time-equal/cbcl-time-equal-013",
        "op-time-equal/cbcl-time-equal-014",
        "op-time-greater-than/cbcl-time-greater-than-001",
        "op-time-greater-than/cbcl-time-greater-than-006",
        "op-time-greater-than/cbcl-time-greater-than-011",
        "op-time-greater-than/cbcl-time-greater-than-012",
        "op-time-greater-than/cbcl-time-greater-than-013",
        "op-time-greater-than/cbcl-time-greater-than-014",
        "op-time-less-than/cbcl-time-less-than-001",
        "op-time-less-than/cbcl-time-less-than-006",
        "op-time-less-than/cbcl-time-less-than-011",
        "op-time-less-than/cbcl-time-less-than-012",
        "op-time-less-than/cbcl-time-less-than-013",
        "op-time-less-than/cbcl-time-less-than-014",
        "op-yearMonthDuration-greater-than/cbcl-yearMonthDuration-greater-than-001",
        "op-yearMonthDuration-greater-than/cbcl-yearMonthDuration-greater-than-002",
        "op-yearMonthDuration-greater-than/cbcl-yearMonthDuration-greater-than-003",
        "op-yearMonthDuration-greater-than/cbcl-yearMonthDuration-greater-than-004",
        "op-yearMonthDuration-greater-than/cbcl-yearMonthDuration-greater-than-005",
        "op-yearMonthDuration-greater-than/cbcl-yearMonthDuration-greater-than-006",
        "op-yearMonthDuration-less-than/cbcl-yearMonthDuration-less-than-001",
        "op-yearMonthDuration-less-than/cbcl-yearMonthDuration-less-than-002",
        "op-yearMonthDuration-less-than/cbcl-yearMonthDuration-less-than-003",
        "op-yearMonthDuration-less-than/cbcl-yearMonthDuration-less-than-004",
        "op-yearMonthDuration-less-than/cbcl-yearMonthDuration-less-than-005",
        "op-yearMonthDuration-less-than/cbcl-yearMonthDuration-less-than-006",
    ];
}
