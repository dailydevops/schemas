namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

/// <summary>
/// The selection of the QT3 test sets <c>fn-abs</c>, <c>fn-ceiling</c>, <c>fn-floor</c>, <c>fn-round</c>, <c>fn-round-half-to-even</c>,
/// <c>fn-number</c>, <c>fn-true</c>, and <c>fn-false</c>. The cases are the ones the XPath 2.0 rule of the QT3 runner selects (the baselines of
/// <c>tests/conformance/baselines/qt3</c>), taken from the pinned commit of the suite (tests/conformance/suites/qt3.json): every one of
/// them is either in <see cref="Cases"/>, where the compiled expression must satisfy its assertion, or in <see cref="Exclusions"/> with
/// the reason and the issue that closes it.
/// </summary>
/// <remarks>
/// <para>
/// Limitation: the QT3 runner of the integration tests still has no evaluator behind it (#295), so these cases run through the lowerings of
/// <see cref="Sequences.Qt3SequenceFixture"/> and not through the runner, and the baselines are unchanged. The end-to-end run stays open
/// until #295.
/// </para>
/// <para>
/// Cases per test set, as selected of applicable: abs 181 of 183; ceiling 86 of 87; floor 87 of 88; round 244 of 245; round-half-to-even 126 of 128; number 63 of 66; true 22 of 24; false 22 of 24.
/// </para>
/// <para>
/// The cases that build an argument of a type derived from <c>xs:integer</c> (<c>xs:int</c>, <c>xs:long</c>, <c>xs:unsignedByte</c>, and
/// so on) are selected since the casting to the derived types exists (XP-23, #299). Rewrite that keeps the value: in the cases
/// <c>K2-ABSFunc-35</c> to <c>-46</c>, <c>K2-CeilingFunc</c>, <c>K2-FloorFunc</c>, and <c>K2-RoundFunc</c> the argument is the context item,
/// the document <c>&lt;e&gt;N&lt;/e&gt;</c> of the environment <c>e0</c>, <c>e1</c>, or <c>e-1</c>, which atomizes to the untyped value
/// <c>N</c>; the case is written with <c>xs:untypedAtomic("N")</c> in place of the dot, because the lowerings of the fixture have no
/// context item and no document.
/// </para>
/// <para>
/// An assertion is written for <see cref="Qt3NumericFixture.Satisfies"/>: <c>T</c> or <c>F</c> for a boolean, <c>S:</c> and <c>N:</c> for
/// a string value (<c>N:</c> with normalized space), <c>E:</c> for one value equal to an expression, <c>D:</c> for a sequence deep-equal
/// to an expression, <c>C:</c> for an item count, <c>Y:</c> for an item of a type, and <c>X:</c> for an error code. Alternatives
/// (<c>any-of</c>) are separated by a tab, and assertions that must all hold (<c>all-of</c>) by the unit separator (U+001F).
/// </para>
/// </remarks>
internal static class Qt3NumericSelection
{
    public static readonly (string Id, string Expression, string Expectation)[] Cases =
    [
        ("fn-abs/K-ABSFunc-1", "abs()", "X:XPST0017"),
        ("fn-abs/K-ABSFunc-2", "abs(1, 2)", "X:XPST0017"),
        ("fn-abs/K-ABSFunc-3", "empty(abs(()))", "T"),
        ("fn-abs/K-ABSFunc-4", "abs(10.5) eq 10.5", "T"),
        ("fn-abs/K-ABSFunc-5", "abs(-10.5) eq 10.5", "T"),
        ("fn-abs/K-ABSFunc-6", "abs(\"a string\")", "X:XPTY0004"),
        ("fn-abs/K2-ABSFunc-1", "fn:abs(xs:integer(-4))", "S:4"),
        ("fn-abs/K2-ABSFunc-10", "fn:abs(xs:unsignedInt(4))", "S:4"),
        ("fn-abs/K2-ABSFunc-11", "fn:abs(xs:unsignedShort(4))", "S:4"),
        ("fn-abs/K2-ABSFunc-12", "fn:abs(xs:unsignedByte(4))", "S:4"),
        ("fn-abs/K2-ABSFunc-13", "fn:abs(xs:positiveInteger(4))", "S:4"),
        ("fn-abs/K2-ABSFunc-14", "fn:abs(xs:integer(-4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-15", "fn:abs(xs:nonPositiveInteger(-4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-16", "fn:abs(xs:negativeInteger(-4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-17", "fn:abs(xs:long(-4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-18", "fn:abs(xs:int(-4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-19", "fn:abs(xs:short(-4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-2", "fn:abs(xs:nonPositiveInteger(-4))", "S:4"),
        ("fn-abs/K2-ABSFunc-20", "fn:abs(xs:byte(-4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-21", "fn:abs(xs:nonNegativeInteger(4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-22", "fn:abs(xs:unsignedLong(4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-23", "fn:abs(xs:unsignedInt(4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-24", "fn:abs(xs:unsignedShort(4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-25", "fn:abs(xs:unsignedByte(4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-26", "fn:abs(xs:positiveInteger(4)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-27", "fn:abs(1.1) instance of xs:integer", "F"),
        ("fn-abs/K2-ABSFunc-28", "fn:abs(1e1) instance of xs:integer", "F"),
        ("fn-abs/K2-ABSFunc-29", "fn:abs(xs:float(1)) instance of xs:double", "F"),
        ("fn-abs/K2-ABSFunc-3", "fn:abs(xs:negativeInteger(-4))", "S:4"),
        ("fn-abs/K2-ABSFunc-30", "fn:abs(xs:float(1)) instance of xs:double", "F"),
        ("fn-abs/K2-ABSFunc-31", "fn:abs(1.1) instance of xs:decimal", "T"),
        ("fn-abs/K2-ABSFunc-32", "fn:abs(1e1) instance of xs:double", "T"),
        ("fn-abs/K2-ABSFunc-33", "fn:abs(xs:float(1)) instance of xs:float", "T"),
        ("fn-abs/K2-ABSFunc-34", "fn:abs(xs:integer(1)) instance of xs:integer", "T"),
        ("fn-abs/K2-ABSFunc-35", "abs(xs:unsignedShort(xs:untypedAtomic(\"0\"))) instance of xs:unsignedShort", "F\tT"),
        ("fn-abs/K2-ABSFunc-36", "abs(xs:unsignedLong(xs:untypedAtomic(\"0\"))) instance of xs:unsignedLong", "F\tT"),
        ("fn-abs/K2-ABSFunc-37", "abs(xs:unsignedInt(xs:untypedAtomic(\"0\"))) instance of xs:unsignedInt", "F\tT"),
        ("fn-abs/K2-ABSFunc-38", "abs(xs:unsignedByte(xs:untypedAtomic(\"0\"))) instance of xs:unsignedByte", "F\tT"),
        (
            "fn-abs/K2-ABSFunc-39",
            "abs(xs:positiveInteger(xs:untypedAtomic(\"1\"))) instance of xs:positiveInteger",
            "F\tT"
        ),
        ("fn-abs/K2-ABSFunc-4", "fn:abs(xs:long(-4))", "S:4"),
        (
            "fn-abs/K2-ABSFunc-40",
            "abs(xs:nonPositiveInteger(xs:untypedAtomic(\"0\"))) instance of xs:nonPositiveInteger",
            "F\tT"
        ),
        (
            "fn-abs/K2-ABSFunc-41",
            "abs(xs:nonNegativeInteger(xs:untypedAtomic(\"0\"))) instance of xs:nonNegativeInteger",
            "F\tT"
        ),
        (
            "fn-abs/K2-ABSFunc-42",
            "abs(xs:negativeInteger(xs:untypedAtomic(\"-1\"))) instance of xs:negativeInteger",
            "F\tT"
        ),
        ("fn-abs/K2-ABSFunc-43", "abs(xs:long(xs:untypedAtomic(\"0\"))) instance of xs:long", "F\tT"),
        ("fn-abs/K2-ABSFunc-44", "abs(xs:int(xs:untypedAtomic(\"0\"))) instance of xs:int", "F\tT"),
        ("fn-abs/K2-ABSFunc-45", "abs(xs:short(xs:untypedAtomic(\"0\"))) instance of xs:short", "F\tT"),
        ("fn-abs/K2-ABSFunc-46", "abs(xs:byte(xs:untypedAtomic(\"0\"))) instance of xs:byte", "F\tT"),
        ("fn-abs/K2-ABSFunc-47", "fn:abs(xs:float(\"-INF\"))", "S:INF"),
        ("fn-abs/K2-ABSFunc-48", "fn:abs(xs:double(\"-INF\"))", "S:INF"),
        ("fn-abs/K2-ABSFunc-49", "fn:abs(xs:float(\"INF\"))", "S:INF"),
        ("fn-abs/K2-ABSFunc-5", "fn:abs(xs:int(-4))", "S:4"),
        ("fn-abs/K2-ABSFunc-50", "fn:abs(xs:double(\"INF\"))", "S:INF"),
        ("fn-abs/K2-ABSFunc-6", "fn:abs(xs:short(-4))", "S:4"),
        ("fn-abs/K2-ABSFunc-7", "fn:abs(xs:byte(-4))", "S:4"),
        ("fn-abs/K2-ABSFunc-8", "fn:abs(xs:nonNegativeInteger(4))", "S:4"),
        ("fn-abs/K2-ABSFunc-9", "fn:abs(xs:unsignedLong(4))", "S:4"),
        ("fn-abs/fn-abs-more-args-001", "fn:abs(xs:double(\"-0\"))", "E:0\u001fY:xs:double"),
        ("fn-abs/fn-abs-more-args-002", "fn:abs(xs:double(\"-INF\"))", "S:INF\u001fY:xs:double"),
        ("fn-abs/fn-abs-more-args-003", "fn:abs(xs:double(\"INF\"))", "S:INF\u001fY:xs:double"),
        ("fn-abs/fn-abs-more-args-004", "fn:abs(xs:double(\"NaN\"))", "S:NaN\u001fY:xs:double"),
        ("fn-abs/fn-abs-more-args-005", "fn:abs(xs:double(\"NaN\"))", "S:NaN\u001fY:xs:double"),
        ("fn-abs/fn-abs-more-args-006", "fn:abs(xs:decimal(\"-0\"))", "E:0\u001fY:xs:decimal"),
        ("fn-abs/fn-abs-more-args-007", "fn:abs(xs:decimal(\"0\"))", "E:0\u001fY:xs:decimal"),
        ("fn-abs/fn-abs-more-args-008", "fn:abs(xs:float(\"-0\"))", "E:0\u001fY:xs:float"),
        ("fn-abs/fn-abs-more-args-009", "fn:abs(xs:float(\"-INF\"))", "S:INF\u001fY:xs:float"),
        ("fn-abs/fn-abs-more-args-010", "fn:abs(xs:float(\"INF\"))", "S:INF\u001fY:xs:float"),
        ("fn-abs/fn-abs-more-args-011", "fn:abs(xs:float(\"NaN\"))", "S:NaN\u001fY:xs:float"),
        ("fn-abs/fn-abs-more-args-012", "fn:abs(xs:float(\"NaN\"))", "S:NaN\u001fY:xs:float"),
        ("fn-abs/fn-abs-more-args-013", "fn:abs(xs:int(\"0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-014", "fn:abs(xs:int(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-015", "fn:abs(xs:integer(\"0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-016", "fn:abs(xs:integer(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-017", "fn:abs(xs:long(\"0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-018", "fn:abs(xs:long(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-019", "fn:abs(xs:nonNegativeInteger(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-020", "fn:abs(xs:nonPositiveInteger(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-021", "fn:abs(xs:short(\"0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-022", "fn:abs(xs:short(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-023", "fn:abs(xs:unsignedLong(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-024", "fn:abs(xs:unsignedShort(\"-0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abs-more-args-025", "fn:abs(xs:decimal(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-026", "fn:abs(xs:decimal(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-027", "fn:abs(xs:decimal(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-028", "fn:abs(xs:decimal(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-029", "fn:abs(xs:int(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-030", "fn:abs(xs:int(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-031", "fn:abs(xs:int(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-032", "fn:abs(xs:int(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-033", "fn:abs(xs:integer(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-034", "fn:abs(xs:integer(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-035", "fn:abs(xs:integer(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-036", "fn:abs(xs:integer(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-037", "fn:abs(xs:long(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-038", "fn:abs(xs:long(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-039", "fn:abs(xs:long(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-040", "fn:abs(xs:long(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-041", "fn:abs(xs:negativeInteger(\"-0\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-042", "fn:abs(xs:negativeInteger(\"0\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-043", "fn:abs(xs:negativeInteger(\"2\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-044", "fn:abs(xs:negativeInteger(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-045", "fn:abs(xs:negativeInteger(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-046", "fn:abs(xs:negativeInteger(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-047", "fn:abs(xs:negativeInteger(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-048", "fn:abs(xs:nonNegativeInteger(\"-2\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-049", "fn:abs(xs:nonNegativeInteger(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-050", "fn:abs(xs:nonNegativeInteger(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-051", "fn:abs(xs:nonNegativeInteger(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-052", "fn:abs(xs:nonNegativeInteger(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-053", "fn:abs(xs:nonPositiveInteger(\"2\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-054", "fn:abs(xs:nonPositiveInteger(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-055", "fn:abs(xs:nonPositiveInteger(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-056", "fn:abs(xs:nonPositiveInteger(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-057", "fn:abs(xs:nonPositiveInteger(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-058", "fn:abs(xs:positiveInteger(\"0\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-059", "fn:abs(xs:positiveInteger(\"-0\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-060", "fn:abs(xs:positiveInteger(\"-2\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-061", "fn:abs(xs:positiveInteger(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-062", "fn:abs(xs:positiveInteger(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-063", "fn:abs(xs:positiveInteger(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-064", "fn:abs(xs:positiveInteger(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-065", "fn:abs(xs:short(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-066", "fn:abs(xs:short(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-067", "fn:abs(xs:short(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-068", "fn:abs(xs:short(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-069", "fn:abs(xs:unsignedLong(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-070", "fn:abs(xs:unsignedLong(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-071", "fn:abs(xs:unsignedLong(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-072", "fn:abs(xs:unsignedLong(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-073", "fn:abs(xs:unsignedShort(\"-INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-074", "fn:abs(xs:unsignedShort(\"INF\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-075", "fn:abs(xs:unsignedShort(\"-NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-076", "fn:abs(xs:unsignedShort(\"NaN\"))", "X:FORG0001"),
        ("fn-abs/fn-abs-more-args-077", "fn:abs(xs:string(\"1\"))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-078", "fn:abs(xs:string(\"hello\"))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-079", "fn:abs(xs:boolean(\"1\"))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-080", "fn:abs(xs:boolean(\"0\"))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-081", "fn:abs(xs:boolean(fn:true()))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-082", "fn:abs(xs:boolean(fn:false()))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-083", "fn:abs(xs:date(\"2002-10-09\"))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-084", "fn:abs(xs:time(\"13:20:00-05:00\"))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-085", "fn:abs(xs:dateTime(\"2002-10-10T12:00:00-05:00\"))", "X:XPTY0004"),
        ("fn-abs/fn-abs-more-args-086", "fn:abs(xs:anyURI(\"www.examples.org\"))", "X:XPTY0004"),
        (
            "fn-abs/fn-absdbl1args-1",
            "fn:abs(xs:double(\"-1.7976931348623157E308\"))",
            "E:1.7976931348623157E308\u001fY:xs:double"
        ),
        ("fn-abs/fn-absdbl1args-2", "fn:abs(xs:double(\"0\"))", "E:0.0e0\u001fY:xs:double"),
        (
            "fn-abs/fn-absdbl1args-3",
            "fn:abs(xs:double(\"1.7976931348623157E308\"))",
            "E:1.7976931348623157E308\u001fY:xs:double"
        ),
        (
            "fn-abs/fn-absdec1args-1",
            "fn:abs(xs:decimal(\"-999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:decimal"
        ),
        (
            "fn-abs/fn-absdec1args-2",
            "fn:abs(xs:decimal(\"617375191608514839\"))",
            "E:617375191608514839\u001fY:xs:decimal"
        ),
        (
            "fn-abs/fn-absdec1args-3",
            "fn:abs(xs:decimal(\"999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:decimal"
        ),
        ("fn-abs/fn-absflt1args-1", "fn:abs(xs:float(\"-3.4028235E38\"))", "E:xs:float(3.4028235E38)\u001fY:xs:float"),
        ("fn-abs/fn-absflt1args-2", "fn:abs(xs:float(\"0\"))", "E:0.0e0\u001fY:xs:float"),
        ("fn-abs/fn-absflt1args-3", "fn:abs(xs:float(\"3.4028235E38\"))", "E:xs:float(3.4028235E38)\u001fY:xs:float"),
        ("fn-abs/fn-absint1args-1", "fn:abs(xs:int(\"-2147483648\"))", "E:2147483648\u001fY:xs:integer"),
        ("fn-abs/fn-absint1args-2", "fn:abs(xs:int(\"-1873914410\"))", "E:1873914410\u001fY:xs:integer"),
        ("fn-abs/fn-absint1args-3", "fn:abs(xs:int(\"2147483647\"))", "E:2147483647\u001fY:xs:integer"),
        (
            "fn-abs/fn-absintg1args-1",
            "fn:abs(xs:integer(\"-999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-absintg1args-2",
            "fn:abs(xs:integer(\"830993497117024304\"))",
            "E:830993497117024304\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-absintg1args-3",
            "fn:abs(xs:integer(\"999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:integer"
        ),
        ("fn-abs/fn-abslng1args-1", "fn:abs(xs:long(\"-92233720368547758\"))", "E:92233720368547758\u001fY:xs:integer"),
        ("fn-abs/fn-abslng1args-2", "fn:abs(xs:long(\"-47175562203048468\"))", "E:47175562203048468\u001fY:xs:integer"),
        ("fn-abs/fn-abslng1args-3", "fn:abs(xs:long(\"92233720368547758\"))", "E:92233720368547758\u001fY:xs:integer"),
        (
            "fn-abs/fn-absnint1args-1",
            "fn:abs(xs:negativeInteger(\"-999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-absnint1args-2",
            "fn:abs(xs:negativeInteger(\"-297014075999096793\"))",
            "E:297014075999096793\u001fY:xs:integer"
        ),
        ("fn-abs/fn-absnint1args-3", "fn:abs(xs:negativeInteger(\"-1\"))", "E:1\u001fY:xs:integer"),
        ("fn-abs/fn-absnni1args-1", "fn:abs(xs:nonNegativeInteger(\"0\"))", "E:0\u001fY:xs:integer"),
        (
            "fn-abs/fn-absnni1args-2",
            "fn:abs(xs:nonNegativeInteger(\"303884545991464527\"))",
            "E:303884545991464527\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-absnni1args-3",
            "fn:abs(xs:nonNegativeInteger(\"999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-absnpi1args-1",
            "fn:abs(xs:nonPositiveInteger(\"-999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-absnpi1args-2",
            "fn:abs(xs:nonPositiveInteger(\"-475688437271870490\"))",
            "E:475688437271870490\u001fY:xs:integer"
        ),
        ("fn-abs/fn-absnpi1args-3", "fn:abs(xs:nonPositiveInteger(\"0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-abspint1args-1", "fn:abs(xs:positiveInteger(\"1\"))", "E:1\u001fY:xs:integer"),
        (
            "fn-abs/fn-abspint1args-2",
            "fn:abs(xs:positiveInteger(\"52704602390610033\"))",
            "E:52704602390610033\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-abspint1args-3",
            "fn:abs(xs:positiveInteger(\"999999999999999999\"))",
            "E:999999999999999999\u001fY:xs:integer"
        ),
        ("fn-abs/fn-abssht1args-1", "fn:abs(xs:short(\"-32768\"))", "E:32768\u001fY:xs:integer"),
        ("fn-abs/fn-abssht1args-2", "fn:abs(xs:short(\"-5324\"))", "E:5324\u001fY:xs:integer"),
        ("fn-abs/fn-abssht1args-3", "fn:abs(xs:short(\"32767\"))", "E:32767\u001fY:xs:integer"),
        ("fn-abs/fn-absulng1args-1", "fn:abs(xs:unsignedLong(\"0\"))", "E:0\u001fY:xs:integer"),
        (
            "fn-abs/fn-absulng1args-2",
            "fn:abs(xs:unsignedLong(\"130747108607674654\"))",
            "E:130747108607674654\u001fY:xs:integer"
        ),
        (
            "fn-abs/fn-absulng1args-3",
            "fn:abs(xs:unsignedLong(\"184467440737095516\"))",
            "E:184467440737095516\u001fY:xs:integer"
        ),
        ("fn-abs/fn-absusht1args-1", "fn:abs(xs:unsignedShort(\"0\"))", "E:0\u001fY:xs:integer"),
        ("fn-abs/fn-absusht1args-2", "fn:abs(xs:unsignedShort(\"44633\"))", "E:44633\u001fY:xs:integer"),
        ("fn-abs/fn-absusht1args-3", "fn:abs(xs:unsignedShort(\"65535\"))", "E:65535\u001fY:xs:integer"),
        ("fn-ceiling/K-CeilingFunc-1", "ceiling()", "X:XPST0017"),
        ("fn-ceiling/K-CeilingFunc-2", "ceiling(1, 2)", "X:XPST0017"),
        ("fn-ceiling/K-CeilingFunc-3", "empty(ceiling(()))", "T"),
        ("fn-ceiling/K-CeilingFunc-4", "ceiling(10.5) eq 11", "T"),
        ("fn-ceiling/K-CeilingFunc-5", "ceiling(-10.5) eq -10", "T"),
        ("fn-ceiling/K-CeilingFunc-6", "ceiling(\"a string\")", "X:XPTY0004"),
        (
            "fn-ceiling/K2-CeilingFunc-1",
            "ceiling(xs:unsignedShort(xs:untypedAtomic(\"0\"))) instance of xs:integer",
            "T"
        ),
        ("fn-ceiling/K2-CeilingFunc-10", "ceiling(xs:int(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-ceiling/K2-CeilingFunc-11", "ceiling(xs:short(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-ceiling/K2-CeilingFunc-12", "ceiling(xs:byte(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        (
            "fn-ceiling/K2-CeilingFunc-2",
            "ceiling(xs:unsignedLong(xs:untypedAtomic(\"0\"))) instance of xs:integer",
            "T"
        ),
        ("fn-ceiling/K2-CeilingFunc-3", "ceiling(xs:unsignedInt(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        (
            "fn-ceiling/K2-CeilingFunc-4",
            "ceiling(xs:unsignedByte(xs:untypedAtomic(\"0\"))) instance of xs:integer",
            "T"
        ),
        (
            "fn-ceiling/K2-CeilingFunc-5",
            "ceiling(xs:positiveInteger(xs:untypedAtomic(\"1\"))) instance of xs:integer",
            "T"
        ),
        (
            "fn-ceiling/K2-CeilingFunc-6",
            "ceiling(xs:nonPositiveInteger(xs:untypedAtomic(\"0\"))) instance of xs:integer",
            "T"
        ),
        (
            "fn-ceiling/K2-CeilingFunc-7",
            "ceiling(xs:nonNegativeInteger(xs:untypedAtomic(\"0\"))) instance of xs:integer",
            "T"
        ),
        (
            "fn-ceiling/K2-CeilingFunc-8",
            "ceiling(xs:negativeInteger(xs:untypedAtomic(\"-1\"))) instance of xs:integer",
            "T"
        ),
        ("fn-ceiling/K2-CeilingFunc-9", "ceiling(xs:long(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-ceiling/fn-ceiling-decimal-1", "ceiling(12.5)", "E:13.0\u001fY:xs:decimal"),
        ("fn-ceiling/fn-ceiling-decimal-2", "ceiling(12.9)", "E:13.0\u001fY:xs:decimal"),
        ("fn-ceiling/fn-ceiling-decimal-3", "ceiling(0.000000001)", "E:1.0\u001fY:xs:decimal"),
        ("fn-ceiling/fn-ceiling-decimal-4", "ceiling(0.0)", "E:0.0\u001fY:xs:decimal"),
        ("fn-ceiling/fn-ceiling-decimal-5", "ceiling(-0.1)", "E:0.0\u001fY:xs:decimal"),
        ("fn-ceiling/fn-ceiling-decimal-6", "ceiling(-12345678.567890)", "E:-12345678\u001fY:xs:decimal"),
        ("fn-ceiling/fn-ceiling-decimal-7", "ceiling(-1234567891234567.2)", "E:-1234567891234567.0\u001fY:xs:decimal"),
        ("fn-ceiling/fn-ceiling-double-1", "ceiling(12.5e0)", "E:13.0e0\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-10", "ceiling(xs:double('-INF'))", "S:-INF\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-11", "ceiling(xs:double('-0'))", "E:-0\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-2", "ceiling(12.9e0)", "E:13.0e0\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-3", "ceiling(0.000000001e0)", "E:1.0e0\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-4", "ceiling(0.0e0)", "E:0.0e0\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-5", "ceiling(-0.1e0)", "E:-0.0e0\u001fE:-0\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-6", "ceiling(-12345678.567890e0)", "E:-12345678e0\u001fY:xs:double"),
        (
            "fn-ceiling/fn-ceiling-double-7",
            "ceiling(-1234567891234567.2e0)",
            "E:-1234567891234567.0e0\u001fY:xs:double"
        ),
        ("fn-ceiling/fn-ceiling-double-8", "ceiling(xs:double('NaN'))", "S:NaN\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-double-9", "ceiling(xs:double('INF'))", "S:INF\u001fY:xs:double"),
        ("fn-ceiling/fn-ceiling-float-1", "ceiling(xs:float(12.5e0))", "E:13.0e0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-10", "ceiling(xs:float(xs:float('-INF')))", "S:-INF\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-11", "ceiling(xs:float(xs:float('-0')))", "E:-0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-2", "ceiling(xs:float(12.9e0))", "E:13.0e0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-3", "ceiling(xs:float(0.000000001e0))", "E:1.0e0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-4", "ceiling(xs:float(0.0e0))", "E:0.0e0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-5", "ceiling(xs:float(-0.1e0))", "E:0.0e0\u001fE:-0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-6", "ceiling(xs:float(-12345678.1e0))", "E:-12345678e0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-7", "ceiling(xs:float(-1234567.2e0))", "E:-1234567e0\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-8", "ceiling(xs:float(xs:float('NaN')))", "S:NaN\u001fY:xs:float"),
        ("fn-ceiling/fn-ceiling-float-9", "ceiling(xs:float(xs:float('INF')))", "S:INF\u001fY:xs:float"),
        (
            "fn-ceiling/fn-ceilingdbl1args-1",
            "fn:ceiling(xs:double(\"-1.7976931348623157E308\"))",
            "S:-1.7976931348623157E308"
        ),
        ("fn-ceiling/fn-ceilingdbl1args-2", "fn:ceiling(xs:double(\"0\"))", "E:0"),
        (
            "fn-ceiling/fn-ceilingdbl1args-3",
            "fn:ceiling(xs:double(\"1.7976931348623157E308\"))",
            "S:1.7976931348623157E308"
        ),
        ("fn-ceiling/fn-ceilingdec1args-1", "fn:ceiling(xs:decimal(\"-999999999999999999\"))", "E:-999999999999999999"),
        ("fn-ceiling/fn-ceilingdec1args-2", "fn:ceiling(xs:decimal(\"617375191608514839\"))", "E:617375191608514839"),
        ("fn-ceiling/fn-ceilingdec1args-3", "fn:ceiling(xs:decimal(\"999999999999999999\"))", "E:999999999999999999"),
        ("fn-ceiling/fn-ceilingflt1args-1", "fn:ceiling(xs:float(\"-3.4028235E38\"))", "S:-3.4028235E38"),
        ("fn-ceiling/fn-ceilingflt1args-2", "fn:ceiling(xs:float(\"0\"))", "E:0"),
        ("fn-ceiling/fn-ceilingflt1args-3", "fn:ceiling(xs:float(\"3.4028235E38\"))", "S:3.4028235E38"),
        ("fn-ceiling/fn-ceilingint1args-1", "fn:ceiling(xs:int(\"-2147483648\"))", "E:-2147483648"),
        ("fn-ceiling/fn-ceilingint1args-2", "fn:ceiling(xs:int(\"-1873914410\"))", "E:-1873914410"),
        ("fn-ceiling/fn-ceilingint1args-3", "fn:ceiling(xs:int(\"2147483647\"))", "E:2147483647"),
        (
            "fn-ceiling/fn-ceilingintg1args-1",
            "fn:ceiling(xs:integer(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        ("fn-ceiling/fn-ceilingintg1args-2", "fn:ceiling(xs:integer(\"830993497117024304\"))", "E:830993497117024304"),
        ("fn-ceiling/fn-ceilingintg1args-3", "fn:ceiling(xs:integer(\"999999999999999999\"))", "E:999999999999999999"),
        ("fn-ceiling/fn-ceilinglng1args-1", "fn:ceiling(xs:long(\"-92233720368547758\"))", "E:-92233720368547758"),
        ("fn-ceiling/fn-ceilinglng1args-2", "fn:ceiling(xs:long(\"-47175562203048468\"))", "E:-47175562203048468"),
        ("fn-ceiling/fn-ceilinglng1args-3", "fn:ceiling(xs:long(\"92233720368547758\"))", "E:92233720368547758"),
        (
            "fn-ceiling/fn-ceilingnint1args-1",
            "fn:ceiling(xs:negativeInteger(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-ceiling/fn-ceilingnint1args-2",
            "fn:ceiling(xs:negativeInteger(\"-297014075999096793\"))",
            "E:-297014075999096793"
        ),
        ("fn-ceiling/fn-ceilingnint1args-3", "fn:ceiling(xs:negativeInteger(\"-1\"))", "E:-1"),
        ("fn-ceiling/fn-ceilingnni1args-1", "fn:ceiling(xs:nonNegativeInteger(\"0\"))", "E:0"),
        (
            "fn-ceiling/fn-ceilingnni1args-2",
            "fn:ceiling(xs:nonNegativeInteger(\"303884545991464527\"))",
            "E:303884545991464527"
        ),
        (
            "fn-ceiling/fn-ceilingnni1args-3",
            "fn:ceiling(xs:nonNegativeInteger(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        (
            "fn-ceiling/fn-ceilingnpi1args-1",
            "fn:ceiling(xs:nonPositiveInteger(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-ceiling/fn-ceilingnpi1args-2",
            "fn:ceiling(xs:nonPositiveInteger(\"-475688437271870490\"))",
            "E:-475688437271870490"
        ),
        ("fn-ceiling/fn-ceilingnpi1args-3", "fn:ceiling(xs:nonPositiveInteger(\"0\"))", "E:0"),
        ("fn-ceiling/fn-ceilingpint1args-1", "fn:ceiling(xs:positiveInteger(\"1\"))", "E:1"),
        (
            "fn-ceiling/fn-ceilingpint1args-2",
            "fn:ceiling(xs:positiveInteger(\"52704602390610033\"))",
            "E:52704602390610033"
        ),
        (
            "fn-ceiling/fn-ceilingpint1args-3",
            "fn:ceiling(xs:positiveInteger(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        ("fn-ceiling/fn-ceilingsht1args-1", "fn:ceiling(xs:short(\"-32768\"))", "E:-32768"),
        ("fn-ceiling/fn-ceilingsht1args-2", "fn:ceiling(xs:short(\"-5324\"))", "E:-5324"),
        ("fn-ceiling/fn-ceilingsht1args-3", "fn:ceiling(xs:short(\"32767\"))", "E:32767"),
        ("fn-ceiling/fn-ceilingulng1args-1", "fn:ceiling(xs:unsignedLong(\"0\"))", "E:0"),
        (
            "fn-ceiling/fn-ceilingulng1args-2",
            "fn:ceiling(xs:unsignedLong(\"130747108607674654\"))",
            "E:130747108607674654"
        ),
        (
            "fn-ceiling/fn-ceilingulng1args-3",
            "fn:ceiling(xs:unsignedLong(\"184467440737095516\"))",
            "E:184467440737095516"
        ),
        ("fn-ceiling/fn-ceilingusht1args-1", "fn:ceiling(xs:unsignedShort(\"0\"))", "E:0"),
        ("fn-ceiling/fn-ceilingusht1args-2", "fn:ceiling(xs:unsignedShort(\"44633\"))", "E:44633"),
        ("fn-ceiling/fn-ceilingusht1args-3", "fn:ceiling(xs:unsignedShort(\"65535\"))", "E:65535"),
        ("fn-floor/K-FloorFunc-1", "floor()", "X:XPST0017"),
        ("fn-floor/K-FloorFunc-2", "floor(1, 2)", "X:XPST0017"),
        ("fn-floor/K-FloorFunc-3", "empty(floor(()))", "T"),
        ("fn-floor/K-FloorFunc-4", "floor(10.5) eq 10", "T"),
        ("fn-floor/K-FloorFunc-5", "floor(-10.5) eq -11", "T"),
        ("fn-floor/K-FloorFunc-6", "floor(\"a string\")", "X:XPTY0004"),
        ("fn-floor/K2-FloorFunc-1", "floor(xs:unsignedShort(xs:untypedAtomic(\"1\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-10", "floor(xs:long(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-11", "floor(xs:int(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-12", "floor(xs:short(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-13", "floor(xs:byte(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-2", "floor(xs:unsignedShort(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-3", "floor(xs:unsignedLong(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-4", "floor(xs:unsignedInt(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-5", "floor(xs:unsignedByte(xs:untypedAtomic(\"0\"))) instance of xs:integer", "T"),
        ("fn-floor/K2-FloorFunc-6", "floor(xs:positiveInteger(xs:untypedAtomic(\"1\"))) instance of xs:integer", "T"),
        (
            "fn-floor/K2-FloorFunc-7",
            "floor(xs:nonPositiveInteger(xs:untypedAtomic(\"0\"))) instance of xs:integer",
            "T"
        ),
        (
            "fn-floor/K2-FloorFunc-8",
            "floor(xs:nonNegativeInteger(xs:untypedAtomic(\"0\"))) instance of xs:integer",
            "T"
        ),
        ("fn-floor/K2-FloorFunc-9", "floor(xs:negativeInteger(xs:untypedAtomic(\"-1\"))) instance of xs:integer", "T"),
        ("fn-floor/fn-floor-decimal-1", "floor(12.5)", "E:12.0\u001fY:xs:decimal"),
        ("fn-floor/fn-floor-decimal-2", "floor(12.9)", "E:12.0\u001fY:xs:decimal"),
        ("fn-floor/fn-floor-decimal-3", "floor(0.000000001)", "E:0.0\u001fY:xs:decimal"),
        ("fn-floor/fn-floor-decimal-4", "floor(0.0)", "E:0.0\u001fY:xs:decimal"),
        ("fn-floor/fn-floor-decimal-5", "floor(-0.1)", "E:-1.0\u001fY:xs:decimal"),
        ("fn-floor/fn-floor-decimal-6", "floor(-12345678.567890)", "E:-12345679\u001fY:xs:decimal"),
        ("fn-floor/fn-floor-decimal-7", "floor(-1234567891234567.2)", "E:-1234567891234568.0\u001fY:xs:decimal"),
        ("fn-floor/fn-floor-double-1", "floor(12.5e0)", "E:12.0e0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-10", "floor(xs:double('-INF'))", "S:-INF\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-11", "floor(xs:double('-0'))", "S:-0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-2", "floor(12.9e0)", "E:12.0e0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-3", "floor(0.000000001e0)", "E:0.0e0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-4", "floor(0.0e0)", "E:0.0e0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-5", "floor(-0.1e0)", "E:-1.0e0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-6", "floor(-12345678.567890e0)", "E:-12345679e0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-7", "floor(-1234567891234567.2e0)", "E:-1234567891234568.0e0\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-8", "floor(xs:double('NaN'))", "S:NaN\u001fY:xs:double"),
        ("fn-floor/fn-floor-double-9", "floor(xs:double('INF'))", "S:INF\u001fY:xs:double"),
        ("fn-floor/fn-floor-float-1", "floor(xs:float(12.5e0))", "E:12.0e0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-10", "floor(xs:float(xs:float('-INF')))", "S:-INF\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-11", "floor(xs:float(xs:float('-0')))", "S:-0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-2", "floor(xs:float(12.9e0))", "E:12.0e0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-3", "floor(xs:float(0.000000001e0))", "E:0.0e0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-4", "floor(xs:float(0.0e0))", "E:0.0e0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-5", "floor(xs:float(-0.1e0))", "E:-1.0e0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-6", "floor(xs:float(-12345678.567890e0))", "E:-12345679e0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-7", "floor(xs:float(-1234567.2e0))", "E:-1234568e0\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-8", "floor(xs:float(xs:float('NaN')))", "S:NaN\u001fY:xs:float"),
        ("fn-floor/fn-floor-float-9", "floor(xs:float(xs:float('INF')))", "S:INF\u001fY:xs:float"),
        (
            "fn-floor/fn-floordbl1args-1",
            "fn:floor(xs:double(\"-1.7976931348623157E308\"))",
            "S:-1.7976931348623157E308"
        ),
        ("fn-floor/fn-floordbl1args-2", "fn:floor(xs:double(\"0\"))", "S:0"),
        ("fn-floor/fn-floordbl1args-3", "fn:floor(xs:double(\"1.7976931348623157E308\"))", "S:1.7976931348623157E308"),
        ("fn-floor/fn-floordec1args-1", "fn:floor(xs:decimal(\"-999999999999999999\"))", "S:-999999999999999999"),
        ("fn-floor/fn-floordec1args-2", "fn:floor(xs:decimal(\"617375191608514839\"))", "S:617375191608514839"),
        ("fn-floor/fn-floordec1args-3", "fn:floor(xs:decimal(\"999999999999999999\"))", "S:999999999999999999"),
        ("fn-floor/fn-floorflt1args-1", "fn:floor(xs:float(\"-3.4028235E38\"))", "S:-3.4028235E38"),
        ("fn-floor/fn-floorflt1args-2", "fn:floor(xs:float(\"0\"))", "S:0"),
        ("fn-floor/fn-floorflt1args-3", "fn:floor(xs:float(\"3.4028235E38\"))", "S:3.4028235E38"),
        ("fn-floor/fn-floorint1args-1", "fn:floor(xs:int(\"-2147483648\"))", "S:-2147483648"),
        ("fn-floor/fn-floorint1args-2", "fn:floor(xs:int(\"-1873914410\"))", "S:-1873914410"),
        ("fn-floor/fn-floorint1args-3", "fn:floor(xs:int(\"2147483647\"))", "S:2147483647"),
        ("fn-floor/fn-floorintg1args-1", "fn:floor(xs:integer(\"-999999999999999999\"))", "S:-999999999999999999"),
        ("fn-floor/fn-floorintg1args-2", "fn:floor(xs:integer(\"830993497117024304\"))", "S:830993497117024304"),
        ("fn-floor/fn-floorintg1args-3", "fn:floor(xs:integer(\"999999999999999999\"))", "S:999999999999999999"),
        ("fn-floor/fn-floorlng1args-1", "fn:floor(xs:long(\"-92233720368547758\"))", "S:-92233720368547758"),
        ("fn-floor/fn-floorlng1args-2", "fn:floor(xs:long(\"-47175562203048468\"))", "S:-47175562203048468"),
        ("fn-floor/fn-floorlng1args-3", "fn:floor(xs:long(\"92233720368547758\"))", "S:92233720368547758"),
        (
            "fn-floor/fn-floornint1args-1",
            "fn:floor(xs:negativeInteger(\"-999999999999999999\"))",
            "S:-999999999999999999"
        ),
        (
            "fn-floor/fn-floornint1args-2",
            "fn:floor(xs:negativeInteger(\"-297014075999096793\"))",
            "S:-297014075999096793"
        ),
        ("fn-floor/fn-floornint1args-3", "fn:floor(xs:negativeInteger(\"-1\"))", "S:-1"),
        ("fn-floor/fn-floornni1args-1", "fn:floor(xs:nonNegativeInteger(\"0\"))", "S:0"),
        (
            "fn-floor/fn-floornni1args-2",
            "fn:floor(xs:nonNegativeInteger(\"303884545991464527\"))",
            "S:303884545991464527"
        ),
        (
            "fn-floor/fn-floornni1args-3",
            "fn:floor(xs:nonNegativeInteger(\"999999999999999999\"))",
            "S:999999999999999999"
        ),
        (
            "fn-floor/fn-floornpi1args-1",
            "fn:floor(xs:nonPositiveInteger(\"-999999999999999999\"))",
            "S:-999999999999999999"
        ),
        (
            "fn-floor/fn-floornpi1args-2",
            "fn:floor(xs:nonPositiveInteger(\"-475688437271870490\"))",
            "S:-475688437271870490"
        ),
        ("fn-floor/fn-floornpi1args-3", "fn:floor(xs:nonPositiveInteger(\"0\"))", "S:0"),
        ("fn-floor/fn-floorpint1args-1", "fn:floor(xs:positiveInteger(\"1\"))", "S:1"),
        ("fn-floor/fn-floorpint1args-2", "fn:floor(xs:positiveInteger(\"52704602390610033\"))", "S:52704602390610033"),
        (
            "fn-floor/fn-floorpint1args-3",
            "fn:floor(xs:positiveInteger(\"999999999999999999\"))",
            "S:999999999999999999"
        ),
        ("fn-floor/fn-floorsht1args-1", "fn:floor(xs:short(\"-32768\"))", "S:-32768"),
        ("fn-floor/fn-floorsht1args-2", "fn:floor(xs:short(\"-5324\"))", "S:-5324"),
        ("fn-floor/fn-floorsht1args-3", "fn:floor(xs:short(\"32767\"))", "S:32767"),
        ("fn-floor/fn-floorulng1args-1", "fn:floor(xs:unsignedLong(\"0\"))", "S:0"),
        ("fn-floor/fn-floorulng1args-2", "fn:floor(xs:unsignedLong(\"130747108607674654\"))", "S:130747108607674654"),
        ("fn-floor/fn-floorulng1args-3", "fn:floor(xs:unsignedLong(\"184467440737095516\"))", "S:184467440737095516"),
        ("fn-floor/fn-floorusht1args-1", "fn:floor(xs:unsignedShort(\"0\"))", "S:0"),
        ("fn-floor/fn-floorusht1args-2", "fn:floor(xs:unsignedShort(\"44633\"))", "S:44633"),
        ("fn-floor/fn-floorusht1args-3", "fn:floor(xs:unsignedShort(\"65535\"))", "S:65535"),
        ("fn-round/K-RoundFunc-1", "round()", "X:XPST0017"),
        ("fn-round/K-RoundFunc-2", "round(1, 2)", "X:XPST0017"),
        ("fn-round/K-RoundFunc-3", "empty(round(()))", "T"),
        ("fn-round/K-RoundFunc-4", "round(1) eq 1", "T"),
        ("fn-round/K-RoundFunc-5", "round(1.1) eq 1", "T"),
        ("fn-round/K-RoundFunc-6", "round(xs:double(1)) eq 1", "T"),
        ("fn-round/K-RoundFunc-7", "round(xs:float(1)) eq 1", "T"),
        ("fn-round/K-RoundFunc-8", "round(2.4999) eq 2", "T"),
        ("fn-round/K-RoundFunc-9", "round(-2.5) eq -2", "T"),
        (
            "fn-round/K2-RoundFunc-1",
            "round(xs:unsignedShort(xs:untypedAtomic(\"0\"))) instance of xs:unsignedShort",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-10", "round(xs:int(xs:untypedAtomic(\"0\"))) instance of xs:int", "F\tT"),
        ("fn-round/K2-RoundFunc-100", "fn:round(xs:double(\"0.54\"))", "E:1"),
        ("fn-round/K2-RoundFunc-101", "fn:round(xs:float(\"-0.54\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-102", "fn:round(xs:double(\"-0.54\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-103", "fn:round(xs:float(\"0.55\"))", "E:1"),
        ("fn-round/K2-RoundFunc-104", "fn:round(xs:double(\"0.55\"))", "E:1"),
        ("fn-round/K2-RoundFunc-105", "fn:round(xs:float(\"-0.55\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-106", "fn:round(xs:double(\"-0.55\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-107", "fn:round(xs:float(\"0.56\"))", "E:1"),
        ("fn-round/K2-RoundFunc-108", "fn:round(xs:double(\"0.56\"))", "E:1"),
        ("fn-round/K2-RoundFunc-109", "fn:round(xs:float(\"-0.56\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-11", "round(xs:short(xs:untypedAtomic(\"0\"))) instance of xs:short", "F\tT"),
        ("fn-round/K2-RoundFunc-110", "fn:round(xs:double(\"-0.56\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-111", "fn:round(xs:float(\"0.59\"))", "E:1"),
        ("fn-round/K2-RoundFunc-112", "fn:round(xs:double(\"0.59\"))", "E:1"),
        ("fn-round/K2-RoundFunc-113", "fn:round(xs:float(\"-0.59\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-114", "fn:round(xs:double(\"-0.59\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-115", "fn:round(xs:float(\"0.50\"))", "E:1"),
        ("fn-round/K2-RoundFunc-116", "fn:round(xs:double(\"0.50\"))", "E:1"),
        ("fn-round/K2-RoundFunc-117", "fn:round(xs:float(\"-0.50\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-118", "fn:round(xs:double(\"-0.50\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-119", "fn:round(xs:float(\"0.61\"))", "E:1"),
        ("fn-round/K2-RoundFunc-12", "round(xs:byte(xs:untypedAtomic(\"0\"))) instance of xs:byte", "F\tT"),
        ("fn-round/K2-RoundFunc-120", "fn:round(xs:double(\"0.61\"))", "E:1"),
        ("fn-round/K2-RoundFunc-121", "fn:round(xs:float(\"-0.61\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-122", "fn:round(xs:double(\"-0.61\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-123", "fn:round(xs:float(\"0.64\"))", "E:1"),
        ("fn-round/K2-RoundFunc-124", "fn:round(xs:double(\"0.64\"))", "E:1"),
        ("fn-round/K2-RoundFunc-125", "fn:round(xs:float(\"-0.64\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-126", "fn:round(xs:double(\"-0.64\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-127", "fn:round(xs:float(\"0.65\"))", "E:1"),
        ("fn-round/K2-RoundFunc-128", "fn:round(xs:double(\"0.65\"))", "E:1"),
        ("fn-round/K2-RoundFunc-129", "fn:round(xs:float(\"-0.65\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-13", "round(xs:double(\"INF\"))", "S:INF"),
        ("fn-round/K2-RoundFunc-130", "fn:round(xs:double(\"-0.65\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-131", "fn:round(xs:float(\"0.66\"))", "E:1"),
        ("fn-round/K2-RoundFunc-132", "fn:round(xs:double(\"0.66\"))", "E:1"),
        ("fn-round/K2-RoundFunc-133", "fn:round(xs:float(\"-0.66\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-134", "fn:round(xs:double(\"-0.66\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-135", "fn:round(xs:float(\"0.69\"))", "E:1"),
        ("fn-round/K2-RoundFunc-136", "fn:round(xs:double(\"0.69\"))", "E:1"),
        ("fn-round/K2-RoundFunc-137", "fn:round(xs:float(\"-0.69\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-138", "fn:round(xs:double(\"-0.69\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-139", "fn:round(xs:float(\"0.60\"))", "E:1"),
        ("fn-round/K2-RoundFunc-14", "round(xs:double(\"-INF\"))", "S:-INF"),
        ("fn-round/K2-RoundFunc-140", "fn:round(xs:double(\"0.60\"))", "E:1"),
        ("fn-round/K2-RoundFunc-141", "fn:round(xs:float(\"-0.60\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-142", "fn:round(xs:double(\"-0.60\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-143", "fn:round(xs:float(\"0.91\"))", "E:1"),
        ("fn-round/K2-RoundFunc-144", "fn:round(xs:double(\"0.91\"))", "E:1"),
        ("fn-round/K2-RoundFunc-145", "fn:round(xs:float(\"-0.91\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-146", "fn:round(xs:double(\"-0.91\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-147", "fn:round(xs:float(\"0.94\"))", "E:1"),
        ("fn-round/K2-RoundFunc-148", "fn:round(xs:double(\"0.94\"))", "E:1"),
        ("fn-round/K2-RoundFunc-149", "fn:round(xs:float(\"-0.94\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-15", "round(xs:double(\"0\"))", "S:0"),
        ("fn-round/K2-RoundFunc-150", "fn:round(xs:double(\"-0.94\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-151", "fn:round(xs:float(\"0.95\"))", "E:1"),
        ("fn-round/K2-RoundFunc-152", "fn:round(xs:double(\"0.95\"))", "E:1"),
        ("fn-round/K2-RoundFunc-153", "fn:round(xs:float(\"-0.95\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-154", "fn:round(xs:double(\"-0.95\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-155", "fn:round(xs:float(\"0.96\"))", "E:1"),
        ("fn-round/K2-RoundFunc-156", "fn:round(xs:double(\"0.96\"))", "E:1"),
        ("fn-round/K2-RoundFunc-157", "fn:round(xs:float(\"-0.96\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-158", "fn:round(xs:double(\"-0.96\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-159", "fn:round(xs:float(\"0.99\"))", "E:1"),
        ("fn-round/K2-RoundFunc-16", "round(xs:double(\"-0\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-160", "fn:round(xs:double(\"0.99\"))", "E:1"),
        ("fn-round/K2-RoundFunc-161", "fn:round(xs:float(\"-0.99\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-162", "fn:round(xs:double(\"-0.99\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-163", "fn:round(xs:float(\"0.90\"))", "E:1"),
        ("fn-round/K2-RoundFunc-164", "fn:round(xs:double(\"0.90\"))", "E:1"),
        ("fn-round/K2-RoundFunc-165", "fn:round(xs:float(\"-0.90\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-166", "fn:round(xs:double(\"-0.90\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-167", "fn:round(xs:float(\"0.101\"))", "E:0"),
        ("fn-round/K2-RoundFunc-168", "fn:round(xs:double(\"0.101\"))", "S:0"),
        ("fn-round/K2-RoundFunc-169", "fn:round(xs:float(\"-0.101\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-17", "round(xs:float(\"INF\"))", "S:INF"),
        ("fn-round/K2-RoundFunc-170", "fn:round(xs:double(\"-0.101\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-171", "fn:round(xs:float(\"0.104\"))", "S:0"),
        ("fn-round/K2-RoundFunc-172", "fn:round(xs:double(\"0.104\"))", "S:0"),
        ("fn-round/K2-RoundFunc-173", "fn:round(xs:float(\"-0.104\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-174", "fn:round(xs:double(\"-0.104\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-175", "fn:round(xs:float(\"0.105\"))", "S:0"),
        ("fn-round/K2-RoundFunc-176", "fn:round(xs:double(\"0.105\"))", "S:0"),
        ("fn-round/K2-RoundFunc-177", "fn:round(xs:float(\"-0.105\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-178", "fn:round(xs:double(\"-0.105\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-179", "fn:round(xs:float(\"0.106\"))", "S:0"),
        ("fn-round/K2-RoundFunc-18", "round(xs:float(\"-INF\"))", "S:-INF"),
        ("fn-round/K2-RoundFunc-180", "fn:round(xs:double(\"0.106\"))", "S:0"),
        ("fn-round/K2-RoundFunc-181", "fn:round(xs:float(\"-0.106\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-182", "fn:round(xs:double(\"-0.106\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-183", "fn:round(xs:float(\"0.109\"))", "S:0"),
        ("fn-round/K2-RoundFunc-184", "fn:round(xs:double(\"0.109\"))", "S:0"),
        ("fn-round/K2-RoundFunc-185", "fn:round(xs:float(\"-0.109\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-186", "fn:round(xs:double(\"-0.109\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-187", "fn:round(xs:float(\"0.100\"))", "S:0"),
        ("fn-round/K2-RoundFunc-188", "fn:round(xs:double(\"0.100\"))", "S:0"),
        ("fn-round/K2-RoundFunc-189", "fn:round(xs:float(\"-0.100\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-19", "round(xs:float(\"0\"))", "S:0"),
        ("fn-round/K2-RoundFunc-190", "fn:round(xs:double(\"-0.100\"))", "S:-0"),
        (
            "fn-round/K2-RoundFunc-2",
            "round(xs:unsignedLong(xs:untypedAtomic(\"0\"))) instance of xs:unsignedLong",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-20", "round(xs:float(\"-0\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-21", "fn:round(xs:float(\"NaN\"))", "S:NaN"),
        ("fn-round/K2-RoundFunc-22", "fn:round(xs:double(\"NaN\"))", "S:NaN"),
        ("fn-round/K2-RoundFunc-23", "fn:round(xs:float(\"0.01\"))", "E:0"),
        ("fn-round/K2-RoundFunc-24", "fn:round(xs:double(\"0.01\"))", "E:0"),
        ("fn-round/K2-RoundFunc-25", "fn:round(xs:float(\"-0.01\"))", "E:-0"),
        ("fn-round/K2-RoundFunc-26", "fn:round(xs:double(\"-0.01\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-27", "fn:round(xs:float(\"0.04\"))", "S:0"),
        ("fn-round/K2-RoundFunc-28", "fn:round(xs:double(\"0.04\"))", "S:0"),
        ("fn-round/K2-RoundFunc-29", "fn:round(xs:float(\"-0.04\"))", "S:-0"),
        (
            "fn-round/K2-RoundFunc-3",
            "round(xs:unsignedInt(xs:untypedAtomic(\"0\"))) instance of xs:unsignedInt",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-30", "fn:round(xs:double(\"-0.04\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-31", "fn:round(xs:float(\"0.05\"))", "S:0"),
        ("fn-round/K2-RoundFunc-32", "fn:round(xs:double(\"0.05\"))", "S:0"),
        ("fn-round/K2-RoundFunc-33", "fn:round(xs:float(\"-0.05\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-34", "fn:round(xs:double(\"-0.05\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-35", "fn:round(xs:float(\"0.06\"))", "S:0"),
        ("fn-round/K2-RoundFunc-36", "fn:round(xs:double(\"0.06\"))", "S:0"),
        ("fn-round/K2-RoundFunc-37", "fn:round(xs:float(\"-0.06\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-38", "fn:round(xs:double(\"-0.06\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-39", "fn:round(xs:float(\"0.09\"))", "S:0"),
        (
            "fn-round/K2-RoundFunc-4",
            "round(xs:unsignedByte(xs:untypedAtomic(\"0\"))) instance of xs:unsignedByte",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-40", "fn:round(xs:double(\"0.09\"))", "S:0"),
        ("fn-round/K2-RoundFunc-41", "fn:round(xs:float(\"-0.09\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-42", "fn:round(xs:double(\"-0.09\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-43", "fn:round(xs:float(\"0.00\"))", "S:0"),
        ("fn-round/K2-RoundFunc-44", "fn:round(xs:double(\"0.00\"))", "S:0"),
        ("fn-round/K2-RoundFunc-45", "fn:round(xs:float(\"-0.00\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-46", "fn:round(xs:double(\"-0.00\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-47", "fn:round(xs:float(\"0.11\"))", "S:0"),
        ("fn-round/K2-RoundFunc-48", "fn:round(xs:double(\"0.11\"))", "S:0"),
        ("fn-round/K2-RoundFunc-49", "fn:round(xs:float(\"-0.11\"))", "S:-0"),
        (
            "fn-round/K2-RoundFunc-5",
            "round(xs:positiveInteger(xs:untypedAtomic(\"1\"))) instance of xs:positiveInteger",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-50", "fn:round(xs:double(\"-0.11\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-51", "fn:round(xs:float(\"0.14\"))", "S:0"),
        ("fn-round/K2-RoundFunc-52", "fn:round(xs:double(\"0.14\"))", "S:0"),
        ("fn-round/K2-RoundFunc-53", "fn:round(xs:float(\"-0.14\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-54", "fn:round(xs:double(\"-0.14\"))", "Y:xs:double\u001fS:-0"),
        ("fn-round/K2-RoundFunc-55", "fn:round(xs:float(\"0.15\"))", "S:0"),
        ("fn-round/K2-RoundFunc-56", "fn:round(xs:double(\"0.15\"))", "S:0"),
        ("fn-round/K2-RoundFunc-57", "fn:round(xs:float(\"-0.15\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-58", "fn:round(xs:double(\"-0.15\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-59", "fn:round(xs:float(\"0.16\"))", "S:0"),
        (
            "fn-round/K2-RoundFunc-6",
            "round(xs:nonPositiveInteger(xs:untypedAtomic(\"0\"))) instance of xs:nonPositiveInteger",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-60", "fn:round(xs:double(\"0.16\"))", "S:0"),
        ("fn-round/K2-RoundFunc-61", "fn:round(xs:float(\"-0.16\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-62", "fn:round(xs:double(\"-0.16\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-63", "fn:round(xs:float(\"0.19\"))", "S:0"),
        ("fn-round/K2-RoundFunc-64", "fn:round(xs:double(\"0.19\"))", "S:0"),
        ("fn-round/K2-RoundFunc-65", "fn:round(xs:float(\"-0.19\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-66", "fn:round(xs:double(\"-0.19\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-67", "fn:round(xs:float(\"0.10\"))", "S:0"),
        ("fn-round/K2-RoundFunc-68", "fn:round(xs:double(\"0.10\"))", "S:0"),
        ("fn-round/K2-RoundFunc-69", "fn:round(xs:float(\"-0.10\"))", "S:-0"),
        (
            "fn-round/K2-RoundFunc-7",
            "round(xs:nonNegativeInteger(xs:untypedAtomic(\"0\"))) instance of xs:nonNegativeInteger",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-70", "fn:round(xs:double(\"-0.10\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-71", "fn:round(xs:float(\"0.41\"))", "S:0"),
        ("fn-round/K2-RoundFunc-72", "fn:round(xs:double(\"0.41\"))", "S:0"),
        ("fn-round/K2-RoundFunc-73", "fn:round(xs:float(\"-0.41\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-74", "fn:round(xs:double(\"-0.41\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-75", "fn:round(xs:float(\"0.44\"))", "S:0"),
        ("fn-round/K2-RoundFunc-76", "fn:round(xs:double(\"0.44\"))", "S:0"),
        ("fn-round/K2-RoundFunc-77", "fn:round(xs:float(\"-0.44\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-78", "fn:round(xs:double(\"-0.44\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-79", "fn:round(xs:float(\"0.45\"))", "S:0"),
        (
            "fn-round/K2-RoundFunc-8",
            "round(xs:negativeInteger(xs:untypedAtomic(\"-1\"))) instance of xs:negativeInteger",
            "F\tT"
        ),
        ("fn-round/K2-RoundFunc-80", "fn:round(xs:double(\"0.45\"))", "S:0"),
        ("fn-round/K2-RoundFunc-81", "fn:round(xs:float(\"-0.45\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-82", "fn:round(xs:double(\"-0.45\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-83", "fn:round(xs:float(\"0.46\"))", "S:0"),
        ("fn-round/K2-RoundFunc-84", "fn:round(xs:double(\"0.46\"))", "S:0"),
        ("fn-round/K2-RoundFunc-85", "fn:round(xs:float(\"-0.46\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-86", "fn:round(xs:double(\"-0.46\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-87", "fn:round(xs:float(\"0.49\"))", "S:0"),
        ("fn-round/K2-RoundFunc-88", "fn:round(xs:double(\"0.49\"))", "S:0"),
        ("fn-round/K2-RoundFunc-89", "fn:round(xs:float(\"-0.49\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-9", "round(xs:long(xs:untypedAtomic(\"0\"))) instance of xs:long", "F\tT"),
        ("fn-round/K2-RoundFunc-90", "fn:round(xs:double(\"-0.49\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-91", "fn:round(xs:float(\"0.40\"))", "S:0"),
        ("fn-round/K2-RoundFunc-92", "fn:round(xs:double(\"0.40\"))", "S:0"),
        ("fn-round/K2-RoundFunc-93", "fn:round(xs:float(\"-0.40\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-94", "fn:round(xs:double(\"-0.40\"))", "S:-0"),
        ("fn-round/K2-RoundFunc-95", "fn:round(xs:float(\"0.51\"))", "S:1"),
        ("fn-round/K2-RoundFunc-96", "fn:round(xs:double(\"0.51\"))", "S:1"),
        ("fn-round/K2-RoundFunc-97", "fn:round(xs:float(\"-0.51\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-98", "fn:round(xs:double(\"-0.51\"))", "E:-1"),
        ("fn-round/K2-RoundFunc-99", "fn:round(xs:float(\"0.54\"))", "E:1"),
        ("fn-round/fn-round-decimal-2", "fn:round(xs:decimal(\"12.1\"))", "E:12\u001fY:xs:decimal"),
        ("fn-round/fn-round-decimal-3", "fn:round(xs:decimal(\"12.7\"))", "E:13\u001fY:xs:decimal"),
        ("fn-round/fn-round-decimal-4", "fn:round(xs:decimal(\"12.5\"))", "E:13\u001fY:xs:decimal"),
        ("fn-round/fn-round-decimal-5", "fn:round(xs:decimal(\"-12.7\"))", "E:-13\u001fY:xs:decimal"),
        ("fn-round/fn-round-decimal-6", "fn:round(xs:decimal(\"-12.1\"))", "E:-12\u001fY:xs:decimal"),
        ("fn-round/fn-round-decimal-7", "fn:round(xs:decimal(\"-12.5\"))", "E:-12\u001fY:xs:decimal"),
        (
            "fn-round/fn-rounddbl1args-1",
            "fn:round(xs:double(\"-1.7976931348623157E308\"))",
            "E:-1.7976931348623157E308"
        ),
        ("fn-round/fn-rounddbl1args-2", "fn:round(xs:double(\"0\"))", "E:0\u001fY:xs:double"),
        ("fn-round/fn-rounddbl1args-3", "fn:round(xs:double(\"1.7976931348623157E308\"))", "E:1.7976931348623157E308"),
        ("fn-round/fn-rounddec1args-1", "fn:round(xs:decimal(\"-999999999999999999\"))", "E:-999999999999999999"),
        (
            "fn-round/fn-rounddec1args-2",
            "fn:round(xs:decimal(\"617375191608514839\"))",
            "E:617375191608514839\u001fY:xs:decimal"
        ),
        ("fn-round/fn-rounddec1args-3", "fn:round(xs:decimal(\"999999999999999999\"))", "E:999999999999999999"),
        ("fn-round/fn-roundflt1args-1", "fn:round(xs:float(\"-3.4028235E38\"))", "E:xs:float(\"-3.4028235E38\")"),
        ("fn-round/fn-roundflt1args-2", "fn:round(xs:float(\"0\"))", "Y:xs:float\u001fE:0"),
        ("fn-round/fn-roundflt1args-3", "fn:round(xs:float(\"3.4028235E38\"))", "E:xs:float(\"3.4028235E38\")"),
        ("fn-round/fn-roundint1args-1", "fn:round(xs:int(\"-2147483648\"))", "E:-2147483648\u001fY:xs:integer"),
        ("fn-round/fn-roundint1args-2", "fn:round(xs:int(\"-1873914410\"))", "E:-1873914410"),
        ("fn-round/fn-roundint1args-3", "fn:round(xs:int(\"2147483647\"))", "E:2147483647"),
        ("fn-round/fn-roundintg1args-1", "fn:round(xs:integer(\"-999999999999999999\"))", "E:-999999999999999999"),
        ("fn-round/fn-roundintg1args-2", "fn:round(xs:integer(\"830993497117024304\"))", "E:830993497117024304"),
        ("fn-round/fn-roundintg1args-3", "fn:round(xs:integer(\"999999999999999999\"))", "E:999999999999999999"),
        ("fn-round/fn-roundlng1args-1", "fn:round(xs:long(\"-92233720368547758\"))", "E:-92233720368547758"),
        ("fn-round/fn-roundlng1args-2", "fn:round(xs:long(\"-47175562203048468\"))", "E:-47175562203048468"),
        ("fn-round/fn-roundlng1args-3", "fn:round(xs:long(\"92233720368547758\"))", "E:92233720368547758"),
        (
            "fn-round/fn-roundnint1args-1",
            "fn:round(xs:negativeInteger(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-round/fn-roundnint1args-2",
            "fn:round(xs:negativeInteger(\"-297014075999096793\"))",
            "E:-297014075999096793"
        ),
        ("fn-round/fn-roundnint1args-3", "fn:round(xs:negativeInteger(\"-1\"))", "E:-1"),
        ("fn-round/fn-roundnni1args-1", "fn:round(xs:nonNegativeInteger(\"0\"))", "E:0"),
        (
            "fn-round/fn-roundnni1args-2",
            "fn:round(xs:nonNegativeInteger(\"303884545991464527\"))",
            "E:303884545991464527"
        ),
        (
            "fn-round/fn-roundnni1args-3",
            "fn:round(xs:nonNegativeInteger(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        (
            "fn-round/fn-roundnpi1args-1",
            "fn:round(xs:nonPositiveInteger(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-round/fn-roundnpi1args-2",
            "fn:round(xs:nonPositiveInteger(\"-475688437271870490\"))",
            "E:-475688437271870490"
        ),
        ("fn-round/fn-roundnpi1args-3", "fn:round(xs:nonPositiveInteger(\"0\"))", "E:0"),
        ("fn-round/fn-roundpint1args-1", "fn:round(xs:positiveInteger(\"1\"))", "E:1"),
        ("fn-round/fn-roundpint1args-2", "fn:round(xs:positiveInteger(\"52704602390610033\"))", "E:52704602390610033"),
        (
            "fn-round/fn-roundpint1args-3",
            "fn:round(xs:positiveInteger(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        ("fn-round/fn-roundsht1args-1", "fn:round(xs:short(\"-32768\"))", "E:-32768"),
        ("fn-round/fn-roundsht1args-2", "fn:round(xs:short(\"-5324\"))", "E:-5324"),
        ("fn-round/fn-roundsht1args-3", "fn:round(xs:short(\"32767\"))", "E:32767"),
        ("fn-round/fn-roundulng1args-1", "fn:round(xs:unsignedLong(\"0\"))", "E:0"),
        ("fn-round/fn-roundulng1args-2", "fn:round(xs:unsignedLong(\"130747108607674654\"))", "E:130747108607674654"),
        ("fn-round/fn-roundulng1args-3", "fn:round(xs:unsignedLong(\"184467440737095516\"))", "E:184467440737095516"),
        ("fn-round/fn-roundusht1args-1", "fn:round(xs:unsignedShort(\"0\"))", "E:0"),
        ("fn-round/fn-roundusht1args-2", "fn:round(xs:unsignedShort(\"44633\"))", "E:44633"),
        ("fn-round/fn-roundusht1args-3", "fn:round(xs:unsignedShort(\"65535\"))", "E:65535"),
        ("fn-round-half-to-even/K-RoundEvenFunc-1", "round-half-to-even()", "X:XPST0017"),
        ("fn-round-half-to-even/K-RoundEvenFunc-2", "round-half-to-even(1.1, 3, \"wrong param\")", "X:XPST0017"),
        ("fn-round-half-to-even/K-RoundEvenFunc-3", "empty(round-half-to-even(()))", "T"),
        ("fn-round-half-to-even/K-RoundEvenFunc-4", "empty(round-half-to-even((), 3))", "T"),
        ("fn-round-half-to-even/K-RoundEvenFunc-5", "round-half-to-even(\"a string\")", "X:XPTY0004"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-1", "round-half-to-even(1) eq 1", "T"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-10", "round-half-to-even(xs:float(\"-0\"))", "E:-0"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-11", "round-half-to-even(xs:double(\"NaN\"))", "S:NaN"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-12", "round-half-to-even(xs:float(\"NaN\"))", "S:NaN"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-13", "round-half-to-even(xs:double(\"-INF\"))", "S:-INF"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-14", "round-half-to-even(xs:float(\"-INF\"))", "S:-INF"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-15", "round-half-to-even(xs:double(\"INF\"))", "S:INF"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-16", "round-half-to-even(xs:float(\"INF\"))", "S:INF"),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-17",
            "round-half-to-even(xs:unsignedShort(\"0\")) instance of xs:unsignedShort",
            "F\tT"
        ),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-18",
            "round-half-to-even(xs:unsignedLong(\"0\")) instance of xs:unsignedLong",
            "F\tT"
        ),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-19",
            "round-half-to-even(xs:unsignedInt(\"0\")) instance of xs:unsignedInt",
            "F\tT"
        ),
        ("fn-round-half-to-even/K2-RoundEvenFunc-2", "round-half-to-even(1.0) eq 1", "T"),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-20",
            "round-half-to-even(xs:unsignedByte(\"0\")) instance of xs:unsignedByte",
            "F\tT"
        ),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-21",
            "round-half-to-even(xs:positiveInteger(\"1\")) instance of xs:positiveInteger",
            "F\tT"
        ),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-22",
            "round-half-to-even(xs:nonPositiveInteger(\"0\")) instance of xs:nonPositiveInteger",
            "F\tT"
        ),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-23",
            "round-half-to-even(xs:nonNegativeInteger(\"0\")) instance of xs:nonNegativeInteger",
            "F\tT"
        ),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-24",
            "round-half-to-even(xs:negativeInteger(\"-1\")) instance of xs:negativeInteger",
            "F\tT"
        ),
        ("fn-round-half-to-even/K2-RoundEvenFunc-25", "round-half-to-even(xs:long(\"0\")) instance of xs:long", "F\tT"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-26", "round-half-to-even(xs:int(\"0\")) instance of xs:int", "F\tT"),
        (
            "fn-round-half-to-even/K2-RoundEvenFunc-27",
            "round-half-to-even(xs:short(\"0\")) instance of xs:short",
            "F\tT"
        ),
        ("fn-round-half-to-even/K2-RoundEvenFunc-28", "round-half-to-even(xs:byte(\"0\")) instance of xs:byte", "F\tT"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-3", "round-half-to-even(0.5) eq 0", "T"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-4", "round-half-to-even(1.5) eq 2", "T"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-5", "round-half-to-even(2.5) eq 2", "T"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-6", "round-half-to-even(3.567812E+3, 2) eq 3567.81E0", "T"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-7", "round-half-to-even(4.7564E-3, 2) eq 0.0E0", "T"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-8", "round-half-to-even(35612.25, -2) eq 35600", "T"),
        ("fn-round-half-to-even/K2-RoundEvenFunc-9", "round-half-to-even(xs:double(\"-0\"))", "E:-0"),
        (
            "fn-round-half-to-even/cbcl-round-half-to-even-001",
            "fn:round-half-to-even(3.567812E+3, 4294967296)",
            "S:3567.812"
        ),
        ("fn-round-half-to-even/cbcl-round-half-to-even-002", "fn:round-half-to-even(12345, 2)", "S:12345"),
        ("fn-round-half-to-even/cbcl-round-half-to-even-003", "fn:round-half-to-even(12345, -2)", "S:12300"),
        ("fn-round-half-to-even/cbcl-round-half-to-even-004", "fn:round-half-to-even(xs:short(12345), 2)", "S:12345"),
        ("fn-round-half-to-even/cbcl-round-half-to-even-005", "fn:round-half-to-even(xs:short(12345), -2)", "S:12300"),
        ("fn-round-half-to-even/cbcl-round-half-to-even-006", "fn:round-half-to-even(12345.6, 2)", "S:12345.6"),
        ("fn-round-half-to-even/cbcl-round-half-to-even-007", "fn:round-half-to-even(12345.6, -2)", "S:12300"),
        (
            "fn-round-half-to-even/cbcl-round-half-to-even-008",
            "fn:round-half-to-even(xs:float(12345.6), 2)",
            "S:12345.6"
        ),
        (
            "fn-round-half-to-even/cbcl-round-half-to-even-009",
            "fn:round-half-to-even(xs:float(12345.6), -2)",
            "S:12300"
        ),
        ("fn-round-half-to-even/cbcl-round-half-to-even-010", "fn:round-half-to-even(123456e-2, 2)", "S:1234.56"),
        ("fn-round-half-to-even/cbcl-round-half-to-even-011", "fn:round-half-to-even(123456e-2, -2)", "S:1200"),
        (
            "fn-round-half-to-even/cbcl-round-half-to-even-012",
            "fn:round-half-to-even(3.567812, 4294967296)",
            "S:3.567812"
        ),
        ("fn-round-half-to-even/cbcl-round-half-to-even-013", "fn:boolean(fn:round-half-to-even(5, 1))", "T"),
        (
            "fn-round-half-to-even/cbcl-round-half-to-even-014",
            "fn:round-half-to-even(123456e-2, \"two\")",
            "X:XPTY0004"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-10",
            "round-half-to-even(4561.234567, 0)",
            "E:4561\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-11",
            "round-half-to-even(4561.000005e0, 0)",
            "E:4561\u001fY:xs:double"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-12",
            "round-half-to-even(xs:float('4561.000005e0'), 0)",
            "E:4561\u001fY:xs:float"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-13",
            "round-half-to-even(4561234567, 0)",
            "E:4561234567\u001fY:xs:integer"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-14",
            "round-half-to-even(xs:float('0.05'), 1)",
            "E:xs:float(0.1)\u001fY:xs:float"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-15",
            "round-half-to-even(xs:float('-0.05'), 1)",
            "E:xs:float(-0.1)\u001fY:xs:float"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-16",
            "round-half-to-even(xs:float('3.75'), 1)",
            "E:xs:float(3.8)\u001fY:xs:float"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-17",
            "round-half-to-even(123.355, 2)",
            "E:123.36\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-18",
            "round-half-to-even(123.365, 2)",
            "E:123.36\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-19",
            "round-half-to-even(123.375, 2)",
            "E:123.38\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-20",
            "round-half-to-even(123.385, 2)",
            "E:123.38\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-21",
            "round-half-to-even(-123.355, 2)",
            "E:-123.36\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-22",
            "round-half-to-even(-123.365, 2)",
            "E:-123.36\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-23",
            "round-half-to-even(-123.375, 2)",
            "E:-123.38\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-24",
            "round-half-to-even(-123.385, 2)",
            "E:-123.38\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-24a",
            "round-half-to-even(12350.00, -2)",
            "E:12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-25",
            "round-half-to-even(-12350.00, -2)",
            "E:-12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-26",
            "round-half-to-even(12450.00, -2)",
            "E:12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-27",
            "round-half-to-even(-12450.00, -2)",
            "E:-12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-28",
            "round-half-to-even(12550.00, -2)",
            "E:12600\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-29",
            "round-half-to-even(-12550.00, -2)",
            "E:-12600\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-3",
            "round-half-to-even(1.234567, 2)",
            "E:1.23\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-30",
            "round-half-to-even(12350, -2)",
            "E:12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-31",
            "round-half-to-even(-12350, -2)",
            "E:-12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-32",
            "round-half-to-even(12450, -2)",
            "E:12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-33",
            "round-half-to-even(-12450, -2)",
            "E:-12400\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-34",
            "round-half-to-even(12550, -2)",
            "E:12600\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-35",
            "round-half-to-even(-12550, -2)",
            "E:-12600\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-36",
            "round-half-to-even(12350e0, -2)",
            "E:12400\u001fY:xs:double"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-37",
            "round-half-to-even(-12350e0, -2)",
            "E:-12400\u001fY:xs:double"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-38",
            "round-half-to-even(12450e0, -2)",
            "E:12400\u001fY:xs:double"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-39",
            "round-half-to-even(-12450e0, -2)",
            "E:-12400\u001fY:xs:double"
        ),
        ("fn-round-half-to-even/fn-round-half-to-even-4", "round-half-to-even(1.000005e0, 2)", "E:1\u001fY:xs:double"),
        (
            "fn-round-half-to-even/fn-round-half-to-even-40",
            "round-half-to-even(12550e0, -2)",
            "E:12600\u001fY:xs:double"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-41",
            "round-half-to-even(-12550e0, -2)",
            "E:-12600\u001fY:xs:double"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-5",
            "round-half-to-even(xs:float('1.000005e0'), 2)",
            "E:1\u001fY:xs:float"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-6",
            "round-half-to-even(4561.234567, -2)",
            "E:4600\u001fY:xs:decimal"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-7",
            "round-half-to-even(4561.000005e0, -2)",
            "E:4600\u001fY:xs:double"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-8",
            "round-half-to-even(xs:float('4561.000005e0'), -2)",
            "E:4600\u001fY:xs:float"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-9",
            "round-half-to-even(4561234567, -2)",
            "E:4561234600\u001fY:xs:integer"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evendbl1args-1",
            "fn:round-half-to-even(xs:double(\"-1.7976931348623157E308\"))",
            "E:-1.7976931348623157E308\tX:FOCA0001"
        ),
        ("fn-round-half-to-even/fn-round-half-to-evendbl1args-2", "fn:round-half-to-even(xs:double(\"0\"))", "E:0"),
        (
            "fn-round-half-to-even/fn-round-half-to-evendbl1args-3",
            "fn:round-half-to-even(xs:double(\"1.7976931348623157E308\"))",
            "E:1.7976931348623157E308\tX:FOCA0001"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evendec1args-1",
            "fn:round-half-to-even(xs:decimal(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evendec1args-2",
            "fn:round-half-to-even(xs:decimal(\"617375191608514839\"))",
            "E:617375191608514839"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evendec1args-3",
            "fn:round-half-to-even(xs:decimal(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenflt1args-1",
            "fn:round-half-to-even(xs:float(\"-3.4028235E38\"))",
            "E:xs:float(\"-3.4028235E38\")\tX:FOCA0001"
        ),
        ("fn-round-half-to-even/fn-round-half-to-evenflt1args-2", "fn:round-half-to-even(xs:float(\"0\"))", "E:0"),
        (
            "fn-round-half-to-even/fn-round-half-to-evenflt1args-3",
            "fn:round-half-to-even(xs:float(\"3.4028235E38\"))",
            "E:xs:float(\"3.4028235E38\")\tX:FOCA0001"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenint1args-1",
            "fn:round-half-to-even(xs:int(\"-2147483648\"))",
            "E:-2147483648"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenint1args-2",
            "fn:round-half-to-even(xs:int(\"-1873914410\"))",
            "E:-1873914410"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenint1args-3",
            "fn:round-half-to-even(xs:int(\"2147483647\"))",
            "E:2147483647"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenintg1args-1",
            "fn:round-half-to-even(xs:integer(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenintg1args-2",
            "fn:round-half-to-even(xs:integer(\"830993497117024304\"))",
            "E:830993497117024304"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenintg1args-3",
            "fn:round-half-to-even(xs:integer(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenlng1args-1",
            "fn:round-half-to-even(xs:long(\"-92233720368547758\"))",
            "E:-92233720368547758"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenlng1args-2",
            "fn:round-half-to-even(xs:long(\"-47175562203048468\"))",
            "E:-47175562203048468"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenlng1args-3",
            "fn:round-half-to-even(xs:long(\"92233720368547758\"))",
            "E:92233720368547758"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennint1args-1",
            "fn:round-half-to-even(xs:negativeInteger(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennint1args-2",
            "fn:round-half-to-even(xs:negativeInteger(\"-297014075999096793\"))",
            "E:-297014075999096793"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennint1args-3",
            "fn:round-half-to-even(xs:negativeInteger(\"-1\"))",
            "E:-1"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennni1args-1",
            "fn:round-half-to-even(xs:nonNegativeInteger(\"0\"))",
            "E:0"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennni1args-2",
            "fn:round-half-to-even(xs:nonNegativeInteger(\"303884545991464527\"))",
            "E:303884545991464527"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennni1args-3",
            "fn:round-half-to-even(xs:nonNegativeInteger(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennpi1args-1",
            "fn:round-half-to-even(xs:nonPositiveInteger(\"-999999999999999999\"))",
            "E:-999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennpi1args-2",
            "fn:round-half-to-even(xs:nonPositiveInteger(\"-475688437271870490\"))",
            "E:-475688437271870490"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evennpi1args-3",
            "fn:round-half-to-even(xs:nonPositiveInteger(\"0\"))",
            "E:0"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenpint1args-1",
            "fn:round-half-to-even(xs:positiveInteger(\"1\"))",
            "E:1"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenpint1args-2",
            "fn:round-half-to-even(xs:positiveInteger(\"52704602390610033\"))",
            "E:52704602390610033"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenpint1args-3",
            "fn:round-half-to-even(xs:positiveInteger(\"999999999999999999\"))",
            "E:999999999999999999"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evensht1args-1",
            "fn:round-half-to-even(xs:short(\"-32768\"))",
            "E:-32768"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evensht1args-2",
            "fn:round-half-to-even(xs:short(\"-5324\"))",
            "E:-5324"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evensht1args-3",
            "fn:round-half-to-even(xs:short(\"32767\"))",
            "E:32767"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenulng1args-1",
            "fn:round-half-to-even(xs:unsignedLong(\"0\"))",
            "E:0"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenulng1args-2",
            "fn:round-half-to-even(xs:unsignedLong(\"130747108607674654\"))",
            "E:130747108607674654"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenulng1args-3",
            "fn:round-half-to-even(xs:unsignedLong(\"184467440737095516\"))",
            "E:184467440737095516"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenusht1args-1",
            "fn:round-half-to-even(xs:unsignedShort(\"0\"))",
            "E:0"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenusht1args-2",
            "fn:round-half-to-even(xs:unsignedShort(\"44633\"))",
            "E:44633"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-evenusht1args-3",
            "fn:round-half-to-even(xs:unsignedShort(\"65535\"))",
            "E:65535"
        ),
        ("fn-number/K-NodeNumberFunc-1", "number(1, 2)", "X:XPST0017"),
        ("fn-number/K-NodeNumberFunc-10", "fn:number(()) instance of xs:double", "T"),
        ("fn-number/K-NodeNumberFunc-11", "not(fn:number(()))", "T"),
        ("fn-number/K-NodeNumberFunc-12", "string(number(xs:anyURI(\"example.com/\"))) eq \"NaN\"", "T"),
        ("fn-number/K-NodeNumberFunc-13", "string(number(xs:anyURI(\"1\"))) eq \"NaN\"", "T"),
        ("fn-number/K-NodeNumberFunc-14", "string(number(\"1\")) eq \"1\"", "T"),
        ("fn-number/K-NodeNumberFunc-15", "string(number(xs:gYear(\"2005\"))) eq \"NaN\"", "T"),
        ("fn-number/K-NodeNumberFunc-2", "fn:number(1) eq 1", "T"),
        ("fn-number/K-NodeNumberFunc-3", "not(fn:number(\"results in NaN\"))", "T"),
        ("fn-number/K-NodeNumberFunc-4", "fn:number(\"results in NaN\") instance of xs:double", "T"),
        ("fn-number/K-NodeNumberFunc-5", "fn:number(1) instance of xs:double", "T"),
        ("fn-number/K-NodeNumberFunc-6", "fn:number(1.1) instance of xs:double", "T"),
        ("fn-number/K-NodeNumberFunc-7", "fn:number(xs:float(3)) instance of xs:double", "T"),
        ("fn-number/K-NodeNumberFunc-8", "fn:number(xs:double(3)) instance of xs:double", "T"),
        ("fn-number/K-NodeNumberFunc-9", "fn:number(\"NaN\") instance of xs:double", "T"),
        ("fn-number/fn-number-1", "fn:number(())", "S:NaN"),
        ("fn-number/fn-number-10", "number(xs:untypedAtomic(\"1000\"))", "Y:xs:double\u001fE:1e3"),
        ("fn-number/fn-number-2", "fn:number(\"A String\")", "S:NaN"),
        ("fn-number/fn-number-3", "fn:number()", "X:XPDY0002"),
        ("fn-number/fn-number-5", "number(true())", "Y:xs:double\u001fE:1.0e0"),
        ("fn-number/fn-number-6", "number(false())", "Y:xs:double\u001fE:0.0e0"),
        ("fn-number/fn-number-7", "number(implicit-timezone())", "Y:xs:double\u001fS:NaN"),
        ("fn-number/fn-number-8", "number(\"  -22e0  \")", "Y:xs:double\u001fE:-22e0"),
        ("fn-number/fn-number-9", "number(\"+22e0\")", "Y:xs:double\u001fE:2.2e1"),
        (
            "fn-number/fn-numberdbl1args-1",
            "fn:number(xs:double(\"-1.7976931348623157E308\"))",
            "S:-1.7976931348623157E308"
        ),
        ("fn-number/fn-numberdbl1args-2", "fn:number(xs:double(\"0\"))", "S:0"),
        (
            "fn-number/fn-numberdbl1args-3",
            "fn:number(xs:double(\"1.7976931348623157E308\"))",
            "S:1.7976931348623157E308"
        ),
        ("fn-number/fn-numberdec1args-1", "fn:number(xs:decimal(\"-999999999999999999\"))", "S:-1.0E18"),
        ("fn-number/fn-numberdec1args-2", "fn:number(xs:decimal(\"617375191608514839\")) eq 617375191608514839", "T"),
        ("fn-number/fn-numberdec1args-3", "fn:number(xs:decimal(\"999999999999999999\"))", "S:1.0E18"),
        ("fn-number/fn-numberflt1args-1", "fn:number(xs:float(\"-3.4028235E38\")) eq -3.4028234663852885E38", "T"),
        ("fn-number/fn-numberflt1args-2", "fn:number(xs:float(\"0\"))", "S:0"),
        (
            "fn-number/fn-numberflt1args-3",
            "fn:number(xs:float(\"3.4028235E38\"))",
            "S:3.4028234663852885E38\tS:3.402823466385289E38\tS:3.4028234663852886E38"
        ),
        ("fn-number/fn-numberint1args-1", "fn:number(xs:int(\"-2147483648\"))", "S:-2.147483648E9"),
        ("fn-number/fn-numberint1args-2", "fn:number(xs:int(\"-1873914410\"))", "S:-1.87391441E9"),
        ("fn-number/fn-numberint1args-3", "fn:number(xs:int(\"2147483647\"))", "S:2.147483647E9"),
        ("fn-number/fn-numberintg1args-1", "fn:number(xs:integer(\"-999999999999999999\"))", "S:-1.0E18"),
        ("fn-number/fn-numberintg1args-2", "fn:number(xs:integer(\"830993497117024304\")) eq 830993497117024304", "T"),
        ("fn-number/fn-numberintg1args-3", "fn:number(xs:integer(\"999999999999999999\"))", "S:1.0E18"),
        ("fn-number/fn-numberlng1args-1", "fn:number(xs:long(\"-92233720368547758\"))", "S:-9.223372036854776E16"),
        ("fn-number/fn-numberlng1args-2", "fn:number(xs:long(\"-47175562203048468\")) eq -47175562203048468", "T"),
        ("fn-number/fn-numberlng1args-3", "fn:number(xs:long(\"92233720368547758\"))", "S:9.223372036854776E16"),
        ("fn-number/fn-numbernint1args-1", "fn:number(xs:negativeInteger(\"-999999999999999999\"))", "S:-1.0E18"),
        (
            "fn-number/fn-numbernint1args-2",
            "fn:number(xs:negativeInteger(\"-297014075999096793\")) eq -297014075999096793",
            "T"
        ),
        ("fn-number/fn-numbernint1args-3", "fn:number(xs:negativeInteger(\"-1\"))", "S:-1"),
        ("fn-number/fn-numbernni1args-1", "fn:number(xs:nonNegativeInteger(\"0\"))", "S:0"),
        (
            "fn-number/fn-numbernni1args-2",
            "fn:number(xs:nonNegativeInteger(\"303884545991464527\")) eq 303884545991464527",
            "T"
        ),
        ("fn-number/fn-numbernni1args-3", "fn:number(xs:nonNegativeInteger(\"999999999999999999\"))", "S:1.0E18"),
        ("fn-number/fn-numbernpi1args-1", "fn:number(xs:nonPositiveInteger(\"-999999999999999999\"))", "S:-1.0E18"),
        (
            "fn-number/fn-numbernpi1args-2",
            "fn:number(xs:nonPositiveInteger(\"-475688437271870490\"))",
            "S:-4.7568843727187049E17\tS:-4.756884372718705E17\tS:-4.7568843727187046E17"
        ),
        ("fn-number/fn-numbernpi1args-3", "fn:number(xs:nonPositiveInteger(\"0\"))", "S:0"),
        ("fn-number/fn-numberpint1args-1", "fn:number(xs:positiveInteger(\"1\"))", "S:1"),
        (
            "fn-number/fn-numberpint1args-2",
            "fn:number(xs:positiveInteger(\"52704602390610033\")) eq 52704602390610033",
            "T"
        ),
        ("fn-number/fn-numberpint1args-3", "fn:number(xs:positiveInteger(\"999999999999999999\"))", "S:1.0E18"),
        ("fn-number/fn-numbersht1args-1", "fn:number(xs:short(\"-32768\"))", "S:-32768"),
        ("fn-number/fn-numbersht1args-2", "fn:number(xs:short(\"-5324\"))", "S:-5324"),
        ("fn-number/fn-numbersht1args-3", "fn:number(xs:short(\"32767\"))", "S:32767"),
        ("fn-number/fn-numberulng1args-1", "fn:number(xs:unsignedLong(\"0\"))", "S:0"),
        (
            "fn-number/fn-numberulng1args-2",
            "fn:number(xs:unsignedLong(\"130747108607674654\"))",
            "S:1.3074710860767466E17\tS:1.30747108607674648E17\tS:1.30747108607674649E17\tS:1.3074710860767465E17\tS:1.30747108607674651E17\tS:1.30747108607674652E17\tS:1.30747108607674653E17\tS:1.30747108607674654E17\tS:1.30747108607674655E17\tS:1.30747108607674656E17\tS:1.30747108607674657E17\tS:1.30747108607674658E17\tS:1.30747108607674659E17\tS:1.30747108607674661E17\tS:1.30747108607674662E17\tS:1.30747108607674663E17\tS:1.30747108607674664E17"
        ),
        (
            "fn-number/fn-numberulng1args-3",
            "fn:number(xs:unsignedLong(\"184467440737095516\")) eq 1.8446744073709551E17",
            "T"
        ),
        ("fn-number/fn-numberusht1args-1", "fn:number(xs:unsignedShort(\"0\"))", "S:0"),
        ("fn-number/fn-numberusht1args-2", "fn:number(xs:unsignedShort(\"44633\"))", "S:44633"),
        ("fn-number/fn-numberusht1args-3", "fn:number(xs:unsignedShort(\"65535\"))", "S:65535"),
        ("fn-true/K-TrueFunc-1", "true(1)", "X:XPST0017"),
        ("fn-true/K-TrueFunc-2", "true() eq true()", "T"),
        ("fn-true/K-TrueFunc-3", "true()", "T"),
        ("fn-true/fn-true-1", "fn:true()", "T\u001fY:xs:boolean"),
        ("fn-true/fn-true-10", "fn:true() ge fn:true()", "T"),
        ("fn-true/fn-true-11", "fn:true() = fn:true()", "T"),
        ("fn-true/fn-true-12", "fn:true() != fn:true()", "F"),
        ("fn-true/fn-true-13", "fn:true() < fn:true()", "F"),
        ("fn-true/fn-true-14", "fn:true() <= fn:true()", "T"),
        ("fn-true/fn-true-15", "fn:true() > fn:true()", "F"),
        ("fn-true/fn-true-16", "fn:true() >= fn:true()", "T"),
        ("fn-true/fn-true-17", "xs:boolean(fn:true())", "T"),
        ("fn-true/fn-true-18", "fn:string(fn:true())", "E:\"true\""),
        ("fn-true/fn-true-19", "fn:concat(xs:string(fn:true()),xs:string(fn:true()))", "S:truetrue"),
        ("fn-true/fn-true-2", "fn:not(fn:true())", "F"),
        ("fn-true/fn-true-20", "fn:contains(xs:string(fn:true()),xs:string(fn:true()))", "T"),
        ("fn-true/fn-true-21", "fn:string-length(xs:string(fn:true()))", "S:4"),
        ("fn-true/fn-true-5", "fn:true() eq fn:true()", "T"),
        ("fn-true/fn-true-6", "fn:true() ne fn:true()", "F"),
        ("fn-true/fn-true-7", "fn:true() lt fn:true()", "F"),
        ("fn-true/fn-true-8", "fn:true() le fn:true()", "T"),
        ("fn-true/fn-true-9", "fn:true() gt fn:true()", "F"),
        ("fn-false/K-FalseFunc-1", "false(1)", "X:XPST0017"),
        ("fn-false/K-FalseFunc-2", "false() eq false()", "T"),
        ("fn-false/K-FalseFunc-3", "not(false())", "T"),
        ("fn-false/fn-false-1", "fn:false()", "F\u001fY:xs:boolean"),
        ("fn-false/fn-false-10", "fn:false() ge fn:false()", "T"),
        ("fn-false/fn-false-11", "fn:false() = fn:false()", "T"),
        ("fn-false/fn-false-12", "fn:false() != fn:false()", "F"),
        ("fn-false/fn-false-13", "fn:false() < fn:false()", "F"),
        ("fn-false/fn-false-14", "fn:false() <= fn:false()", "T"),
        ("fn-false/fn-false-15", "fn:false() > fn:false()", "F"),
        ("fn-false/fn-false-16", "fn:false() >= fn:false()", "T"),
        ("fn-false/fn-false-17", "xs:boolean(fn:false())", "F"),
        ("fn-false/fn-false-18", "fn:string(fn:false())", "E:\"false\""),
        ("fn-false/fn-false-19", "fn:concat(xs:string(fn:false()),xs:string(fn:false()))", "S:falsefalse"),
        ("fn-false/fn-false-2", "fn:not(fn:false())", "T"),
        ("fn-false/fn-false-20", "fn:contains(xs:string(fn:false()),xs:string(fn:false()))", "T"),
        ("fn-false/fn-false-21", "fn:string-length(xs:string(fn:false()))", "S:5"),
        ("fn-false/fn-false-5", "fn:false() eq fn:false()", "T"),
        ("fn-false/fn-false-6", "fn:false() ne fn:false()", "F"),
        ("fn-false/fn-false-7", "fn:false() lt fn:false()", "F"),
        ("fn-false/fn-false-8", "fn:false() le fn:false()", "T"),
        ("fn-false/fn-false-9", "fn:false() gt fn:false()", "F"),
    ];

    /// <summary>The applicable cases that this selection leaves out, each with the reason and the issue that closes it.</summary>
    public static readonly (string Id, string Reason)[] Exclusions =
    [
        (
            "fn-abs/cbcl-abs-001",
            "the fixture of the sequence group has no lowering for the context item expression or a predicate; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-abs/fn-abs-1",
            "the fixture of the sequence group has no lowering for the for expression, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-ceiling/fn-ceiling-1",
            "the fixture of the sequence group has no lowering for the for expression, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-floor/fn-floor-1",
            "the fixture of the sequence group has no lowering for the for expression, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-round/fn-round-1",
            "the fixture of the sequence group has no lowering for the for expression, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-1",
            "the fixture of the sequence group has no lowering for the for expression, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-round-half-to-even/fn-round-half-to-even-2",
            "the fixture of the sequence group has no lowering for the for expression, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-number/K-NodeNumberFunc-16",
            "the fixture of the sequence group has no lowering for the context item expression or a predicate; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-number/K-NodeNumberFunc-17",
            "the fixture of the sequence group has no lowering for the context item expression or a predicate; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-number/fn-number-4",
            "the fixture of the sequence group has no lowering for the context item expression or a predicate; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-true/fn-true-3",
            "the fixture of the sequence group has no lowering for the logical operators, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-true/fn-true-4",
            "the fixture of the sequence group has no lowering for the logical operators, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-false/fn-false-3",
            "the fixture of the sequence group has no lowering for the logical operators, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-false/fn-false-4",
            "the fixture of the sequence group has no lowering for the logical operators, which #226 evaluates; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
    ];
}
