namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

/// <summary>
/// The selection of the QT3 test sets <c>fn-concat</c>, <c>fn-substring</c>, <c>fn-substring-before</c>, <c>fn-substring-after</c>,
/// <c>fn-contains</c>, <c>fn-starts-with</c>, <c>fn-ends-with</c>, <c>fn-translate</c>, <c>fn-normalize-space</c>,
/// <c>fn-string-length</c>, <c>fn-upper-case</c>, and <c>fn-lower-case</c> (the twelve sets of the work package), and of the four
/// further sets of the string group, <c>fn-string-join</c>, <c>fn-normalize-unicode</c>, <c>fn-codepoints-to-string</c>, and
/// <c>fn-string-to-codepoints</c>. The cases are the ones the XPath 2.0 rule of the QT3 runner selects (the baselines of
/// <c>tests/conformance/baselines/qt3</c>), taken from the pinned commit of the suite (tests/conformance/suites/qt3.json): every one
/// of them is either in <see cref="Cases"/>, where the compiled expression must satisfy its assertion, or in <see cref="Exclusions"/>
/// with the reason and the issue that closes it.
/// </summary>
/// <remarks>
/// <para>
/// Limitation: the QT3 runner of the integration tests still has no evaluator behind it (#295, XP-14), so these cases run through the
/// lowerings of <see cref="Sequences.Qt3SequenceFixture"/> and not through the runner, and the baselines are unchanged. The end-to-end run
/// stays open until #295.
/// </para>
/// <para>
/// Cases per test set, as selected of applicable: concat 91 of 95; substring 42 of 48; substring-before 31 of 35; substring-after 32 of 36; contains 28 of 35; starts-with 28 of 33; ends-with 26 of 32; translate 41 of 42; normalize-space 31 of 35; string-length 26 of 33; upper-case 25 of 29; lower-case 24 of 28; string-join 32 of 34; normalize-unicode 37 of 45; codepoints-to-string 54 of 68; string-to-codepoints 35 of 44. In all, 538 of 672 applicable cases are selected and 134 are excluded.
/// </para>
/// <para>
/// An assertion is written for <see cref="Sequences.Qt3SequenceFixture.Satisfies"/>: <c>T</c> or <c>F</c> for a boolean, <c>S:</c> and
/// <c>N:</c> for a string value (<c>N:</c> with normalized space), <c>E:</c> for one value equal to an expression, <c>D:</c> for a
/// sequence deep-equal to an expression, <c>C:</c> for an item count, and <c>X:</c> for an error code. Alternatives (<c>any-of</c>)
/// are separated by a tab, and assertions that must all hold (<c>all-of</c>) by the unit separator (U+001F).
/// </para>
/// </remarks>
internal static class Qt3StringSelection
{
    public static readonly (string Id, string Expression, string Expectation)[] Cases =
    [
        (
            "fn-concat/fn-concatintg2args-1",
            "fn:concat(xs:integer(\"-999999999999999999\"),xs:integer(\"-999999999999999999\"))",
            "S:-999999999999999999-999999999999999999"
        ),
        (
            "fn-concat/fn-concatintg2args-2",
            "fn:concat(xs:integer(\"830993497117024304\"),xs:integer(\"-999999999999999999\"))",
            "S:830993497117024304-999999999999999999"
        ),
        (
            "fn-concat/fn-concatintg2args-3",
            "fn:concat(xs:integer(\"999999999999999999\"),xs:integer(\"-999999999999999999\"))",
            "S:999999999999999999-999999999999999999"
        ),
        (
            "fn-concat/fn-concatintg2args-4",
            "fn:concat(xs:integer(\"-999999999999999999\"),xs:integer(\"830993497117024304\"))",
            "S:-999999999999999999830993497117024304"
        ),
        (
            "fn-concat/fn-concatintg2args-5",
            "fn:concat(xs:integer(\"-999999999999999999\"),xs:integer(\"999999999999999999\"))",
            "S:-999999999999999999999999999999999999"
        ),
        (
            "fn-concat/fn-concatdec2args-1",
            "fn:concat(xs:decimal(\"-999999999999999999\"),xs:decimal(\"-999999999999999999\"))",
            "S:-999999999999999999-999999999999999999"
        ),
        (
            "fn-concat/fn-concatdec2args-2",
            "fn:concat(xs:decimal(\"617375191608514839\"),xs:decimal(\"-999999999999999999\"))",
            "S:617375191608514839-999999999999999999"
        ),
        (
            "fn-concat/fn-concatdec2args-3",
            "fn:concat(xs:decimal(\"999999999999999999\"),xs:decimal(\"-999999999999999999\"))",
            "S:999999999999999999-999999999999999999"
        ),
        (
            "fn-concat/fn-concatdec2args-4",
            "fn:concat(xs:decimal(\"-999999999999999999\"),xs:decimal(\"617375191608514839\"))",
            "S:-999999999999999999617375191608514839"
        ),
        (
            "fn-concat/fn-concatdec2args-5",
            "fn:concat(xs:decimal(\"-999999999999999999\"),xs:decimal(\"999999999999999999\"))",
            "S:-999999999999999999999999999999999999"
        ),
        (
            "fn-concat/fn-concatdbl2args-1",
            "fn:concat(xs:double(\"-1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\"))",
            "S:-1.7976931348623157E308-1.7976931348623157E308"
        ),
        (
            "fn-concat/fn-concatdbl2args-2",
            "fn:concat(xs:double(\"0\"),xs:double(\"-1.7976931348623157E308\"))",
            "S:0-1.7976931348623157E308"
        ),
        (
            "fn-concat/fn-concatdbl2args-3",
            "fn:concat(xs:double(\"1.7976931348623157E308\"),xs:double(\"-1.7976931348623157E308\"))",
            "S:1.7976931348623157E308-1.7976931348623157E308"
        ),
        (
            "fn-concat/fn-concatdbl2args-4",
            "fn:concat(xs:double(\"-1.7976931348623157E308\"),xs:double(\"0\"))",
            "S:-1.7976931348623157E3080"
        ),
        (
            "fn-concat/fn-concatdbl2args-5",
            "fn:concat(xs:double(\"-1.7976931348623157E308\"),xs:double(\"1.7976931348623157E308\"))",
            "S:-1.7976931348623157E3081.7976931348623157E308"
        ),
        (
            "fn-concat/fn-concatflt2args-1",
            "fn:concat(xs:float(\"-3.4028235E38\"),xs:float(\"-3.4028235E38\"))",
            "S:-3.4028235E38-3.4028235E38"
        ),
        ("fn-concat/fn-concatflt2args-2", "fn:concat(xs:float(\"0\"),xs:float(\"-3.4028235E38\"))", "S:0-3.4028235E38"),
        (
            "fn-concat/fn-concatflt2args-3",
            "fn:concat(xs:float(\"3.4028235E38\"),xs:float(\"-3.4028235E38\"))",
            "S:3.4028235E38-3.4028235E38"
        ),
        ("fn-concat/fn-concatflt2args-4", "fn:concat(xs:float(\"-3.4028235E38\"),xs:float(\"0\"))", "S:-3.4028235E380"),
        (
            "fn-concat/fn-concatflt2args-5",
            "fn:concat(xs:float(\"-3.4028235E38\"),xs:float(\"3.4028235E38\"))",
            "S:-3.4028235E383.4028235E38"
        ),
        ("fn-concat/fn-concat-1", "fn:concat('un', 'grateful')", "S:ungrateful"),
        (
            "fn-concat/fn-concat-2",
            "fn:concat('Thy ', (), 'old ', \"groans\", \"\", ' ring', ' yet', ' in', ' my', ' ancient',' ears.')",
            "S:Thy old groans ring yet in my ancient ears."
        ),
        ("fn-concat/fn-concat-3", "fn:concat('Ciao!',())", "S:Ciao!"),
        (
            "fn-concat/fn-concat-4",
            "fn:concat('Ingratitude, ', 'thou ', 'marble-hearted', ' fiend!')",
            "S:Ingratitude, thou marble-hearted fiend!"
        ),
        ("fn-concat/fn-concat-5", "fn:concat(\"AB\",\"CD\")", "S:ABCD"),
        ("fn-concat/fn-concat-6", "fn:concat(\"abc\",\"de\")", "S:abcde"),
        ("fn-concat/fn-concat-7", "fn:concat(\"ABCDE\",\"abcde\")", "S:ABCDEabcde"),
        ("fn-concat/fn-concat-8", "fn:count(fn:concat(\"\",\"\"))", "S:1"),
        ("fn-concat/fn-concat-9", "fn:concat(fn:upper-case(\"Abc\"),fn:upper-case(\"DH\"))", "S:ABCDH"),
        ("fn-concat/fn-concat-10", "fn:concat(\"**\",\"***\")", "S:*****"),
        ("fn-concat/fn-concat-11", "fn:concat(fn:concat(\"zzz\",\"zz\"),\"123\")", "S:zzzzz123"),
        ("fn-concat/fn-concat-13", "fn:string(fn:concat(\"abc\",\"de\"))", "S:abcde"),
        ("fn-concat/fn-concat-15", "fn:concat(\"%$\",\"#@!\")", "S:%$#@!"),
        ("fn-concat/fn-concat-16", "fn:concat(\"concat\",\"concat\")", "S:concatconcat"),
        ("fn-concat/K-ConcatFunc-1", "concat()", "X:XPST0017"),
        ("fn-concat/K-ConcatFunc-2", "concat(\"a string\")", "X:XPST0017"),
        ("fn-concat/K-ConcatFunc-3", "concat(\"ab\", \"c\") eq \"abc\"", "T"),
        ("fn-concat/K-ConcatFunc-4", "concat(\"ab\", \"c\") instance of xs:string", "T"),
        ("fn-concat/K-ConcatFunc-5", "concat((), ()) instance of xs:string", "T"),
        ("fn-concat/K-ConcatFunc-6", "concat((), ()) eq \"\"", "T"),
        (
            "fn-concat/K-ConcatFunc-7",
            "concat('a', 'b', 'c', (), 'd', 'e', 'f', 'g', 'h', ' ', 'i', 'j', 'k l') eq \"abcdefgh ijk l\"",
            "T"
        ),
        ("fn-concat/K-ConcatFunc-8", "concat(1, 2, 3) eq \"123\"", "T"),
        ("fn-concat/K-ConcatFunc-9", "concat(1, \"2\", 3) eq \"123\"", "T"),
        ("fn-concat/K2-ConcatFunc-1", "concat((\"a\", \"b\"), \"c\")", "X:XPTY0004"),
        ("fn-concat/K2-ConcatFunc-2", "concat(\"1\", \"2\", \"3\", (\"a\", \"b\"), \"c\")", "X:XPTY0004"),
        ("fn-concat/K2-ConcatFunc-3", "concat(\"1\", \"2\", \"3\", \"c\", (\"a\", \"b\"))", "X:XPTY0004"),
        (
            "fn-concat/fn-concatint2args-1",
            "fn:concat(xs:int(\"-2147483648\"),xs:int(\"-2147483648\"))",
            "S:-2147483648-2147483648"
        ),
        (
            "fn-concat/fn-concatint2args-2",
            "fn:concat(xs:int(\"-1873914410\"),xs:int(\"-2147483648\"))",
            "S:-1873914410-2147483648"
        ),
        (
            "fn-concat/fn-concatint2args-3",
            "fn:concat(xs:int(\"2147483647\"),xs:int(\"-2147483648\"))",
            "S:2147483647-2147483648"
        ),
        (
            "fn-concat/fn-concatint2args-4",
            "fn:concat(xs:int(\"-2147483648\"),xs:int(\"-1873914410\"))",
            "S:-2147483648-1873914410"
        ),
        (
            "fn-concat/fn-concatint2args-5",
            "fn:concat(xs:int(\"-2147483648\"),xs:int(\"2147483647\"))",
            "S:-21474836482147483647"
        ),
        (
            "fn-concat/fn-concatlng2args-1",
            "fn:concat(xs:long(\"-92233720368547758\"),xs:long(\"-92233720368547758\"))",
            "S:-92233720368547758-92233720368547758"
        ),
        (
            "fn-concat/fn-concatlng2args-2",
            "fn:concat(xs:long(\"-47175562203048468\"),xs:long(\"-92233720368547758\"))",
            "S:-47175562203048468-92233720368547758"
        ),
        (
            "fn-concat/fn-concatlng2args-3",
            "fn:concat(xs:long(\"92233720368547758\"),xs:long(\"-92233720368547758\"))",
            "S:92233720368547758-92233720368547758"
        ),
        (
            "fn-concat/fn-concatlng2args-4",
            "fn:concat(xs:long(\"-92233720368547758\"),xs:long(\"-47175562203048468\"))",
            "S:-92233720368547758-47175562203048468"
        ),
        (
            "fn-concat/fn-concatlng2args-5",
            "fn:concat(xs:long(\"-92233720368547758\"),xs:long(\"92233720368547758\"))",
            "S:-9223372036854775892233720368547758"
        ),
        (
            "fn-concat/fn-concatnint2args-1",
            "fn:concat(xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-999999999999999999\"))",
            "S:-999999999999999999-999999999999999999"
        ),
        (
            "fn-concat/fn-concatnint2args-2",
            "fn:concat(xs:negativeInteger(\"-297014075999096793\"),xs:negativeInteger(\"-999999999999999999\"))",
            "S:-297014075999096793-999999999999999999"
        ),
        (
            "fn-concat/fn-concatnint2args-3",
            "fn:concat(xs:negativeInteger(\"-1\"),xs:negativeInteger(\"-999999999999999999\"))",
            "S:-1-999999999999999999"
        ),
        (
            "fn-concat/fn-concatnint2args-4",
            "fn:concat(xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-297014075999096793\"))",
            "S:-999999999999999999-297014075999096793"
        ),
        (
            "fn-concat/fn-concatnint2args-5",
            "fn:concat(xs:negativeInteger(\"-999999999999999999\"),xs:negativeInteger(\"-1\"))",
            "S:-999999999999999999-1"
        ),
        (
            "fn-concat/fn-concatnni2args-1",
            "fn:concat(xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"0\"))",
            "S:00"
        ),
        (
            "fn-concat/fn-concatnni2args-2",
            "fn:concat(xs:nonNegativeInteger(\"303884545991464527\"),xs:nonNegativeInteger(\"0\"))",
            "S:3038845459914645270"
        ),
        (
            "fn-concat/fn-concatnni2args-3",
            "fn:concat(xs:nonNegativeInteger(\"999999999999999999\"),xs:nonNegativeInteger(\"0\"))",
            "S:9999999999999999990"
        ),
        (
            "fn-concat/fn-concatnni2args-4",
            "fn:concat(xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"303884545991464527\"))",
            "S:0303884545991464527"
        ),
        (
            "fn-concat/fn-concatnni2args-5",
            "fn:concat(xs:nonNegativeInteger(\"0\"),xs:nonNegativeInteger(\"999999999999999999\"))",
            "S:0999999999999999999"
        ),
        (
            "fn-concat/fn-concatnpi2args-1",
            "fn:concat(xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-999999999999999999\"))",
            "S:-999999999999999999-999999999999999999"
        ),
        (
            "fn-concat/fn-concatnpi2args-2",
            "fn:concat(xs:nonPositiveInteger(\"-475688437271870490\"),xs:nonPositiveInteger(\"-999999999999999999\"))",
            "S:-475688437271870490-999999999999999999"
        ),
        (
            "fn-concat/fn-concatnpi2args-3",
            "fn:concat(xs:nonPositiveInteger(\"0\"),xs:nonPositiveInteger(\"-999999999999999999\"))",
            "S:0-999999999999999999"
        ),
        (
            "fn-concat/fn-concatnpi2args-4",
            "fn:concat(xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"-475688437271870490\"))",
            "S:-999999999999999999-475688437271870490"
        ),
        (
            "fn-concat/fn-concatnpi2args-5",
            "fn:concat(xs:nonPositiveInteger(\"-999999999999999999\"),xs:nonPositiveInteger(\"0\"))",
            "S:-9999999999999999990"
        ),
        ("fn-concat/fn-concatpint2args-1", "fn:concat(xs:positiveInteger(\"1\"),xs:positiveInteger(\"1\"))", "S:11"),
        (
            "fn-concat/fn-concatpint2args-2",
            "fn:concat(xs:positiveInteger(\"52704602390610033\"),xs:positiveInteger(\"1\"))",
            "S:527046023906100331"
        ),
        (
            "fn-concat/fn-concatpint2args-3",
            "fn:concat(xs:positiveInteger(\"999999999999999999\"),xs:positiveInteger(\"1\"))",
            "S:9999999999999999991"
        ),
        (
            "fn-concat/fn-concatpint2args-4",
            "fn:concat(xs:positiveInteger(\"1\"),xs:positiveInteger(\"52704602390610033\"))",
            "S:152704602390610033"
        ),
        (
            "fn-concat/fn-concatpint2args-5",
            "fn:concat(xs:positiveInteger(\"1\"),xs:positiveInteger(\"999999999999999999\"))",
            "S:1999999999999999999"
        ),
        ("fn-concat/fn-concatsht2args-1", "fn:concat(xs:short(\"-32768\"),xs:short(\"-32768\"))", "S:-32768-32768"),
        ("fn-concat/fn-concatsht2args-2", "fn:concat(xs:short(\"-5324\"),xs:short(\"-32768\"))", "S:-5324-32768"),
        ("fn-concat/fn-concatsht2args-3", "fn:concat(xs:short(\"32767\"),xs:short(\"-32768\"))", "S:32767-32768"),
        ("fn-concat/fn-concatsht2args-4", "fn:concat(xs:short(\"-32768\"),xs:short(\"-5324\"))", "S:-32768-5324"),
        ("fn-concat/fn-concatsht2args-5", "fn:concat(xs:short(\"-32768\"),xs:short(\"32767\"))", "S:-3276832767"),
        ("fn-concat/fn-concatulng2args-1", "fn:concat(xs:unsignedLong(\"0\"),xs:unsignedLong(\"0\"))", "S:00"),
        (
            "fn-concat/fn-concatulng2args-2",
            "fn:concat(xs:unsignedLong(\"130747108607674654\"),xs:unsignedLong(\"0\"))",
            "S:1307471086076746540"
        ),
        (
            "fn-concat/fn-concatulng2args-3",
            "fn:concat(xs:unsignedLong(\"184467440737095516\"),xs:unsignedLong(\"0\"))",
            "S:1844674407370955160"
        ),
        (
            "fn-concat/fn-concatulng2args-4",
            "fn:concat(xs:unsignedLong(\"0\"),xs:unsignedLong(\"130747108607674654\"))",
            "S:0130747108607674654"
        ),
        (
            "fn-concat/fn-concatulng2args-5",
            "fn:concat(xs:unsignedLong(\"0\"),xs:unsignedLong(\"184467440737095516\"))",
            "S:0184467440737095516"
        ),
        ("fn-concat/fn-concatusht2args-1", "fn:concat(xs:unsignedShort(\"0\"),xs:unsignedShort(\"0\"))", "S:00"),
        (
            "fn-concat/fn-concatusht2args-2",
            "fn:concat(xs:unsignedShort(\"44633\"),xs:unsignedShort(\"0\"))",
            "S:446330"
        ),
        (
            "fn-concat/fn-concatusht2args-3",
            "fn:concat(xs:unsignedShort(\"65535\"),xs:unsignedShort(\"0\"))",
            "S:655350"
        ),
        (
            "fn-concat/fn-concatusht2args-4",
            "fn:concat(xs:unsignedShort(\"0\"),xs:unsignedShort(\"44633\"))",
            "S:044633"
        ),
        (
            "fn-concat/fn-concatusht2args-5",
            "fn:concat(xs:unsignedShort(\"0\"),xs:unsignedShort(\"65535\"))",
            "S:065535"
        ),
        ("fn-substring/fn-substring-2", "fn:substring(\"metadata\", 4, 3)", "S:ada"),
        ("fn-substring/fn-substring-3", "fn:substring(\"12345\", 1.5, 2.6)", "S:234"),
        ("fn-substring/fn-substring-4", "fn:substring(\"12345\", 0, 3)", "S:12"),
        ("fn-substring/fn-substring-5", "fn:count(fn:substring(\"12345\", 5, -3))", "S:1"),
        ("fn-substring/fn-substring-6", "fn:substring(\"12345\", -3, 5)", "S:1"),
        ("fn-substring/fn-substring-7", "fn:count(fn:substring(\"12345\", 0 div 0E0, 3))", "S:1"),
        ("fn-substring/fn-substring-8", "fn:count(fn:substring(\"12345\", 1, 0 div 0E0))", "S:1"),
        ("fn-substring/fn-substring-9", "fn:count(fn:substring((), 1, 3))", "S:1"),
        ("fn-substring/fn-substring-10", "fn:substring(\"12345\", -42, 1 div 0E0)", "S:12345"),
        ("fn-substring/fn-substring-11", "fn:count(fn:substring(\"12345\", -1 div 0E0, 1 div 0E0))", "S:1"),
        ("fn-substring/fn-substring-12", "fn:count(fn:substring(\"\",0))", "S:1"),
        ("fn-substring/fn-substring-15", "fn:substring(fn:substring(\"ABCDE\",1),1)", "S:ABCDE"),
        ("fn-substring/fn-substring-16", "fn:substring(\"substring\",1)", "S:substring"),
        ("fn-substring/fn-substring-17", "fn:concat(fn:substring(\"ABC\",1),\"DEF\")", "S:ABCDEF"),
        ("fn-substring/fn-substring-18", "fn:contains(fn:substring(\"ABCDEF\",1),\"DEF\")", "T"),
        ("fn-substring/fn-substring-19", "fn:substring(\"!@#$%^*()\",1)", "S:!@#$%^*()"),
        ("fn-substring/fn-substring-20", "fn:substring(\"ABCD\",xs:double(1))", "S:ABCD"),
        ("fn-substring/fn-substring-21", "fn:substring(\"ABCDE\",1+1)", "S:BCDE"),
        ("fn-substring/fn-substring-23", "substring(\"abcd\U00010001efgh\", 6)", "E:\"efgh\""),
        ("fn-substring/fn-substring-24", "substring(\"abcd\U00010001efgh\", 5, 2)", "E:\"\U00010001e\""),
        ("fn-substring/fn-substring-25", "substring(\"\U00010001\", 1, 2)", "E:\"\U00010001\""),
        ("fn-substring/fn-substring-26", "substring(\"\U00010001\", 2, 1)", "E:\"\""),
        ("fn-substring/fn-substring-27", "substring(\"\U00010001\", 0, 2)", "E:\"\U00010001\""),
        ("fn-substring/fn-substring-28", "substring(\"\U00010001\", 0, 3)", "E:\"\U00010001\""),
        ("fn-substring/fn-substring-29", "substring(\"\U00010001\U00010001\", 3)", "E:\"\""),
        ("fn-substring/fn-substring-30", "substring(\"\U00010001\U00010001\", 0)", "E:\"\U00010001\U00010001\""),
        ("fn-substring/fn-substring-31", "substring(\"12345\", -3.1e0, 5.2e0) eq \"1\"", "T"),
        ("fn-substring/K-SubstringFunc-1", "sub-string(\"a string\")", "X:XPST0017"),
        ("fn-substring/K-SubstringFunc-2", "sub-string(\"a string\", 1, 2, \"wrong param\")", "X:XPST0017"),
        ("fn-substring/K-SubstringFunc-3", "substring((), 1, 2) eq \"\"", "T"),
        ("fn-substring/K-SubstringFunc-4", "substring((), 1) eq \"\"", "T"),
        ("fn-substring/K-SubstringFunc-5", "substring(\"12345\", 1.5, 2.6) eq \"234\"", "T"),
        ("fn-substring/K-SubstringFunc-6", "substring((), 1, 3) eq \"\"", "T"),
        ("fn-substring/K-SubstringFunc-7", "substring(\"motor car\", 6) eq \" car\"", "T"),
        ("fn-substring/K-SubstringFunc-8", "substring(\"12345\", 0, 3) eq \"12\"", "T"),
        ("fn-substring/K-SubstringFunc-9", "substring(\"metadata\", 4, 3) eq \"ada\"", "T"),
        ("fn-substring/K-SubstringFunc-10", "substring(\"12345\", 0 div 0E0, 3) eq \"\"", "T"),
        ("fn-substring/K-SubstringFunc-11", "substring(\"12345\", 1, 0 div 0E0) eq \"\"", "T"),
        ("fn-substring/K-SubstringFunc-12", "substring(\"12345\", -3, 5) eq \"1\"", "T"),
        ("fn-substring/K-SubstringFunc-13", "substring(\"12345\", -42, 1 div 0E0) eq \"12345\"", "T"),
        ("fn-substring/K-SubstringFunc-14", "substring(\"12345\", -1 div 0E0, 1 div 0E0) eq \"\"", "T"),
        ("fn-substring/K-SubstringFunc-15", "substring(\"12345\", 5, -3) eq \"\"", "T"),
        ("fn-substring-before/fn-substring-before-1", "fn:substring-before(\"\",\"\")", "E:\"\"\u001FC:1"),
        (
            "fn-substring-before/fn-substring-before-2",
            "fn:substring-before(\"\",\"A Character String\")",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-before/fn-substring-before-3",
            "fn:substring-before(\"A Character String\",\"\")",
            "E:\"\"\u001FC:1"
        ),
        ("fn-substring-before/fn-substring-before-4", "fn:substring-before((),\"\")", "E:\"\"\u001FC:1"),
        ("fn-substring-before/fn-substring-before-5", "fn:substring-before(\"\",())", "E:\"\"\u001FC:1"),
        (
            "fn-substring-before/fn-substring-before-6",
            "fn:substring-before(\"A Character String\",())",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-before/fn-substring-before-7",
            "fn:substring-before((),\"A Character String\")",
            "E:\"\"\u001FC:1"
        ),
        ("fn-substring-before/fn-substring-before-8", "fn:substring-before(\"AAAAABBBBBCCCCC\",\"BBBBB\")", "S:AAAAA"),
        ("fn-substring-before/fn-substring-before-9", "fn:substring-before(\"AAAAABBBBB\",\" \")", "E:\"\"\u001FC:1"),
        ("fn-substring-before/fn-substring-before-10", "fn:substring-before(\" \",\"AAAAABBBBB\")", "E:\"\"\u001FC:1"),
        (
            "fn-substring-before/fn-substring-before-13",
            "fn:substring-before(xs:string(\"A\"),\"A\")",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-before/fn-substring-before-14",
            "fn:substring-before(\"A\",xs:string(\"A\"))",
            "E:\"\"\u001FC:1"
        ),
        ("fn-substring-before/fn-substring-before-15", "fn:substring-before(\"A\",\"a\")", "E:\"\"\u001FC:1"),
        ("fn-substring-before/fn-substring-before-16", "fn:substring-before(\"a\",\"A\")", "E:\"\"\u001FC:1"),
        (
            "fn-substring-before/fn-substring-before-17",
            "fn:substring-before(\"substring-before\",\"substring-before\")",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-before/fn-substring-before-18",
            "fn:substring-before(\"substring-beforesubstring-before\",\"substring-before\")",
            "E:\"\"\u001FC:1"
        ),
        ("fn-substring-before/fn-substring-before-19", "fn:substring-before(\"****\",\"***\")", "E:\"\"\u001FC:1"),
        ("fn-substring-before/fn-substring-before-20", "fn:substring-before(\"12345\",\"2345\")", "E:\"1\"\u001FC:1"),
        (
            "fn-substring-before/fn-substring-before-21",
            "fn:substring-before(\"substring-before\",\"erofeb-gnirtsbus\")",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-before/fn-substring-before-22",
            "substring-before(\"banana\", \"a\",\n         \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "E:\"b\""
        ),
        (
            "fn-substring-before/fn-substring-before-25",
            "substring-before(\"\U00010001\U00010002\U00010003\", \"\U00010002\")",
            "E:\"\U00010001\""
        ),
        ("fn-substring-before/K-SubstringBeforeFunc-1", "substring-before()", "X:XPST0017"),
        ("fn-substring-before/K-SubstringBeforeFunc-2", "substring-before(())", "X:XPST0017"),
        (
            "fn-substring-before/K-SubstringBeforeFunc-3",
            "substring-before((), (), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\",\n         \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-substring-before/K-SubstringBeforeFunc-4",
            "substring-before(\"a string\", \"a string\",\n         \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "X:FOCH0002"
        ),
        (
            "fn-substring-before/K-SubstringBeforeFunc-5",
            "substring-before(\"foo\", \"oo\",\n         \"http://www.w3.org/2005/xpath-functions/collation/codepoint\") eq \"f\"",
            "T"
        ),
        ("fn-substring-before/K-SubstringBeforeFunc-6", "substring-before(\"tattoo\", \"attoo\") eq \"t\"", "T"),
        ("fn-substring-before/K-SubstringBeforeFunc-7", "substring-before(\"tattoo\", \"tatto\") eq \"\"", "T"),
        ("fn-substring-before/K-SubstringBeforeFunc-8", "substring-before((), ()) eq \"\"", "T"),
        ("fn-substring-before/K-SubstringBeforeFunc-9", "substring-before(\"a string\", \"\") eq \"\"", "T"),
        (
            "fn-substring-before/K-SubstringBeforeFunc-10",
            "substring-before(\"a string\", \"not in other\") eq \"\"",
            "T"
        ),
        ("fn-substring-after/fn-substring-after-1", "fn:substring-after(\"\",\"\")", "E:\"\"\u001FC:1"),
        (
            "fn-substring-after/fn-substring-after-2",
            "fn:substring-after(\"\",\"A Character String\")",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-after/fn-substring-after-3",
            "fn:substring-after(\"A Character String\",\"\")",
            "E:\"A Character String\"\u001FC:1"
        ),
        ("fn-substring-after/fn-substring-after-4", "fn:substring-after((),\"\")", "E:\"\"\u001FC:1"),
        ("fn-substring-after/fn-substring-after-5", "fn:substring-after(\"\",())", "E:\"\"\u001FC:1"),
        (
            "fn-substring-after/fn-substring-after-6",
            "fn:substring-after(\"A Character String\",())",
            "E:\"A Character String\"\u001FC:1"
        ),
        ("fn-substring-after/fn-substring-after-7", "fn:substring-after((),\"A Character String\")", "E:\"\"\u001FC:1"),
        ("fn-substring-after/fn-substring-after-8", "fn:substring-after(\"AAAAABBBBBCCCCC\",\"BBBBB\")", "S:CCCCC"),
        ("fn-substring-after/fn-substring-after-9", "fn:substring-after(\"AAAAABBBBB\",\" \")", "E:\"\"\u001FC:1"),
        ("fn-substring-after/fn-substring-after-10", "fn:substring-after(\" \",\"AAAAABBBBB\")", "E:\"\"\u001FC:1"),
        ("fn-substring-after/fn-substring-after-13", "fn:substring-after(xs:string(\"A\"),\"A\")", "E:\"\"\u001FC:1"),
        ("fn-substring-after/fn-substring-after-14", "fn:substring-after(\"A\",xs:string(\"A\"))", "E:\"\"\u001FC:1"),
        ("fn-substring-after/fn-substring-after-15", "fn:substring-after(\"A\",\"a\")", "E:\"\"\u001FC:1"),
        ("fn-substring-after/fn-substring-after-16", "fn:substring-after(\"a\",\"A\")", "E:\"\"\u001FC:1"),
        (
            "fn-substring-after/fn-substring-after-17",
            "fn:substring-after(\"substring-after\",\"substring-after\")",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-after/fn-substring-after-18",
            "fn:substring-after(\"substring-aftersubstring-after\",\"substring-after\")",
            "S:substring-after"
        ),
        ("fn-substring-after/fn-substring-after-19", "fn:substring-after(\"****\",\"***\")", "S:*"),
        ("fn-substring-after/fn-substring-after-20", "fn:substring-after(\"12345\",\"1234\")", "S:5"),
        (
            "fn-substring-after/fn-substring-after-21",
            "fn:substring-after(\"substring-after\",\"refta-gnirtsbus\")",
            "E:\"\"\u001FC:1"
        ),
        (
            "fn-substring-after/fn-substring-after-22",
            "substring-after(\"banana\", \"a\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "E:\"nana\""
        ),
        (
            "fn-substring-after/fn-substring-after-25",
            "substring-after(\"\U00010001\U00010002\U00010003\", \"\U00010002\")",
            "E:\"\U00010003\""
        ),
        ("fn-substring-after/K-SubstringAfterFunc-1", "substring-after()", "X:XPST0017"),
        ("fn-substring-after/K-SubstringAfterFunc-2", "substring-after(())", "X:XPST0017"),
        (
            "fn-substring-after/K-SubstringAfterFunc-3",
            "substring-after((), (), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-substring-after/K-SubstringAfterFunc-4",
            "substring-after(\"a string\", \"a string\", \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "X:FOCH0002"
        ),
        (
            "fn-substring-after/K-SubstringAfterFunc-5",
            "substring-after(\"foo\", \"fo\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\") eq \"o\"",
            "T"
        ),
        ("fn-substring-after/K-SubstringAfterFunc-6", "substring-after(\"tattoo\", \"tat\") eq \"too\"", "T"),
        ("fn-substring-after/K-SubstringAfterFunc-7", "substring-after(\"tattoo\", \"tattoo\") eq \"\"", "T"),
        ("fn-substring-after/K-SubstringAfterFunc-8", "substring-after(\"abcdefgedij\", \"def\") eq \"gedij\"", "T"),
        ("fn-substring-after/K-SubstringAfterFunc-9", "substring-after((), ()) eq \"\"", "T"),
        ("fn-substring-after/K-SubstringAfterFunc-10", "substring-after(\"a string\", ()) eq \"a string\"", "T"),
        ("fn-substring-after/K-SubstringAfterFunc-11", "substring-after(\"a string\", \"not in other\") eq \"\"", "T"),
        (
            "fn-contains/fn-contains2args-1",
            "fn:contains(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-contains/fn-contains2args-2",
            "fn:contains(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-contains/fn-contains2args-3",
            "fn:contains(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-contains/fn-contains2args-4",
            "fn:contains(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-contains/fn-contains2args-5",
            "fn:contains(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        ("fn-contains/fn-contains-1", "fn:contains(\"\",\"\")", "T"),
        ("fn-contains/fn-contains-2", "fn:contains(\"\",\"A Character String\")", "F"),
        ("fn-contains/fn-contains-3", "fn:contains(\"A Character String\",\"\")", "T"),
        ("fn-contains/fn-contains-4", "fn:contains((),\"\")", "T"),
        ("fn-contains/fn-contains-5", "fn:contains(\"\",())", "T"),
        ("fn-contains/fn-contains-6", "fn:contains(\"A Character String\",())", "T"),
        ("fn-contains/fn-contains-7", "fn:contains((),\"A Character String\")", "F"),
        ("fn-contains/fn-contains-8", "fn:contains(\"AAAAABBBBBCCCCC\",\"BBBBB\")", "T"),
        ("fn-contains/fn-contains-9", "fn:contains(\"AAAAABBBBB\",\" \")", "F"),
        ("fn-contains/fn-contains-10", "fn:contains(\" \",\"AAAAABBBBB\")", "F"),
        ("fn-contains/fn-contains-13", "fn:contains(xs:string(\"A\"),\"A\")", "T"),
        ("fn-contains/fn-contains-14", "fn:contains(\"A\",xs:string(\"A\"))", "T"),
        ("fn-contains/fn-contains-15", "fn:contains(\"A\",\"a\")", "F"),
        ("fn-contains/fn-contains-16", "fn:contains(\"a\",\"A\")", "F"),
        ("fn-contains/K-ContainsFunc-1", "contains()", "X:XPST0017"),
        ("fn-contains/K-ContainsFunc-2", "contains(())", "X:XPST0017"),
        (
            "fn-contains/K-ContainsFunc-3",
            "contains((), (), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-contains/K-ContainsFunc-4",
            "contains(\"a string\", \"a string\", \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "X:FOCH0002"
        ),
        (
            "fn-contains/K-ContainsFunc-5",
            "contains(\"foo\", \"foo\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "T"
        ),
        ("fn-contains/K-ContainsFunc-6", "contains(\"tattoo\", \"t\")", "T"),
        ("fn-contains/K-ContainsFunc-8", "contains(\"\", ())", "T"),
        ("fn-contains/K-ContainsFunc-9", "contains(\"a string\", ())", "T"),
        ("fn-contains/K-ContainsFunc-10", "contains(\"foo\", \"foo\")", "T"),
        (
            "fn-starts-with/fn-starts-with2args-1",
            "fn:starts-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-starts-with/fn-starts-with2args-2",
            "fn:starts-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-starts-with/fn-starts-with2args-3",
            "fn:starts-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-starts-with/fn-starts-with2args-4",
            "fn:starts-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-starts-with/fn-starts-with2args-5",
            "fn:starts-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        ("fn-starts-with/fn-starts-with-1", "fn:starts-with(\"\",\"\")", "T"),
        ("fn-starts-with/fn-starts-with-2", "fn:starts-with(\"\",\"A Character String\")", "F"),
        ("fn-starts-with/fn-starts-with-3", "fn:starts-with(\"A Character String\",\"\")", "T"),
        ("fn-starts-with/fn-starts-with-4", "fn:starts-with((),\"\")", "T"),
        ("fn-starts-with/fn-starts-with-5", "fn:starts-with(\"\",())", "T"),
        ("fn-starts-with/fn-starts-with-6", "fn:starts-with(\"A Character String\",())", "T"),
        ("fn-starts-with/fn-starts-with-7", "fn:starts-with((),\"A Character String\")", "F"),
        ("fn-starts-with/fn-starts-with-8", "fn:starts-with(\"AAAAABBBBBCCCCC\",\"BBBBB\")", "F"),
        ("fn-starts-with/fn-starts-with-9", "fn:starts-with(\"AAAAABBBBB\",\" \")", "F"),
        ("fn-starts-with/fn-starts-with-10", "fn:starts-with(\" \",\"AAAAABBBBB\")", "F"),
        ("fn-starts-with/fn-starts-with-13", "fn:starts-with(xs:string(\"A\"),\"A\")", "T"),
        ("fn-starts-with/fn-starts-with-14", "fn:starts-with(\"A\",xs:string(\"A\"))", "T"),
        ("fn-starts-with/fn-starts-with-15", "fn:starts-with(\"A\",\"a\")", "F"),
        ("fn-starts-with/fn-starts-with-16", "fn:starts-with(\"a\",\"A\")", "F"),
        ("fn-starts-with/K-StartsWithFunc-1", "starts-with()", "X:XPST0017"),
        ("fn-starts-with/K-StartsWithFunc-2", "starts-with(())", "X:XPST0017"),
        (
            "fn-starts-with/K-StartsWithFunc-3",
            "starts-with((), (), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-starts-with/K-StartsWithFunc-4",
            "starts-with(\"a string\", \"a string\", \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "X:FOCH0002"
        ),
        (
            "fn-starts-with/K-StartsWithFunc-5",
            "starts-with(\"foo\", \"foo\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "T"
        ),
        ("fn-starts-with/K-StartsWithFunc-6", "starts-with(\"foo\", \"foo\")", "T"),
        ("fn-starts-with/K-StartsWithFunc-7", "starts-with(\"tattoo\", \"tat\")", "T"),
        ("fn-starts-with/K-StartsWithFunc-9", "starts-with((), ())", "T"),
        ("fn-starts-with/K-StartsWithFunc-10", "starts-with(\"a string\", ())", "T"),
        (
            "fn-ends-with/fn-ends-with2args-1",
            "fn:ends-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-ends-with/fn-ends-with2args-2",
            "fn:ends-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-ends-with/fn-ends-with2args-3",
            "fn:ends-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-ends-with/fn-ends-with2args-4",
            "fn:ends-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        (
            "fn-ends-with/fn-ends-with2args-5",
            "fn:ends-with(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "T"
        ),
        ("fn-ends-with/fn-ends-with-1", "fn:ends-with(\"\",\"\")", "T"),
        ("fn-ends-with/fn-ends-with-2", "fn:ends-with(\"\",\"A Character String\")", "F"),
        ("fn-ends-with/fn-ends-with-3", "fn:ends-with(\"A Character String\",\"\")", "T"),
        ("fn-ends-with/fn-ends-with-4", "fn:ends-with((),\"\")", "T"),
        ("fn-ends-with/fn-ends-with-5", "fn:ends-with(\"\",())", "T"),
        ("fn-ends-with/fn-ends-with-6", "fn:ends-with(\"A Character String\",())", "T"),
        ("fn-ends-with/fn-ends-with-7", "fn:ends-with((),\"A Character String\")", "F"),
        ("fn-ends-with/fn-ends-with-8", "fn:ends-with(\"AAAAABBBBBCCCCC\",\"BBBBB\")", "F"),
        ("fn-ends-with/fn-ends-with-9", "fn:ends-with(\"AAAAABBBBB\",\" \")", "F"),
        ("fn-ends-with/fn-ends-with-10", "fn:ends-with(\" \",\"AAAAABBBBB\")", "F"),
        ("fn-ends-with/fn-ends-with-13", "fn:ends-with(xs:string(\"A\"),\"A\")", "T"),
        ("fn-ends-with/fn-ends-with-14", "fn:ends-with(\"A\",xs:string(\"A\"))", "T"),
        ("fn-ends-with/fn-ends-with-15", "fn:ends-with(\"A\",\"a\")", "F"),
        ("fn-ends-with/fn-ends-with-16", "fn:ends-with(\"a\",\"A\")", "F"),
        ("fn-ends-with/K-EndsWithFunc-1", "ends-with()", "X:XPST0017"),
        ("fn-ends-with/K-EndsWithFunc-2", "ends-with(())", "X:XPST0017"),
        (
            "fn-ends-with/K-EndsWithFunc-3",
            "ends-with((), (), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-ends-with/K-EndsWithFunc-4",
            "ends-with(\"a string\", \"a string\", \"http://www.example.com/COLLATION/NOT/SUPPORTED\")",
            "X:FOCH0002"
        ),
        (
            "fn-ends-with/K-EndsWithFunc-5",
            "ends-with(\"foo\", \"foo\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "T"
        ),
        ("fn-ends-with/K-EndsWithFunc-6", "ends-with(\"tattoo\", \"tattoo\")", "T"),
        ("fn-ends-with/K-EndsWithFunc-9", "ends-with((), ())", "T"),
        (
            "fn-translate/fn-translate3args-1",
            "translate('---abcABCxyz---','-abcABCxyz','1ABCabcXYZ')",
            "S:111ABCabcXYZ111"
        ),
        ("fn-translate/fn-translate3args-2", "translate('newline\ntab\tspace ','\n\t ','123')", "S:newline1tab2space3"),
        ("fn-translate/fn-translate3args-3", "translate('','-','x')", "S:"),
        ("fn-translate/fn-translate3args-4", "translate((),'-','x')", "S:"),
        ("fn-translate/fn-translate3args-5", "translate(1,'-','x')", "X:XPTY0004"),
        ("fn-translate/fn-translate3args-6", "translate('abc',1,'x')", "X:XPTY0004"),
        ("fn-translate/fn-translate3args-7", "translate('abc','x',1)", "X:XPTY0004"),
        ("fn-translate/fn-translate3args-8", "translate('abc')", "X:XPST0017"),
        ("fn-translate/fn-translate-1", "fn:translate(\"bar\",\"abc\",\"ABC\")", "S:BAr"),
        ("fn-translate/fn-translate-2", "fn:translate(\"--aaa--\",\"abc-\",\"ABC\")", "S:AAA"),
        ("fn-translate/fn-translate-3", "fn:translate(\"abcdabc\", \"abc\", \"AB\")", "S:ABdAB"),
        (
            "fn-translate/fn-translate-4",
            "fn:translate(\"acdefghijklmnopqrstuvwxyz\", \"abcdefghijklmnopqrstuvwxyz\", \"ABCDEFGHIJKLMNOPQRSTUVWXYZ\")",
            "S:ACDEFGHIJKLMNOPQRSTUVWXYZ"
        ),
        (
            "fn-translate/fn-translate-5",
            "fn:translate(\"ABCDEFGHIJKLMNOPQRSTUVWXYZ\",\"ABCDEFGHIJKLMNOPQRSTUVWXYZ\",\"abcdefghijklmnopqrstuvwxyz\")",
            "S:abcdefghijklmnopqrstuvwxyz"
        ),
        ("fn-translate/fn-translate-6", "fn:count(fn:translate(\"\",\"\",\"\"))", "S:1"),
        ("fn-translate/fn-translate-7", "fn:translate(\"ABC\", \"ABC\", \"ABC\")", "S:ABC"),
        ("fn-translate/fn-translate-8", "fn:translate(\"123\", \"123\", \"123\")", "S:123"),
        ("fn-translate/fn-translate-9", "fn:translate(\"123ABC\", \"123ABC\", \"123ABC\")", "S:123ABC"),
        (
            "fn-translate/fn-translate-10",
            "fn:translate(fn:string(\"ABC\"), fn:string(\"ABC\"), fn:string(\"ABC\"))",
            "S:ABC"
        ),
        ("fn-translate/fn-translate-11", "fn:string(fn:translate(\"ABC\", \"ABC\", \"ABC\"))", "S:ABC"),
        ("fn-translate/fn-translate-12", "fn:string-length(fn:translate(\"ABC\",\"ABC\",\"ABC\"))", "S:3"),
        ("fn-translate/fn-translate-13", "xs:decimal(fn:translate(\"123\",\"123\",\"123\"))", "S:123"),
        ("fn-translate/fn-translate-14", "xs:integer(fn:translate(\"123\",\"123\",\"123\"))", "S:123"),
        ("fn-translate/fn-translate-15", "xs:float(fn:translate(\"123\",\"123\",\"123\"))", "S:123"),
        ("fn-translate/fn-translate-16", "xs:double(fn:translate(\"123\",\"123\",\"123\"))", "S:123"),
        ("fn-translate/fn-translate-17", "translate(\"abcd\", \"\U00010001a\", \"xy\")", "E:\"ybcd\""),
        ("fn-translate/fn-translate-18", "translate(\"abcd\", \"xa\", \"\U00010001y\")", "E:\"ybcd\""),
        ("fn-translate/fn-translate-19", "translate(\"abcd\U00010004e\", \"a\U00010004e\", \"XYZ\")", "E:\"XbcdYZ\""),
        (
            "fn-translate/fn-translate-20",
            "translate(\"abcd\U00010004e\", \"a\U00010004e\", \"\U00010006YZ\")",
            "E:\"\U00010006bcdYZ\""
        ),
        ("fn-translate/K-TranslateFunc-1", "translate()", "X:XPST0017"),
        ("fn-translate/K-TranslateFunc-2", "translate(\"string\", \"map string\")", "X:XPST0017"),
        (
            "fn-translate/K-TranslateFunc-3",
            "translate(\"arg\", \"map string\", \"transString\", \"wrong param\")",
            "X:XPST0017"
        ),
        ("fn-translate/K-TranslateFunc-4", "translate(\"--aaa--\",\"-\",\"\") eq \"aaa\"", "T"),
        ("fn-translate/K-TranslateFunc-5", "translate(\"--aaa--\",\"bbb++\",\"\") eq \"--aaa--\"", "T"),
        ("fn-translate/K-TranslateFunc-6", "translate(\"argstr\", \"\", \"matrs\") eq \"argstr\"", "T"),
        ("fn-translate/K-TranslateFunc-7", "translate((), \"map\", \"trans\") eq \"\"", "T"),
        ("fn-translate/K-TranslateFunc-8", "translate(\"abcdabc\", \"abc\", \"AB\") eq \"ABdAB\"", "T"),
        ("fn-translate/K-TranslateFunc-9", "translate(\"bar\",\"abc\",\"ABC\") eq \"BAr\"", "T"),
        ("fn-translate/K-TranslateFunc-10", "translate(\"--aaa--\",\"abc-\",\"ABC\") eq \"AAA\"", "T"),
        ("fn-translate/K2-TranslateFunc-1", "fn:translate(\"arg\", (), \"transString\")", "X:XPTY0004"),
        ("fn-translate/K2-TranslateFunc-2", "fn:translate(\"arg\", \"mapString\", ())", "X:XPTY0004"),
        (
            "fn-translate/cbcl-fn-translate-002",
            "translate(codepoints-to-string(65536 to 65537),codepoints-to-string(65536 to 65537),\"l\")",
            "S:l"
        ),
        (
            "fn-normalize-space/fn-normalize-space1args-1",
            "fn:normalize-space(\"This is a charac\")",
            "S:This is a charac"
        ),
        ("fn-normalize-space/fn-normalize-space1args-2", "fn:normalize-space(\"This is a ch\")", "S:This is a ch"),
        (
            "fn-normalize-space/fn-normalize-space1args-3",
            "fn:normalize-space(\"This is a charac\")",
            "S:This is a charac"
        ),
        (
            "fn-normalize-space/fn-normalize-space-1",
            "fn:normalize-space(\" The wealthy curled darlings of our nation. \")",
            "S:The wealthy curled darlings of our nation."
        ),
        ("fn-normalize-space/fn-normalize-space-2", "fn:normalize-space()", "X:XPDY0002"),
        (
            "fn-normalize-space/fn-normalize-space-3",
            "fn:normalize-space(\"This\ttext should contains no tabs\")",
            "S:This text should contains no tabs"
        ),
        (
            "fn-normalize-space/fn-normalize-space-4",
            "fn:normalize-space(\"This text should contains\nno newline characters.\")",
            "S:This text should contains no newline characters."
        ),
        (
            "fn-normalize-space/fn-normalize-space-5",
            "fn:normalize-space(\"This\ttext\tshould\tcontains\tno\ttab\tcharacters.\")",
            "S:This text should contains no tab characters."
        ),
        (
            "fn-normalize-space/fn-normalize-space-6",
            "fn:normalize-space(\"This\ntext\nshould\ncontains\nno\nnewline\ncharacters.\")",
            "S:This text should contains no newline characters."
        ),
        (
            "fn-normalize-space/fn-normalize-space-7",
            "fn:normalize-space(\"This text\tshould contains no tabs or\nnewline characters.\")",
            "S:This text should contains no tabs or newline characters."
        ),
        (
            "fn-normalize-space/fn-normalize-space-8",
            "fn:normalize-space(\"This\t text\t should\t contains\n no tabs or newline characters.\")",
            "S:This text should contains no tabs or newline characters."
        ),
        ("fn-normalize-space/fn-normalize-space-9", "fn:normalize-space(\"    \")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-10", "fn:normalize-space(\" \")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-11", "fn:normalize-space(\"\")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-12", "fn:normalize-space(\"\t\")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-13", "fn:normalize-space(\"\t\t\")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-14", "fn:normalize-space(\"\n\")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-15", "fn:normalize-space(\"\r\n\r\n\")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-16", "fn:normalize-space(\" \t  \t \")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-17", "fn:normalize-space(\" \r  \n \")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-18", "fn:normalize-space(\"\t\n\")", "E:\"\"\u001FC:1"),
        ("fn-normalize-space/fn-normalize-space-19", "fn:normalize-space(\"\t12345\")", "S:12345"),
        ("fn-normalize-space/fn-normalize-space-20", "fn:normalize-space(fn:string(\" ABC \"))", "S:ABC"),
        ("fn-normalize-space/fn-normalize-space-21", "fn:normalize-space(fn:normalize-space(\" ABC\"))", "S:ABC"),
        ("fn-normalize-space/K-NormalizeSpaceFunc-1", "normalize-space(\"a string\", \"wrong param\")", "X:XPST0017"),
        ("fn-normalize-space/K-NormalizeSpaceFunc-3", "normalize-space(\"foo\") eq \"foo\"", "T"),
        ("fn-normalize-space/K-NormalizeSpaceFunc-4", "normalize-space(\" foo\") eq \"foo\"", "T"),
        ("fn-normalize-space/K-NormalizeSpaceFunc-5", "normalize-space(\"foo \") eq \"foo\"", "T"),
        ("fn-normalize-space/K-NormalizeSpaceFunc-6", "normalize-space(()) eq \"\"", "T"),
        ("fn-normalize-space/K-NormalizeSpaceFunc-7", "normalize-space(\"f o o \") eq \"f o o\"", "T"),
        (
            "fn-normalize-space/K-NormalizeSpaceFunc-8",
            "normalize-space(\" 143 1239 fhjkls \") eq \"143 1239 fhjkls\"",
            "T"
        ),
        ("fn-string-length/fn-string-length1args-1", "fn:string-length(xs:string(\"This is a characte\"))", "E:18"),
        ("fn-string-length/fn-string-length1args-2", "fn:string-length(xs:string(\"This is a characte\"))", "E:18"),
        ("fn-string-length/fn-string-length1args-3", "fn:string-length(xs:string(\"This is a characte\"))", "E:18"),
        (
            "fn-string-length/fn-string-length-1",
            "fn:string-length(\"Harp not on that string, madam; that is past.\")",
            "E:45"
        ),
        ("fn-string-length/fn-string-length-2", "fn:string-length(())", "E:0"),
        ("fn-string-length/fn-string-length-3", "fn:string-length(\"12345\")", "E:5"),
        ("fn-string-length/fn-string-length-4", "fn:string-length(\"12345abcd\")", "E:9"),
        ("fn-string-length/fn-string-length-5", "fn:string-length(\"ABCD\")", "E:4"),
        ("fn-string-length/fn-string-length-6", "fn:string-length(\"abcde\")", "E:5"),
        ("fn-string-length/fn-string-length-7", "fn:string-length(\"ABCDEabcde\")", "E:10"),
        ("fn-string-length/fn-string-length-8", "fn:string-length(\"\")", "E:0"),
        ("fn-string-length/fn-string-length-9", "fn:string-length(fn:string(\"AbcDH\"))", "E:5"),
        ("fn-string-length/fn-string-length-10", "fn:string-length(\"*****\")", "E:5"),
        ("fn-string-length/fn-string-length-11", "fn:string-length(\"zzzzz\") + fn:string-length(\"zzzzz\")", "E:10"),
        (
            "fn-string-length/fn-string-length-13",
            "fn:concat(fn:string-length(\"abcde\"), fn:string-length(\"fghi\"))",
            "E:\"54\""
        ),
        ("fn-string-length/fn-string-length-15", "fn:string-length(\"%$#@!\")", "E:5"),
        ("fn-string-length/fn-string-length-16", "fn:string-length(\"string-length\")", "E:13"),
        ("fn-string-length/fn-string-length-18", "fn:string-length()", "X:XPDY0002"),
        ("fn-string-length/fn-string-length-20", "string-length(\"\U00010002\")", "E:1"),
        ("fn-string-length/K-StringLengthFunc-1", "string-length(\"a string\", \"wrong param\")", "X:XPST0017"),
        ("fn-string-length/K-StringLengthFunc-2", "string-length(\"ebv\") eq 3", "T"),
        ("fn-string-length/K-StringLengthFunc-3", "string-length(\"ebv\") instance of xs:integer", "T"),
        ("fn-string-length/K-StringLengthFunc-4", "string-length(()) eq 0", "T"),
        ("fn-string-length/K-StringLengthFunc-5", "string-length(\"\") eq 0", "T"),
        ("fn-string-length/K-StringLengthFunc-6", "string-length(()) instance of xs:integer", "T"),
        (
            "fn-string-length/K-StringLengthFunc-8",
            "string-length(\"Harp not on that string, madam; that is past.\") eq 45",
            "T"
        ),
        (
            "fn-upper-case/fn-upper-case1args-1",
            "fn:upper-case(xs:string(\"This is a characte\"))",
            "S:THIS IS A CHARACTE"
        ),
        (
            "fn-upper-case/fn-upper-case1args-2",
            "fn:upper-case(xs:string(\"This is a characte\"))",
            "S:THIS IS A CHARACTE"
        ),
        (
            "fn-upper-case/fn-upper-case1args-3",
            "fn:upper-case(xs:string(\"This is a characte\"))",
            "S:THIS IS A CHARACTE"
        ),
        ("fn-upper-case/fn-upper-case-1", "fn:upper-case(\"ABc!D\")", "S:ABC!D"),
        ("fn-upper-case/fn-upper-case-2", "fn:count(fn:upper-case(()))", "S:1"),
        ("fn-upper-case/fn-upper-case-3", "fn:upper-case(\"12345\")", "S:12345"),
        ("fn-upper-case/fn-upper-case-4", "fn:upper-case(\"12345abcd\")", "S:12345ABCD"),
        ("fn-upper-case/fn-upper-case-5", "fn:upper-case(\"ABCD\")", "S:ABCD"),
        ("fn-upper-case/fn-upper-case-6", "fn:upper-case(\"abcde\")", "S:ABCDE"),
        ("fn-upper-case/fn-upper-case-7", "fn:upper-case(\"ABCDEabcde\")", "S:ABCDEABCDE"),
        ("fn-upper-case/fn-upper-case-8", "fn:count(fn:upper-case(\"\"))", "S:1"),
        ("fn-upper-case/fn-upper-case-9", "fn:upper-case(lower-case(\"AbcDH\"))", "S:ABCDH"),
        ("fn-upper-case/fn-upper-case-10", "fn:upper-case(\"*****\")", "S:*****"),
        ("fn-upper-case/fn-upper-case-11", "fn:upper-case(upper-case(\"zzzzz\"))", "S:ZZZZZ"),
        (
            "fn-upper-case/fn-upper-case-13",
            "fn:concat(fn:upper-case(\"abcde\"), fn:upper-case(\"fghi\"))",
            "S:ABCDEFGHI"
        ),
        ("fn-upper-case/fn-upper-case-15", "fn:upper-case(\"%$#@!\")", "S:%$#@!"),
        ("fn-upper-case/fn-upper-case-16", "fn:upper-case(\"upper-case\")", "S:UPPER-CASE"),
        (
            "fn-upper-case/fn-upper-case-18",
            "fn:string-to-codepoints(fn:upper-case(fn:codepoints-to-string(160 to 256)))",
            "D:160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, \n            177, 178, 179, 180, 924, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, \n            194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, \n            211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 83, 83, 192, 193, 194, \n            195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, \n            212, 213, 214, 247, 216, 217, 218, 219, 220, 221, 222, 376, 256"
        ),
        (
            "fn-upper-case/fn-upper-case-20",
            "fn:string-to-codepoints(fn:upper-case(fn:codepoints-to-string(459)))",
            "E:458"
        ),
        (
            "fn-upper-case/fn-upper-case-21",
            "fn:string-to-codepoints(fn:upper-case(fn:codepoints-to-string(223)))",
            "D:83, 83"
        ),
        (
            "fn-upper-case/fn-upper-case-22",
            "fn:string-to-codepoints(fn:upper-case(fn:codepoints-to-string(64279)))",
            "D:1348, 1341"
        ),
        ("fn-upper-case/K-UpperCaseFunc-1", "upper-case()", "X:XPST0017"),
        ("fn-upper-case/K-UpperCaseFunc-2", "upper-case(\"string\", \"wrong param\")", "X:XPST0017"),
        ("fn-upper-case/K-UpperCaseFunc-3", "upper-case(()) eq \"\"", "T"),
        ("fn-upper-case/K-UpperCaseFunc-4", "lower-case(\"ABc!D\") eq \"abc!d\"", "T"),
        (
            "fn-lower-case/fn-lower-case1args-1",
            "fn:lower-case(xs:string(\"This is a characte\"))",
            "S:this is a characte"
        ),
        (
            "fn-lower-case/fn-lower-case1args-2",
            "fn:lower-case(xs:string(\"This is a characte\"))",
            "S:this is a characte"
        ),
        (
            "fn-lower-case/fn-lower-case1args-3",
            "fn:lower-case(xs:string(\"This is a characte\"))",
            "S:this is a characte"
        ),
        ("fn-lower-case/fn-lower-case-1", "fn:lower-case(\"ABc!D\")", "S:abc!d"),
        ("fn-lower-case/fn-lower-case-2", "fn:count(fn:lower-case(()))", "S:1"),
        ("fn-lower-case/fn-lower-case-3", "fn:lower-case(\"12345\")", "S:12345"),
        ("fn-lower-case/fn-lower-case-4", "fn:lower-case(\"12345abcd\")", "S:12345abcd"),
        ("fn-lower-case/fn-lower-case-5", "fn:lower-case(\"ABCD\")", "S:abcd"),
        ("fn-lower-case/fn-lower-case-6", "fn:lower-case(\"abcde\")", "S:abcde"),
        ("fn-lower-case/fn-lower-case-7", "fn:lower-case(\"ABCDEabcde\")", "S:abcdeabcde"),
        ("fn-lower-case/fn-lower-case-8", "fn:count(fn:lower-case(\"\"))", "S:1"),
        ("fn-lower-case/fn-lower-case-9", "fn:lower-case(upper-case(\"AbcDH\"))", "S:abcdh"),
        ("fn-lower-case/fn-lower-case-10", "fn:lower-case(\"*****\")", "S:*****"),
        ("fn-lower-case/fn-lower-case-11", "fn:lower-case(lower-case(\"zzzzz\"))", "S:zzzzz"),
        (
            "fn-lower-case/fn-lower-case-13",
            "fn:concat(fn:lower-case(\"abcde\"), fn:lower-case(\"fghi\"))",
            "S:abcdefghi"
        ),
        ("fn-lower-case/fn-lower-case-15", "fn:lower-case(\"%$#@!\")", "S:%$#@!"),
        ("fn-lower-case/fn-lower-case-16", "fn:lower-case(\"lower-case\")", "S:lower-case"),
        (
            "fn-lower-case/fn-lower-case-18",
            "fn:string-to-codepoints(fn:lower-case(fn:codepoints-to-string(160 to 256)))",
            "D:160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, \n            177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 224, 225, \n            226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, \n            243, 244, 245, 246, 215, 248, 249, 250, 251, 252, 253, 254, 223, 224, 225, 226, 227, \n            228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, \n            245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 257"
        ),
        (
            "fn-lower-case/fn-lower-case-20",
            "fn:string-to-codepoints(fn:lower-case(fn:codepoints-to-string(459)))",
            "E:460"
        ),
        (
            "fn-lower-case/fn-lower-case-21",
            "fn:string-to-codepoints(fn:lower-case(fn:codepoints-to-string(304)))",
            "D:105, 775"
        ),
        ("fn-lower-case/K-LowerCaseFunc-1", "lower-case()", "X:XPST0017"),
        ("fn-lower-case/K-LowerCaseFunc-2", "lower-case(\"string\", \"wrong param\")", "X:XPST0017"),
        ("fn-lower-case/K-LowerCaseFunc-3", "lower-case(()) eq \"\"", "T"),
        ("fn-lower-case/K-LowerCaseFunc-4", "upper-case(\"abCd0\") eq \"ABCD0\"", "T"),
        (
            "fn-string-join/fn-string-join2args-1",
            "fn:string-join(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "S:This is a characte"
        ),
        (
            "fn-string-join/fn-string-join2args-2",
            "fn:string-join(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "S:This is a characte"
        ),
        (
            "fn-string-join/fn-string-join2args-3",
            "fn:string-join(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "S:This is a characte"
        ),
        (
            "fn-string-join/fn-string-join2args-4",
            "fn:string-join(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "S:This is a characte"
        ),
        (
            "fn-string-join/fn-string-join2args-5",
            "fn:string-join(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "S:This is a characte"
        ),
        (
            "fn-string-join/fn-string-join-1",
            "fn:string-join(('Now', 'is', 'the', 'time', '...'), ' ')",
            "S:Now is the time ..."
        ),
        (
            "fn-string-join/fn-string-join-2",
            "fn:string-join(('Blow, ', 'blow, ', 'thou ', 'winter ', 'wind!'), '')",
            "S:Blow, blow, thou winter wind!"
        ),
        ("fn-string-join/fn-string-join-3", "fn:count(fn:string-join((), 'separator'))", "S:1"),
        ("fn-string-join/fn-string-join-4", "fn:count(fn:string-join((),\"\"))", "S:1"),
        ("fn-string-join/fn-string-join-5", "fn:count(fn:string-join(\"\",\"\"))", "S:1"),
        ("fn-string-join/fn-string-join-6", "fn:count(fn:string-join(\"\",\"A Character String\"))", "S:1"),
        ("fn-string-join/fn-string-join-7", "fn:count(fn:string-join((),\"A Character String\"))", "S:1"),
        ("fn-string-join/fn-string-join-8", "fn:string-join((\"AAAAABBBBBCCCCC\"),\"BBBBB\")", "S:AAAAABBBBBCCCCC"),
        ("fn-string-join/fn-string-join-9", "fn:string-join((\"AAAAABBBBB\"),\" \")", "S:AAAAABBBBB"),
        ("fn-string-join/fn-string-join-10", "fn:count(fn:string-join((\" \"),\"AAAAABBBBB\"))", "S:1"),
        ("fn-string-join/fn-string-join-13", "fn:string-join((xs:string(\"A\")),\"A\")", "S:A"),
        ("fn-string-join/fn-string-join-14", "fn:string-join((\"A\"),xs:string(\"A\"))", "S:A"),
        ("fn-string-join/fn-string-join-15", "fn:string-join((\"A\"),\"a\")", "S:A"),
        ("fn-string-join/fn-string-join-16", "fn:string-join((\"a\"),\"A\")", "S:a"),
        ("fn-string-join/fn-string-join-17", "fn:string-join(\"string-join\",\"string-join\")", "S:string-join"),
        (
            "fn-string-join/fn-string-join-18",
            "fn:string-join((\"string-joinstring-join\"),\"string-join\")",
            "S:string-joinstring-join"
        ),
        ("fn-string-join/fn-string-join-19", "fn:string-join(\"****\",\"***\")", "S:****"),
        ("fn-string-join/fn-string-join-20", "fn:string-join(\"12345\",\"1234\")", "S:12345"),
        ("fn-string-join/fn-string-join-21", "fn:string-join(\"string-join\",\"nioj-gnirts\")", "S:string-join"),
        ("fn-string-join/fn-string-join-27", "fn:string-join(1 to 5, \"\")", "X:XPTY0004"),
        ("fn-string-join/K-StringJoinFunc-1", "string-join(\"a string\")", "X:XPST0017"),
        ("fn-string-join/K-StringJoinFunc-2", "string-join(\"a string\", \"a string\", \"wrong param\")", "X:XPST0017"),
        (
            "fn-string-join/K-StringJoinFunc-3",
            "string-join(('Now', 'is', 'the', 'time', '...'), ' ') eq \"Now is the time ...\"",
            "T"
        ),
        ("fn-string-join/K-StringJoinFunc-4", "string-join((\"abc\", \"def\"), \"\") eq \"abcdef\"", "T"),
        (
            "fn-string-join/K-StringJoinFunc-5",
            "string-join(('Blow, ', 'blow, ', 'thou ', 'winter ', 'wind!'), '') eq \"Blow, blow, thou winter wind!\"",
            "T"
        ),
        ("fn-string-join/K-StringJoinFunc-6", "string-join((), 'separator') eq \"\"", "T"),
        ("fn-string-join/K-StringJoinFunc-7", "string-join(\"a string\", ())", "X:XPTY0004"),
        (
            "fn-normalize-unicode/fn-normalize-unicode1args-1",
            "normalize-unicode('Nothing to normalize.')",
            "S:Nothing to normalize."
        ),
        ("fn-normalize-unicode/fn-normalize-unicode1args-2", "normalize-unicode(())", "S:"),
        (
            "fn-normalize-unicode/fn-normalize-unicode1args-6",
            "(normalize-unicode('\u00C5') eq normalize-unicode('\u212B'))",
            "T"
        ),
        ("fn-normalize-unicode/fn-normalize-unicode1args-7", "normalize-unicode(12)", "X:XPTY0004"),
        ("fn-normalize-unicode/fn-normalize-unicode1args-8", "normalize-unicode('','','')", "X:XPST0017"),
        (
            "fn-normalize-unicode/fn-normalize-unicode2args-1",
            "normalize-unicode('Nothing to normalize.', 'NFC')",
            "S:Nothing to normalize."
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode2args-2",
            "normalize-unicode('Nothing to normalize.', 'nFc')",
            "S:Nothing to normalize."
        ),
        ("fn-normalize-unicode/fn-normalize-unicode2args-3", "normalize-unicode((), 'NFC')", "S:"),
        (
            "fn-normalize-unicode/fn-normalize-unicode2args-4",
            "(normalize-unicode('\u00C5', '') eq normalize-unicode('\u212B', ''))",
            "F"
        ),
        ("fn-normalize-unicode/fn-normalize-unicode2args-5", "normalize-unicode('',())", "X:XPTY0004"),
        ("fn-normalize-unicode/fn-normalize-unicode2args-6", "normalize-unicode('',12)", "X:XPTY0004"),
        (
            "fn-normalize-unicode/fn-normalize-unicode-1",
            "fn:concat(fn:normalize-unicode(\"\u00C5\",\" NFC \"),fn:normalize-unicode(\"\u00C5\",\"NFC\"))",
            "S:\u00C5\u00C5"
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode-2",
            "fn:normalize-unicode(\"\u00E8\",\"chancesareyoudonotsupportthis123ifyoudowaoo\")",
            "X:FOCH0003"
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode-3",
            "fn:upper-case(fn:normalize-unicode(\"normalizedstring\"))",
            "S:NORMALIZEDSTRING"
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode-4",
            "fn:lower-case(fn:normalize-unicode(\"NORMALIZEDSTRING\"))",
            "S:normalizedstring"
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode-5",
            "fn:string-length(fn:normalize-unicode(\"NORMALIZEDSTRING\"))",
            "S:16"
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode-6",
            "fn:concat(fn:normalize-unicode(\"NORMALIZEDSTRING\"),\"another string\")",
            "S:NORMALIZEDSTRINGanother string"
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode-7",
            "fn:substring(fn:normalize-unicode(\"NORMALIZEDSTRING\"),5)",
            "S:ALIZEDSTRING"
        ),
        ("fn-normalize-unicode/fn-normalize-unicode-8", "string-to-codepoints(normalize-unicode('\uFDD0'))", "S:64976"),
        (
            "fn-normalize-unicode/fn-normalize-unicode-9",
            "string-to-codepoints(fn:normalize-unicode('e\u0302\uFDD0e\u0302\uFDD0e\u0302'))",
            "D:234, 64976, 234, 64976, 234"
        ),
        (
            "fn-normalize-unicode/fn-normalize-unicode-10",
            "string-to-codepoints(fn:normalize-unicode('e\uFDD0\u0302'))",
            "D:101, 64976, 770"
        ),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-1", "normalize-unicode()", "X:XPST0017"),
        (
            "fn-normalize-unicode/K-NormalizeUnicodeFunc-2",
            "normalize-unicode(\"a string\", \"NFC\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-normalize-unicode/K-NormalizeUnicodeFunc-3",
            "normalize-unicode(\"a string\", \"example.com/notSupported/\")",
            "X:FOCH0003"
        ),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-4", "normalize-unicode(\"foo\") eq \"foo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-5", "normalize-unicode(\"foo\", \"NFC\") eq \"foo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-6", "normalize-unicode(\"foo\", \"NFD\") eq \"foo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-7", "normalize-unicode(\"foo\", \"NFKD\") eq \"foo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-8", "normalize-unicode(\"foo\", \"NFKC\") eq \"foo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-9", "normalize-unicode(\"f oo\", \"\") eq \"f oo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-10", "normalize-unicode(\"foo\", \"\") eq \"foo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-11", "normalize-unicode(\"f oo\") eq \"f oo\"", "T"),
        ("fn-normalize-unicode/K-NormalizeUnicodeFunc-12", "normalize-unicode(\"f oo\", \"NFC\") eq \"f oo\"", "T"),
        (
            "fn-normalize-unicode/cbcl-fn-normalize-unicode-001a",
            "normalize-unicode(\"blah\",\"FULLY-NORMALIZED\")",
            "X:FOCH0003"
        ),
        ("fn-normalize-unicode/cbcl-fn-normalize-unicode-004", "normalize-unicode((),\"NFC\")", "S:"),
        ("fn-normalize-unicode/cbcl-fn-normalize-unicode-005", "normalize-unicode(\"\",\"NFC\")", "S:"),
        (
            "fn-normalize-unicode/cbcl-fn-normalize-unicode-006a",
            "normalize-unicode(codepoints-to-string(2494),\"FULLY-NORMALIZED\")",
            "X:FOCH0003"
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string1args-1",
            "codepoints-to-string((98,223,1682,12365,63744))",
            "S:b\u00DF\u0692\u304D\uF900"
        ),
        ("fn-codepoints-to-string/fn-codepoints-to-string1args-2", "codepoints-to-string(())", "S:"),
        ("fn-codepoints-to-string/fn-codepoints-to-string1args-3", "codepoints-to-string('hello')", "X:XPTY0004"),
        ("fn-codepoints-to-string/fn-codepoints-to-string1args-4", "codepoints-to-string((),())", "X:XPST0017"),
        ("fn-codepoints-to-string/fn-codepoints-to-string-1", "fn:codepoints-to-string(0)", "X:FOCH0001"),
        ("fn-codepoints-to-string/fn-codepoints-to-string-2", "fn:codepoints-to-string(10000000)", "X:FOCH0001"),
        ("fn-codepoints-to-string/fn-codepoints-to-string-3", "fn:codepoints-to-string(49)", "S:1"),
        ("fn-codepoints-to-string/fn-codepoints-to-string-4", "fn:codepoints-to-string(97)", "S:a"),
        ("fn-codepoints-to-string/fn-codepoints-to-string-5", "fn:codepoints-to-string((49,97))", "S:1a"),
        ("fn-codepoints-to-string/fn-codepoints-to-string-6", "fn:codepoints-to-string((35, 42, 94, 36))", "S:#*^$"),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-7",
            "fn:codepoints-to-string((99,111,100,101,112,111,105,110,116,115,45,116,111,45,115,116,114,105,110,103))",
            "S:codepoints-to-string"
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-8",
            "xs:string(fn:codepoints-to-string((65,32,83,116,114,105,110,103)))",
            "S:A String"
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-9",
            "fn:upper-case(fn:codepoints-to-string((65,32,83,84,82,73,78,71)))",
            "S:A STRING"
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-10",
            "fn:lower-case(fn:codepoints-to-string((97,32,115,116,114,105,110,103)))",
            "S:a string"
        ),
        ("fn-codepoints-to-string/fn-codepoints-to-string-11", "fn:codepoints-to-string(xs:integer(97))", "S:a"),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-12",
            "fn:codepoints-to-string(xs:integer(fn:avg((65,32,83,116,114,105,110,103))))",
            "S:["
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-13",
            "fn:concat(fn:codepoints-to-string((49,97)),\"1a\")",
            "S:1a1a"
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-14",
            "fn:string-to-codepoints(fn:codepoints-to-string((49,97)))",
            "D:49, 97"
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-15",
            "fn:string-length(fn:codepoints-to-string((49,97)))",
            "E:2"
        ),
        (
            "fn-codepoints-to-string/fn-codepoints-to-string-16",
            "fn:string-join((fn:codepoints-to-string((49,97)),'ab'),'')",
            "S:1aab"
        ),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-1", "codepoints-to-string()", "X:XPST0017"),
        (
            "fn-codepoints-to-string/K-CodepointToStringFunc-2",
            "codepoints-to-string((84, 104), \"INVALID\")",
            "X:XPST0017"
        ),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-3", "codepoints-to-string(()) eq \"\"", "T"),
        (
            "fn-codepoints-to-string/K-CodepointToStringFunc-4",
            "codepoints-to-string((87, 36, 56, 87, 102, 96)) eq \"W$8Wf`\"",
            "T"
        ),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-5", "codepoints-to-string(57343)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-6", "codepoints-to-string(-500)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-7", "codepoints-to-string(0)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-8", "codepoints-to-string(8)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-9", "codepoints-to-string(9) eq \"\t\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-10", "codepoints-to-string(10) eq \"\n\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-11", "codepoints-to-string(11)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-12", "codepoints-to-string(12)", "X:FOCH0001"),
        (
            "fn-codepoints-to-string/K-CodepointToStringFunc-14",
            "string-to-codepoints(codepoints-to-string(14))",
            "E:14\tX:FOCH0001"
        ),
        (
            "fn-codepoints-to-string/K-CodepointToStringFunc-15",
            "string-to-codepoints(codepoints-to-string(31))",
            "E:31\tX:FOCH0001"
        ),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-16", "codepoints-to-string(32) eq \" \"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-17", "codepoints-to-string(27637) eq \"\u6BF5\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-18", "codepoints-to-string(55295) eq \"\uD7FF\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-19", "codepoints-to-string(55296)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-20", "codepoints-to-string(57343)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-21", "codepoints-to-string(57344) eq \"\uE000\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-22", "codepoints-to-string(61438) eq \"\uEFFE\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-23", "codepoints-to-string(65533) eq \"\uFFFD\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-24", "codepoints-to-string(65534)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-25", "codepoints-to-string(65535)", "X:FOCH0001"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-26", "codepoints-to-string(65536) eq \"\U00010000\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-27", "codepoints-to-string(589823) eq \"\U0008FFFF\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-28", "codepoints-to-string(1114111) eq \"\U0010FFFF\"", "T"),
        ("fn-codepoints-to-string/K-CodepointToStringFunc-29", "codepoints-to-string(1114112)", "X:FOCH0001"),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-006",
            "fn:codepoints-to-string( 55295 to 55297 )",
            "X:FOCH0001"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-007",
            "fn:codepoints-to-string( 55296 to 57343 )",
            "X:FOCH0001"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-008",
            "fn:codepoints-to-string( 65535 to 70000 )",
            "X:FOCH0001"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-009",
            "fn:codepoints-to-string( 65530 to 70000 )",
            "X:FOCH0001"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-015",
            "deep-equal( fn:string-to-codepoints(fn:codepoints-to-string(65536 to 66000)), 65536 to 66000 )",
            "T"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-016",
            "deep-equal( fn:string-to-codepoints(fn:codepoints-to-string(65536 to 100000)), 65536 to 100000 )",
            "T"
        ),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints1args-1",
            "fn:string-to-codepoints(xs:string(\"This is a characte\"))",
            "D:84, 104, 105, 115, 32, 105, 115, 32, 97, 32, 99, 104, 97, 114, 97, 99, 116, 101"
        ),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints1args-2",
            "fn:string-to-codepoints(xs:string(\"This is a characte\"))",
            "D:84, 104, 105, 115, 32, 105, 115, 32, 97, 32, 99, 104, 97, 114, 97, 99, 116, 101"
        ),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints1args-3",
            "fn:string-to-codepoints(xs:string(\"This is a characte\"))",
            "D:84, 104, 105, 115, 32, 105, 115, 32, 97, 32, 99, 104, 97, 114, 97, 99, 116, 101"
        ),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints1args-4",
            "string-to-codepoints('b\u00DF\u0692\u304D\uF900')",
            "D:98, 223, 1682, 12365, 63744"
        ),
        ("fn-string-to-codepoints/fn-string-to-codepoints1args-5", "fn:string-to-codepoints(\"\")", "C:0"),
        ("fn-string-to-codepoints/fn-string-to-codepoints1args-6", "string-to-codepoints(())", "C:0"),
        ("fn-string-to-codepoints/fn-string-to-codepoints1args-7", "string-to-codepoints(12)", "X:XPTY0004"),
        ("fn-string-to-codepoints/fn-string-to-codepoints1args-8", "string-to-codepoints('abc','def')", "X:XPST0017"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-1", "fn:string-to-codepoints('1')", "E:49"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-2", "fn:string-to-codepoints('a')", "E:97"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-3", "fn:string-to-codepoints('1a')", "D:49, 97"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-4", "fn:string-to-codepoints('#*^$')", "D:35, 42, 94, 36"),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints-5",
            "fn:string-to-codepoints('string-to-codepoints')",
            "D:115, 116, 114, 105, 110, 103, 45, 116, 111, 45, 99, 111, 100, 101, 112, 111, 105, 110, 116, 115"
        ),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints-6",
            "fn:string-to-codepoints(xs:string(\"A String\"))",
            "D:65, 32, 83, 116, 114, 105, 110, 103"
        ),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints-7",
            "fn:string-to-codepoints(fn:upper-case(\"A String\"))",
            "D:(65, 32, 83, 84, 82, 73, 78, 71)"
        ),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints-8",
            "fn:string-to-codepoints(fn:lower-case(\"A String\"))",
            "D:(97, 32, 115, 116, 114, 105, 110, 103)"
        ),
        ("fn-string-to-codepoints/fn-string-to-codepoints-9", "fn:count(fn:string-to-codepoints(\"A String\"))", "E:8"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-10", "fn:avg(fn:string-to-codepoints(\"A String\"))", "E:91"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-11", "fn:empty(fn:string-to-codepoints(\"A String\"))", "F"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-12", "fn:empty(fn:string-to-codepoints(()))", "T"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-13", "fn:exists(fn:string-to-codepoints(()))", "F"),
        ("fn-string-to-codepoints/fn-string-to-codepoints-14", "fn:exists(fn:string-to-codepoints(\"A String\"))", "T"),
        (
            "fn-string-to-codepoints/fn-string-to-codepoints-15",
            "fn:deep-equal(fn:string-to-codepoints(\"\U00010001\U00010002\"), (65537, 65538))",
            "T"
        ),
        ("fn-string-to-codepoints/K-StringToCodepointFunc-1", "string-to-codepoints()", "X:XPST0017"),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-2",
            "string-to-codepoints(\"str\", \"INVALID\")",
            "X:XPST0017"
        ),
        ("fn-string-to-codepoints/K-StringToCodepointFunc-3", "empty(string-to-codepoints(()))", "T"),
        ("fn-string-to-codepoints/K-StringToCodepointFunc-4", "empty(string-to-codepoints(\"\"))", "T"),
        ("fn-string-to-codepoints/K-StringToCodepointFunc-5", "count(string-to-codepoints(\"123\")) eq 3", "T"),
        ("fn-string-to-codepoints/K-StringToCodepointFunc-6", "count(string-to-codepoints(\"\")) eq 0", "T"),
        ("fn-string-to-codepoints/K-StringToCodepointFunc-7", "empty(string-to-codepoints(\"\"))", "T"),
        ("fn-string-to-codepoints/K-StringToCodepointFunc-8", "string-to-codepoints(\"e\")", "E:101"),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-9",
            "deep-equal(string-to-codepoints(\"ee\"), (101, 101))",
            "T"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-10",
            "deep-equal(string-to-codepoints(\"eee\"), (101, 101, 101))",
            "T"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-12",
            "deep-equal(string-to-codepoints(\"Th\u00E9r\u00E8se\"), (84, 104, 233, 114, 232, 115, 101))",
            "T"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-13",
            "codepoints-to-string((87, 36, 56, 87, 102, 96)) eq \"W$8Wf`\"",
            "T"
        ),
    ];

    /// <summary>Every applicable case that the selection leaves out, one by one, with the reason and the issue that closes it.</summary>
    public static readonly (string Id, string Reason)[] Exclusions =
    [
        ("fn-concat/fn-concat-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-concat/fn-concat-14", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-concat/fn-concat-17", "the test fixture has no lowering for this expression form: XP-14 (#295)"),
        ("fn-concat/cbcl-concat-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-substring/fn-substring-1", "the assertion (assert-type) needs the evaluator-backed runner: XP-14 (#295)"),
        ("fn-substring/fn-substring-13", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-substring/fn-substring-14", "a function of the boolean and numeric group: XP-58 (#271)"),
        (
            "fn-substring/fn-substring-22",
            "the case needs a context document, namespace, or base URI of its environment, which the test fixture does not supply: XP-14 (#295)"
        ),
        ("fn-substring/cbcl-substring-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-substring/cbcl-substring-002", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-substring-before/fn-substring-before-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-substring-before/fn-substring-before-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        (
            "fn-substring-before/fn-substring-before-23",
            "the case needs a context document, namespace, or base URI of its environment, which the test fixture does not supply: XP-14 (#295)"
        ),
        ("fn-substring-before/cbcl-substring-before-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-substring-after/fn-substring-after-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-substring-after/fn-substring-after-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        (
            "fn-substring-after/fn-substring-after-23",
            "the case needs a context document, namespace, or base URI of its environment, which the test fixture does not supply: XP-14 (#295)"
        ),
        ("fn-substring-after/cbcl-substring-after-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-contains/fn-contains-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-contains/fn-contains-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-contains/K-ContainsFunc-7", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-contains/K-ContainsFunc-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-contains/cbcl-contains-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-contains/cbcl-contains-002", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-contains/cbcl-contains-003", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-starts-with/fn-starts-with-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-starts-with/fn-starts-with-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-starts-with/K-StartsWithFunc-8", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-starts-with/K-StartsWithFunc-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-starts-with/cbcl-starts-with-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-ends-with/fn-ends-with-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-ends-with/fn-ends-with-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-ends-with/K-EndsWithFunc-7", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-ends-with/K-EndsWithFunc-8", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-ends-with/K-EndsWithFunc-10", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-ends-with/cbcl-ends-with-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-translate/cbcl-fn-translate-001", "a function of the boolean and numeric group: XP-58 (#271)"),
        (
            "fn-normalize-space/fn-normalize-space1args-4",
            "the assertion (assert-type) needs the evaluator-backed runner: XP-14 (#295)"
        ),
        (
            "fn-normalize-space/fn-normalize-space0args-1",
            "the case needs a context document, namespace, or base URI of its environment, which the test fixture does not supply: XP-14 (#295)"
        ),
        ("fn-normalize-space/K-NormalizeSpaceFunc-2", "a function of the boolean and numeric group: XP-58 (#271)"),
        (
            "fn-normalize-space/K-NormalizeSpaceFunc-9",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        ("fn-string-length/fn-string-length-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-string-length/fn-string-length-14", "a function of the boolean and numeric group: XP-58 (#271)"),
        (
            "fn-string-length/fn-string-length-17",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-length/fn-string-length-19",
            "the case needs a context document, namespace, or base URI of its environment, which the test fixture does not supply: XP-14 (#295)"
        ),
        (
            "fn-string-length/fn-string-length-24",
            "the case needs a context document, namespace, or base URI of its environment, which the test fixture does not supply: XP-14 (#295)"
        ),
        (
            "fn-string-length/fn-string-length-25",
            "the case needs a context document, namespace, or base URI of its environment, which the test fixture does not supply: XP-14 (#295)"
        ),
        ("fn-string-length/K-StringLengthFunc-7", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-upper-case/fn-upper-case-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-upper-case/fn-upper-case-14", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-upper-case/fn-upper-case-17", "the test fixture has no lowering for this expression form: XP-14 (#295)"),
        (
            "fn-upper-case/fn-upper-case-19",
            "the case declares the dependency unicode-version 7.0, and its expected values differ from the tables of the pinned Unicode 18.0.0 at U+037F and U+03F3, which the tables map to each other: #100 (AP-16)"
        ),
        ("fn-lower-case/fn-lower-case-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-lower-case/fn-lower-case-14", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-lower-case/fn-lower-case-17", "the test fixture has no lowering for this expression form: XP-14 (#295)"),
        (
            "fn-lower-case/fn-lower-case-19",
            "the case declares the dependency unicode-version 7.0, and its expected values differ from the tables of the pinned Unicode 18.0.0 at U+037F and U+03F3, which the tables map to each other: #100 (AP-16)"
        ),
        ("fn-string-join/fn-string-join-11", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-string-join/fn-string-join-12", "a function of the boolean and numeric group: XP-58 (#271)"),
        ("fn-normalize-unicode/fn-normalize-unicode1args-3", "a regular expression function: XP-24 (#266)"),
        ("fn-normalize-unicode/fn-normalize-unicode1args-4", "a regular expression function: XP-24 (#266)"),
        ("fn-normalize-unicode/fn-normalize-unicode1args-5", "a regular expression function: XP-24 (#266)"),
        (
            "fn-normalize-unicode/K-NormalizeUnicodeFunc-13",
            "fn:error needs the evaluator-backed runner and its error alternatives: XP-14 (#295)"
        ),
        (
            "fn-normalize-unicode/cbcl-fn-normalize-unicode-001",
            "the case expects the fully-normalized form of fn:normalize-unicode: DEF-75 (#87)"
        ),
        (
            "fn-normalize-unicode/cbcl-fn-normalize-unicode-002",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-normalize-unicode/cbcl-fn-normalize-unicode-003",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-normalize-unicode/cbcl-fn-normalize-unicode-006",
            "the case expects the fully-normalized form of fn:normalize-unicode: DEF-75 (#87)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-005",
            "a range of more than 10^6 items needs the lazy range lowering: XP-15 (#226)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-010",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-011",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-012",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-013",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-014",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-017",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-019",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-020",
            "a function of the boolean and numeric group: XP-58 (#271)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-023",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-024",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-025",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-026",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-codepoints-to-string/cbcl-codepoints-to-string-027",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-11",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-14",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-15",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-16",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-17",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-18",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-19",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-20",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
        (
            "fn-string-to-codepoints/K-StringToCodepointFunc-21",
            "the test fixture has no lowering for this expression form: XP-14 (#295)"
        ),
    ];
}
