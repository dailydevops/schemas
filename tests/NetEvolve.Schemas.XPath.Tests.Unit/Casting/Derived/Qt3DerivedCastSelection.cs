namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

/// <summary>
/// The selection of the QT3 test set <c>prod-CastExpr.derived</c>, the two sets <c>xs-normalizedString</c> and <c>xs-token</c>, and the
/// cases of <c>prod-CastExpr</c> that name a derived type and that the casting packages before XP-23 left out (<c>K2-SeqExprCast-480</c>,
/// <c>489</c> to <c>494</c>, and <c>501</c> to <c>503</c>). The cases are taken from the pinned commit of the suite
/// (tests/conformance/suites/qt3.json); the expectation is a tab-separated list of alternatives: <c>T</c> or <c>F</c> for a boolean,
/// <c>S:</c> and <c>N:</c> for a string value (<c>N:</c> with normalized space), <c>E:</c> for a value equal to an expression, and <c>X:</c>
/// for an error code.
/// </summary>
/// <remarks>
/// <para>
/// Every case of the three sets is in <see cref="Cases"/> or in <see cref="Excluded"/>, and an exclusion names its reason one by one.
/// The set <c>prod-CastExpr.derived</c> carries the dependency <c>XP30+ XQ10+</c>, so the QT3 selector of the integration tests picks none
/// of its cases for XPath 2.0, and the runner of the integration tests has no evaluator behind it yet (#295); these cases therefore run
/// through the lowerings of the production code, as the selections of the other casting packages do.
/// </para>
/// <para>
/// Value-preserving rewrite: the expression of a case in the form <c>let $value := E return $value cast as T</c> is written as
/// <c>E cast as T</c>, because the variable is used once and XPath 2.0 has no <see langword="let"/> expression (#355). An <c>any-of</c> assertion
/// lists all its alternatives; the assertion <c>string-length(string($result)) gt N</c> of the cases <c>cbcl-cast-decimal-001</c> to <c>004</c>
/// and <c>cbcl-cast-integer-001</c> to <c>004</c> is the alternative <c>G:N</c>, a string value longer than N characters.
/// </para>
/// </remarks>
internal static class Qt3DerivedCastSelection
{
    public static readonly (string Id, string Expression, string Expectation)[] Cases =
    [
        ("prod-CastExpr.derived/cast-derived-1", "xs:float(10.0) cast as xs:decimal", "E:10"),
        ("prod-CastExpr.derived/cast-derived-2", "xs:float(10.0) cast as xs:decimal", "E:10"),
        ("prod-CastExpr.derived/cast-derived-3", "xs:float(-10.0) cast as xs:nonPositiveInteger", "E:-10"),
        ("prod-CastExpr.derived/cast-derived-4", "xs:float(10.0) cast as xs:long", "E:10"),
        ("prod-CastExpr.derived/cast-derived-5", "xs:float(10.0) cast as xs:nonNegativeInteger", "E:10"),
        ("prod-CastExpr.derived/cast-derived-6", "xs:float(-10.0) cast as xs:negativeInteger", "E:-10"),
        ("prod-CastExpr.derived/cast-derived-7", "xs:float(10.0) cast as xs:int", "E:10"),
        ("prod-CastExpr.derived/cast-derived-8", "xs:float(10.0) cast as xs:unsignedLong", "E:10"),
        ("prod-CastExpr.derived/cast-derived-9", "xs:float(10.0) cast as xs:positiveInteger", "E:10"),
        ("prod-CastExpr.derived/cast-derived-10", "xs:float(10.0) cast as xs:short", "E:10"),
        ("prod-CastExpr.derived/cast-derived-11", "xs:float(10.0) cast as xs:unsignedInt", "E:10"),
        ("prod-CastExpr.derived/cast-derived-12", "xs:float(10.0) cast as xs:unsignedShort", "E:10"),
        ("prod-CastExpr.derived/cast-derived-13", "xs:float(10.0) cast as xs:unsignedByte", "E:10"),
        ("prod-CastExpr.derived/cast-derived-14", "xs:float(10.0) cast as xs:byte", "E:10"),
        ("prod-CastExpr.derived/cast-derived-15", "xs:double(10E2) cast as xs:decimal", "E:1000"),
        ("prod-CastExpr.derived/cast-derived-16", "xs:double(10E2) cast as xs:integer", "E:1000"),
        ("prod-CastExpr.derived/cast-derived-17", "xs:double(10E2) cast as xs:positiveInteger", "E:1000"),
        ("prod-CastExpr.derived/cast-derived-18", "xs:double(10E2) cast as xs:long", "E:1000"),
        ("prod-CastExpr.derived/cast-derived-19", "xs:double(10E2) cast as xs:int", "E:1000"),
        ("prod-CastExpr.derived/cast-derived-20", "xs:double(10E2) cast as xs:unsignedLong", "E:1000"),
        ("prod-CastExpr.derived/cast-derived-21", "xs:double(10E2) cast as xs:short", "E:1000"),
        ("prod-CastExpr.derived/cast-to-parent-1", "xs:decimal(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-2", "xs:integer(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-3", "xs:nonPositiveInteger(-10.0) cast as xs:float", "E:-10"),
        ("prod-CastExpr.derived/cast-to-parent-4", "xs:long(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-5", "xs:nonNegativeInteger(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-6", "xs:negativeInteger(-10.0) cast as xs:float", "E:-10"),
        ("prod-CastExpr.derived/cast-to-parent-7", "xs:int(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-8", "xs:unsignedLong(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-9", "xs:positiveInteger(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-10", "xs:short(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-11", "xs:unsignedInt(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-12", "xs:unsignedShort(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-13", "xs:unsignedByte(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-14", "xs:byte(10.0) cast as xs:float", "E:10"),
        ("prod-CastExpr.derived/cast-to-parent-15", "xs:decimal(10E2) cast as xs:double", "E:1000"),
        ("prod-CastExpr.derived/cast-to-parent-16", "xs:integer(10E2) cast as xs:double", "E:1000"),
        ("prod-CastExpr.derived/cast-to-parent-17", "xs:positiveInteger(10E2) cast as xs:double", "E:1000"),
        ("prod-CastExpr.derived/cast-to-parent-18", "xs:long(10E2) cast as xs:double", "E:1000"),
        ("prod-CastExpr.derived/cast-to-parent-19", "xs:int(10E2) cast as xs:double", "E:1000"),
        ("prod-CastExpr.derived/cast-to-parent-20", "xs:unsignedLong(10E2) cast as xs:double", "E:1000"),
        ("prod-CastExpr.derived/cast-to-parent-21", "xs:short(10E2) cast as xs:double", "E:1000"),
        ("prod-CastExpr.derived/cast-within-1", "xs:integer(-10.0) cast as xs:nonPositiveInteger", "E:-10"),
        ("prod-CastExpr.derived/cast-within-2", "xs:integer(10.0) cast as xs:long", "E:10"),
        ("prod-CastExpr.derived/cast-within-3", "xs:integer(10.0) cast as xs:nonNegativeInteger", "E:10"),
        ("prod-CastExpr.derived/cast-within-4", "xs:integer(-10.0) cast as xs:negativeInteger", "E:-10"),
        ("prod-CastExpr.derived/cast-within-5", "xs:integer(10.0) cast as xs:int", "E:10"),
        ("prod-CastExpr.derived/cast-within-6", "xs:integer(10.0) cast as xs:unsignedLong", "E:10"),
        ("prod-CastExpr.derived/cast-within-7", "xs:integer(10.0) cast as xs:positiveInteger", "E:10"),
        ("prod-CastExpr.derived/cast-within-8", "xs:integer(10.0) cast as xs:short", "E:10"),
        ("prod-CastExpr.derived/cast-within-9", "xs:integer(10.0) cast as xs:unsignedInt", "E:10"),
        ("prod-CastExpr.derived/cast-within-10", "xs:integer(10.0) cast as xs:byte", "E:10"),
        ("prod-CastExpr.derived/cast-within-11", "xs:integer(10.0) cast as xs:unsignedShort", "E:10"),
        ("prod-CastExpr.derived/cast-within-12", "xs:integer(10.0) cast as xs:unsignedByte", "E:10"),
        ("prod-CastExpr.derived/cast-within-13", "xs:long(10.0) cast as xs:integer", "E:10"),
        ("prod-CastExpr.derived/cast-within-14", "xs:long(10.0) cast as xs:nonNegativeInteger", "E:10"),
        ("prod-CastExpr.derived/cast-within-15", "xs:long(-10) cast as xs:negativeInteger", "E:-10"),
        ("prod-CastExpr.derived/cast-within-16", "xs:long(10) cast as xs:int", "E:10"),
        ("prod-CastExpr.derived/cast-within-17", "xs:long(10) cast as xs:unsignedLong", "E:10"),
        ("prod-CastExpr.derived/cast-within-18", "xs:long(10) cast as xs:positiveInteger", "E:10"),
        ("prod-CastExpr.derived/cast-within-19", "xs:long(10) cast as xs:short", "E:10"),
        ("prod-CastExpr.derived/cast-within-20", "xs:long(10) cast as xs:unsignedInt", "E:10"),
        ("prod-CastExpr.derived/cast-within-21", "xs:long(10) cast as xs:byte", "E:10"),
        ("prod-CastExpr.derived/cbcl-cast-byte-001", "xs:byte(128)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-byte-002", "xs:byte(-129)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-byte-003", "xs:byte(\"128\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-byte-004", "xs:byte(\"-129\")", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-dayTimeDuration-001",
            "\"P11768614336404564651D\" cast as xs:dayTimeDuration",
            "X:FODT0002\tE:xs:dayTimeDuration(\"P11768614336404564651D\")"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-dayTimeDuration-002",
            "\"-P11768614336404564651D\" cast as xs:dayTimeDuration",
            "X:FODT0002\tE:xs:dayTimeDuration(\"-P11768614336404564651D\")"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-decimal-001",
            "1.7976931348623157E+308 cast as xs:decimal",
            "X:FOCA0001\tG:300"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-decimal-002",
            "-1.7976931348623157E+308 cast as xs:decimal",
            "X:FOCA0001\tG:300"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-decimal-003",
            "xs:float('3.402823e38') cast as xs:decimal",
            "X:FOCA0001\tG:36"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-decimal-004",
            "xs:float('-3.402823e38') cast as xs:decimal",
            "X:FOCA0001\tG:36"
        ),
        ("prod-CastExpr.derived/cbcl-cast-duration-001", "\"-P768614336404564651Y\" cast as xs:duration", "X:FODT0002"),
        ("prod-CastExpr.derived/cbcl-cast-duration-002", "\"P768614336404564651Y\" cast as xs:duration", "X:FODT0002"),
        ("prod-CastExpr.derived/cbcl-cast-entity-001", "xs:NCName('entity') cast as xs:ENTITY", "S:entity"),
        ("prod-CastExpr.derived/cbcl-cast-entity-002", "fn:current-time() cast as xs:ENTITY", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-id-001", "xs:NCName('id') cast as xs:ID", "S:id"),
        ("prod-CastExpr.derived/cbcl-cast-id-002", "fn:current-time() cast as xs:ID", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-idref-001", "xs:NCName('idref') cast as xs:IDREF", "S:idref"),
        ("prod-CastExpr.derived/cbcl-cast-idref-002", "fn:current-time() cast as xs:IDREF", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-int-001", "xs:int(2147483648)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-int-002", "xs:int(-2147483649)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-int-003", "xs:int(\"2147483648\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-int-004", "xs:int(\"-2147483649\")", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-integer-001",
            "1.7976931348623157E+308 cast as xs:integer",
            "X:FOCA0003\tG:300"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-integer-002",
            "-1.7976931348623157E+308 cast as xs:integer",
            "X:FOCA0003\tG:300"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-integer-003",
            "xs:float('3.402823e38') cast as xs:integer",
            "X:FOCA0003\tG:36"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-integer-004",
            "xs:float('-3.402823e38') cast as xs:integer",
            "X:FOCA0003\tG:36"
        ),
        ("prod-CastExpr.derived/cbcl-cast-language-001", "xs:language('en-gb') cast as xs:language", "S:en-gb"),
        ("prod-CastExpr.derived/cbcl-cast-long-001", "xs:long(9223372036854775808)", "X:FORG0001\tX:FOAR0002"),
        ("prod-CastExpr.derived/cbcl-cast-long-002", "xs:long(-9223372036854775809)", "X:FORG0001\tX:FOAR0002"),
        ("prod-CastExpr.derived/cbcl-cast-long-003", "xs:long(\"9223372036854775808\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-long-004", "xs:long(\"-9223372036854775809\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-name-001", "xs:NCName('NCName') cast as xs:Name", "S:NCName"),
        ("prod-CastExpr.derived/cbcl-cast-name-002", "fn:current-time() cast as xs:Name", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-ncname-001", "xs:ID('id') cast as xs:NCName", "S:id"),
        ("prod-CastExpr.derived/cbcl-cast-ncname-002", "fn:current-time() cast as xs:NCName", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-negativeInteger-001", "xs:negativeInteger(0)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-negativeInteger-002", "xs:negativeInteger(\"0\")", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-nmtoken-001b",
            "for $string in (\"\r\n\t foobar \n\r\t\" cast as xs:NMTOKEN) return not(contains($string, '\t') or contains($string, '\n') or contains($string, '\r') or string-length($string) ne 6)",
            "T"
        ),
        ("prod-CastExpr.derived/cbcl-cast-nmtoken-002", "xs:NMTOKEN('NMTOKEN') cast as xs:NMTOKEN", "S:NMTOKEN"),
        ("prod-CastExpr.derived/cbcl-cast-nonNegativeInteger-001", "xs:nonNegativeInteger(-1)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-nonNegativeInteger-002", "xs:nonNegativeInteger(\"-1\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-nonPositiveInteger-001", "xs:nonPositiveInteger(1)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-nonPositiveInteger-002", "xs:nonPositiveInteger(\"1\")", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-normalizedString-001b",
            "for $string in (\"\r foo \t bar \n\" cast as xs:normalizedString)\n        return not(contains($string, '\t') or \n                   contains($string, '\n') or \n                   contains($string, '\r') or \n                   string-length($string) ne 13)",
            "T"
        ),
        ("prod-CastExpr.derived/cbcl-cast-positiveInteger-001", "xs:positiveInteger(0)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-positiveInteger-002", "xs:positiveInteger(\"0\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-short-001", "xs:short(32768)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-short-002", "xs:short(-32769)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-short-003", "xs:short(\"32768\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-short-004", "xs:short(\"-32769\")", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-token-001b",
            "for $string in (\"\r foo \t bar \n\" cast as xs:token) \n        return not(contains($string, '\t') or \n                   contains($string, '\n') or \n                   contains($string, '\r') or \n                   string-length($string) ne 7)",
            "T"
        ),
        ("prod-CastExpr.derived/cbcl-cast-unsignedByte-001", "xs:unsignedByte(256)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedByte-002", "xs:unsignedByte(-1)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedByte-003", "xs:unsignedByte(\"256\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedByte-004", "xs:unsignedByte(\"-1\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedInt-001", "xs:unsignedInt(4294967296)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedInt-002", "xs:unsignedInt(-1)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedInt-003", "xs:unsignedInt(\"4294967296\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedInt-004", "xs:unsignedInt(\"-1\")", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-unsignedLong-001",
            "xs:unsignedLong(18446744073709551616)",
            "X:FORG0001\tX:FOAR0002"
        ),
        ("prod-CastExpr.derived/cbcl-cast-unsignedLong-002", "xs:unsignedLong(-1)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedLong-003", "xs:unsignedLong(\"18446744073709551616\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedLong-004", "xs:unsignedLong(\"-1\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedShort-001", "xs:unsignedShort(65536)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedShort-002", "xs:unsignedShort(-1)", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedShort-003", "xs:unsignedShort(\"65536\")", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedShort-004", "xs:unsignedShort(\"-1\")", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-yearMonthDuration-001",
            "\"-P768614336404564651Y\" cast as xs:yearMonthDuration",
            "X:FODT0002"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-yearMonthDuration-002",
            "\"P768614336404564651Y\" cast as xs:yearMonthDuration",
            "X:FODT0002"
        ),
        ("prod-CastExpr.derived/cbcl-cast-date-003", "\"18446744073709551616-05-15\" cast as xs:date", "X:FODT0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-date-004",
            "\"18446744073709551616-QQ-15\" cast as xs:date",
            "X:FORG0001\tX:FODT0001"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-dateTime-003",
            "\"18446744073709551616-05-15T16:15:00\" cast as xs:dateTime",
            "X:FODT0001"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-dateTime-004",
            "\"18446744073709551616-QQ-15T16:15:00\" cast as xs:dateTime",
            "X:FORG0001\tX:FODT0001"
        ),
        ("prod-CastExpr.derived/cbcl-cast-nonNegativeInteger-003", "xs:nonNegativeInteger(\"-00\")", "S:0"),
        ("prod-CastExpr.derived/cbcl-cast-nonPositiveInteger-003", "xs:nonPositiveInteger(\"+00\")", "S:0"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedByte-005", "xs:unsignedByte(\"-00\")", "S:0"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedInt-005", "xs:unsignedInt(\"-00\")", "S:0"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedLong-005", "xs:unsignedLong(\"-00\")", "S:0"),
        ("prod-CastExpr.derived/cbcl-cast-unsignedShort-005", "xs:unsignedShort(\"-00\")", "S:0"),
        (
            "prod-CastExpr.derived/cbcl-cast-gYear-001",
            "\"99999999999999999999999999999\" cast as xs:gYear",
            "X:FODT0001"
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-gYearMonth-001",
            "\"99999999999999999999999999999-01\" cast as xs:gYearMonth",
            "X:FODT0001"
        ),
        ("prod-CastExpr.derived/cbcl-cast-gYear-002", "\"0000\" cast as xs:gYear", "X:FORG0001"),
        ("prod-CastExpr.derived/cbcl-cast-gYear-003", "\"-0000\" cast as xs:gYear", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-cast-gYearMonth-002",
            "\"99999999999999999999999999999-XX\" cast as xs:gYearMonth",
            "X:FORG0001"
        ),
        ("prod-CastExpr.derived/cbcl-cast-gYearMonth-003", "\"0000-05\" cast as xs:gYearMonth", "X:FORG0001"),
        (
            "prod-CastExpr.derived/cbcl-case-anyUri-001",
            "\"http://example.com?query=\" cast as xs:anyURI",
            "S:http://example.com?query="
        ),
        (
            "xs-normalizedString/cbcl-normalizedstring-001",
            "xs:normalizedString(xs:normalizedString(\"test\"))",
            "S:test"
        ),
        ("xs-normalizedString/cbcl-normalizedstring-002b", "xs:normalizedString('\t')", "S: "),
        ("xs-normalizedString/cbcl-normalizedstring-003", "xs:normalizedString(5)", "S:5"),
        ("xs-normalizedString/cbcl-normalizedstring-004", "\"&#x9;\" castable as xs:normalizedString", "T"),
        ("xs-normalizedString/cbcl-normalizedstring-005", "5 castable as xs:normalizedString", "T"),
        (
            "xs-normalizedString/cbcl-normalizedstring-006",
            "xs:normalizedString(\"test\") castable as xs:normalizedString",
            "T"
        ),
        ("xs-token/cbcl-token-001", "xs:token(xs:token(\"test\"))", "S:test"),
        ("xs-token/cbcl-token-002b", "string-length(xs:token('\t'))", "S:0"),
        ("xs-token/cbcl-token-003", "xs:token(5)", "S:5"),
        ("xs-token/cbcl-token-004", "\"&#x9;\" castable as xs:token", "T"),
        ("xs-token/cbcl-token-005", "5 castable as xs:token", "T"),
        ("xs-token/cbcl-token-006", "xs:token(\"test\") castable as xs:token", "T"),
        (
            "prod-CastExpr/K2-SeqExprCast-480",
            "xs:untypedAtomic(\"xs:untypedAtomic\"), xs:dateTime(\"2002-10-10T23:02:12Z\"), xs:date(\"2002-10-10Z\"), xs:time(\"23:02:12Z\"), xs:duration(\"P12M\"), xs:dayTimeDuration(\"PT1S\"), xs:yearMonthDuration(\"P1M\"), xs:float(\"3e3\"), xs:double(\"4e4\"), xs:decimal(\"2.0\"), xs:integer(\"16\"), xs:nonPositiveInteger(\"0\"), xs:negativeInteger(\"-4\"), xs:long(\"5\"), xs:int(\"6\"), xs:short(\"7\"), xs:byte(\"8\"), xs:nonNegativeInteger(\"9\"), xs:unsignedLong(\"10\"), xs:unsignedInt(\"11\"), xs:unsignedShort(\"12\"), xs:unsignedByte(\"13\"), xs:positiveInteger(\"14\"), xs:gYearMonth(\"1976-02Z\"), xs:gYear(\"2005-12:00\"), xs:gMonthDay(\"--12-25-14:00\"), xs:gDay(\"---25-14:00\"), xs:gMonth(\"--12-14:00\"), xs:boolean(\"true\"), xs:base64Binary(\"aaaa\"), xs:hexBinary(\"FFFF\"), xs:anyURI(\"http://example.com/\"), xs:QName(\"localName\"), xs:string(\"An xs:string\"), xs:normalizedString(\"normalizedString\"), xs:token(\"token\"), xs:language(\"language\"), xs:NMTOKEN(\"NMTOKEN\"), xs:Name(\"Name\"), xs:NCName(\"NCName\"), xs:ID(\"ID\"), xs:IDREF(\"IDREF\"), xs:ENTITY(\"ENTITY\")",
            "N:\n             xs:untypedAtomic 2002-10-10T23:02:12Z 2002-10-10Z 23:02:12Z P1Y PT1S P1M 3000 40000 2 16 0\n             -4 5 6 7 8 9 10 11 12 13 14 1976-02Z 2005-12:00 --12-25-14:00 ---25-14:00 --12-14:00 true\n             aaaa FFFF http://example.com/ localName An xs:string normalizedString token language\n             NMTOKEN Name NCName ID IDREF ENTITY"
        ),
        ("prod-CastExpr/K2-SeqExprCast-489", "xs:language(xs:base64Binary(\"1111\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-490", "xs:language(xs:hexBinary(\"11\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-491", "xs:language(xs:gMonth(\"--10\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-492", "xs:language(xs:gDay(\"---11\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-493", "xs:language(xs:gMonthDay(\"--10-15\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-494", "xs:language(xs:gYear(\"4004\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-501", "xs:language(xs:duration(\"P1M\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-502", "xs:language(xs:dayTimeDuration(\"P1DT4H\"))", "X:FORG0001"),
        ("prod-CastExpr/K2-SeqExprCast-503", "xs:language(xs:yearMonthDuration(\"P4Y5M\"))", "X:FORG0001"),
    ];

    /// <summary>The cases of the three sets that are left out, each with its reason.</summary>
    public static readonly (string Id, string Reason)[] Excluded =
    [
        (
            "prod-CastExpr.derived/cbcl-cast-derived-001",
            "The case imports a schema, and schema-aware processing is not supported (DEF-40, #52)."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-date-001",
            "The case expects an implementation-defined year range smaller than the 17-digit years of the datatype layer, which accepts the year (the temporal casting rows, #301)."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-date-002",
            "The case expects an implementation-defined year range smaller than the 17-digit years of the datatype layer, which accepts the year (the temporal casting rows, #301)."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-dateTime-001",
            "The case expects an implementation-defined year range smaller than the 17-digit years of the datatype layer, which accepts the year (the temporal casting rows, #301)."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-dateTime-002",
            "The case expects an implementation-defined year range smaller than the 17-digit years of the datatype layer, which accepts the year (the temporal casting rows, #301)."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-nmtoken-001",
            "The case is the XQuery 1.0 variant, whose string literal uses character references; the XPath 2.0 variant \"cbcl-cast-nmtoken-001b\" is selected."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-normalizedString-001",
            "The case is the XQuery 1.0 variant, whose string literal uses character references; the XPath 2.0 variant \"cbcl-cast-normalizedString-001b\" is selected."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-token-001",
            "The case is the XQuery 1.0 variant, whose string literal uses character references; the XPath 2.0 variant \"cbcl-cast-token-001b\" is selected."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-gYear-003a",
            "The case depends on XSD 1.1, and XPath 2.0 refers to XML Schema 1.0."
        ),
        (
            "prod-CastExpr.derived/cbcl-cast-gYearMonth-004",
            "The case depends on XSD 1.1, and XPath 2.0 refers to XML Schema 1.0."
        ),
        (
            "xs-normalizedString/cbcl-normalizedstring-002",
            "The case is the XQuery 1.0 variant, whose string literal uses character references; the XPath 2.0 variant \"cbcl-normalizedstring-002b\" is selected."
        ),
        (
            "xs-token/cbcl-token-002",
            "The case is the XQuery 1.0 variant, whose string literal uses character references; the XPath 2.0 variant \"cbcl-token-002b\" is selected."
        ),
    ];
}
