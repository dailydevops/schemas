namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

/// <summary>
/// The selection of the QT3 test sets <c>fn-count</c>, <c>fn-sum</c>, <c>fn-avg</c>, <c>fn-min</c>, <c>fn-max</c>,
/// <c>fn-distinct-values</c>, <c>fn-deep-equal</c>, <c>fn-index-of</c>, <c>fn-subsequence</c>, <c>fn-exists</c>, and <c>fn-empty</c> that
/// this package covers: the XPath 2.0 cases (the dependency rule of the QT3 selector of the integration tests) without a further
/// dependency and without an environment, whose expression is made only of the literals, constructors, operators, and functions that the
/// libraries of this repository lower and evaluate today, and whose result is an assertion of <see cref="Qt3SequenceFixture"/> or, for a type or a list of assertions that all hold, of <see cref="Numerics.Qt3NumericFixture"/>. The
/// cases are taken from the pinned commit of the suite (tests/conformance/suites/qt3.json).
/// </summary>
/// <remarks>
/// <para>
/// Limitation: these cases were selected before the QT3 runner of the integration tests had an evaluator, and they run through the lowerings of
/// <see cref="Qt3SequenceFixture"/> (test-local for the literals, function calls, and ranges; production for arithmetic, comparison,
/// cast, and castable) and not through the runner. The end-to-end run is the QT3 runner (#295), whose baselines list only the cases that do not pass.
/// </para>
/// <para>
/// Cases per test set, as three numbers: selected by the XPath 2.0 rule; of those, the ones without a further dependency or environment
/// and with an assertion that the fixture checks; and the ones in this selection: count 74, 60, 56; sum 221, 220, 196; avg 237, 235, 200; min 180, 180, 167; max 181, 181, 167;
/// distinct-values 91, 82, 77; deep-equal 161, 159, 117; index-of 53, 51, 44; subsequence 98, 98, 68; exists 56, 56, 49; empty 52, 52, 43.
/// </para>
/// <para>
/// Cases of the sets that are left out of the selection, with the reason (190 of 1374). The first selection left out 688, counted by the
/// reasons that follow; since the casting to the derived types exists (XP-23, #299), 498 of them are selected: the cases whose expression
/// builds an <c>xs:int</c>, <c>xs:long</c>, <c>xs:short</c>, <c>xs:byte</c>, an unsigned or a signed integer subtype, or a derived string
/// type, and that no other reason keeps out. The four cases <c>fn-max/fn-max-13</c>, <c>fn-max/fn-max-18</c>, <c>fn-min/fn-min-13</c>, and
/// <c>fn-min/fn-min-18</c> are selected too, since <c>fn:min</c> and <c>fn:max</c> keep the derived string type of the selected item (#767). The reasons of the first selection: a constructor function of a type outside the casting table of the seven types
/// (<c>xs:int</c>, <c>xs:long</c>, <c>xs:short</c>, the unsigned and the signed integer subtypes, <c>xs:anyURI</c>, <c>xs:date</c>,
/// <c>xs:dateTime</c>, <c>xs:time</c>, the duration types, <c>xs:hexBinary</c>), 575 cases (XP-23 and XP-59 add the rows); a function that belongs to another work package (<c>fn:not</c>, <c>fn:true</c>, <c>fn:false</c>,
/// <c>fn:boolean</c>, <c>fn:QName</c>, <c>fn:concat</c>, <c>fn:string-length</c>, <c>fn:error</c>, and others), 52 cases; an expression
/// form that the lowerings of the fixture lack (for, if, quantified and logical expressions, variables, and path steps; the error is
/// XP0002), 47 cases; a range of more than 10^6 items, which the eager range of the fixture does not build and which needs the lazy range
/// of the range lowering (6 cases, among them <c>fn-count/cbcl-count-001</c> and <c>fn-subsequence/cbcl-subsequence-010</c> to
/// <c>-014</c>); a function of the date and time group that needs a host slot (the clock and the implicit timezone) that the fixture does
/// not supply (<c>fn:current-time</c>, <c>fn:current-date</c>, <c>fn:implicit-timezone</c>, <c>fn:adjust-dateTime-to-timezone</c>), 4 cases;
/// a case whose expected value is computed with <c>fn:true</c> or <c>fn:false</c> (<c>fn-subsequence/fn-subsequence-mix-args-017</c>,
/// <c>-019</c>, <c>-020</c>), 3 cases; and an integer literal beyond 64 bits (<c>fn-avg/cbcl-avg-001</c>), which the literal lowering of
/// the test registry does not read, 1 case.
/// </para>
/// <para>
/// Cases that expect XPST0017 for a call with the wrong number of arguments (for example <c>count()</c> and <c>sum(1, 1, "wrong param")</c>)
/// check the arity ranges of the registration; with the group not registered they passed, because an unknown function raises the same
/// code, so they are among the tests that passed in the first commit of the pull request.
/// </para>
/// </remarks>
internal static class Qt3SequenceSelection
{
    public static readonly (string Id, string Expression, string Expectation)[] Cases =
    [
        ("fn-count/fn-countintg1args-1", "fn:count((xs:integer(\"-999999999999999999\")))", "E:1"),
        ("fn-count/fn-countintg1args-2", "fn:count((xs:integer(\"830993497117024304\")))", "E:1"),
        ("fn-count/fn-countintg1args-3", "fn:count((xs:integer(\"999999999999999999\")))", "E:1"),
        ("fn-count/fn-countdec1args-1", "fn:count((xs:decimal(\"-999999999999999999\")))", "E:1"),
        ("fn-count/fn-countdec1args-2", "fn:count((xs:decimal(\"617375191608514839\")))", "E:1"),
        ("fn-count/fn-countdec1args-3", "fn:count((xs:decimal(\"999999999999999999\")))", "E:1"),
        ("fn-count/fn-countdbl1args-1", "fn:count((xs:double(\"-1.7976931348623157E308\")))", "E:1"),
        ("fn-count/fn-countdbl1args-2", "fn:count((xs:double(\"0\")))", "E:1"),
        ("fn-count/fn-countdbl1args-3", "fn:count((xs:double(\"1.7976931348623157E308\")))", "E:1"),
        ("fn-count/fn-countflt1args-1", "fn:count((xs:float(\"-3.4028235E38\")))", "E:1"),
        ("fn-count/fn-countflt1args-2", "fn:count((xs:float(\"0\")))", "E:1"),
        ("fn-count/fn-countflt1args-3", "fn:count((xs:float(\"3.4028235E38\")))", "E:1"),
        ("fn-count/K-SeqCountFunc-1", "count()", "X:XPST0017"),
        ("fn-count/K-SeqCountFunc-2", "count(1, ())", "X:XPST0017"),
        ("fn-count/K-SeqCountFunc-3", "count( () ) eq 0", "T"),
        ("fn-count/K-SeqCountFunc-4", "count( (1, 2, 3) ) eq 3", "T"),
        ("fn-count/K-SeqCountFunc-5", "count( (1, 2, ()) ) eq 2", "T"),
        ("fn-count/K-SeqCountFunc-6", "count(()) eq 0", "T"),
        ("fn-count/K-SeqCountFunc-7", "count(((((()))))) eq 0", "T"),
        ("fn-count/K-SeqCountFunc-8", "count( (((), (), ()), (), (), (), ()) ) eq 0", "T"),
        ("fn-count/K-SeqCountFunc-9", "count( (1, 2, 3) ) eq 3", "T"),
        ("fn-count/K-SeqCountFunc-10", "count( ((), \"one\", 2, \"three\")) eq 3", "T"),
        ("fn-count/K-SeqCountFunc-11", "count( (\"one\", (2, \"three\")) ) eq 3", "T"),
        ("fn-count/K-SeqCountFunc-12", "count((1, 2)) eq 2", "T"),
        ("fn-count/K-SeqCountFunc-13", "count((1, 2, 3, \"four\")) eq 4", "T"),
        ("fn-count/K-SeqCountFunc-14", "count((1, 2, 3, \"four\")) eq 4", "T"),
        ("fn-count/K-SeqCountFunc-15", "count(1 to 3) eq 3", "T"),
        ("fn-count/K-SeqCountFunc-16", "count(reverse((1, 2, 3))) eq 3", "T"),
        ("fn-count/K-SeqCountFunc-17", "count(reverse((1, 2, 3))) eq 3", "T"),
        ("fn-count/fn-countint1args-1", "fn:count((xs:int(\"-2147483648\")))", "E:1"),
        ("fn-count/fn-countint1args-2", "fn:count((xs:int(\"-1873914410\")))", "E:1"),
        ("fn-count/fn-countint1args-3", "fn:count((xs:int(\"2147483647\")))", "E:1"),
        ("fn-count/fn-countlng1args-1", "fn:count((xs:long(\"-92233720368547758\")))", "E:1"),
        ("fn-count/fn-countlng1args-2", "fn:count((xs:long(\"-47175562203048468\")))", "E:1"),
        ("fn-count/fn-countlng1args-3", "fn:count((xs:long(\"92233720368547758\")))", "E:1"),
        ("fn-count/fn-countnint1args-1", "fn:count((xs:negativeInteger(\"-999999999999999999\")))", "E:1"),
        ("fn-count/fn-countnint1args-2", "fn:count((xs:negativeInteger(\"-297014075999096793\")))", "E:1"),
        ("fn-count/fn-countnint1args-3", "fn:count((xs:negativeInteger(\"-1\")))", "E:1"),
        ("fn-count/fn-countnni1args-1", "fn:count((xs:nonNegativeInteger(\"0\")))", "E:1"),
        ("fn-count/fn-countnni1args-2", "fn:count((xs:nonNegativeInteger(\"303884545991464527\")))", "E:1"),
        ("fn-count/fn-countnni1args-3", "fn:count((xs:nonNegativeInteger(\"999999999999999999\")))", "E:1"),
        ("fn-count/fn-countnpi1args-1", "fn:count((xs:nonPositiveInteger(\"-999999999999999999\")))", "E:1"),
        ("fn-count/fn-countnpi1args-2", "fn:count((xs:nonPositiveInteger(\"-475688437271870490\")))", "E:1"),
        ("fn-count/fn-countnpi1args-3", "fn:count((xs:nonPositiveInteger(\"0\")))", "E:1"),
        ("fn-count/fn-countpint1args-1", "fn:count((xs:positiveInteger(\"1\")))", "E:1"),
        ("fn-count/fn-countpint1args-2", "fn:count((xs:positiveInteger(\"52704602390610033\")))", "E:1"),
        ("fn-count/fn-countpint1args-3", "fn:count((xs:positiveInteger(\"999999999999999999\")))", "E:1"),
        ("fn-count/fn-countsht1args-1", "fn:count((xs:short(\"-32768\")))", "E:1"),
        ("fn-count/fn-countsht1args-2", "fn:count((xs:short(\"-5324\")))", "E:1"),
        ("fn-count/fn-countsht1args-3", "fn:count((xs:short(\"32767\")))", "E:1"),
        ("fn-count/fn-countulng1args-1", "fn:count((xs:unsignedLong(\"0\")))", "E:1"),
        ("fn-count/fn-countulng1args-2", "fn:count((xs:unsignedLong(\"130747108607674654\")))", "E:1"),
        ("fn-count/fn-countulng1args-3", "fn:count((xs:unsignedLong(\"184467440737095516\")))", "E:1"),
        ("fn-count/fn-countusht1args-1", "fn:count((xs:unsignedShort(\"0\")))", "E:1"),
        ("fn-count/fn-countusht1args-2", "fn:count((xs:unsignedShort(\"44633\")))", "E:1"),
        ("fn-count/fn-countusht1args-3", "fn:count((xs:unsignedShort(\"65535\")))", "E:1"),
        ("fn-sum/fn-sumintg1args-1", "fn:sum((xs:integer(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-sum/fn-sumintg1args-2", "fn:sum((xs:integer(\"830993497117024304\")))", "E:830993497117024304"),
        ("fn-sum/fn-sumintg1args-3", "fn:sum((xs:integer(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-sum/fn-sumdec1args-1", "fn:sum((xs:decimal(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-sum/fn-sumdec1args-2", "fn:sum((xs:decimal(\"617375191608514839\")))", "E:617375191608514839"),
        ("fn-sum/fn-sumdec1args-3", "fn:sum((xs:decimal(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-sum/fn-sumdbl1args-1", "fn:sum((xs:double(\"-1.7976931348623157E308\")))", "E:-1.7976931348623157E308"),
        ("fn-sum/fn-sumdbl1args-2", "fn:sum((xs:double(\"0\")))", "E:0"),
        ("fn-sum/fn-sumdbl1args-3", "fn:sum((xs:double(\"1.7976931348623157E308\")))", "E:1.7976931348623157E308"),
        ("fn-sum/fn-sumflt1args-1", "fn:sum((xs:float(\"-3.4028235E38\")))", "E:xs:float(\"-3.4028235E38\")"),
        ("fn-sum/fn-sumflt1args-2", "fn:sum((xs:float(\"0\")))", "E:0"),
        ("fn-sum/fn-sumflt1args-3", "fn:sum((xs:float(\"3.4028235E38\")))", "E:xs:float(\"3.4028235E38\")"),
        (
            "fn-sum/fn-sumintg2args-1",
            "fn:sum((xs:integer(\"830993497117024304\"),xs:integer(\"-999999999999999999\")))",
            "E:-169006502882975695"
        ),
        (
            "fn-sum/fn-sumintg2args-2",
            "fn:sum((xs:integer(\"999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumintg2args-3",
            "fn:sum((xs:integer(\"-999999999999999999\"),xs:integer(\"830993497117024304\")))",
            "E:-169006502882975695"
        ),
        (
            "fn-sum/fn-sumintg2args-4",
            "fn:sum((xs:integer(\"-999999999999999999\"),xs:integer(\"999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumdec2args-1",
            "fn:sum((xs:decimal(\"617375191608514839\"),xs:decimal(\"-999999999999999999\")))",
            "E:-382624808391485160"
        ),
        (
            "fn-sum/fn-sumdec2args-2",
            "fn:sum((xs:decimal(\"999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumdec2args-3",
            "fn:sum((xs:decimal(\"-999999999999999999\"),xs:decimal(\"617375191608514839\")))",
            "E:-382624808391485160"
        ),
        (
            "fn-sum/fn-sumdec2args-4",
            "fn:sum((xs:decimal(\"-999999999999999999\"),xs:decimal(\"999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumdbl2args-1",
            "fn:sum((xs:double(\"0\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-sum/fn-sumdbl2args-2",
            "fn:sum((xs:double(\"1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumdbl2args-3",
            "fn:sum((xs:double(\"-1.7976931348623157E308\"),xs:double(\"0\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-sum/fn-sumdbl2args-4",
            "fn:sum((xs:double(\"-1.7976931348623157E308\"),xs:double(\"1.7976931348623157E308\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumflt2args-1",
            "fn:sum((xs:float(\"0\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        ("fn-sum/fn-sumflt2args-2", "fn:sum((xs:float(\"3.4028235E38\"),xs:float(\"-3.4028235E38\")))", "E:0"),
        (
            "fn-sum/fn-sumflt2args-3",
            "fn:sum((xs:float(\"-3.4028235E38\"),xs:float(\"0\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        ("fn-sum/fn-sumflt2args-4", "fn:sum((xs:float(\"-3.4028235E38\"),xs:float(\"3.4028235E38\")))", "E:0"),
        (
            "fn-sum/fn-sumintg3args-1",
            "fn:sum((xs:integer(\"999999999999999999\"),xs:integer(\"-999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumintg3args-2",
            "fn:sum((xs:integer(\"-999999999999999999\"),xs:integer(\"999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumintg3args-3",
            "fn:sum((xs:integer(\"0\"),xs:integer(\"0\"),xs:integer(\"830993497117024304\")))",
            "E:830993497117024304"
        ),
        (
            "fn-sum/fn-sumintg3args-4",
            "fn:sum((xs:integer(\"-999999999999999999\"),xs:integer(\"-999999999999999999\"),xs:integer(\"999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumdec3args-1",
            "fn:sum((xs:decimal(\"999999999999999999\"),xs:decimal(\"-999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumdec3args-2",
            "fn:sum((xs:decimal(\"-999999999999999999\"),xs:decimal(\"999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumdec3args-3",
            "fn:sum((xs:decimal(\"0\"),xs:decimal(\"0\"),xs:decimal(\"617375191608514839\")))",
            "E:617375191608514839"
        ),
        (
            "fn-sum/fn-sumdec3args-4",
            "fn:sum((xs:decimal(\"-999999999999999999\"),xs:decimal(\"-999999999999999999\"),xs:decimal(\"999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumdbl3args-1",
            "fn:sum((xs:double(\"0\"),xs:double(\"-1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "S:-INF\tX:FOAR0002"
        ),
        (
            "fn-sum/fn-sumdbl3args-2",
            "fn:sum((xs:double(\"1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-sum/fn-sumdbl3args-3",
            "fn:sum((xs:double(\"-1.7976931348623157E308\"),xs:double(\"0\"),xs:double(\"0\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-sum/fn-sumdbl3args-4",
            "fn:sum((xs:double(\"-1.7976931348623157E308\"),xs:double(\"1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        ("fn-sum/fn-sumdbl3args-5", "fn:sum((xs:double(\"0\"),xs:double(\"0\"),xs:double(\"0\")))", "E:0"),
        (
            "fn-sum/fn-sumdbl3args-6",
            "fn:sum((xs:double(\"-1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\"),xs:double(\"1.7976931348623157E308\")))",
            "S:-INF\tE:-1.7976931348623157E308\tX:FOAR0002"
        ),
        (
            "fn-sum/fn-sumflt3args-1",
            "fn:sum((xs:float(\"0\"),xs:float(\"-3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "S:-INF\tX:FOAR0002"
        ),
        (
            "fn-sum/fn-sumflt3args-2",
            "fn:sum((xs:float(\"3.4028235E38\"),xs:float(\"-3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        (
            "fn-sum/fn-sumflt3args-3",
            "fn:sum((xs:float(\"-3.4028235E38\"),xs:float(\"0\"),xs:float(\"0\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        (
            "fn-sum/fn-sumflt3args-4",
            "fn:sum((xs:float(\"-3.4028235E38\"),xs:float(\"3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        ("fn-sum/fn-sumflt3args-5", "fn:sum((xs:float(\"0\"),xs:float(\"0\"),xs:float(\"0\")))", "E:0"),
        (
            "fn-sum/fn-sumflt3args-6",
            "fn:sum((xs:float(\"-3.4028235E38\"),xs:float(\"-3.4028235E38\"),xs:float(\"3.4028235E38\")))",
            "S:-INF\tE:xs:float(\"-3.4028235E38\")\tX:FOAR0002"
        ),
        ("fn-sum/K-SeqSUMFunc-1", "sum()", "X:XPST0017"),
        ("fn-sum/K-SeqSUMFunc-2", "sum(1, 1, \"wrong param\")", "X:XPST0017"),
        ("fn-sum/K-SeqSUMFunc-3", "sum((), 3) eq 3", "T"),
        ("fn-sum/K-SeqSUMFunc-4", "sum((), 3) instance of xs:integer", "T"),
        ("fn-sum/K-SeqSUMFunc-5", "empty(sum((), ()))", "T"),
        ("fn-sum/K-SeqSUMFunc-6", "sum((), 0.0) instance of xs:decimal", "T"),
        ("fn-sum/K-SeqSUMFunc-7", "sum((), 0.0) eq 0.0", "T"),
        ("fn-sum/K-SeqSUMFunc-8", "sum(()) eq 0", "T"),
        ("fn-sum/K-SeqSUMFunc-9", "sum(()) instance of xs:integer", "T"),
        ("fn-sum/K-SeqSUMFunc-10", "sum((-5, -0, -3, -6)) eq -14", "T"),
        ("fn-sum/K-SeqSUMFunc-11", "string(sum((1, 2, 3, xs:float(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-sum/K-SeqSUMFunc-12", "string(sum((1, 2, 3, xs:double(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-sum/K-SeqSUMFunc-13", "string(sum((xs:double(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-sum/K-SeqSUMFunc-14", "string(sum((xs:float(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-sum/K-SeqSUMFunc-15", "string(sum((1, 2, xs:double(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-sum/K-SeqSUMFunc-16", "string(sum((1, 2, xs:float(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-sum/K-SeqSUMFunc-17", "sum(xs:untypedAtomic(\"3\")) instance of xs:double", "T"),
        ("fn-sum/K-SeqSUMFunc-18", "sum((1, 2, xs:untypedAtomic(\"3\"))) instance of xs:double", "T"),
        ("fn-sum/K-SeqSUMFunc-19", "sum((1, 2, xs:untypedAtomic(\"3\"))) eq 6", "T"),
        ("fn-sum/K-SeqSUMFunc-20", "sum((xs:float(1), 2, xs:untypedAtomic(\"3\"))) eq 6", "T"),
        ("fn-sum/K-SeqSUMFunc-21", "sum((xs:float(1), 2, xs:untypedAtomic(\"3\"))) instance of xs:double", "T"),
        ("fn-sum/K-SeqSUMFunc-22", "sum(remove((1.0, xs:float(1), 2, xs:untypedAtomic(\"3\")), 1)) eq 6", "T"),
        ("fn-sum/K-SeqSUMFunc-23", "sum(\"a string\")", "X:FORG0006"),
        ("fn-sum/K-SeqSUMFunc-27", "sum(((),())) eq 0", "T"),
        ("fn-sum/K-SeqSUMFunc-28", "sum(()) eq 0", "T"),
        ("fn-sum/K-SeqSUMFunc-29", "sum((3, 4, 5)) eq 12", "T"),
        ("fn-sum/K2-SeqSUMFunc-1", "sum((), ())", "C:0"),
        ("fn-sum/K2-SeqSUMFunc-2", "sum((xs:float('NaN'), 2, 3, 4, xs:double('NaN'))) instance of xs:double", "T"),
        ("fn-sum/K2-SeqSUMFunc-3", "sum((xs:float('NaN'), 2, 3.3, 4, xs:double('NaN'))) instance of xs:double", "T"),
        ("fn-sum/K2-SeqSUMFunc-4", "sum(xs:unsignedShort(\"1\")) instance of xs:unsignedShort", "T"),
        ("fn-sum/cbcl-sum-func-001", "sum(1 to 10)", "S:55"),
        ("fn-sum/fn-sumint1args-1", "fn:sum((xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-sum/fn-sumint1args-2", "fn:sum((xs:int(\"-1873914410\")))", "E:-1873914410"),
        ("fn-sum/fn-sumint1args-3", "fn:sum((xs:int(\"2147483647\")))", "E:2147483647"),
        ("fn-sum/fn-sumint2args-1", "fn:sum((xs:int(\"-1873914410\"),xs:int(\"-273569238\")))", "E:-2147483648"),
        ("fn-sum/fn-sumint2args-2", "fn:sum((xs:int(\"2147483647\"),xs:int(\"-2147483648\")))", "E:-1"),
        ("fn-sum/fn-sumint2args-3", "fn:sum((xs:int(\"-273569238\"),xs:int(\"-1873914410\")))", "E:-2147483648"),
        ("fn-sum/fn-sumint2args-4", "fn:sum((xs:int(\"-2147483648\"),xs:int(\"2147483647\")))", "E:-1"),
        (
            "fn-sum/fn-sumint3args-1",
            "fn:sum((xs:int(\"-1873914410\"),xs:int(\"-273569238\"),xs:int(\"-273569238\")))",
            "E:-2421052886"
        ),
        (
            "fn-sum/fn-sumint3args-2",
            "fn:sum((xs:int(\"-273569238\"),xs:int(\"-1873914410\"),xs:int(\"0\")))",
            "E:-2147483648"
        ),
        ("fn-sum/fn-sumint3args-3", "fn:sum((xs:int(\"0\"),xs:int(\"0\"),xs:int(\"-1873914410\")))", "E:-1873914410"),
        ("fn-sum/fn-sumint3args-4", "fn:sum((xs:int(\"0\"),xs:int(\"0\"),xs:int(\"2147483647\")))", "E:2147483647"),
        ("fn-sum/fn-sumlng1args-1", "fn:sum((xs:long(\"-92233720368547758\")))", "E:-92233720368547758"),
        ("fn-sum/fn-sumlng1args-2", "fn:sum((xs:long(\"-47175562203048468\")))", "E:-47175562203048468"),
        ("fn-sum/fn-sumlng1args-3", "fn:sum((xs:long(\"92233720368547758\")))", "E:92233720368547758"),
        (
            "fn-sum/fn-sumlng2args-1",
            "fn:sum((xs:long(\"-47175562203048468\"),xs:long(\"-45058158165499290\")))",
            "E:-92233720368547758"
        ),
        ("fn-sum/fn-sumlng2args-2", "fn:sum((xs:long(\"92233720368547758\"),xs:long(\"-92233720368547758\")))", "E:0"),
        (
            "fn-sum/fn-sumlng2args-3",
            "fn:sum((xs:long(\"-45058158165499290\"),xs:long(\"-47175562203048468\")))",
            "E:-92233720368547758"
        ),
        ("fn-sum/fn-sumlng2args-4", "fn:sum((xs:long(\"-92233720368547758\"),xs:long(\"92233720368547758\")))", "E:0"),
        (
            "fn-sum/fn-sumlng3args-1",
            "fn:sum((xs:long(\"-47175562203048468\"),xs:long(\"-45058158165499290\"),xs:long(\"-45058158165499290\")))",
            "E:-137291878534047048"
        ),
        (
            "fn-sum/fn-sumlng3args-2",
            "fn:sum((xs:long(\"92233720368547758\"),xs:long(\"-92233720368547758\"),xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-sum/fn-sumlng3args-3",
            "fn:sum((xs:long(\"-45058158165499290\"),xs:long(\"-47175562203048468\"),xs:long(\"0\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-sum/fn-sumlng3args-4",
            "fn:sum((xs:long(\"-92233720368547758\"),xs:long(\"92233720368547758\"),xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-sum/fn-sumlng3args-5",
            "fn:sum((xs:long(\"0\"),xs:long(\"0\"),xs:long(\"-47175562203048468\")))",
            "E:-47175562203048468"
        ),
        (
            "fn-sum/fn-sumlng3args-6",
            "fn:sum((xs:long(\"-92233720368547758\"),xs:long(\"-92233720368547758\"),xs:long(\"92233720368547758\")))",
            "E:-92233720368547758"
        ),
        ("fn-sum/fn-sumnint1args-1", "fn:sum((xs:negativeInteger(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-sum/fn-sumnint1args-2", "fn:sum((xs:negativeInteger(\"-297014075999096793\")))", "E:-297014075999096793"),
        ("fn-sum/fn-sumnint1args-3", "fn:sum((xs:negativeInteger(\"-1\")))", "E:-1"),
        (
            "fn-sum/fn-sumnint2args-1",
            "fn:sum((xs:negativeInteger(\"-297014075999096793\"),xs:negativeInteger(\"-702985924000903206\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnint2args-2",
            "fn:sum((xs:negativeInteger(\"-1\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-1000000000000000000"
        ),
        (
            "fn-sum/fn-sumnint2args-3",
            "fn:sum((xs:negativeInteger(\"-702985924000903206\"),xs:negativeInteger(\"-297014075999096793\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnint2args-4",
            "fn:sum((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-1\")))",
            "E:-1000000000000000000"
        ),
        (
            "fn-sum/fn-sumnint3args-1",
            "fn:sum((xs:negativeInteger(\"-297014075999096793\"),xs:negativeInteger(\"-702985924000903206\"),xs:negativeInteger(\"-702985924000903206\")))",
            "E:-1702985924000903205"
        ),
        (
            "fn-sum/fn-sumnint3args-2",
            "fn:sum((xs:negativeInteger(\"-1\"),xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-1999999999999999999"
        ),
        ("fn-sum/fn-sumnni1args-1", "fn:sum((xs:nonNegativeInteger(\"0\")))", "E:0"),
        ("fn-sum/fn-sumnni1args-2", "fn:sum((xs:nonNegativeInteger(\"303884545991464527\")))", "E:303884545991464527"),
        ("fn-sum/fn-sumnni1args-3", "fn:sum((xs:nonNegativeInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-sum/fn-sumnni2args-1", "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\")))", "E:0"),
        (
            "fn-sum/fn-sumnni2args-2",
            "fn:sum((xs:nonNegativeInteger(\"303884545991464527\"),xs:nonNegativeInteger(\"0\")))",
            "E:303884545991464527"
        ),
        (
            "fn-sum/fn-sumnni2args-3",
            "fn:sum((xs:nonNegativeInteger(\"999999999999999999\"),xs:nonNegativeInteger(\"0\")))",
            "E:999999999999999999"
        ),
        (
            "fn-sum/fn-sumnni2args-4",
            "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"303884545991464527\")))",
            "E:303884545991464527"
        ),
        (
            "fn-sum/fn-sumnni2args-5",
            "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-sum/fn-sumnni3args-1",
            "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumnni3args-2",
            "fn:sum((xs:nonNegativeInteger(\"303884545991464527\"),xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\")))",
            "E:303884545991464527"
        ),
        (
            "fn-sum/fn-sumnni3args-3",
            "fn:sum((xs:nonNegativeInteger(\"999999999999999999\"),xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\")))",
            "E:999999999999999999"
        ),
        (
            "fn-sum/fn-sumnni3args-4",
            "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"303884545991464527\"),xs:nonNegativeInteger(\"0\")))",
            "E:303884545991464527"
        ),
        (
            "fn-sum/fn-sumnni3args-5",
            "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"999999999999999999\"),xs:nonNegativeInteger(\"0\")))",
            "E:999999999999999999"
        ),
        (
            "fn-sum/fn-sumnni3args-6",
            "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"303884545991464527\")))",
            "E:303884545991464527"
        ),
        (
            "fn-sum/fn-sumnni3args-7",
            "fn:sum((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi1args-1",
            "fn:sum((xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi1args-2",
            "fn:sum((xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-475688437271870490"
        ),
        ("fn-sum/fn-sumnpi1args-3", "fn:sum((xs:nonPositiveInteger(\"0\")))", "E:0"),
        (
            "fn-sum/fn-sumnpi2args-1",
            "fn:sum((xs:nonPositiveInteger(\"-475688437271870490\"),xs:nonPositiveInteger(\"-524311562728129509\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi2args-2",
            "fn:sum((xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi2args-3",
            "fn:sum((xs:nonPositiveInteger(\"-524311562728129509\"),xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi2args-4",
            "fn:sum((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"0\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi3args-1",
            "fn:sum((xs:nonPositiveInteger(\"-475688437271870490\"),xs:nonPositiveInteger(\"-524311562728129509\"),xs:nonPositiveInteger(\"-524311562728129509\")))",
            "E:-1524311562728129508"
        ),
        (
            "fn-sum/fn-sumnpi3args-2",
            "fn:sum((xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-1999999999999999998"
        ),
        (
            "fn-sum/fn-sumnpi3args-3",
            "fn:sum((xs:nonPositiveInteger(\"-524311562728129509\"),xs:nonPositiveInteger(\"-475688437271870490\"),xs:nonPositiveInteger(\"0\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi3args-4",
            "fn:sum((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"0\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-sum/fn-sumnpi3args-5",
            "fn:sum((xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-475688437271870490"
        ),
        (
            "fn-sum/fn-sumnpi3args-6",
            "fn:sum((xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"0\")))",
            "E:0"
        ),
        ("fn-sum/fn-sumpint1args-1", "fn:sum((xs:positiveInteger(\"1\")))", "E:1"),
        ("fn-sum/fn-sumpint1args-2", "fn:sum((xs:positiveInteger(\"52704602390610033\")))", "E:52704602390610033"),
        ("fn-sum/fn-sumpint1args-3", "fn:sum((xs:positiveInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-sum/fn-sumpint2args-1", "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\")))", "E:2"),
        (
            "fn-sum/fn-sumpint2args-2",
            "fn:sum((xs:positiveInteger(\"52704602390610033\"),xs:positiveInteger(\"1\")))",
            "E:52704602390610034"
        ),
        (
            "fn-sum/fn-sumpint2args-3",
            "fn:sum((xs:positiveInteger(\"999999999999999998\"),xs:positiveInteger(\"1\")))",
            "E:999999999999999999"
        ),
        (
            "fn-sum/fn-sumpint2args-4",
            "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"52704602390610033\")))",
            "E:52704602390610034"
        ),
        (
            "fn-sum/fn-sumpint2args-5",
            "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"999999999999999998\")))",
            "E:999999999999999999"
        ),
        (
            "fn-sum/fn-sumpint3args-1",
            "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\")))",
            "E:3"
        ),
        (
            "fn-sum/fn-sumpint3args-2",
            "fn:sum((xs:positiveInteger(\"52704602390610033\"),xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\")))",
            "E:52704602390610035"
        ),
        (
            "fn-sum/fn-sumpint3args-3",
            "fn:sum((xs:positiveInteger(\"999999999999999998\"),xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\")))",
            "E:1000000000000000000"
        ),
        (
            "fn-sum/fn-sumpint3args-4",
            "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"52704602390610033\"),xs:positiveInteger(\"1\")))",
            "E:52704602390610035"
        ),
        (
            "fn-sum/fn-sumpint3args-5",
            "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"999999999999999998\"),xs:positiveInteger(\"1\")))",
            "E:1000000000000000000"
        ),
        (
            "fn-sum/fn-sumpint3args-6",
            "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\"),xs:positiveInteger(\"52704602390610033\")))",
            "E:52704602390610035"
        ),
        (
            "fn-sum/fn-sumpint3args-7",
            "fn:sum((xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\"),xs:positiveInteger(\"999999999999999998\")))",
            "E:1000000000000000000"
        ),
        ("fn-sum/fn-sumsht1args-1", "fn:sum((xs:short(\"-32768\")))", "E:-32768"),
        ("fn-sum/fn-sumsht1args-2", "fn:sum((xs:short(\"-5324\")))", "E:-5324"),
        ("fn-sum/fn-sumsht1args-3", "fn:sum((xs:short(\"32767\")))", "E:32767"),
        ("fn-sum/fn-sumsht2args-1", "fn:sum((xs:short(\"-5324\"),xs:short(\"-27444\")))", "E:-32768"),
        ("fn-sum/fn-sumsht2args-2", "fn:sum((xs:short(\"32767\"),xs:short(\"-32768\")))", "E:-1"),
        ("fn-sum/fn-sumsht2args-3", "fn:sum((xs:short(\"-27444\"),xs:short(\"-5324\")))", "E:-32768"),
        ("fn-sum/fn-sumsht2args-4", "fn:sum((xs:short(\"-32768\"),xs:short(\"32767\")))", "E:-1"),
        (
            "fn-sum/fn-sumsht3args-1",
            "fn:sum((xs:short(\"-5324\"),xs:short(\"-27444\"),xs:short(\"-27444\")))",
            "E:-60212"
        ),
        ("fn-sum/fn-sumsht3args-2", "fn:sum((xs:short(\"-27444\"),xs:short(\"-5324\"),xs:short(\"0\")))", "E:-32768"),
        ("fn-sum/fn-sumsht3args-3", "fn:sum((xs:short(\"0\"),xs:short(\"0\"),xs:short(\"-5324\")))", "E:-5324"),
        ("fn-sum/fn-sumsht3args-4", "fn:sum((xs:short(\"0\"),xs:short(\"0\"),xs:short(\"32767\")))", "E:32767"),
        ("fn-sum/fn-sumulng1args-1", "fn:sum((xs:unsignedLong(\"0\")))", "E:0"),
        ("fn-sum/fn-sumulng1args-2", "fn:sum((xs:unsignedLong(\"130747108607674654\")))", "E:130747108607674654"),
        ("fn-sum/fn-sumulng1args-3", "fn:sum((xs:unsignedLong(\"184467440737095516\")))", "E:184467440737095516"),
        ("fn-sum/fn-sumulng2args-1", "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\")))", "E:0"),
        (
            "fn-sum/fn-sumulng2args-2",
            "fn:sum((xs:unsignedLong(\"130747108607674654\"),xs:unsignedLong(\"0\")))",
            "E:130747108607674654"
        ),
        (
            "fn-sum/fn-sumulng2args-3",
            "fn:sum((xs:unsignedLong(\"184467440737095516\"),xs:unsignedLong(\"0\")))",
            "E:184467440737095516"
        ),
        (
            "fn-sum/fn-sumulng2args-4",
            "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"130747108607674654\")))",
            "E:130747108607674654"
        ),
        (
            "fn-sum/fn-sumulng2args-5",
            "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"184467440737095516\")))",
            "E:184467440737095516"
        ),
        (
            "fn-sum/fn-sumulng3args-1",
            "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumulng3args-2",
            "fn:sum((xs:unsignedLong(\"130747108607674654\"),xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\")))",
            "E:130747108607674654"
        ),
        (
            "fn-sum/fn-sumulng3args-3",
            "fn:sum((xs:unsignedLong(\"184467440737095516\"),xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\")))",
            "E:184467440737095516"
        ),
        (
            "fn-sum/fn-sumulng3args-4",
            "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"130747108607674654\"),xs:unsignedLong(\"0\")))",
            "E:130747108607674654"
        ),
        (
            "fn-sum/fn-sumulng3args-5",
            "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"184467440737095516\"),xs:unsignedLong(\"0\")))",
            "E:184467440737095516"
        ),
        (
            "fn-sum/fn-sumulng3args-6",
            "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\"),xs:unsignedLong(\"130747108607674654\")))",
            "E:130747108607674654"
        ),
        (
            "fn-sum/fn-sumulng3args-7",
            "fn:sum((xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\"),xs:unsignedLong(\"184467440737095516\")))",
            "E:184467440737095516"
        ),
        ("fn-sum/fn-sumusht1args-1", "fn:sum((xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-sum/fn-sumusht1args-2", "fn:sum((xs:unsignedShort(\"44633\")))", "E:44633"),
        ("fn-sum/fn-sumusht1args-3", "fn:sum((xs:unsignedShort(\"65535\")))", "E:65535"),
        ("fn-sum/fn-sumusht2args-1", "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-sum/fn-sumusht2args-2", "fn:sum((xs:unsignedShort(\"44633\"),xs:unsignedShort(\"0\")))", "E:44633"),
        ("fn-sum/fn-sumusht2args-3", "fn:sum((xs:unsignedShort(\"65535\"),xs:unsignedShort(\"0\")))", "E:65535"),
        ("fn-sum/fn-sumusht2args-4", "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"44633\")))", "E:44633"),
        ("fn-sum/fn-sumusht2args-5", "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"65535\")))", "E:65535"),
        (
            "fn-sum/fn-sumusht3args-1",
            "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\")))",
            "E:0"
        ),
        (
            "fn-sum/fn-sumusht3args-2",
            "fn:sum((xs:unsignedShort(\"44633\"),xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\")))",
            "E:44633"
        ),
        (
            "fn-sum/fn-sumusht3args-3",
            "fn:sum((xs:unsignedShort(\"65535\"),xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\")))",
            "E:65535"
        ),
        (
            "fn-sum/fn-sumusht3args-4",
            "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"44633\"),xs:unsignedShort(\"0\")))",
            "E:44633"
        ),
        (
            "fn-sum/fn-sumusht3args-5",
            "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"65535\"),xs:unsignedShort(\"0\")))",
            "E:65535"
        ),
        (
            "fn-sum/fn-sumusht3args-6",
            "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\"),xs:unsignedShort(\"44633\")))",
            "E:44633"
        ),
        (
            "fn-sum/fn-sumusht3args-7",
            "fn:sum((xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\"),xs:unsignedShort(\"65535\")))",
            "E:65535"
        ),
        ("fn-avg/fn-avgintg1args-1", "fn:avg((xs:integer(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-avg/fn-avgintg1args-2", "fn:avg((xs:integer(\"830993497117024304\")))", "E:830993497117024304"),
        ("fn-avg/fn-avgintg1args-3", "fn:avg((xs:integer(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-avg/fn-avgdec1args-1", "fn:avg((xs:decimal(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-avg/fn-avgdec1args-2", "fn:avg((xs:decimal(\"617375191608514839\")))", "E:617375191608514839"),
        ("fn-avg/fn-avgdec1args-3", "fn:avg((xs:decimal(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-avg/fn-avgdbl1args-1", "fn:avg((xs:double(\"-1.7976931348623157E308\")))", "E:-1.7976931348623157E308"),
        ("fn-avg/fn-avgdbl1args-2", "fn:avg((xs:double(\"0\")))", "E:0"),
        ("fn-avg/fn-avgdbl1args-3", "fn:avg((xs:double(\"1.7976931348623157E308\")))", "E:1.7976931348623157E308"),
        ("fn-avg/fn-avgflt1args-1", "fn:avg((xs:float(\"-3.4028235E38\")))", "E:xs:float('-3.4028235E38')"),
        ("fn-avg/fn-avgflt1args-2", "fn:avg((xs:float(\"0\")))", "E:0"),
        ("fn-avg/fn-avgflt1args-3", "fn:avg((xs:float(\"3.4028235E38\")))", "E:xs:float(\"3.4028235E38\")"),
        (
            "fn-avg/fn-avgintg2args-1",
            "fn:avg((xs:integer(\"-999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-avg/fn-avgintg2args-2",
            "fn:avg((xs:integer(\"830993497117024304\"),xs:integer(\"-999999999999999999\")))",
            "E:-84503251441487847.5"
        ),
        (
            "fn-avg/fn-avgintg2args-3",
            "fn:avg((xs:integer(\"999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-avg/fn-avgintg2args-4",
            "fn:avg((xs:integer(\"-999999999999999999\"),xs:integer(\"830993497117024304\")))",
            "E:-84503251441487847.5"
        ),
        (
            "fn-avg/fn-avgintg2args-5",
            "fn:avg((xs:integer(\"-999999999999999999\"),xs:integer(\"999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-avg/fn-avgdec2args-1",
            "fn:avg((xs:decimal(\"-999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-avg/fn-avgdec2args-2",
            "fn:avg((xs:decimal(\"617375191608514839\"),xs:decimal(\"-999999999999999999\")))",
            "E:-191312404195742580"
        ),
        (
            "fn-avg/fn-avgdec2args-3",
            "fn:avg((xs:decimal(\"999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-avg/fn-avgdec2args-4",
            "fn:avg((xs:decimal(\"-999999999999999999\"),xs:decimal(\"617375191608514839\")))",
            "E:-191312404195742580"
        ),
        (
            "fn-avg/fn-avgdec2args-5",
            "fn:avg((xs:decimal(\"-999999999999999999\"),xs:decimal(\"999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-avg/fn-avgdbl2args-1",
            "fn:avg((xs:double(\"-1.7976931348623157E150\"),xs:double(\"-1.7976931348623157E150\"))) eq -1.7976931348623157E150",
            "T"
        ),
        (
            "fn-avg/fn-avgdbl2args-2",
            "fn:avg((xs:double(\"0\"),xs:double(\"-1.7976931348623157E308\"))) eq -8.9884656743115785E307",
            "T"
        ),
        (
            "fn-avg/fn-avgdbl2args-3",
            "fn:avg((xs:double(\"1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:0"
        ),
        (
            "fn-avg/fn-avgdbl2args-4",
            "fn:avg((xs:double(\"-1.7976931348623157E308\"),xs:double(\"0\"))) eq -8.9884656743115785E307",
            "T"
        ),
        (
            "fn-avg/fn-avgdbl2args-5",
            "fn:avg((xs:double(\"-1.7976931348623157E308\"),xs:double(\"1.7976931348623157E308\")))",
            "E:0"
        ),
        (
            "fn-avg/fn-avgflt2args-1",
            "fn:avg((xs:float(\"-3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "S:-INF\tE:xs:float(\"-3.4028235E38\")\tX:FOAR0002"
        ),
        (
            "fn-avg/fn-avgflt2args-2",
            "fn:avg((xs:float(\"0\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float('-1.7014117E38')"
        ),
        ("fn-avg/fn-avgflt2args-3", "fn:avg((xs:float(\"3.4028235E38\"),xs:float(\"-3.4028235E38\")))", "E:0"),
        (
            "fn-avg/fn-avgflt2args-4",
            "fn:avg((xs:float(\"-3.4028235E38\"),xs:float(\"0\")))",
            "E:xs:float('-1.7014117E38')"
        ),
        ("fn-avg/fn-avgflt2args-5", "fn:avg((xs:float(\"-3.4028235E38\"),xs:float(\"3.4028235E38\")))", "E:0"),
        ("fn-avg/fn-avg-mix-args-001", "fn:avg( (3, 4, 5) )", "E:4"),
        ("fn-avg/fn-avg-mix-args-003", "fn:avg(())", "C:0"),
        ("fn-avg/fn-avg-mix-args-004", "fn:avg(( xs:float('INF'), xs:float('-INF')))", "S:NaN"),
        ("fn-avg/fn-avg-mix-args-005", "fn:avg(( (3, 4, 5), xs:float('NaN') ))", "S:NaN"),
        ("fn-avg/fn-avg-mix-args-007", "fn:avg(( fn:count(\"Hello\") ))", "E:1"),
        ("fn-avg/fn-avg-mix-args-008", "fn:avg( ( ( xs:integer(\"100\"), xs:integer(\"-100\"))))", "E:0"),
        (
            "fn-avg/fn-avg-mix-args-009",
            "fn:avg( ( ( xs:decimal(\"-1.000000000001\"), xs:integer(\"-100\"))))",
            "E:-50.5000000000005\tE:-50.5"
        ),
        ("fn-avg/fn-avg-mix-args-010", "fn:avg(( (xs:decimal(\"1.01\"), xs:integer(\"12\") )))", "E:6.505"),
        ("fn-avg/fn-avg-mix-args-011", "fn:avg(( (xs:float(\"NaN\"), 100, (), 2)))", "S:NaN"),
        (
            "fn-avg/fn-avg-mix-args-012",
            "fn:avg(( (xs:float(\"-3.4028235E38\"), xs:decimal(\"-999999999999999999\") )))",
            "E:xs:float(\"-1.7014117E38\")"
        ),
        (
            "fn-avg/fn-avg-mix-args-013",
            "fn:avg(( (xs:float(\"0\"), xs:decimal(\"-999999999999999999\") ))) eq xs:float(\"-4.9999999999999999E17\")",
            "T"
        ),
        (
            "fn-avg/fn-avg-mix-args-014",
            "fn:avg(( (xs:float(\"3.4028235E38\"), xs:decimal(\"-999999999999999999\") )))",
            "E:xs:float(\"1.7014117E38\")"
        ),
        (
            "fn-avg/fn-avg-mix-args-015",
            "fn:avg(( (xs:float(\"-0\"), xs:decimal(\"-999999999999999999\") ))) eq xs:float(\"-4.9999999999999999E17\")",
            "T"
        ),
        ("fn-avg/fn-avg-mix-args-016", "fn:avg(( (xs:float(\"NaN\"), xs:decimal(\"-999999999999999999\") )))", "S:NaN"),
        ("fn-avg/fn-avg-mix-args-017", "fn:avg(( (xs:float(\"INF\"), xs:decimal(\"-999999999999999999\") )))", "S:INF"),
        ("fn-avg/fn-avg-mix-args-019", "fn:avg(( (xs:float(\"-INF\"), xs:decimal(\"2.34\"))))", "S:-INF"),
        (
            "fn-avg/fn-avg-mix-args-020",
            "fn:avg(( (xs:double(\"-1.7976931348623157E308\"), xs:integer(\"-999999999999999999\") ) )) eq xs:double(\"-8.988465674311579E307\")",
            "T"
        ),
        (
            "fn-avg/fn-avg-mix-args-021",
            "fn:avg(( (xs:double(\"0\"), xs:integer(\"-999999999999999999\") ) ))",
            "E:-5.0E17"
        ),
        (
            "fn-avg/fn-avg-mix-args-022",
            "fn:avg(( (xs:double(\"1.7976931348623157E308\"), xs:integer(\"-999999999999999999\") ) )) eq xs:double(\"8.988465674311579E307\")",
            "T"
        ),
        (
            "fn-avg/fn-avg-mix-args-023",
            "fn:avg(( (xs:double(\"-0\"), xs:integer(\"-999999999999999999\") ) ))",
            "E:-5.0E17"
        ),
        (
            "fn-avg/fn-avg-mix-args-024",
            "fn:avg(( (xs:double(\"-INF\"), xs:integer(\"-999999999999999999\") ) ))",
            "S:-INF"
        ),
        (
            "fn-avg/fn-avg-mix-args-025",
            "fn:avg(( (xs:double(\"NaN\"), xs:integer(\"-999999999999999999\") ) ))",
            "S:NaN"
        ),
        ("fn-avg/fn-avg-mix-args-026", "fn:avg(( (xs:double(\"1.34\"), xs:float(\"INF\"))))", "S:INF"),
        ("fn-avg/fn-avg-mix-args-027", "fn:avg(( (xs:double(\"INF\"), 2, 3)))", "S:INF"),
        ("fn-avg/fn-avg-mix-args-031", "fn:avg(( fn:empty(\"Hello\") ))", "X:FORG0006"),
        ("fn-avg/fn-avg-mix-args-033", "fn:avg(( (xs:string(\"xyz\"), (), (), \"a\" , \"b\") ))", "X:FORG0006"),
        ("fn-avg/fn-avg-mix-args-035", "fn:avg(( (xs:integer(\"100\"), xs:string(\"abc\"))))", "X:FORG0006"),
        (
            "fn-avg/fn-avg-mix-args-036",
            "fn:avg(( (xs:integer(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-037",
            "fn:avg(( (xs:integer(\"830993497117024304\"), \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-038",
            "fn:avg(( (xs:integer(\"999999999999999999\"), \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-039",
            "fn:avg(( (xs:decimal(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-040",
            "fn:avg(( (xs:decimal(\"617375191608514839\"), \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-041",
            "fn:avg(( (xs:decimal(\"999999999999999999\"), \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-043",
            "fn:avg(( (xs:float(\"-3.4028235E38\"), xs:decimal(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-044",
            "fn:avg(( (xs:float(\"0\"), xs:decimal(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-045",
            "fn:avg(( (xs:float(\"3.4028235E38\"), xs:decimal(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-046",
            "fn:avg(( (xs:float(\"-0\"), xs:decimal(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-047",
            "fn:avg(( (xs:float(\"NaN\"), xs:decimal(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "S:NaN\tX:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-048",
            "fn:avg(( (xs:float(\"INF\"), xs:decimal(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        ("fn-avg/fn-avg-mix-args-049", "fn:avg(( (xs:float(\"1.01\"), xs:string(\"a\"))))", "X:FORG0006"),
        ("fn-avg/fn-avg-mix-args-050", "fn:avg(( (xs:float(\"-INF\"), xs:decimal(\"2.34\"), \"abc\")))", "X:FORG0006"),
        (
            "fn-avg/fn-avg-mix-args-051",
            "fn:avg(( (xs:double(\"-1.7976931348623157E308\"), xs:integer(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-052",
            "fn:avg(( (xs:double(\"0\"), xs:integer(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-053",
            "fn:avg(( (xs:double(\"1.7976931348623157E308\"), xs:integer(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-054",
            "fn:avg(( (xs:double(\"-0\"), xs:integer(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-055",
            "fn:avg(( (xs:double(\"-INF\"), xs:integer(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "X:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-056",
            "fn:avg(( (xs:double(\"NaN\"), xs:integer(\"-999999999999999999\") , \"a\", (), \"3\") ))",
            "S:NaN\tX:FORG0006"
        ),
        (
            "fn-avg/fn-avg-mix-args-058",
            "fn:avg(( (xs:boolean(\"false\"), xs:string(\"xyz\"), (), (), \"a\" , \"b\")))",
            "X:FORG0006"
        ),
        ("fn-avg/fn-avg-mix-args-061", "fn:avg(( (xs:boolean(\"1\"), xs:double(\"-INF\"), \"s\")))", "X:FORG0006"),
        ("fn-avg/K-SeqAVGFunc-1", "avg()", "X:XPST0017"),
        ("fn-avg/K-SeqAVGFunc-2", "avg(1, \"wrong param\")", "X:XPST0017"),
        ("fn-avg/K-SeqAVGFunc-3", "empty(avg(()))", "T"),
        ("fn-avg/K-SeqAVGFunc-4", "avg((3, 3, 3)) eq 3", "T"),
        ("fn-avg/K-SeqAVGFunc-5", "avg((-3, -3, -3)) eq -3", "T"),
        ("fn-avg/K-SeqAVGFunc-6", "avg((xs:float(1), xs:integer(3), xs:float(3))) instance of xs:float", "T"),
        ("fn-avg/K-SeqAVGFunc-7", "avg((xs:float(1), xs:integer(3), xs:decimal(3))) instance of xs:float", "T"),
        ("fn-avg/K-SeqAVGFunc-8", "avg((xs:float(1), xs:integer(3), xs:double(3))) instance of xs:double", "T"),
        ("fn-avg/K-SeqAVGFunc-9", "avg((xs:integer(1), xs:integer(3), xs:decimal(3))) instance of xs:decimal", "T"),
        ("fn-avg/K-SeqAVGFunc-10", "avg((xs:float(1), xs:integer(0), xs:float(5))) eq 2.0", "T"),
        ("fn-avg/K-SeqAVGFunc-11", "avg((xs:float(1), xs:integer(0), xs:untypedAtomic(-4))) eq -1", "T"),
        ("fn-avg/K-SeqAVGFunc-12", "avg((xs:float(1), xs:integer(0), xs:untypedAtomic(3))) instance of xs:double", "T"),
        (
            "fn-avg/K-SeqAVGFunc-13",
            "avg((xs:untypedAtomic(3), xs:integer(0), xs:decimal(1))) instance of xs:double",
            "T"
        ),
        ("fn-avg/K-SeqAVGFunc-14", "string(avg((3, 3, xs:double(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-15", "string(avg((3, xs:double(\"NaN\"), 3))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-16", "string(avg((xs:double(\"NaN\"), 3, 3))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-17", "empty(avg(()))", "T"),
        ("fn-avg/K-SeqAVGFunc-18", "empty(avg(((), ())))", "T"),
        ("fn-avg/K-SeqAVGFunc-19", "avg((-5, -0, -3, -6)) eq -3.5", "T"),
        ("fn-avg/K-SeqAVGFunc-20", "string(avg((1, 2, 3, xs:float(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-21", "string(avg((1, 2, 3, xs:double(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-22", "string(avg((xs:double(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-23", "string(avg((xs:float(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-24", "string(avg((1, 2, xs:double(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-25", "string(avg((1, 2, xs:float(\"NaN\"), 1, 2, 3))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-26", "avg(xs:untypedAtomic(\"3\")) instance of xs:double", "T"),
        ("fn-avg/K-SeqAVGFunc-27", "avg((1, 2, xs:untypedAtomic(\"3\"))) instance of xs:double", "T"),
        ("fn-avg/K-SeqAVGFunc-28", "avg((1, 2, xs:untypedAtomic(\"3\"))) eq 2", "T"),
        ("fn-avg/K-SeqAVGFunc-29", "avg((xs:float(1), 2, xs:untypedAtomic(\"3\"))) eq 2", "T"),
        ("fn-avg/K-SeqAVGFunc-30", "avg((xs:float(1), 2, xs:untypedAtomic(\"3\"))) instance of xs:double", "T"),
        ("fn-avg/K-SeqAVGFunc-31", "avg(\"a string\")", "X:FORG0006"),
        ("fn-avg/K-SeqAVGFunc-34", "avg(\"a string\")", "X:FORG0006"),
        ("fn-avg/K-SeqAVGFunc-35", "avg((xs:untypedAtomic(3), xs:integer(3), xs:string(1)))", "X:FORG0006"),
        ("fn-avg/K-SeqAVGFunc-36", "avg((xs:string(1), xs:integer(3), xs:untypedAtomic(3)))", "X:FORG0006"),
        ("fn-avg/K-SeqAVGFunc-37", "avg((xs:float(2), xs:integer(3), \"a string\", xs:double(2)))", "X:FORG0006"),
        ("fn-avg/K-SeqAVGFunc-40", "empty(avg( () ))", "T"),
        ("fn-avg/K-SeqAVGFunc-41", "string(avg((xs:float('INF'), xs:float('-INF')))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-42", "string(avg(((3, 4, 5), xs:float('NaN')))) eq \"NaN\"", "T"),
        ("fn-avg/K-SeqAVGFunc-43", "avg((3, 4, 5)) eq 4.0", "T"),
        ("fn-avg/K2-SeqAVGFunc-1", "avg((xs:float('NaN'), 2, 3, 4, xs:double('NaN'))) instance of xs:double", "T"),
        ("fn-avg/K2-SeqAVGFunc-2", "avg((xs:float('NaN'), 2, 3.3, 4, xs:double('NaN'))) instance of xs:double", "T"),
        ("fn-avg/K2-SeqAVGFunc-3", "avg((1, 1))", "E:1"),
        ("fn-avg/fn-avg-1", "fn:avg((\"a\",\"b\",\"c\"))", "X:FORG0006"),
        ("fn-avg/fn-avg-2", "fn:avg(())", "C:0"),
        ("fn-avg/cbcl-avg-011", "avg((xs:double(\"NaN\"), day-from-date(current-date())))", "S:NaN"),
        ("fn-avg/cbcl-avg-012", "avg((xs:float(\"NaN\"), day-from-date(current-date())))", "S:NaN"),
        ("fn-avg/cbcl-avg-013", "empty(avg(()))", "T\tX:XPST0005"),
        ("fn-avg/fn-avgint1args-1", "fn:avg((xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-avg/fn-avgint1args-2", "fn:avg((xs:int(\"-1873914410\")))", "E:-1873914410"),
        ("fn-avg/fn-avgint1args-3", "fn:avg((xs:int(\"2147483647\")))", "E:2147483647"),
        ("fn-avg/fn-avgint2args-1", "fn:avg((xs:int(\"-2147483648\"),xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-avg/fn-avgint2args-2", "fn:avg((xs:int(\"-1873914410\"),xs:int(\"-2147483648\")))", "E:-2010699029"),
        ("fn-avg/fn-avgint2args-3", "fn:avg((xs:int(\"2147483647\"),xs:int(\"-2147483648\")))", "E:-0.5"),
        ("fn-avg/fn-avgint2args-4", "fn:avg((xs:int(\"-2147483648\"),xs:int(\"-1873914410\")))", "E:-2010699029"),
        ("fn-avg/fn-avgint2args-5", "fn:avg((xs:int(\"-2147483648\"),xs:int(\"2147483647\")))", "E:-0.5"),
        ("fn-avg/fn-avglng1args-1", "fn:avg((xs:long(\"-92233720368547758\")))", "E:-92233720368547758"),
        ("fn-avg/fn-avglng1args-2", "fn:avg((xs:long(\"-47175562203048468\")))", "E:-47175562203048468"),
        ("fn-avg/fn-avglng1args-3", "fn:avg((xs:long(\"92233720368547758\")))", "E:92233720368547758"),
        (
            "fn-avg/fn-avglng2args-1",
            "fn:avg((xs:long(\"-92233720368547758\"),xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-avg/fn-avglng2args-2",
            "fn:avg((xs:long(\"-47175562203048468\"),xs:long(\"-92233720368547758\")))",
            "E:-69704641285798113"
        ),
        ("fn-avg/fn-avglng2args-3", "fn:avg((xs:long(\"92233720368547758\"),xs:long(\"-92233720368547758\")))", "E:0"),
        (
            "fn-avg/fn-avglng2args-4",
            "fn:avg((xs:long(\"-92233720368547758\"),xs:long(\"-47175562203048468\")))",
            "E:-69704641285798113"
        ),
        ("fn-avg/fn-avglng2args-5", "fn:avg((xs:long(\"-92233720368547758\"),xs:long(\"92233720368547758\")))", "E:0"),
        ("fn-avg/fn-avgnint1args-1", "fn:avg((xs:negativeInteger(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-avg/fn-avgnint1args-2", "fn:avg((xs:negativeInteger(\"-297014075999096793\")))", "E:-297014075999096793"),
        ("fn-avg/fn-avgnint1args-3", "fn:avg((xs:negativeInteger(\"-1\")))", "E:-1"),
        (
            "fn-avg/fn-avgnint2args-1",
            "fn:avg((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-avg/fn-avgnint2args-2",
            "fn:avg((xs:negativeInteger(\"-297014075999096793\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-648507037999548396"
        ),
        (
            "fn-avg/fn-avgnint2args-3",
            "fn:avg((xs:negativeInteger(\"-1\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-500000000000000000"
        ),
        (
            "fn-avg/fn-avgnint2args-4",
            "fn:avg((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-297014075999096793\")))",
            "E:-648507037999548396"
        ),
        (
            "fn-avg/fn-avgnint2args-5",
            "fn:avg((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-1\")))",
            "E:-500000000000000000"
        ),
        ("fn-avg/fn-avgnni1args-1", "fn:avg((xs:nonNegativeInteger(\"0\")))", "E:0"),
        ("fn-avg/fn-avgnni1args-2", "fn:avg((xs:nonNegativeInteger(\"303884545991464527\")))", "E:303884545991464527"),
        ("fn-avg/fn-avgnni1args-3", "fn:avg((xs:nonNegativeInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-avg/fn-avgnni2args-1", "fn:avg((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\")))", "E:0"),
        (
            "fn-avg/fn-avgnni2args-2",
            "fn:avg((xs:nonNegativeInteger(\"303884545991464527\"),xs:nonNegativeInteger(\"0\")))",
            "E:151942272995732263.5"
        ),
        (
            "fn-avg/fn-avgnni2args-3",
            "fn:avg((xs:nonNegativeInteger(\"999999999999999999\"),xs:nonNegativeInteger(\"0\")))",
            "E:499999999999999999.5"
        ),
        (
            "fn-avg/fn-avgnni2args-4",
            "fn:avg((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"303884545991464527\")))",
            "E:151942272995732263.5"
        ),
        (
            "fn-avg/fn-avgnni2args-5",
            "fn:avg((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"999999999999999999\")))",
            "E:499999999999999999.5"
        ),
        (
            "fn-avg/fn-avgnpi1args-1",
            "fn:avg((xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-avg/fn-avgnpi1args-2",
            "fn:avg((xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-475688437271870490"
        ),
        ("fn-avg/fn-avgnpi1args-3", "fn:avg((xs:nonPositiveInteger(\"0\")))", "E:0"),
        (
            "fn-avg/fn-avgnpi2args-1",
            "fn:avg((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-avg/fn-avgnpi2args-2",
            "fn:avg((xs:nonPositiveInteger(\"-475688437271870490\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-737844218635935244.5"
        ),
        (
            "fn-avg/fn-avgnpi2args-3",
            "fn:avg((xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-499999999999999999.5"
        ),
        (
            "fn-avg/fn-avgnpi2args-4",
            "fn:avg((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-737844218635935244.5"
        ),
        (
            "fn-avg/fn-avgnpi2args-5",
            "fn:avg((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"0\")))",
            "E:-499999999999999999.5"
        ),
        ("fn-avg/fn-avgpint1args-1", "fn:avg((xs:positiveInteger(\"1\")))", "E:1"),
        ("fn-avg/fn-avgpint1args-2", "fn:avg((xs:positiveInteger(\"52704602390610033\")))", "E:52704602390610033"),
        ("fn-avg/fn-avgpint1args-3", "fn:avg((xs:positiveInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-avg/fn-avgpint2args-1", "fn:avg((xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\")))", "E:1"),
        (
            "fn-avg/fn-avgpint2args-2",
            "fn:avg((xs:positiveInteger(\"52704602390610033\"),xs:positiveInteger(\"1\")))",
            "E:26352301195305017"
        ),
        (
            "fn-avg/fn-avgpint2args-3",
            "fn:avg((xs:positiveInteger(\"999999999999999999\"),xs:positiveInteger(\"1\")))",
            "E:500000000000000000"
        ),
        (
            "fn-avg/fn-avgpint2args-4",
            "fn:avg((xs:positiveInteger(\"1\"),xs:positiveInteger(\"52704602390610033\")))",
            "E:26352301195305017"
        ),
        (
            "fn-avg/fn-avgpint2args-5",
            "fn:avg((xs:positiveInteger(\"1\"),xs:positiveInteger(\"999999999999999999\")))",
            "E:500000000000000000"
        ),
        ("fn-avg/fn-avgsht1args-1", "fn:avg((xs:short(\"-32768\")))", "E:-32768"),
        ("fn-avg/fn-avgsht1args-2", "fn:avg((xs:short(\"-5324\")))", "E:-5324"),
        ("fn-avg/fn-avgsht1args-3", "fn:avg((xs:short(\"32767\")))", "E:32767"),
        ("fn-avg/fn-avgsht2args-1", "fn:avg((xs:short(\"-32768\"),xs:short(\"-32768\")))", "E:-32768"),
        ("fn-avg/fn-avgsht2args-2", "fn:avg((xs:short(\"-5324\"),xs:short(\"-32768\")))", "E:-19046"),
        ("fn-avg/fn-avgsht2args-3", "fn:avg((xs:short(\"32767\"),xs:short(\"-32768\")))", "E:-0.5"),
        ("fn-avg/fn-avgsht2args-4", "fn:avg((xs:short(\"-32768\"),xs:short(\"-5324\")))", "E:-19046"),
        ("fn-avg/fn-avgsht2args-5", "fn:avg((xs:short(\"-32768\"),xs:short(\"32767\")))", "E:-0.5"),
        ("fn-avg/fn-avgulng1args-1", "fn:avg((xs:unsignedLong(\"0\")))", "E:0"),
        ("fn-avg/fn-avgulng1args-2", "fn:avg((xs:unsignedLong(\"130747108607674654\")))", "E:130747108607674654"),
        ("fn-avg/fn-avgulng1args-3", "fn:avg((xs:unsignedLong(\"184467440737095516\")))", "E:184467440737095516"),
        ("fn-avg/fn-avgulng2args-1", "fn:avg((xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\")))", "E:0"),
        (
            "fn-avg/fn-avgulng2args-2",
            "fn:avg((xs:unsignedLong(\"130747108607674654\"),xs:unsignedLong(\"0\")))",
            "E:65373554303837327"
        ),
        (
            "fn-avg/fn-avgulng2args-3",
            "fn:avg((xs:unsignedLong(\"184467440737095516\"),xs:unsignedLong(\"0\")))",
            "E:92233720368547758"
        ),
        (
            "fn-avg/fn-avgulng2args-4",
            "fn:avg((xs:unsignedLong(\"0\"),xs:unsignedLong(\"130747108607674654\")))",
            "E:65373554303837327"
        ),
        (
            "fn-avg/fn-avgulng2args-5",
            "fn:avg((xs:unsignedLong(\"0\"),xs:unsignedLong(\"184467440737095516\")))",
            "E:92233720368547758"
        ),
        ("fn-avg/fn-avgusht1args-1", "fn:avg((xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-avg/fn-avgusht1args-2", "fn:avg((xs:unsignedShort(\"44633\")))", "E:44633"),
        ("fn-avg/fn-avgusht1args-3", "fn:avg((xs:unsignedShort(\"65535\")))", "E:65535"),
        ("fn-avg/fn-avgusht2args-1", "fn:avg((xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-avg/fn-avgusht2args-2", "fn:avg((xs:unsignedShort(\"44633\"),xs:unsignedShort(\"0\")))", "E:22316.5"),
        ("fn-avg/fn-avgusht2args-3", "fn:avg((xs:unsignedShort(\"65535\"),xs:unsignedShort(\"0\")))", "E:32767.5"),
        ("fn-avg/fn-avgusht2args-4", "fn:avg((xs:unsignedShort(\"0\"),xs:unsignedShort(\"44633\")))", "E:22316.5"),
        ("fn-avg/fn-avgusht2args-5", "fn:avg((xs:unsignedShort(\"0\"),xs:unsignedShort(\"65535\")))", "E:32767.5"),
        ("fn-min/fn-minintg1args-1", "fn:min((xs:integer(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-min/fn-minintg1args-2", "fn:min((xs:integer(\"830993497117024304\")))", "E:830993497117024304"),
        ("fn-min/fn-minintg1args-3", "fn:min((xs:integer(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-min/fn-mindec1args-1", "fn:min((xs:decimal(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-min/fn-mindec1args-2", "fn:min((xs:decimal(\"617375191608514839\")))", "E:617375191608514839"),
        ("fn-min/fn-mindec1args-3", "fn:min((xs:decimal(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-min/fn-mindbl1args-1", "fn:min((xs:double(\"-1.7976931348623157E308\")))", "E:-1.7976931348623157E308"),
        ("fn-min/fn-mindbl1args-2", "fn:min((xs:double(\"0\")))", "E:0"),
        ("fn-min/fn-mindbl1args-3", "fn:min((xs:double(\"1.7976931348623157E308\")))", "E:1.7976931348623157E308"),
        ("fn-min/fn-minflt1args-1", "fn:min((xs:float(\"-3.4028235E38\")))", "E:xs:float(\"-3.4028235E38\")"),
        ("fn-min/fn-minflt1args-2", "fn:min((xs:float(\"0\")))", "E:0"),
        ("fn-min/fn-minflt1args-3", "fn:min((xs:float(\"3.4028235E38\")))", "E:xs:float(\"3.4028235E38\")"),
        (
            "fn-min/fn-minintg2args-1",
            "fn:min((xs:integer(\"-999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minintg2args-2",
            "fn:min((xs:integer(\"830993497117024304\"),xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minintg2args-3",
            "fn:min((xs:integer(\"999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minintg2args-4",
            "fn:min((xs:integer(\"-999999999999999999\"),xs:integer(\"830993497117024304\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minintg2args-5",
            "fn:min((xs:integer(\"-999999999999999999\"),xs:integer(\"999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-mindec2args-1",
            "fn:min((xs:decimal(\"-999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-mindec2args-2",
            "fn:min((xs:decimal(\"617375191608514839\"),xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-mindec2args-3",
            "fn:min((xs:decimal(\"999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-mindec2args-4",
            "fn:min((xs:decimal(\"-999999999999999999\"),xs:decimal(\"617375191608514839\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-mindec2args-5",
            "fn:min((xs:decimal(\"-999999999999999999\"),xs:decimal(\"999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-mindbl2args-1",
            "fn:min((xs:double(\"-1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-min/fn-mindbl2args-2",
            "fn:min((xs:double(\"0\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-min/fn-mindbl2args-3",
            "fn:min((xs:double(\"1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-min/fn-mindbl2args-4",
            "fn:min((xs:double(\"-1.7976931348623157E308\"),xs:double(\"0\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-min/fn-mindbl2args-5",
            "fn:min((xs:double(\"-1.7976931348623157E308\"),xs:double(\"1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        (
            "fn-min/fn-minflt2args-1",
            "fn:min((xs:float(\"-3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        (
            "fn-min/fn-minflt2args-2",
            "fn:min((xs:float(\"0\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        (
            "fn-min/fn-minflt2args-3",
            "fn:min((xs:float(\"3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        (
            "fn-min/fn-minflt2args-4",
            "fn:min((xs:float(\"-3.4028235E38\"),xs:float(\"0\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        (
            "fn-min/fn-minflt2args-5",
            "fn:min((xs:float(\"-3.4028235E38\"),xs:float(\"3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        ("fn-min/K-SeqMINFunc-1", "min()", "X:XPST0017"),
        (
            "fn-min/K-SeqMINFunc-2",
            "min(\"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        ("fn-min/K-SeqMINFunc-3", "empty(min(()))", "T"),
        ("fn-min/K-SeqMINFunc-4", "min((3, 3, 3, 3, 3, 3)) eq 3", "T"),
        ("fn-min/K-SeqMINFunc-5", "min((3, 1, 1, 1, 1, 1)) eq 1", "T"),
        ("fn-min/K-SeqMINFunc-6", "min((1, 1, 1, 1, 1, 3)) eq 1", "T"),
        ("fn-min/K-SeqMINFunc-7", "min((3, 1, 5, 1, 1, 3)) eq 1", "T"),
        ("fn-min/K-SeqMINFunc-8", "min((3, -5.0, 5, 1, -3, 3)) eq -5.0", "T"),
        ("fn-min/K-SeqMINFunc-9", "min(xs:untypedAtomic(\"3\")) instance of xs:double", "T"),
        ("fn-min/K-SeqMINFunc-10", "min(xs:untypedAtomic(\"3\")) eq 3", "T"),
        ("fn-min/K-SeqMINFunc-11", "min((xs:untypedAtomic(\"1\"), 3, 2)) instance of xs:double", "T"),
        ("fn-min/K-SeqMINFunc-12", "min((3, xs:float(2), xs:untypedAtomic(\"1\"))) eq 1", "T"),
        ("fn-min/K-SeqMINFunc-13", "min((3, xs:float(2), xs:untypedAtomic(\"1\"))) instance of xs:double", "T"),
        ("fn-min/K-SeqMINFunc-14", "min((1, xs:float(2), xs:decimal(3))) instance of xs:float", "T"),
        ("fn-min/K-SeqMINFunc-15", "min((3, xs:untypedAtomic(\"1\"), xs:float(2))) instance of xs:double", "T"),
        ("fn-min/K-SeqMINFunc-16", "string(min((1, xs:untypedAtomic(\"NaN\"), xs:float(2)))) eq \"NaN\"", "T"),
        (
            "fn-min/K-SeqMINFunc-17",
            "string(min((xs:float(\"NaN\"), xs:untypedAtomic(\"3\"), xs:float(2)))) eq \"NaN\"",
            "T"
        ),
        (
            "fn-min/K-SeqMINFunc-18",
            "min((xs:float(\"NaN\"), xs:untypedAtomic(\"3\"), xs:double(2))) instance of xs:double",
            "T"
        ),
        ("fn-min/K-SeqMINFunc-19", "min((xs:float(\"NaN\"), 1, 1, 2, xs:double(\"NaN\"))) instance of xs:double", "T"),
        ("fn-min/K-SeqMINFunc-20", "min((xs:double(\"NaN\"), 1, 1, 2, xs:float(\"NaN\"))) instance of xs:double", "T"),
        ("fn-min/K-SeqMINFunc-21", "min((xs:float(\"NaN\"), 1, \"a string\"))", "X:FORG0006"),
        ("fn-min/K-SeqMINFunc-22", "min((\"a string\", 1, xs:float(\"NaN\")))", "X:FORG0006"),
        ("fn-min/K-SeqMINFunc-23", "max((xs:float(\"NaN\"), 1, xs:untypedAtomic(\"one\")))", "X:FORG0001"),
        ("fn-min/K-SeqMINFunc-24", "max((xs:untypedAtomic(\"one\"), 1, xs:float(\"NaN\")))", "X:FORG0001"),
        ("fn-min/K-SeqMINFunc-25", "string(min((xs:double(\"NaN\"), xs:double(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-min/K-SeqMINFunc-26", "string(min((xs:float(\"NaN\"), xs:float(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-min/K-SeqMINFunc-27", "string(min((3, xs:double(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-min/K-SeqMINFunc-28", "string(min((3, xs:float(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-min/K-SeqMINFunc-29", "min((3, xs:double(\"NaN\"))) instance of xs:double", "T"),
        ("fn-min/K-SeqMINFunc-30", "min((3, xs:float(\"NaN\"))) instance of xs:float", "T"),
        (
            "fn-min/K-SeqMINFunc-31",
            "string(min((xs:float(-3), xs:untypedAtomic(\"3\"), xs:double(\"NaN\")))) eq \"NaN\"",
            "T"
        ),
        (
            "fn-min/K-SeqMINFunc-32",
            "min((xs:float(-3), xs:untypedAtomic(\"3\"), xs:double(\"NaN\"))) instance of xs:double",
            "T"
        ),
        ("fn-min/K-SeqMINFunc-33", "string(min(xs:float(\"NaN\"))) eq \"NaN\"", "T"),
        ("fn-min/K-SeqMINFunc-34", "string(min(xs:double(\"NaN\"))) eq \"NaN\"", "T"),
        ("fn-min/K-SeqMINFunc-35", "min(xs:untypedAtomic(\"three\"))", "X:FORG0001"),
        ("fn-min/K-SeqMINFunc-36", "min((xs:untypedAtomic(\"3\"), \"a string\"))", "X:FORG0006"),
        ("fn-min/K-SeqMINFunc-37", "min((\"a string\", xs:untypedAtomic(\"3\")))", "X:FORG0006"),
        ("fn-min/K-SeqMINFunc-41", "min((\"a string\")) eq \"a string\"", "T"),
        ("fn-min/K-SeqMINFunc-43", "min((5, 5.0e0)) eq 5.0e0", "T"),
        ("fn-min/K-SeqMINFunc-44", "min((5.0e0, 5)) eq 5.0e0", "T"),
        ("fn-min/K-SeqMINFunc-45", "min((3, 5.0e0)) eq 3", "T"),
        ("fn-min/K-SeqMINFunc-46", "min((5.0e0, 3)) eq 3", "T"),
        ("fn-min/K-SeqMINFunc-48", "min((3,4,5)) eq 3", "T"),
        ("fn-min/K-SeqMINFunc-49", "min((5, 5.0e0)) eq 5.0e0", "T"),
        ("fn-min/K-SeqMINFunc-50", "min((3,4, \"Zero\"))", "X:FORG0006"),
        ("fn-min/K-SeqMINFunc-51", "min((xs:float(0.0E0), xs:float(-0.0E0))) eq xs:float(\"-0\")", "T"),
        (
            "fn-min/K2-SeqMINFunc-4",
            "min((\"str1\", \"str2\"), \"http://example.com/UNSUPPORTED_COLLATION\")",
            "X:FOCH0002"
        ),
        (
            "fn-min/K2-SeqMINFunc-6",
            "min((\"str1\", \"str2\"), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", ())",
            "X:XPST0017"
        ),
        ("fn-min/K2-SeqMINFunc-7", "min((5, 5.0e0)) instance of xs:double", "T"),
        ("fn-min/K2-SeqMINFunc-8", "min((5.0e0, 5)) instance of xs:double", "T"),
        ("fn-min/K2-SeqMINFunc-9", "min((3, 5.0e0)) instance of xs:double", "T"),
        ("fn-min/K2-SeqMINFunc-10", "min((5.0e0, 3)) instance of xs:double", "T"),
        ("fn-min/K2-SeqMINFunc-11", "min((1, 1, 1, 1, 1.0)) instance of xs:decimal", "T"),
        ("fn-min/K2-SeqMINFunc-12", "min((1.0, 1, 1, 1, 1)) instance of xs:decimal", "T"),
        ("fn-min/K2-SeqMINFunc-13", "min((1.0, 1, 1.0, 1, 1)) instance of xs:decimal", "T"),
        ("fn-min/K2-SeqMINFunc-14", "min((\"a\", \"b\", \"c\")) eq \"a\"", "T"),
        ("fn-min/K2-SeqMINFunc-15", "min(xs:unsignedShort(\"1\")) instance of xs:unsignedShort", "T"),
        ("fn-min/fn-min-1", "fn:min((3,4,\"Zero\"))", "X:FORG0006"),
        ("fn-min/fn-min-2", "fn:min((xs:integer(5000000),xs:double(3e8)))", "E:5.0E6"),
        ("fn-min/fn-min-11", "min((\"a\", \"b\", \"c\", \"d\"))", "S:a"),
        ("fn-min/fn-min-13", "min((xs:NCName('a'), xs:ID('b'), xs:token('c')))", "E:\"a\"Y:xs:NCName"),
        ("fn-min/fn-min-18", "min((xs:token(\"http\"), xs:anyURI(\"http://b.com\")))", "E:\"http\"Y:xs:token"),
        (
            "fn-min/fn-min-14",
            "min((xs:positiveInteger(123), xs:unsignedShort(124)))",
            "E:123\u001fY:xs:nonNegativeInteger"
        ),
        ("fn-min/fn-minint1args-1", "fn:min((xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-min/fn-minint1args-2", "fn:min((xs:int(\"-1873914410\")))", "E:-1873914410"),
        ("fn-min/fn-minint1args-3", "fn:min((xs:int(\"2147483647\")))", "E:2147483647"),
        ("fn-min/fn-minint2args-1", "fn:min((xs:int(\"-2147483648\"),xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-min/fn-minint2args-2", "fn:min((xs:int(\"-1873914410\"),xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-min/fn-minint2args-3", "fn:min((xs:int(\"2147483647\"),xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-min/fn-minint2args-4", "fn:min((xs:int(\"-2147483648\"),xs:int(\"-1873914410\")))", "E:-2147483648"),
        ("fn-min/fn-minint2args-5", "fn:min((xs:int(\"-2147483648\"),xs:int(\"2147483647\")))", "E:-2147483648"),
        ("fn-min/fn-minlng1args-1", "fn:min((xs:long(\"-92233720368547758\")))", "E:-92233720368547758"),
        ("fn-min/fn-minlng1args-2", "fn:min((xs:long(\"-47175562203048468\")))", "E:-47175562203048468"),
        ("fn-min/fn-minlng1args-3", "fn:min((xs:long(\"92233720368547758\")))", "E:92233720368547758"),
        (
            "fn-min/fn-minlng2args-1",
            "fn:min((xs:long(\"-92233720368547758\"),xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-min/fn-minlng2args-2",
            "fn:min((xs:long(\"-47175562203048468\"),xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-min/fn-minlng2args-3",
            "fn:min((xs:long(\"92233720368547758\"),xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-min/fn-minlng2args-4",
            "fn:min((xs:long(\"-92233720368547758\"),xs:long(\"-47175562203048468\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-min/fn-minlng2args-5",
            "fn:min((xs:long(\"-92233720368547758\"),xs:long(\"92233720368547758\")))",
            "E:-92233720368547758"
        ),
        ("fn-min/fn-minnint1args-1", "fn:min((xs:negativeInteger(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-min/fn-minnint1args-2", "fn:min((xs:negativeInteger(\"-297014075999096793\")))", "E:-297014075999096793"),
        ("fn-min/fn-minnint1args-3", "fn:min((xs:negativeInteger(\"-1\")))", "E:-1"),
        (
            "fn-min/fn-minnint2args-1",
            "fn:min((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnint2args-2",
            "fn:min((xs:negativeInteger(\"-297014075999096793\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnint2args-3",
            "fn:min((xs:negativeInteger(\"-1\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnint2args-4",
            "fn:min((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-297014075999096793\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnint2args-5",
            "fn:min((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-1\")))",
            "E:-999999999999999999"
        ),
        ("fn-min/fn-minnni1args-1", "fn:min((xs:nonNegativeInteger(\"0\")))", "E:0"),
        ("fn-min/fn-minnni1args-2", "fn:min((xs:nonNegativeInteger(\"303884545991464527\")))", "E:303884545991464527"),
        ("fn-min/fn-minnni1args-3", "fn:min((xs:nonNegativeInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-min/fn-minnni2args-1", "fn:min((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\")))", "E:0"),
        (
            "fn-min/fn-minnni2args-2",
            "fn:min((xs:nonNegativeInteger(\"303884545991464527\"),xs:nonNegativeInteger(\"0\")))",
            "E:0"
        ),
        (
            "fn-min/fn-minnni2args-3",
            "fn:min((xs:nonNegativeInteger(\"999999999999999999\"),xs:nonNegativeInteger(\"0\")))",
            "E:0"
        ),
        (
            "fn-min/fn-minnni2args-4",
            "fn:min((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"303884545991464527\")))",
            "E:0"
        ),
        (
            "fn-min/fn-minnni2args-5",
            "fn:min((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-min/fn-minnpi1args-1",
            "fn:min((xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnpi1args-2",
            "fn:min((xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-475688437271870490"
        ),
        ("fn-min/fn-minnpi1args-3", "fn:min((xs:nonPositiveInteger(\"0\")))", "E:0"),
        (
            "fn-min/fn-minnpi2args-1",
            "fn:min((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnpi2args-2",
            "fn:min((xs:nonPositiveInteger(\"-475688437271870490\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnpi2args-3",
            "fn:min((xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnpi2args-4",
            "fn:min((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-min/fn-minnpi2args-5",
            "fn:min((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"0\")))",
            "E:-999999999999999999"
        ),
        ("fn-min/fn-minpint1args-1", "fn:min((xs:positiveInteger(\"1\")))", "E:1"),
        ("fn-min/fn-minpint1args-2", "fn:min((xs:positiveInteger(\"52704602390610033\")))", "E:52704602390610033"),
        ("fn-min/fn-minpint1args-3", "fn:min((xs:positiveInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-min/fn-minpint2args-1", "fn:min((xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\")))", "E:1"),
        (
            "fn-min/fn-minpint2args-2",
            "fn:min((xs:positiveInteger(\"52704602390610033\"),xs:positiveInteger(\"1\")))",
            "E:1"
        ),
        (
            "fn-min/fn-minpint2args-3",
            "fn:min((xs:positiveInteger(\"999999999999999999\"),xs:positiveInteger(\"1\")))",
            "E:1"
        ),
        (
            "fn-min/fn-minpint2args-4",
            "fn:min((xs:positiveInteger(\"1\"),xs:positiveInteger(\"52704602390610033\")))",
            "E:1"
        ),
        (
            "fn-min/fn-minpint2args-5",
            "fn:min((xs:positiveInteger(\"1\"),xs:positiveInteger(\"999999999999999999\")))",
            "E:1"
        ),
        ("fn-min/fn-minsht1args-1", "fn:min((xs:short(\"-32768\")))", "E:-32768"),
        ("fn-min/fn-minsht1args-2", "fn:min((xs:short(\"-5324\")))", "E:-5324"),
        ("fn-min/fn-minsht1args-3", "fn:min((xs:short(\"32767\")))", "E:32767"),
        ("fn-min/fn-minsht2args-1", "fn:min((xs:short(\"-32768\"),xs:short(\"-32768\")))", "E:-32768"),
        ("fn-min/fn-minsht2args-2", "fn:min((xs:short(\"-5324\"),xs:short(\"-32768\")))", "E:-32768"),
        ("fn-min/fn-minsht2args-3", "fn:min((xs:short(\"32767\"),xs:short(\"-32768\")))", "E:-32768"),
        ("fn-min/fn-minsht2args-4", "fn:min((xs:short(\"-32768\"),xs:short(\"-5324\")))", "E:-32768"),
        ("fn-min/fn-minsht2args-5", "fn:min((xs:short(\"-32768\"),xs:short(\"32767\")))", "E:-32768"),
        ("fn-min/fn-minulng1args-1", "fn:min((xs:unsignedLong(\"0\")))", "E:0"),
        ("fn-min/fn-minulng1args-2", "fn:min((xs:unsignedLong(\"130747108607674654\")))", "E:130747108607674654"),
        ("fn-min/fn-minulng1args-3", "fn:min((xs:unsignedLong(\"184467440737095516\")))", "E:184467440737095516"),
        ("fn-min/fn-minulng2args-1", "fn:min((xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\")))", "E:0"),
        ("fn-min/fn-minulng2args-2", "fn:min((xs:unsignedLong(\"130747108607674654\"),xs:unsignedLong(\"0\")))", "E:0"),
        ("fn-min/fn-minulng2args-3", "fn:min((xs:unsignedLong(\"184467440737095516\"),xs:unsignedLong(\"0\")))", "E:0"),
        ("fn-min/fn-minulng2args-4", "fn:min((xs:unsignedLong(\"0\"),xs:unsignedLong(\"130747108607674654\")))", "E:0"),
        ("fn-min/fn-minulng2args-5", "fn:min((xs:unsignedLong(\"0\"),xs:unsignedLong(\"184467440737095516\")))", "E:0"),
        ("fn-min/fn-minusht1args-1", "fn:min((xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-min/fn-minusht1args-2", "fn:min((xs:unsignedShort(\"44633\")))", "E:44633"),
        ("fn-min/fn-minusht1args-3", "fn:min((xs:unsignedShort(\"65535\")))", "E:65535"),
        ("fn-min/fn-minusht2args-1", "fn:min((xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-min/fn-minusht2args-2", "fn:min((xs:unsignedShort(\"44633\"),xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-min/fn-minusht2args-3", "fn:min((xs:unsignedShort(\"65535\"),xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-min/fn-minusht2args-4", "fn:min((xs:unsignedShort(\"0\"),xs:unsignedShort(\"44633\")))", "E:0"),
        ("fn-min/fn-minusht2args-5", "fn:min((xs:unsignedShort(\"0\"),xs:unsignedShort(\"65535\")))", "E:0"),
        ("fn-max/fn-maxintg1args-1", "fn:max((xs:integer(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-max/fn-maxintg1args-2", "fn:max((xs:integer(\"830993497117024304\")))", "E:830993497117024304"),
        ("fn-max/fn-maxintg1args-3", "fn:max((xs:integer(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-max/fn-maxdec1args-1", "fn:max((xs:decimal(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-max/fn-maxdec1args-2", "fn:max((xs:decimal(\"617375191608514839\")))", "E:617375191608514839"),
        ("fn-max/fn-maxdec1args-3", "fn:max((xs:decimal(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-max/fn-maxdbl1args-1", "fn:max((xs:double(\"-1.7976931348623157E308\")))", "E:-1.7976931348623157E308"),
        ("fn-max/fn-maxdbl1args-2", "fn:max((xs:double(\"0\")))", "E:0"),
        ("fn-max/fn-maxdbl1args-3", "fn:max((xs:double(\"1.7976931348623157E308\")))", "E:1.7976931348623157E308"),
        ("fn-max/fn-maxflt1args-1", "fn:max((xs:float(\"-3.4028235E38\")))", "E:xs:float(\"-3.4028235E38\")"),
        ("fn-max/fn-maxflt1args-2", "fn:max((xs:float(\"0\")))", "E:0"),
        ("fn-max/fn-maxflt1args-3", "fn:max((xs:float(\"3.4028235E38\")))", "E:xs:float(\"3.4028235E38\")"),
        (
            "fn-max/fn-maxintg2args-1",
            "fn:max((xs:integer(\"-999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-max/fn-maxintg2args-2",
            "fn:max((xs:integer(\"830993497117024304\"),xs:integer(\"-999999999999999999\")))",
            "E:830993497117024304"
        ),
        (
            "fn-max/fn-maxintg2args-3",
            "fn:max((xs:integer(\"999999999999999999\"),xs:integer(\"-999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-max/fn-maxintg2args-4",
            "fn:max((xs:integer(\"-999999999999999999\"),xs:integer(\"830993497117024304\")))",
            "E:830993497117024304"
        ),
        (
            "fn-max/fn-maxintg2args-5",
            "fn:max((xs:integer(\"-999999999999999999\"),xs:integer(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-max/fn-maxdec2args-1",
            "fn:max((xs:decimal(\"-999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-max/fn-maxdec2args-2",
            "fn:max((xs:decimal(\"617375191608514839\"),xs:decimal(\"-999999999999999999\")))",
            "E:617375191608514839"
        ),
        (
            "fn-max/fn-maxdec2args-3",
            "fn:max((xs:decimal(\"999999999999999999\"),xs:decimal(\"-999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-max/fn-maxdec2args-4",
            "fn:max((xs:decimal(\"-999999999999999999\"),xs:decimal(\"617375191608514839\")))",
            "E:617375191608514839"
        ),
        (
            "fn-max/fn-maxdec2args-5",
            "fn:max((xs:decimal(\"-999999999999999999\"),xs:decimal(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-max/fn-maxdbl2args-1",
            "fn:max((xs:double(\"-1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        ("fn-max/fn-maxdbl2args-2", "fn:max((xs:double(\"0\"),xs:double(\"-1.7976931348623157E308\")))", "E:0"),
        (
            "fn-max/fn-maxdbl2args-3",
            "fn:max((xs:double(\"1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\")))",
            "E:1.7976931348623157E308"
        ),
        ("fn-max/fn-maxdbl2args-4", "fn:max((xs:double(\"-1.7976931348623157E308\"),xs:double(\"0\")))", "E:0"),
        (
            "fn-max/fn-maxdbl2args-5",
            "fn:max((xs:double(\"-1.7976931348623157E308\"),xs:double(\"1.7976931348623157E308\")))",
            "E:1.7976931348623157E308"
        ),
        (
            "fn-max/fn-maxflt2args-1",
            "fn:max((xs:float(\"-3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(-3.4028235E38)"
        ),
        ("fn-max/fn-maxflt2args-2", "fn:max((xs:float(\"0\"),xs:float(\"-3.4028235E38\")))", "E:0"),
        (
            "fn-max/fn-maxflt2args-3",
            "fn:max((xs:float(\"3.4028235E38\"),xs:float(\"-3.4028235E38\")))",
            "E:xs:float(3.4028235E38)"
        ),
        ("fn-max/fn-maxflt2args-4", "fn:max((xs:float(\"-3.4028235E38\"),xs:float(\"0\")))", "E:0"),
        (
            "fn-max/fn-maxflt2args-5",
            "fn:max((xs:float(\"-3.4028235E38\"),xs:float(\"3.4028235E38\")))",
            "E:xs:float(3.4028235E38)"
        ),
        ("fn-max/K-SeqMAXFunc-1", "max()", "X:XPST0017"),
        (
            "fn-max/K-SeqMAXFunc-2",
            "max(\"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        ("fn-max/K-SeqMAXFunc-3", "empty(max(()))", "T"),
        ("fn-max/K-SeqMAXFunc-4", "max((3, 3, 3, 3, 3, 3)) eq 3", "T"),
        ("fn-max/K-SeqMAXFunc-5", "max((3, 1, 1, 1, 1, 1)) eq 3", "T"),
        ("fn-max/K-SeqMAXFunc-6", "max((1, 1, 1, 1, 1, 3)) eq 3", "T"),
        ("fn-max/K-SeqMAXFunc-7", "max((3, 1, 5, 1, 1, 3)) eq 5", "T"),
        ("fn-max/K-SeqMAXFunc-8", "max((3, -5.0, 5, 1, -3, 3)) eq 5", "T"),
        ("fn-max/K-SeqMAXFunc-9", "max(xs:untypedAtomic(\"3\")) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-10", "max(xs:untypedAtomic(\"3\")) eq 3", "T"),
        ("fn-max/K-SeqMAXFunc-11", "max((xs:untypedAtomic(\"3\"), 1, 2)) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-12", "max((1, xs:float(2), xs:untypedAtomic(\"3\"))) eq 3", "T"),
        ("fn-max/K-SeqMAXFunc-13", "max((1, xs:float(2), xs:untypedAtomic(\"3\"))) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-14", "max((1, xs:float(2), xs:decimal(3))) instance of xs:float", "T"),
        ("fn-max/K-SeqMAXFunc-15", "max((1, xs:untypedAtomic(\"3\"), xs:float(2))) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-16", "string(max((1, xs:untypedAtomic(\"NaN\"), xs:float(2)))) eq \"NaN\"", "T"),
        (
            "fn-max/K-SeqMAXFunc-17",
            "string(max((xs:float(\"NaN\"), xs:untypedAtomic(\"3\"), xs:float(2)))) eq \"NaN\"",
            "T"
        ),
        (
            "fn-max/K-SeqMAXFunc-18",
            "max((xs:float(\"NaN\"), xs:untypedAtomic(\"3\"), xs:float(2))) instance of xs:double",
            "T"
        ),
        ("fn-max/K-SeqMAXFunc-19", "max((xs:float(\"NaN\"), 1, 1, 2, xs:double(\"NaN\"))) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-20", "max((xs:double(\"NaN\"), 1, 1, 2, xs:float(\"NaN\"))) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-21", "max((xs:float(\"NaN\"), 1, \"a string\"))", "X:FORG0006"),
        ("fn-max/K-SeqMAXFunc-22", "max((\"a string\", 1, xs:float(\"NaN\")))", "X:FORG0006"),
        ("fn-max/K-SeqMAXFunc-23", "max((xs:float(\"NaN\"), 1, xs:untypedAtomic(\"one\")))", "X:FORG0001"),
        ("fn-max/K-SeqMAXFunc-24", "max((xs:untypedAtomic(\"one\"), 1, xs:float(\"NaN\")))", "X:FORG0001"),
        ("fn-max/K-SeqMAXFunc-25", "string(max((xs:double(\"NaN\"), xs:double(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-max/K-SeqMAXFunc-26", "string(max((xs:float(\"NaN\"), xs:float(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-max/K-SeqMAXFunc-27", "string(max((3, xs:double(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-max/K-SeqMAXFunc-28", "string(max((3, xs:float(\"NaN\")))) eq \"NaN\"", "T"),
        ("fn-max/K-SeqMAXFunc-29", "max((3, xs:double(\"NaN\"))) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-30", "max((3, xs:float(\"NaN\"))) instance of xs:float", "T"),
        (
            "fn-max/K-SeqMAXFunc-31",
            "string(max((xs:float(-3), xs:untypedAtomic(\"3\"), xs:double(\"NaN\")))) eq \"NaN\"",
            "T"
        ),
        (
            "fn-max/K-SeqMAXFunc-32",
            "max((xs:float(-3), xs:untypedAtomic(\"3\"), xs:double(\"NaN\"))) instance of xs:double",
            "T"
        ),
        ("fn-max/K-SeqMAXFunc-33", "string(max(xs:float(\"NaN\"))) eq \"NaN\"", "T"),
        ("fn-max/K-SeqMAXFunc-34", "string(max(xs:double(\"NaN\"))) eq \"NaN\"", "T"),
        ("fn-max/K-SeqMAXFunc-35", "max(xs:untypedAtomic(\"three\"))", "X:FORG0001"),
        ("fn-max/K-SeqMAXFunc-36", "max((xs:untypedAtomic(\"3\"), \"a string\"))", "X:FORG0006"),
        ("fn-max/K-SeqMAXFunc-37", "max((\"a string\", xs:untypedAtomic(\"3\")))", "X:FORG0006"),
        ("fn-max/K-SeqMAXFunc-41", "max((\"a string\")) eq \"a string\"", "T"),
        ("fn-max/K-SeqMAXFunc-43", "max((5, 5.0e0)) eq 5.0e0", "T"),
        ("fn-max/K-SeqMAXFunc-44", "max((5, 5.0e0)) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-45", "max((5, 3.0e0)) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-46", "max((5.0e0, 5)) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-47", "max((3, 5.0e0)) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-48", "max((5.0e0, 3)) instance of xs:double", "T"),
        ("fn-max/K-SeqMAXFunc-49", "max((1, 1, 1, 1, 1.0)) instance of xs:decimal", "T"),
        ("fn-max/K-SeqMAXFunc-50", "max((1.0, 1, 1, 1, 1)) instance of xs:decimal", "T"),
        ("fn-max/K-SeqMAXFunc-51", "max((1.0, 1, 1.0, 1, 1)) instance of xs:decimal", "T"),
        ("fn-max/K-SeqMAXFunc-52", "max((5.0e0, 5)) eq 5.0e0", "T"),
        ("fn-max/K-SeqMAXFunc-53", "max((3, 5.0e0)) eq 5.0e0", "T"),
        ("fn-max/K-SeqMAXFunc-54", "max((5.0e0, 3)) eq 5.0e0", "T"),
        ("fn-max/K-SeqMAXFunc-56", "max((3,4,5)) eq 5", "T"),
        ("fn-max/K-SeqMAXFunc-57", "max((5, 5.0e0)) eq 5.0e0", "T"),
        ("fn-max/K-SeqMAXFunc-58", "max((3,4, \"Zero\"))", "X:FORG0006"),
        ("fn-max/K-SeqMAXFunc-59", "max((\"a\", \"b\", \"c\")) eq \"c\"", "T"),
        (
            "fn-max/K2-SeqMAXFunc-4",
            "max((\"str1\", \"str2\"), \"http://example.com/UNSUPPORTED_COLLATION\")",
            "X:FOCH0002"
        ),
        (
            "fn-max/K2-SeqMAXFunc-5",
            "max((\"str1\", \"str2\"), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", ())",
            "X:XPST0017"
        ),
        ("fn-max/K2-SeqMAXFunc-7", "max(xs:unsignedShort(\"1\")) instance of xs:unsignedShort", "T"),
        ("fn-max/fn-max-1", "fn:max((3,4,\"Zero\"))", "X:FORG0006"),
        ("fn-max/fn-max-2", "fn:max((xs:integer(5000000000),xs:double(3e0)))", "E:5.0E9"),
        ("fn-max/fn-max-11", "max((\"a\", \"b\", \"c\", \"d\"))", "S:d"),
        ("fn-max/fn-max-13", "max((xs:NCName('c'), xs:ID('b'), xs:token('a')))", "E:\"c\"Y:xs:NCName"),
        ("fn-max/fn-max-18", "max((xs:token(\"zither\"), xs:anyURI(\"http://b.com\")))", "E:\"zither\"Y:xs:token"),
        (
            "fn-max/fn-max-14",
            "max((xs:positiveInteger(123), xs:unsignedShort(124)))",
            "E:124\u001fY:xs:nonNegativeInteger"
        ),
        ("fn-max/fn-maxint1args-1", "fn:max((xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-max/fn-maxint1args-2", "fn:max((xs:int(\"-1873914410\")))", "E:-1873914410"),
        ("fn-max/fn-maxint1args-3", "fn:max((xs:int(\"2147483647\")))", "E:2147483647"),
        ("fn-max/fn-maxint2args-1", "fn:max((xs:int(\"-2147483648\"),xs:int(\"-2147483648\")))", "E:-2147483648"),
        ("fn-max/fn-maxint2args-2", "fn:max((xs:int(\"-1873914410\"),xs:int(\"-2147483648\")))", "E:-1873914410"),
        ("fn-max/fn-maxint2args-3", "fn:max((xs:int(\"2147483647\"),xs:int(\"-2147483648\")))", "E:2147483647"),
        ("fn-max/fn-maxint2args-4", "fn:max((xs:int(\"-2147483648\"),xs:int(\"-1873914410\")))", "E:-1873914410"),
        ("fn-max/fn-maxint2args-5", "fn:max((xs:int(\"-2147483648\"),xs:int(\"2147483647\")))", "E:2147483647"),
        ("fn-max/fn-maxlng1args-1", "fn:max((xs:long(\"-92233720368547758\")))", "E:-92233720368547758"),
        ("fn-max/fn-maxlng1args-2", "fn:max((xs:long(\"-47175562203048468\")))", "E:-47175562203048468"),
        ("fn-max/fn-maxlng1args-3", "fn:max((xs:long(\"92233720368547758\")))", "E:92233720368547758"),
        (
            "fn-max/fn-maxlng2args-1",
            "fn:max((xs:long(\"-92233720368547758\"),xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-max/fn-maxlng2args-2",
            "fn:max((xs:long(\"-47175562203048468\"),xs:long(\"-92233720368547758\")))",
            "E:-47175562203048468"
        ),
        (
            "fn-max/fn-maxlng2args-3",
            "fn:max((xs:long(\"92233720368547758\"),xs:long(\"-92233720368547758\")))",
            "E:92233720368547758"
        ),
        (
            "fn-max/fn-maxlng2args-4",
            "fn:max((xs:long(\"-92233720368547758\"),xs:long(\"-47175562203048468\")))",
            "E:-47175562203048468"
        ),
        (
            "fn-max/fn-maxlng2args-5",
            "fn:max((xs:long(\"-92233720368547758\"),xs:long(\"92233720368547758\")))",
            "E:92233720368547758"
        ),
        ("fn-max/fn-maxnint1args-1", "fn:max((xs:negativeInteger(\"-999999999999999999\")))", "E:-999999999999999999"),
        ("fn-max/fn-maxnint1args-2", "fn:max((xs:negativeInteger(\"-297014075999096793\")))", "E:-297014075999096793"),
        ("fn-max/fn-maxnint1args-3", "fn:max((xs:negativeInteger(\"-1\")))", "E:-1"),
        (
            "fn-max/fn-maxnint2args-1",
            "fn:max((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-max/fn-maxnint2args-2",
            "fn:max((xs:negativeInteger(\"-297014075999096793\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-297014075999096793"
        ),
        (
            "fn-max/fn-maxnint2args-3",
            "fn:max((xs:negativeInteger(\"-1\"),xs:negativeInteger(\"-999999999999999999\")))",
            "E:-1"
        ),
        (
            "fn-max/fn-maxnint2args-4",
            "fn:max((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-297014075999096793\")))",
            "E:-297014075999096793"
        ),
        (
            "fn-max/fn-maxnint2args-5",
            "fn:max((xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-1\")))",
            "E:-1"
        ),
        ("fn-max/fn-maxnni1args-1", "fn:max((xs:nonNegativeInteger(\"0\")))", "E:0"),
        ("fn-max/fn-maxnni1args-2", "fn:max((xs:nonNegativeInteger(\"303884545991464527\")))", "E:303884545991464527"),
        ("fn-max/fn-maxnni1args-3", "fn:max((xs:nonNegativeInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-max/fn-maxnni2args-1", "fn:max((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\")))", "E:0"),
        (
            "fn-max/fn-maxnni2args-2",
            "fn:max((xs:nonNegativeInteger(\"303884545991464527\"),xs:nonNegativeInteger(\"0\")))",
            "E:303884545991464527"
        ),
        (
            "fn-max/fn-maxnni2args-3",
            "fn:max((xs:nonNegativeInteger(\"999999999999999999\"),xs:nonNegativeInteger(\"0\")))",
            "E:999999999999999999"
        ),
        (
            "fn-max/fn-maxnni2args-4",
            "fn:max((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"303884545991464527\")))",
            "E:303884545991464527"
        ),
        (
            "fn-max/fn-maxnni2args-5",
            "fn:max((xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-max/fn-maxnpi1args-1",
            "fn:max((xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-max/fn-maxnpi1args-2",
            "fn:max((xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-475688437271870490"
        ),
        ("fn-max/fn-maxnpi1args-3", "fn:max((xs:nonPositiveInteger(\"0\")))", "E:0"),
        (
            "fn-max/fn-maxnpi2args-1",
            "fn:max((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-max/fn-maxnpi2args-2",
            "fn:max((xs:nonPositiveInteger(\"-475688437271870490\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-475688437271870490"
        ),
        (
            "fn-max/fn-maxnpi2args-3",
            "fn:max((xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:0"
        ),
        (
            "fn-max/fn-maxnpi2args-4",
            "fn:max((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-475688437271870490"
        ),
        (
            "fn-max/fn-maxnpi2args-5",
            "fn:max((xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"0\")))",
            "E:0"
        ),
        ("fn-max/fn-maxpint1args-1", "fn:max((xs:positiveInteger(\"1\")))", "E:1"),
        ("fn-max/fn-maxpint1args-2", "fn:max((xs:positiveInteger(\"52704602390610033\")))", "E:52704602390610033"),
        ("fn-max/fn-maxpint1args-3", "fn:max((xs:positiveInteger(\"999999999999999999\")))", "E:999999999999999999"),
        ("fn-max/fn-maxpint2args-1", "fn:max((xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\")))", "E:1"),
        (
            "fn-max/fn-maxpint2args-2",
            "fn:max((xs:positiveInteger(\"52704602390610033\"),xs:positiveInteger(\"1\")))",
            "E:52704602390610033"
        ),
        (
            "fn-max/fn-maxpint2args-3",
            "fn:max((xs:positiveInteger(\"999999999999999999\"),xs:positiveInteger(\"1\")))",
            "E:999999999999999999"
        ),
        (
            "fn-max/fn-maxpint2args-4",
            "fn:max((xs:positiveInteger(\"1\"),xs:positiveInteger(\"52704602390610033\")))",
            "E:52704602390610033"
        ),
        (
            "fn-max/fn-maxpint2args-5",
            "fn:max((xs:positiveInteger(\"1\"),xs:positiveInteger(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        ("fn-max/fn-maxsht1args-1", "fn:max((xs:short(\"-32768\")))", "E:-32768"),
        ("fn-max/fn-maxsht1args-2", "fn:max((xs:short(\"-5324\")))", "E:-5324"),
        ("fn-max/fn-maxsht1args-3", "fn:max((xs:short(\"32767\")))", "E:32767"),
        ("fn-max/fn-maxsht2args-1", "fn:max((xs:short(\"-32768\"),xs:short(\"-32768\")))", "E:-32768"),
        ("fn-max/fn-maxsht2args-2", "fn:max((xs:short(\"-5324\"),xs:short(\"-32768\")))", "E:-5324"),
        ("fn-max/fn-maxsht2args-3", "fn:max((xs:short(\"32767\"),xs:short(\"-32768\")))", "E:32767"),
        ("fn-max/fn-maxsht2args-4", "fn:max((xs:short(\"-32768\"),xs:short(\"-5324\")))", "E:-5324"),
        ("fn-max/fn-maxsht2args-5", "fn:max((xs:short(\"-32768\"),xs:short(\"32767\")))", "E:32767"),
        ("fn-max/fn-maxulng1args-1", "fn:max((xs:unsignedLong(\"0\")))", "E:0"),
        ("fn-max/fn-maxulng1args-2", "fn:max((xs:unsignedLong(\"130747108607674654\")))", "E:130747108607674654"),
        ("fn-max/fn-maxulng1args-3", "fn:max((xs:unsignedLong(\"184467440737095516\")))", "E:184467440737095516"),
        ("fn-max/fn-maxulng2args-1", "fn:max((xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\")))", "E:0"),
        (
            "fn-max/fn-maxulng2args-2",
            "fn:max((xs:unsignedLong(\"130747108607674654\"),xs:unsignedLong(\"0\")))",
            "E:130747108607674654"
        ),
        (
            "fn-max/fn-maxulng2args-3",
            "fn:max((xs:unsignedLong(\"184467440737095516\"),xs:unsignedLong(\"0\")))",
            "E:184467440737095516"
        ),
        (
            "fn-max/fn-maxulng2args-4",
            "fn:max((xs:unsignedLong(\"0\"),xs:unsignedLong(\"130747108607674654\")))",
            "E:130747108607674654"
        ),
        (
            "fn-max/fn-maxulng2args-5",
            "fn:max((xs:unsignedLong(\"0\"),xs:unsignedLong(\"184467440737095516\")))",
            "E:184467440737095516"
        ),
        ("fn-max/fn-maxusht1args-1", "fn:max((xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-max/fn-maxusht1args-2", "fn:max((xs:unsignedShort(\"44633\")))", "E:44633"),
        ("fn-max/fn-maxusht1args-3", "fn:max((xs:unsignedShort(\"65535\")))", "E:65535"),
        ("fn-max/fn-maxusht2args-1", "fn:max((xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\")))", "E:0"),
        ("fn-max/fn-maxusht2args-2", "fn:max((xs:unsignedShort(\"44633\"),xs:unsignedShort(\"0\")))", "E:44633"),
        ("fn-max/fn-maxusht2args-3", "fn:max((xs:unsignedShort(\"65535\"),xs:unsignedShort(\"0\")))", "E:65535"),
        ("fn-max/fn-maxusht2args-4", "fn:max((xs:unsignedShort(\"0\"),xs:unsignedShort(\"44633\")))", "E:44633"),
        ("fn-max/fn-maxusht2args-5", "fn:max((xs:unsignedShort(\"0\"),xs:unsignedShort(\"65535\")))", "E:65535"),
        (
            "fn-distinct-values/fn-distinct-valuesintg1args-1",
            "fn:distinct-values((xs:integer(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesintg1args-2",
            "fn:distinct-values((xs:integer(\"830993497117024304\")))",
            "E:830993497117024304"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesintg1args-3",
            "fn:distinct-values((xs:integer(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesdec1args-1",
            "fn:distinct-values((xs:decimal(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesdec1args-2",
            "fn:distinct-values((xs:decimal(\"617375191608514839\")))",
            "E:617375191608514839"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesdec1args-3",
            "fn:distinct-values((xs:decimal(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesdbl1args-1",
            "fn:distinct-values((xs:double(\"-1.7976931348623157E308\")))",
            "E:-1.7976931348623157E308"
        ),
        ("fn-distinct-values/fn-distinct-valuesdbl1args-2", "fn:distinct-values((xs:double(\"0\")))", "E:0"),
        (
            "fn-distinct-values/fn-distinct-valuesdbl1args-3",
            "fn:distinct-values((xs:double(\"1.7976931348623157E308\")))",
            "E:1.7976931348623157E308"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesflt1args-1",
            "fn:distinct-values((xs:float(\"-3.4028235E38\")))",
            "E:xs:float(\"-3.4028235E38\")"
        ),
        ("fn-distinct-values/fn-distinct-valuesflt1args-2", "fn:distinct-values((xs:float(\"0\")))", "E:0"),
        (
            "fn-distinct-values/fn-distinct-valuesflt1args-3",
            "fn:distinct-values((xs:float(\"3.4028235E38\")))",
            "E:xs:float(\"3.4028235E38\")"
        ),
        ("fn-distinct-values/fn-distinct-values-mixed-args-001", "fn:distinct-values((1, 2))", "S:2 1\tS:1 2"),
        ("fn-distinct-values/fn-distinct-values-mixed-args-002", "fn:distinct-values(( 1, (1), ((1)) ))", "E:1"),
        ("fn-distinct-values/fn-distinct-values-mixed-args-004", "fn:distinct-values(( xs:integer(1), 1))", "E:1"),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-007",
            "fn:distinct-values(( xs:string(\"hello\"), \"hello\"))",
            "S:hello"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-008",
            "fn:distinct-values(( xs:string(\"\"), \"\", ''))",
            "S:"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-012",
            "fn:distinct-values((xs:decimal('1.2'), xs:float('1.2')))",
            "S:1.2"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-013",
            "fn:distinct-values((xs:decimal('1.2'), xs:double('1.2')))",
            "S:1.2"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-014",
            "fn:distinct-values((xs:float('NaN'), 'NaN'))",
            "S:NaN NaN"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-015",
            "fn:distinct-values((xs:float('INF'), 'INF'))",
            "S:INF INF"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-016",
            "fn:distinct-values((xs:float('-INF'), '-INF'))",
            "S:-INF -INF"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-017",
            "fn:distinct-values((xs:float('INF'), xs:float('INF')))",
            "S:INF"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-019",
            "fn:distinct-values((xs:float('NaN'), xs:float('NaN')))",
            "S:NaN"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-020",
            "fn:distinct-values((xs:float('NaN'), xs:float('NaN')))",
            "S:NaN"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-021",
            "fn:distinct-values((xs:float('NaN'), xs:double('NaN')))",
            "S:NaN"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-022",
            "fn:distinct-values((xs:float('INF'), xs:double('INF')))",
            "S:INF"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-023",
            "fn:distinct-values((xs:float('-INF'), xs:double('-INF')))",
            "S:-INF"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-024",
            "fn:distinct-values((xs:double('-INF'), xs:double('INF')))",
            "S:INF -INF\tS:-INF INF"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-025",
            "fn:distinct-values((xs:double('NaN'), xs:double('NaN')))",
            "S:NaN"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-026",
            "fn:distinct-values((xs:double('NaN'), xs:double('NaN')))",
            "S:NaN"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-027",
            "fn:distinct-values((\"NaN\", \"-NaN\"))",
            "S:NaN -NaN\tS:-NaN NaN"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-028",
            "fn:distinct-values((\"-INF\", \"INF\"))",
            "S:INF -INF\tS:-INF INF"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-030",
            "fn:distinct-values((xs:boolean('true'), xs:boolean('1')))",
            "T"
        ),
        (
            "fn-distinct-values/fn-distinct-values-mixed-args-031",
            "fn:distinct-values((xs:boolean('false'), xs:boolean('0')))",
            "F"
        ),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-1", "distinct-values()", "X:XPST0017"),
        (
            "fn-distinct-values/K-SeqDistinctValuesFunc-2",
            "deep-equal(distinct-values((1, 2, 3), \"http://www.example.com/COLLATION/NOT/SUPPORTED\"), (1, 2, 3))",
            "T\tX:FOCH0002"
        ),
        (
            "fn-distinct-values/K-SeqDistinctValuesFunc-3",
            "distinct-values(\"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-distinct-values/K-SeqDistinctValuesFunc-4",
            "distinct-values(\"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\") eq \"a string\"",
            "T"
        ),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-5", "empty(distinct-values(()))", "T\tX:XPST0005"),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-7", "distinct-values((1, 1))", "E:1\tX:XPTY0004"),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-8", "distinct-values((-3, -3))", "E:-3\tX:XPTY0004"),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-9", "count(distinct-values((1, 2.0, 3, 2))) eq 3", "T"),
        (
            "fn-distinct-values/K-SeqDistinctValuesFunc-10",
            "deep-equal(distinct-values((1, 2.0, 3, 2)), (1, 2.0, 3))",
            "T"
        ),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-11", "exists(distinct-values((1, 2, 3, 1)))", "T"),
        (
            "fn-distinct-values/K-SeqDistinctValuesFunc-13",
            "count(distinct-values((1, 2, 2, current-time()))) eq 3",
            "T"
        ),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-14", "count(distinct-values(())) eq 0", "T\tX:XPST0005"),
        ("fn-distinct-values/K-SeqDistinctValuesFunc-15", "count(distinct-values(current-time())) eq 1", "T"),
        (
            "fn-distinct-values/K2-SeqDistinctValuesFunc-1",
            "fn:distinct-values((\"1\", \"2\", \"3\"), \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "X:FOCH0002"
        ),
        (
            "fn-distinct-values/cbcl-distinct-values-001",
            "count(distinct-values((xs:integer(\"3\"), xs:float(\"3\"), xs:float(\"NaN\"), xs:double(\"3\"), xs:double(\"NaN\"), xs:decimal(\"3\"), xs:float(\"3\"))))",
            "S:2"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesint1args-1",
            "fn:distinct-values((xs:int(\"-2147483648\")))",
            "E:-2147483648"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesint1args-2",
            "fn:distinct-values((xs:int(\"-1873914410\")))",
            "E:-1873914410"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesint1args-3",
            "fn:distinct-values((xs:int(\"2147483647\")))",
            "E:2147483647"
        ),
        (
            "fn-distinct-values/fn-distinct-valueslng1args-1",
            "fn:distinct-values((xs:long(\"-92233720368547758\")))",
            "E:-92233720368547758"
        ),
        (
            "fn-distinct-values/fn-distinct-valueslng1args-2",
            "fn:distinct-values((xs:long(\"-47175562203048468\")))",
            "E:-47175562203048468"
        ),
        (
            "fn-distinct-values/fn-distinct-valueslng1args-3",
            "fn:distinct-values((xs:long(\"92233720368547758\")))",
            "E:92233720368547758"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnint1args-1",
            "fn:distinct-values((xs:negativeInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnint1args-2",
            "fn:distinct-values((xs:negativeInteger(\"-297014075999096793\")))",
            "E:-297014075999096793"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnint1args-3",
            "fn:distinct-values((xs:negativeInteger(\"-1\")))",
            "E:-1"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnni1args-1",
            "fn:distinct-values((xs:nonNegativeInteger(\"0\")))",
            "E:0"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnni1args-2",
            "fn:distinct-values((xs:nonNegativeInteger(\"303884545991464527\")))",
            "E:303884545991464527"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnni1args-3",
            "fn:distinct-values((xs:nonNegativeInteger(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnpi1args-1",
            "fn:distinct-values((xs:nonPositiveInteger(\"-999999999999999999\")))",
            "E:-999999999999999999"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnpi1args-2",
            "fn:distinct-values((xs:nonPositiveInteger(\"-475688437271870490\")))",
            "E:-475688437271870490"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesnpi1args-3",
            "fn:distinct-values((xs:nonPositiveInteger(\"0\")))",
            "E:0"
        ),
        ("fn-distinct-values/fn-distinct-valuespint1args-1", "fn:distinct-values((xs:positiveInteger(\"1\")))", "E:1"),
        (
            "fn-distinct-values/fn-distinct-valuespint1args-2",
            "fn:distinct-values((xs:positiveInteger(\"52704602390610033\")))",
            "E:52704602390610033"
        ),
        (
            "fn-distinct-values/fn-distinct-valuespint1args-3",
            "fn:distinct-values((xs:positiveInteger(\"999999999999999999\")))",
            "E:999999999999999999"
        ),
        ("fn-distinct-values/fn-distinct-valuessht1args-1", "fn:distinct-values((xs:short(\"-32768\")))", "E:-32768"),
        ("fn-distinct-values/fn-distinct-valuessht1args-2", "fn:distinct-values((xs:short(\"-5324\")))", "E:-5324"),
        ("fn-distinct-values/fn-distinct-valuessht1args-3", "fn:distinct-values((xs:short(\"32767\")))", "E:32767"),
        ("fn-distinct-values/fn-distinct-valuesulng1args-1", "fn:distinct-values((xs:unsignedLong(\"0\")))", "E:0"),
        (
            "fn-distinct-values/fn-distinct-valuesulng1args-2",
            "fn:distinct-values((xs:unsignedLong(\"130747108607674654\")))",
            "E:130747108607674654"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesulng1args-3",
            "fn:distinct-values((xs:unsignedLong(\"184467440737095516\")))",
            "E:184467440737095516"
        ),
        ("fn-distinct-values/fn-distinct-valuesusht1args-1", "fn:distinct-values((xs:unsignedShort(\"0\")))", "E:0"),
        (
            "fn-distinct-values/fn-distinct-valuesusht1args-2",
            "fn:distinct-values((xs:unsignedShort(\"44633\")))",
            "E:44633"
        ),
        (
            "fn-distinct-values/fn-distinct-valuesusht1args-3",
            "fn:distinct-values((xs:unsignedShort(\"65535\")))",
            "E:65535"
        ),
        (
            "fn-deep-equal/fn-deep-equalintg2args-1",
            "fn:deep-equal((xs:integer(\"-999999999999999999\")),(xs:integer(\"-999999999999999999\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalintg2args-2",
            "fn:deep-equal((xs:integer(\"830993497117024304\")),(xs:integer(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalintg2args-3",
            "fn:deep-equal((xs:integer(\"999999999999999999\")),(xs:integer(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalintg2args-4",
            "fn:deep-equal((xs:integer(\"-999999999999999999\")),(xs:integer(\"830993497117024304\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalintg2args-5",
            "fn:deep-equal((xs:integer(\"-999999999999999999\")),(xs:integer(\"999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldec2args-1",
            "fn:deep-equal((xs:decimal(\"-999999999999999999\")),(xs:decimal(\"-999999999999999999\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equaldec2args-2",
            "fn:deep-equal((xs:decimal(\"617375191608514839\")),(xs:decimal(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldec2args-3",
            "fn:deep-equal((xs:decimal(\"999999999999999999\")),(xs:decimal(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldec2args-4",
            "fn:deep-equal((xs:decimal(\"-999999999999999999\")),(xs:decimal(\"617375191608514839\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldec2args-5",
            "fn:deep-equal((xs:decimal(\"-999999999999999999\")),(xs:decimal(\"999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldbl2args-1",
            "fn:deep-equal((xs:double(\"-1.7976931348623157E308\")),(xs:double(\"-1.7976931348623157E308\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equaldbl2args-2",
            "fn:deep-equal((xs:double(\"0\")),(xs:double(\"-1.7976931348623157E308\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldbl2args-3",
            "fn:deep-equal((xs:double(\"1.7976931348623157E308\")),(xs:double(\"-1.7976931348623157E308\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldbl2args-4",
            "fn:deep-equal((xs:double(\"-1.7976931348623157E308\")),(xs:double(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equaldbl2args-5",
            "fn:deep-equal((xs:double(\"-1.7976931348623157E308\")),(xs:double(\"1.7976931348623157E308\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalflt2args-1",
            "fn:deep-equal((xs:float(\"-3.4028235E38\")),(xs:float(\"-3.4028235E38\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalflt2args-2",
            "fn:deep-equal((xs:float(\"0\")),(xs:float(\"-3.4028235E38\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalflt2args-3",
            "fn:deep-equal((xs:float(\"3.4028235E38\")),(xs:float(\"-3.4028235E38\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalflt2args-4",
            "fn:deep-equal((xs:float(\"-3.4028235E38\")),(xs:float(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalflt2args-5",
            "fn:deep-equal((xs:float(\"-3.4028235E38\")),(xs:float(\"3.4028235E38\")))",
            "F"
        ),
        ("fn-deep-equal/fn-deep-equal-mix-args-001", "fn:deep-equal( (1,2) , (2,1))", "F"),
        ("fn-deep-equal/fn-deep-equal-mix-args-002", "fn:deep-equal( (1,2) , (1,2))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-003", "fn:deep-equal( xs:string(\"A\") , \"A\")", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-004", "fn:deep-equal( xs:string(\"A\") , \"a\")", "F"),
        ("fn-deep-equal/fn-deep-equal-mix-args-005", "fn:deep-equal( xs:string(\"A\") , (\"A\"))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-006", "fn:deep-equal( xs:string(\"\") , (\"\"))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-007", "fn:deep-equal( () , ())", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-008", "fn:deep-equal( (()) , ())", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-009", "fn:deep-equal( ( ) , ())", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-010", "fn:deep-equal(xs:string(\"abc\"), xs:string(\"cba\"))", "F"),
        ("fn-deep-equal/fn-deep-equal-mix-args-013", "fn:deep-equal( xs:integer(1) , xs:decimal(1.0))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-014", "fn:deep-equal( xs:integer(1) , xs:integer(-1))", "F"),
        ("fn-deep-equal/fn-deep-equal-mix-args-015", "fn:deep-equal( xs:integer(1) , xs:float(1.0))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-016", "fn:deep-equal( xs:integer(1) , xs:double(1.0))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-017", "fn:deep-equal( xs:decimal(1.01) , xs:float(1.01))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-018", "fn:deep-equal( xs:decimal(1.01) , xs:double(1.01))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-019", "fn:deep-equal( xs:float(1.01) , xs:double(1.01))", "F"),
        ("fn-deep-equal/fn-deep-equal-mix-args-020", "fn:deep-equal( xs:float(\"INF\") , xs:double(\"INF\"))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-021", "fn:deep-equal( xs:float(\"-INF\") , xs:double(\"-INF\"))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-022", "fn:deep-equal( xs:float(\"NaN\") , xs:double(\"NaN\"))", "T"),
        ("fn-deep-equal/fn-deep-equal-mix-args-023", "fn:deep-equal( xs:boolean(\"1\") , xs:boolean(\"true\"))", "T"),
        (
            "fn-deep-equal/fn-deep-equal-mix-args-024",
            "fn:deep-equal( xs:boolean(\"true\") , xs:boolean(\"true\"))",
            "T"
        ),
        ("fn-deep-equal/fn-deep-equal-mix-args-026", "fn:deep-equal( xs:boolean(\"0\") , xs:boolean(\"false\"))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-1", "deep-equal()", "X:XPST0017"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-2", "deep-equal(\"a string\")", "X:XPST0017"),
        (
            "fn-deep-equal/K-SeqDeepEqualFunc-3",
            "deep-equal(\"a string\", \"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-deep-equal/K-SeqDeepEqualFunc-4",
            "deep-equal(\"a string\", \"a string\", \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "S:true\tX:FOCH0002"
        ),
        ("fn-deep-equal/K-SeqDeepEqualFunc-5", "deep-equal(\"a string\", \"a string\", ())", "S:true\tX:XPTY0004"),
        (
            "fn-deep-equal/K-SeqDeepEqualFunc-6",
            "deep-equal(\"a string\", \"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "T"
        ),
        ("fn-deep-equal/K-SeqDeepEqualFunc-8", "deep-equal(xs:float(\"NaN\"), xs:float(\"NaN\"))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-9", "deep-equal(xs:double(\"NaN\"), xs:double(\"NaN\"))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-10", "deep-equal(xs:float(\"NaN\"), xs:double(\"NaN\"))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-11", "deep-equal(xs:double(\"NaN\"), xs:float(\"NaN\"))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-25", "deep-equal((1, 1, 1), (1, 1, 1))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-32", "deep-equal(index-of(20, 20), (1))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-33", "deep-equal(index-of((20, 40), 20), (1))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-34", "deep-equal(index-of((20, 20), 20), (1, 2))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-35", "deep-equal(index-of((20, 40, 20), 20), (1, 3))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-36", "deep-equal((1, 1, \"str\"), (1, 1, \"str\"))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-37", "deep-equal((1, \"str\", 1), (1, \"str\", 1))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-38", "deep-equal((\"str\", 1, 1), (\"str\", 1, 1))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-39", "deep-equal((1, 1, ()), (1, 1, ()))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-40", "deep-equal((1, (), 1), (1, (), 1))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-41", "deep-equal(((), 1, 1), ((), 1, 1))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-52", "deep-equal((), reverse(0 to -5))", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-53", "deep-equal((), 0 to -5)", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-54", "deep-equal(reverse(0 to -5), ())", "T"),
        ("fn-deep-equal/K-SeqDeepEqualFunc-55", "deep-equal(0 to -5, ())", "T"),
        ("fn-deep-equal/K2-SeqDeepEqualFunc-35", "deep-equal(\"a\", xs:NCName(\"a\"))", "T"),
        (
            "fn-deep-equal/K2-SeqDeepEqualFunc-4",
            "deep-equal((current-time(), current-time(), current-time()), (current-time(), current-time()))",
            "F"
        ),
        ("fn-deep-equal/K2-SeqDeepEqualFunc-8", "deep-equal()", "X:XPST0017"),
        ("fn-deep-equal/K2-SeqDeepEqualFunc-9", "deep-equal((1, 2, 3))", "X:XPST0017"),
        (
            "fn-deep-equal/fn-deep-equalint2args-1",
            "fn:deep-equal((xs:int(\"-2147483648\")),(xs:int(\"-2147483648\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalint2args-2",
            "fn:deep-equal((xs:int(\"-1873914410\")),(xs:int(\"-2147483648\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalint2args-3",
            "fn:deep-equal((xs:int(\"2147483647\")),(xs:int(\"-2147483648\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalint2args-4",
            "fn:deep-equal((xs:int(\"-2147483648\")),(xs:int(\"-1873914410\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalint2args-5",
            "fn:deep-equal((xs:int(\"-2147483648\")),(xs:int(\"2147483647\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equallng2args-1",
            "fn:deep-equal((xs:long(\"-92233720368547758\")),(xs:long(\"-92233720368547758\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equallng2args-2",
            "fn:deep-equal((xs:long(\"-47175562203048468\")),(xs:long(\"-92233720368547758\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equallng2args-3",
            "fn:deep-equal((xs:long(\"92233720368547758\")),(xs:long(\"-92233720368547758\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equallng2args-4",
            "fn:deep-equal((xs:long(\"-92233720368547758\")),(xs:long(\"-47175562203048468\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equallng2args-5",
            "fn:deep-equal((xs:long(\"-92233720368547758\")),(xs:long(\"92233720368547758\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnint2args-1",
            "fn:deep-equal((xs:negativeInteger(\"-999999999999999999\")),(xs:negativeInteger(\"-999999999999999999\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalnint2args-2",
            "fn:deep-equal((xs:negativeInteger(\"-297014075999096793\")),(xs:negativeInteger(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnint2args-3",
            "fn:deep-equal((xs:negativeInteger(\"-1\")),(xs:negativeInteger(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnint2args-4",
            "fn:deep-equal((xs:negativeInteger(\"-999999999999999999\")),(xs:negativeInteger(\"-297014075999096793\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnint2args-5",
            "fn:deep-equal((xs:negativeInteger(\"-999999999999999999\")),(xs:negativeInteger(\"-1\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnni2args-1",
            "fn:deep-equal((xs:nonNegativeInteger(\"0\")),(xs:nonNegativeInteger(\"0\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalnni2args-2",
            "fn:deep-equal((xs:nonNegativeInteger(\"303884545991464527\")),(xs:nonNegativeInteger(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnni2args-3",
            "fn:deep-equal((xs:nonNegativeInteger(\"999999999999999999\")),(xs:nonNegativeInteger(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnni2args-4",
            "fn:deep-equal((xs:nonNegativeInteger(\"0\")),(xs:nonNegativeInteger(\"303884545991464527\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnni2args-5",
            "fn:deep-equal((xs:nonNegativeInteger(\"0\")),(xs:nonNegativeInteger(\"999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnpi2args-1",
            "fn:deep-equal((xs:nonPositiveInteger(\"-999999999999999999\")),(xs:nonPositiveInteger(\"-999999999999999999\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalnpi2args-2",
            "fn:deep-equal((xs:nonPositiveInteger(\"-475688437271870490\")),(xs:nonPositiveInteger(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnpi2args-3",
            "fn:deep-equal((xs:nonPositiveInteger(\"0\")),(xs:nonPositiveInteger(\"-999999999999999999\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnpi2args-4",
            "fn:deep-equal((xs:nonPositiveInteger(\"-999999999999999999\")),(xs:nonPositiveInteger(\"-475688437271870490\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalnpi2args-5",
            "fn:deep-equal((xs:nonPositiveInteger(\"-999999999999999999\")),(xs:nonPositiveInteger(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalpint2args-1",
            "fn:deep-equal((xs:positiveInteger(\"1\")),(xs:positiveInteger(\"1\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalpint2args-2",
            "fn:deep-equal((xs:positiveInteger(\"52704602390610033\")),(xs:positiveInteger(\"1\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalpint2args-3",
            "fn:deep-equal((xs:positiveInteger(\"999999999999999999\")),(xs:positiveInteger(\"1\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalpint2args-4",
            "fn:deep-equal((xs:positiveInteger(\"1\")),(xs:positiveInteger(\"52704602390610033\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalpint2args-5",
            "fn:deep-equal((xs:positiveInteger(\"1\")),(xs:positiveInteger(\"999999999999999999\")))",
            "F"
        ),
        ("fn-deep-equal/fn-deep-equalsht2args-1", "fn:deep-equal((xs:short(\"-32768\")),(xs:short(\"-32768\")))", "T"),
        ("fn-deep-equal/fn-deep-equalsht2args-2", "fn:deep-equal((xs:short(\"-5324\")),(xs:short(\"-32768\")))", "F"),
        ("fn-deep-equal/fn-deep-equalsht2args-3", "fn:deep-equal((xs:short(\"32767\")),(xs:short(\"-32768\")))", "F"),
        ("fn-deep-equal/fn-deep-equalsht2args-4", "fn:deep-equal((xs:short(\"-32768\")),(xs:short(\"-5324\")))", "F"),
        ("fn-deep-equal/fn-deep-equalsht2args-5", "fn:deep-equal((xs:short(\"-32768\")),(xs:short(\"32767\")))", "F"),
        (
            "fn-deep-equal/fn-deep-equalulng2args-1",
            "fn:deep-equal((xs:unsignedLong(\"0\")),(xs:unsignedLong(\"0\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalulng2args-2",
            "fn:deep-equal((xs:unsignedLong(\"130747108607674654\")),(xs:unsignedLong(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalulng2args-3",
            "fn:deep-equal((xs:unsignedLong(\"184467440737095516\")),(xs:unsignedLong(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalulng2args-4",
            "fn:deep-equal((xs:unsignedLong(\"0\")),(xs:unsignedLong(\"130747108607674654\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalulng2args-5",
            "fn:deep-equal((xs:unsignedLong(\"0\")),(xs:unsignedLong(\"184467440737095516\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalusht2args-1",
            "fn:deep-equal((xs:unsignedShort(\"0\")),(xs:unsignedShort(\"0\")))",
            "T"
        ),
        (
            "fn-deep-equal/fn-deep-equalusht2args-2",
            "fn:deep-equal((xs:unsignedShort(\"44633\")),(xs:unsignedShort(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalusht2args-3",
            "fn:deep-equal((xs:unsignedShort(\"65535\")),(xs:unsignedShort(\"0\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalusht2args-4",
            "fn:deep-equal((xs:unsignedShort(\"0\")),(xs:unsignedShort(\"44633\")))",
            "F"
        ),
        (
            "fn-deep-equal/fn-deep-equalusht2args-5",
            "fn:deep-equal((xs:unsignedShort(\"0\")),(xs:unsignedShort(\"65535\")))",
            "F"
        ),
        ("fn-index-of/fn-indexof-mix-args-001", "fn:index-of ((10, 20, 30, 30, 20, 10), 20)", "S:2 5"),
        ("fn-index-of/fn-indexof-mix-args-002", "fn:index-of ((10, 20, 30, 40), 35)", "S:"),
        (
            "fn-index-of/fn-indexof-mix-args-003",
            "fn:index-of ((\"a\", \"sport\", \"and\", \"a\", \"pastime\"), \"a\")",
            "S:1 4"
        ),
        (
            "fn-index-of/fn-indexof-mix-args-004",
            "fn:index-of((\"sport\", \"\", \"and\", \"\", \"\", \"pastime\"), \"\")",
            "S:2 4 5"
        ),
        ("fn-index-of/fn-indexof-mix-args-005", "fn:index-of((),\"\")", "S:"),
        ("fn-index-of/fn-indexof-mix-args-006", "fn:index-of((\"sport\"), \"\")", "S:"),
        ("fn-index-of/fn-indexof-mix-args-007", "fn:index-of((),fn:exactly-one(xs:untypedAtomic('')))", "S:"),
        (
            "fn-index-of/fn-indexof-mix-args-008",
            "fn:index-of((xs:float('NaN')), fn:exactly-one(xs:float('NaN')))",
            "S:"
        ),
        (
            "fn-index-of/fn-indexof-mix-args-009",
            "fn:index-of((xs:double('NaN')), fn:exactly-one(xs:double('NaN')))",
            "S:"
        ),
        (
            "fn-index-of/fn-indexof-mix-args-010",
            "fn:index-of((xs:double('INF')), fn:exactly-one(xs:double('INF')))",
            "E:1"
        ),
        (
            "fn-index-of/fn-indexof-mix-args-011",
            "fn:index-of((xs:double('-INF')), fn:exactly-one(xs:double('-INF')))",
            "E:1"
        ),
        (
            "fn-index-of/fn-indexof-mix-args-012",
            "fn:index-of((xs:decimal('9.99999999999999999999999999')), fn:exactly-one(xs:decimal('9.99999999999999999999999999')))",
            "E:1"
        ),
        (
            "fn-index-of/fn-indexof-mix-args-013",
            "fn:index-of((xs:decimal('9.99999999999999999999999999')), fn:exactly-one(xs:decimal('9.9999999999999999999999999')))",
            "S:"
        ),
        ("fn-index-of/fn-indexof-mix-args-016", "fn:index-of((1, (1,2,3)),1)", "S:1 2"),
        ("fn-index-of/fn-indexof-mix-args-017", "fn:index-of(((1),(1), (2,1), (0,1)),1)", "S:1 2 4 6"),
        (
            "fn-index-of/fn-indexof-mix-args-020",
            "fn:index-of((\"sport\", \"\", \"and\", \"\", \"\", \"pastime\"), \"\")",
            "S:2 4 5"
        ),
        ("fn-index-of/fn-indexof-mix-args-021", "fn:index-of( (\"\", 1, \"\"), \"\")", "S:1 3"),
        ("fn-index-of/fn-indexof-mix-args-022", "fn:index-of((1,2,3,4))", "X:XPST0017"),
        ("fn-index-of/K-SeqIndexOfFunc-1", "index-of()", "X:XPST0017"),
        ("fn-index-of/K-SeqIndexOfFunc-2", "index-of(1)", "X:XPST0017"),
        ("fn-index-of/K-SeqIndexOfFunc-3", "index-of((1, 2, 3), 1, ())", "X:XPTY0004"),
        (
            "fn-index-of/K-SeqIndexOfFunc-4",
            "index-of(\"a string\", \"a string\", \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "X:FOCH0002"
        ),
        (
            "fn-index-of/K-SeqIndexOfFunc-5",
            "index-of(\"a string\", \"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-index-of/K-SeqIndexOfFunc-6",
            "index-of(\"a string\", \"a string\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "E:1"
        ),
        ("fn-index-of/K-SeqIndexOfFunc-7", "empty(index-of(xs:double(\"NaN\"), xs:float(\"NaN\")))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-8", "empty(index-of(xs:float(\"NaN\"), xs:double(\"NaN\")))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-9", "empty(index-of(xs:float(\"NaN\"), xs:float(\"NaN\")))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-10", "empty(index-of(xs:double(\"NaN\"), xs:double(\"NaN\")))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-11", "empty(index-of(4, \"4\"))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-12", "index-of(4, 4)", "E:1"),
        ("fn-index-of/K-SeqIndexOfFunc-13", "empty(index-of((), 4))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-14", "index-of(4, 4)", "E:1"),
        ("fn-index-of/K-SeqIndexOfFunc-15", "deep-equal((1, 2, 3, 4, 5, 6), index-of((4, 4, 4, 4, 4, 4), 4))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-18", "deep-equal(index-of((1, 2, \"three\", 5, 5, 6), 5), (4, 5))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-19", "empty(index-of((10, 20, 30, 40), 35))", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-20", "deep-equal(index-of((10, 20, 30, 30, 20, 10), 20), (2, 5))", "T"),
        (
            "fn-index-of/K-SeqIndexOfFunc-21",
            "deep-equal(index-of((\"a\", \"sport\", \"and\", \"a\", \"pastime\"), \"a\"), (1, 4))",
            "T"
        ),
        ("fn-index-of/K-SeqIndexOfFunc-22", "count(index-of((1, 2, 3, 2, 1), 2)) eq 2", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-23", "count(index-of((1, 2, 3, 2, 1), 1)) eq 2", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-24", "count(index-of((1, 2, 3, 2, 1), 3)) eq 1", "T"),
        ("fn-index-of/K-SeqIndexOfFunc-25", "count(index-of((1, 2, 3, 2, 1), 4)) eq 0", "T"),
        ("fn-index-of/cbcl-fn-indexof-1", "index-of(1 to 5,4)", "S:4"),
        (
            "fn-index-of/fn-indexof-mix-args-014",
            "fn:index-of((xs:positiveInteger('1'),xs:positiveInteger('2')), fn:exactly-one(xs:positiveInteger('2')))",
            "E:2"
        ),
        (
            "fn-index-of/fn-indexof-mix-args-015",
            "fn:index-of((xs:negativeInteger('-2'), xs:negativeInteger('-1')), fn:exactly-one(xs:negativeInteger('-1')))",
            "E:2"
        ),
        ("fn-subsequence/fn-subsequence-mix-args-001", "fn:subsequence( (\"a\", \"b\", \"c\"), 1, 1)", "D:\"a\""),
        ("fn-subsequence/fn-subsequence-mix-args-002", "fn:subsequence( (\"a\", \"b\", \"c\"), 3, 12)", "D:\"c\""),
        (
            "fn-subsequence/fn-subsequence-mix-args-003",
            "fn:subsequence( (\"a\", xs:string(\"\"),\"b\", \"c\"), 1, 3)",
            "D:\"a\", \"\", \"b\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-004",
            "fn:subsequence( (\"a\", xs:string(\"hello\"),\"b\", \"c\"), 1, 3)",
            "D:\"a\", \"hello\", \"b\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-006",
            "fn:subsequence( (\"a\", (), (), \"b\", \"c\"), 1, 3)",
            "D:\"a\", \"b\", \"c\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-007",
            "fn:subsequence( (\"a\", xs:integer(\"100\"), xs:integer(\"-100\"), \"b\", \"c\"),2,4)",
            "D:100, -100, \"b\", \"c\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-008",
            "fn:subsequence( (\"a\", xs:decimal(\"-1.000000000001\"), xs:integer(\"-100\"), \"b\", \"c\"), 2,3)",
            "D:-1.000000000001, -100, \"b\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-009",
            "fn:subsequence( (\"a\", xs:float(\"INF\"), \"b\", \"c\"),-2,3)",
            "C:0"
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-010",
            "fn:subsequence ( (\"a\", xs:float(\"-INF\"), \"b\", \"c\"), 1,2)",
            "D:\"a\", xs:float(\"-INF\")"
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-011",
            "fn:subsequence ( (\"a\", xs:float(\"NaN\"), \"b\", \"c\"), 0, 2)",
            "E:\"a\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-012",
            "fn:subsequence ( (\"a\", xs:float(\"1.01\"), \"b\", \"c\"), 2,4)",
            "D:xs:float(1.01), \"b\", \"c\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-013",
            "fn:subsequence ( (\"a\", xs:double(\"NaN\"), \"b\", \"c\"), 2, 20)",
            "D:xs:double('NaN'), \"b\", \"c\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-014",
            "fn:subsequence ( (\"a\", xs:double(\"1.01\"), \"b\", \"c\"), 2,3)",
            "D:xs:double(1.01), \"b\", \"c\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-015",
            "fn:subsequence ( (\"a\", xs:double(\"-INF\"), \"b\", \"c\"), 2,2)",
            "D:xs:double('-INF'), \"b\""
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-016",
            "fn:subsequence ( (\"a\", xs:double(\"INF\"), \"b\", \"c\"), 2, 1)",
            "E:xs:double(\"INF\")"
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-018",
            "fn:subsequence ( (\"a\", xs:boolean(\"0\"), \"b\", \"c\"), 2,1)",
            "F"
        ),
        (
            "fn-subsequence/fn-subsequence-mix-args-024",
            "fn:subsequence (1 to 10, xs:double('-INF'), xs:double('INF'))",
            "C:0"
        ),
        ("fn-subsequence/K-SeqSubsequenceFunc-1", "subsequence()", "X:XPST0017"),
        ("fn-subsequence/K-SeqSubsequenceFunc-2", "subsequence(1)", "X:XPST0017"),
        ("fn-subsequence/K-SeqSubsequenceFunc-3", "subsequence(1, 1, 1, 1)", "X:XPST0017"),
        ("fn-subsequence/K-SeqSubsequenceFunc-4", "empty(subsequence((), 2, 3))", "T\tX:XPST0005"),
        ("fn-subsequence/K-SeqSubsequenceFunc-5", "empty(subsequence((1, 2, 3), 2, -10))", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-6", "count(subsequence((1, 2, 3, \"four\"), 4)) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-7", "count(subsequence((1, 2, 3, \"four\"), 4, 1)) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-8", "subsequence((1, 2, 3.1, \"four\"), 4)", "E:\"four\""),
        ("fn-subsequence/K-SeqSubsequenceFunc-9", "subsequence((1, 2, 3.1, \"four\"), 4, 1)", "E:\"four\""),
        (
            "fn-subsequence/K-SeqSubsequenceFunc-10",
            "subsequence((1, 2, 3.1, \"four\"), 3, 1) instance of xs:decimal",
            "T"
        ),
        ("fn-subsequence/K-SeqSubsequenceFunc-11", "subsequence((1, 2, 3.1, \"four\"), 1, 1)", "E:1"),
        ("fn-subsequence/K-SeqSubsequenceFunc-12", "empty(subsequence((1, 2, 3, \"four\"), 4, -3))", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-13", "empty(subsequence((1, 2, 3, \"four\"), -4, -3))", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-14", "subsequence((1, 2, 3), 1, 1) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-15", "subsequence((1, 2, 3), 1, 1) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-16", "subsequence((1, 2, 3), 3) eq 3", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-17", "count(subsequence((1, 2, 3), 1, 1)) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-18", "count(subsequence((1, 2, 3), 1, 3)) eq 3", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-19", "count(subsequence((1, 2, 3, \"four\"), 4)) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-20", "count(subsequence(1 to 3, 1, 1)) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-21", "empty(subsequence((1, 2), 4))", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-22", "subsequence((5, 6, 7, 8), 2, 1) eq 6", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-23", "count(subsequence((1, 2), 2)) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-24", "count(subsequence((1, 2, 3, \"four\"), 2)) eq 3", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-25", "count(subsequence((1, 2, 3, \"four\"), 2, 2)) eq 2", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-26", "subsequence((1, 2, 3.1, \"four\"), 1, 1)", "E:1"),
        ("fn-subsequence/K-SeqSubsequenceFunc-27", "1 eq \"a string\"", "X:XPTY0004"),
        ("fn-subsequence/K-SeqSubsequenceFunc-28", "1 eq subsequence((\"1\", 2, 3.1, \"four\"), 1, 1)", "X:XPTY0004"),
        ("fn-subsequence/K-SeqSubsequenceFunc-29", "subsequence((\"1\", 2, 3.1, \"four\"), 1, 1) eq 1", "X:XPTY0004"),
        ("fn-subsequence/K-SeqSubsequenceFunc-31", "count(subsequence((1, 2, 2, current-time()), 2, 2)) eq 2", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-32", "count(subsequence(remove(current-time(), 1), 1, 1)) eq 0", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-33", "deep-equal(1, subsequence((1, 2, current-time()), 1, 1))", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-35", "empty(subsequence((current-time(), 1), 4))", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-36", "count(subsequence((current-time(), 1), 4)) eq 0", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-37", "count(subsequence((current-time(), 2 , 3), 1)) eq 3", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-38", "count(subsequence((current-time(), 2 , 3), 3)) eq 1", "T"),
        ("fn-subsequence/K-SeqSubsequenceFunc-39", "count(subsequence((current-time(), 2 , 3, 4), 2, 2)) eq 2", "T"),
        ("fn-subsequence/K2-SeqSubsequenceFunc-2", "subsequence((1, 2, 3), 1, 0)", "C:0"),
        ("fn-subsequence/K2-SeqSubsequenceFunc-3", "subsequence((1, 2, 3, 4, 5), 4, -1)", "C:0"),
        ("fn-subsequence/K2-SeqSubsequenceFunc-4", "fn:subsequence((1,2,3), 1.1, 1)", "E:1"),
        ("fn-subsequence/K2-SeqSubsequenceFunc-5", "fn:subsequence((1,2,3), 1.8, 1)", "E:2"),
        ("fn-subsequence/K2-SeqSubsequenceFunc-6", "fn:subsequence((1,2,3), 1.4, 1.4)", "E:1"),
        ("fn-subsequence/K2-SeqSubsequenceFunc-7", "fn:subsequence((1,2,3), 1.5, 1.5)", "D:2, 3"),
        ("fn-subsequence/K2-SeqSubsequenceFunc-10", "subsequence((1, 2, 3), 1, \"string\")", "X:XPTY0004"),
        (
            "fn-subsequence/cbcl-subsequence-001",
            "count(subsequence(1 to 10, xs:double(\"-INF\"), xs:double(\"INF\")))",
            "S:0"
        ),
        ("fn-subsequence/cbcl-subsequence-002", "count(subsequence(1 to 10, 2, xs:double(\"NaN\")))", "S:0"),
        ("fn-subsequence/cbcl-subsequence-003", "count(subsequence(1 to 10, xs:double(\"NaN\"), 4))", "S:0"),
        ("fn-subsequence/cbcl-subsequence-004", "count(subsequence(1 to 10, xs:double(\"-INF\")))", "S:10"),
        ("fn-subsequence/cbcl-subsequence-005", "count(subsequence(1 to 10, xs:double(\"NaN\")))", "S:0"),
        ("fn-subsequence/cbcl-subsequence-008", "subsequence(1 to 100, 99, 2147483648)", "S:99 100\tX:XPDY0130"),
        ("fn-subsequence/cbcl-subsequence-009", "count(subsequence(1 to 100, -2147483648, 20))", "S:0"),
        ("fn-exists/fn-existsintg1args-1", "fn:exists((xs:integer(\"-999999999999999999\")))", "T"),
        ("fn-exists/fn-existsintg1args-2", "fn:exists((xs:integer(\"830993497117024304\")))", "T"),
        ("fn-exists/fn-existsintg1args-3", "fn:exists((xs:integer(\"999999999999999999\")))", "T"),
        ("fn-exists/fn-existsdec1args-1", "fn:exists((xs:decimal(\"-999999999999999999\")))", "T"),
        ("fn-exists/fn-existsdec1args-2", "fn:exists((xs:decimal(\"617375191608514839\")))", "T"),
        ("fn-exists/fn-existsdec1args-3", "fn:exists((xs:decimal(\"999999999999999999\")))", "T"),
        ("fn-exists/fn-existsdbl1args-1", "fn:exists((xs:double(\"-1.7976931348623157E308\")))", "T"),
        ("fn-exists/fn-existsdbl1args-2", "fn:exists((xs:double(\"0\")))", "T"),
        ("fn-exists/fn-existsdbl1args-3", "fn:exists((xs:double(\"1.7976931348623157E308\")))", "T"),
        ("fn-exists/fn-existsflt1args-1", "fn:exists((xs:float(\"-3.4028235E38\")))", "T"),
        ("fn-exists/fn-existsflt1args-2", "fn:exists((xs:float(\"0\")))", "T"),
        ("fn-exists/fn-existsflt1args-3", "fn:exists((xs:float(\"3.4028235E38\")))", "T"),
        ("fn-exists/K-SeqExistsFunc-1", "exists(1, 2)", "X:XPST0017"),
        ("fn-exists/K-SeqExistsFunc-2", "exists()", "X:XPST0017"),
        ("fn-exists/K-SeqExistsFunc-4", "exists(1)", "T"),
        ("fn-exists/K-SeqExistsFunc-5", "exists((1))", "T"),
        ("fn-exists/K-SeqExistsFunc-6", "exists((1, 2, 3))", "T"),
        ("fn-exists/K-SeqExistsFunc-9", "exists(reverse((1, 2, 3)))", "T"),
        ("fn-exists/K-SeqExistsFunc-10", "exists(timezone-from-dateTime(current-dateTime()))", "T"),
        ("fn-exists/K-SeqExistsFunc-11", "exists(timezone-from-date(current-date()))", "T"),
        ("fn-exists/K-SeqExistsFunc-12", "exists(timezone-from-time(current-time()))", "T"),
        ("fn-exists/K-SeqExistsFunc-13", "empty(remove(current-time(), 1))", "T"),
        ("fn-exists/fn-existsint1args-1", "fn:exists((xs:int(\"-2147483648\")))", "T"),
        ("fn-exists/fn-existsint1args-2", "fn:exists((xs:int(\"-1873914410\")))", "T"),
        ("fn-exists/fn-existsint1args-3", "fn:exists((xs:int(\"2147483647\")))", "T"),
        ("fn-exists/fn-existslng1args-1", "fn:exists((xs:long(\"-92233720368547758\")))", "T"),
        ("fn-exists/fn-existslng1args-2", "fn:exists((xs:long(\"-47175562203048468\")))", "T"),
        ("fn-exists/fn-existslng1args-3", "fn:exists((xs:long(\"92233720368547758\")))", "T"),
        ("fn-exists/fn-existsnint1args-1", "fn:exists((xs:negativeInteger(\"-999999999999999999\")))", "T"),
        ("fn-exists/fn-existsnint1args-2", "fn:exists((xs:negativeInteger(\"-297014075999096793\")))", "T"),
        ("fn-exists/fn-existsnint1args-3", "fn:exists((xs:negativeInteger(\"-1\")))", "T"),
        ("fn-exists/fn-existsnni1args-1", "fn:exists((xs:nonNegativeInteger(\"0\")))", "T"),
        ("fn-exists/fn-existsnni1args-2", "fn:exists((xs:nonNegativeInteger(\"303884545991464527\")))", "T"),
        ("fn-exists/fn-existsnni1args-3", "fn:exists((xs:nonNegativeInteger(\"999999999999999999\")))", "T"),
        ("fn-exists/fn-existsnpi1args-1", "fn:exists((xs:nonPositiveInteger(\"-999999999999999999\")))", "T"),
        ("fn-exists/fn-existsnpi1args-2", "fn:exists((xs:nonPositiveInteger(\"-475688437271870490\")))", "T"),
        ("fn-exists/fn-existsnpi1args-3", "fn:exists((xs:nonPositiveInteger(\"0\")))", "T"),
        ("fn-exists/fn-existspint1args-1", "fn:exists((xs:positiveInteger(\"1\")))", "T"),
        ("fn-exists/fn-existspint1args-2", "fn:exists((xs:positiveInteger(\"52704602390610033\")))", "T"),
        ("fn-exists/fn-existspint1args-3", "fn:exists((xs:positiveInteger(\"999999999999999999\")))", "T"),
        ("fn-exists/fn-existssht1args-1", "fn:exists((xs:short(\"-32768\")))", "T"),
        ("fn-exists/fn-existssht1args-2", "fn:exists((xs:short(\"-5324\")))", "T"),
        ("fn-exists/fn-existssht1args-3", "fn:exists((xs:short(\"32767\")))", "T"),
        ("fn-exists/fn-existsulng1args-1", "fn:exists((xs:unsignedLong(\"0\")))", "T"),
        ("fn-exists/fn-existsulng1args-2", "fn:exists((xs:unsignedLong(\"130747108607674654\")))", "T"),
        ("fn-exists/fn-existsulng1args-3", "fn:exists((xs:unsignedLong(\"184467440737095516\")))", "T"),
        ("fn-exists/fn-existsusht1args-1", "fn:exists((xs:unsignedShort(\"0\")))", "T"),
        ("fn-exists/fn-existsusht1args-2", "fn:exists((xs:unsignedShort(\"44633\")))", "T"),
        ("fn-exists/fn-existsusht1args-3", "fn:exists((xs:unsignedShort(\"65535\")))", "T"),
        ("fn-empty/fn-emptyintg1args-1", "fn:empty((xs:integer(\"-999999999999999999\")))", "F"),
        ("fn-empty/fn-emptyintg1args-2", "fn:empty((xs:integer(\"830993497117024304\")))", "F"),
        ("fn-empty/fn-emptyintg1args-3", "fn:empty((xs:integer(\"999999999999999999\")))", "F"),
        ("fn-empty/fn-emptydec1args-1", "fn:empty((xs:decimal(\"-999999999999999999\")))", "F"),
        ("fn-empty/fn-emptydec1args-2", "fn:empty((xs:decimal(\"617375191608514839\")))", "F"),
        ("fn-empty/fn-emptydec1args-3", "fn:empty((xs:decimal(\"999999999999999999\")))", "F"),
        ("fn-empty/fn-emptydbl1args-1", "fn:empty((xs:double(\"-1.7976931348623157E308\")))", "F"),
        ("fn-empty/fn-emptydbl1args-2", "fn:empty((xs:double(\"0\")))", "F"),
        ("fn-empty/fn-emptydbl1args-3", "fn:empty((xs:double(\"1.7976931348623157E308\")))", "F"),
        ("fn-empty/fn-emptyflt1args-1", "fn:empty((xs:float(\"-3.4028235E38\")))", "F"),
        ("fn-empty/fn-emptyflt1args-2", "fn:empty((xs:float(\"0\")))", "F"),
        ("fn-empty/fn-emptyflt1args-3", "fn:empty((xs:float(\"3.4028235E38\")))", "F"),
        ("fn-empty/K-SeqEmptyFunc-1", "empty(1, 2)", "X:XPST0017"),
        ("fn-empty/K-SeqEmptyFunc-2", "empty()", "X:XPST0017"),
        ("fn-empty/K-SeqEmptyFunc-7", "empty(())", "T"),
        ("fn-empty/K-SeqEmptyFunc-8", "empty( ((), (), ()) )", "T"),
        ("fn-empty/fn-emptyint1args-1", "fn:empty((xs:int(\"-2147483648\")))", "F"),
        ("fn-empty/fn-emptyint1args-2", "fn:empty((xs:int(\"-1873914410\")))", "F"),
        ("fn-empty/fn-emptyint1args-3", "fn:empty((xs:int(\"2147483647\")))", "F"),
        ("fn-empty/fn-emptylng1args-1", "fn:empty((xs:long(\"-92233720368547758\")))", "F"),
        ("fn-empty/fn-emptylng1args-2", "fn:empty((xs:long(\"-47175562203048468\")))", "F"),
        ("fn-empty/fn-emptylng1args-3", "fn:empty((xs:long(\"92233720368547758\")))", "F"),
        ("fn-empty/fn-emptynint1args-1", "fn:empty((xs:negativeInteger(\"-999999999999999999\")))", "F"),
        ("fn-empty/fn-emptynint1args-2", "fn:empty((xs:negativeInteger(\"-297014075999096793\")))", "F"),
        ("fn-empty/fn-emptynint1args-3", "fn:empty((xs:negativeInteger(\"-1\")))", "F"),
        ("fn-empty/fn-emptynni1args-1", "fn:empty((xs:nonNegativeInteger(\"0\")))", "F"),
        ("fn-empty/fn-emptynni1args-2", "fn:empty((xs:nonNegativeInteger(\"303884545991464527\")))", "F"),
        ("fn-empty/fn-emptynni1args-3", "fn:empty((xs:nonNegativeInteger(\"999999999999999999\")))", "F"),
        ("fn-empty/fn-emptynpi1args-1", "fn:empty((xs:nonPositiveInteger(\"-999999999999999999\")))", "F"),
        ("fn-empty/fn-emptynpi1args-2", "fn:empty((xs:nonPositiveInteger(\"-475688437271870490\")))", "F"),
        ("fn-empty/fn-emptynpi1args-3", "fn:empty((xs:nonPositiveInteger(\"0\")))", "F"),
        ("fn-empty/fn-emptypint1args-1", "fn:empty((xs:positiveInteger(\"1\")))", "F"),
        ("fn-empty/fn-emptypint1args-2", "fn:empty((xs:positiveInteger(\"52704602390610033\")))", "F"),
        ("fn-empty/fn-emptypint1args-3", "fn:empty((xs:positiveInteger(\"999999999999999999\")))", "F"),
        ("fn-empty/fn-emptysht1args-1", "fn:empty((xs:short(\"-32768\")))", "F"),
        ("fn-empty/fn-emptysht1args-2", "fn:empty((xs:short(\"-5324\")))", "F"),
        ("fn-empty/fn-emptysht1args-3", "fn:empty((xs:short(\"32767\")))", "F"),
        ("fn-empty/fn-emptyulng1args-1", "fn:empty((xs:unsignedLong(\"0\")))", "F"),
        ("fn-empty/fn-emptyulng1args-2", "fn:empty((xs:unsignedLong(\"130747108607674654\")))", "F"),
        ("fn-empty/fn-emptyulng1args-3", "fn:empty((xs:unsignedLong(\"184467440737095516\")))", "F"),
        ("fn-empty/fn-emptyusht1args-1", "fn:empty((xs:unsignedShort(\"0\")))", "F"),
        ("fn-empty/fn-emptyusht1args-2", "fn:empty((xs:unsignedShort(\"44633\")))", "F"),
        ("fn-empty/fn-emptyusht1args-3", "fn:empty((xs:unsignedShort(\"65535\")))", "F"),
    ];
}
