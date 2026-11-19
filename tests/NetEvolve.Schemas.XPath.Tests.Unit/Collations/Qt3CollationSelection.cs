namespace NetEvolve.Schemas.XPath.Tests.Unit.Collations;

/// <summary>
/// The selection of the QT3 test sets <c>fn-compare</c> and <c>fn-codepoint-equal</c>. The cases are the ones the XPath 2.0 rule of the
/// QT3 runner selects (the baselines of <c>tests/conformance/baselines/qt3</c>), taken from the pinned commit of the suite
/// (tests/conformance/suites/qt3.json): 57 of <c>fn-compare</c> and 30 of <c>fn-codepoint-equal</c>, 87 in all. Every one of them is in
/// <see cref="Cases"/>, where the compiled expression must satisfy its assertion, so 100 % of the applicable cases pass and none is
/// excluded.
/// </summary>
/// <remarks>
/// <para>
/// Limitation: the QT3 runner of the integration tests still has no evaluator behind it (#295, XP-14), so these cases run through the
/// lowerings of <see cref="Functions.Sequences.Qt3SequenceFixture"/> and not through the runner, and the baselines are unchanged. The
/// end-to-end run stays open until #295.
/// </para>
/// <para>
/// The cases of the two sets that the rule does not select are listed in <see cref="NotApplicable"/> one by one with the reason: the
/// Unicode Collation Algorithm collations (DEF-43, #55), the HTML ASCII case-insensitive collation (DEF-76, #88), a collation of the
/// test catalog other than the codepoint collation, and the cases that need XPath 3.0 or the XQuery prolog. Every collation URI other
/// than the codepoint collation raises FOCH0002 here, as <c>fn-compare/fn-compare-22</c> and <c>fn-compare/K-compareFunc-15</c> assert.
/// </para>
/// <para>
/// An assertion is written for <see cref="Functions.Sequences.Qt3SequenceFixture.Satisfies"/>: <c>T</c> or <c>F</c> for a boolean, <c>E:</c> for one
/// value equal to an expression, <c>C:</c> for an item count, and <c>X:</c> for an error code. Alternatives (<c>any-of</c>) are separated by
/// a tab. The type assertion of <c>compare-001</c> (<c>xs:integer</c>) is pinned by the case <c>K-compareFunc-14</c> and by the function tests.
/// </para>
/// </remarks>
internal static class Qt3CollationSelection
{
    public static readonly (string Id, string Expression, string Expectation)[] Cases =
    [
        (
            "fn-compare/fn-compare2args-1",
            "fn:compare(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "E:0"
        ),
        (
            "fn-compare/fn-compare2args-2",
            "fn:compare(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "E:0"
        ),
        (
            "fn-compare/fn-compare2args-3",
            "fn:compare(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "E:0"
        ),
        (
            "fn-compare/fn-compare2args-4",
            "fn:compare(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "E:0"
        ),
        (
            "fn-compare/fn-compare2args-5",
            "fn:compare(xs:string(\"This is a characte\"),xs:string(\"This is a characte\"))",
            "E:0"
        ),
        ("fn-compare/fn-compare-1", "fn:compare(\"\",\"\")", "E:0"),
        ("fn-compare/fn-compare-2", "fn:compare(\"\",\"A Character String\")", "E:-1"),
        ("fn-compare/fn-compare-3", "fn:compare(\"A Character String\",\"\")", "E:1"),
        ("fn-compare/fn-compare-4", "fn:count(fn:compare((),\"\"))", "E:0"),
        ("fn-compare/fn-compare-5", "fn:count(fn:compare(\"\",()))", "E:0"),
        ("fn-compare/fn-compare-6", "fn:count(fn:compare(\"A Character String\",()))", "E:0"),
        ("fn-compare/fn-compare-7", "fn:count(fn:compare((),\"A Character String\"))", "E:0"),
        ("fn-compare/fn-compare-8", "fn:compare(\"AAAAABBBBBCCCCC\",\"BBBBB\")", "E:-1"),
        ("fn-compare/fn-compare-9", "fn:compare(\"AAAAABBBBB\",\" \")", "E:1"),
        ("fn-compare/fn-compare-10", "fn:compare(\" \",\"AAAAABBBBB\")", "E:-1"),
        ("fn-compare/fn-compare-11", "fn:not(fn:compare(\"A\",\"A\"))", "T"),
        ("fn-compare/fn-compare-12", "fn:not(fn:compare(\"A\",\"B\"))", "F"),
        ("fn-compare/fn-compare-13", "fn:compare(xs:string(\"A\"),\"A\")", "E:0"),
        ("fn-compare/fn-compare-14", "fn:compare(\"A\",xs:string(\"A\"))", "E:0"),
        ("fn-compare/fn-compare-15", "fn:compare(\"A\",\"a\")", "E:-1"),
        ("fn-compare/fn-compare-16", "fn:compare(\"a\",\"A\")", "E:1"),
        ("fn-compare/fn-compare-17", "fn:compare(\"compare\",\"compare\")", "E:0"),
        ("fn-compare/fn-compare-18", "fn:compare(\"comparecompare\",\"compare\")", "E:1"),
        ("fn-compare/fn-compare-19", "fn:compare(\"****\",\"***\")", "E:1"),
        ("fn-compare/fn-compare-20", "fn:compare(\"12345\",\"1234\")", "E:1"),
        ("fn-compare/fn-compare-21", "fn:compare(\"compare\",\"erapmoc\")", "E:-1"),
        ("fn-compare/fn-compare-22", "fn:compare(\"a\",\"a\",\"CollationA\")", "X:FOCH0002"),
        ("fn-compare/K-compareFunc-1", "codepoint-equal()", "X:XPST0017"),
        ("fn-compare/K-compareFunc-2", "codepoint-equal(())", "X:XPST0017"),
        ("fn-compare/K-compareFunc-3", "codepoint-equal((), (), ())", "X:XPST0017"),
        ("fn-compare/K-compareFunc-4", "empty(codepoint-equal((), \"a string\"))", "T"),
        ("fn-compare/K-compareFunc-5", "empty(codepoint-equal(\"a string\", ()))", "T"),
        ("fn-compare/K-compareFunc-6", "codepoint-equal(\"a string\", \"a string\")", "T"),
        ("fn-compare/K-compareFunc-7", "not(codepoint-equal(\"cow\", \"a string\"))", "T"),
        ("fn-compare/K-compareFunc-8", "compare()", "X:XPST0017"),
        ("fn-compare/K-compareFunc-9", "compare(())", "X:XPST0017"),
        (
            "fn-compare/K-compareFunc-10",
            "compare((), (), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", \"wrong param\")",
            "X:XPST0017"
        ),
        ("fn-compare/K-compareFunc-11", "empty(compare((), \"a string\"))", "T"),
        ("fn-compare/K-compareFunc-12", "empty(compare(\"a string\", ()))", "T"),
        (
            "fn-compare/K-compareFunc-13",
            "empty(compare(\"a string\", (), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\"))",
            "T"
        ),
        ("fn-compare/K-compareFunc-14", "compare(\"str\", \"str\") instance of xs:integer", "T"),
        (
            "fn-compare/K-compareFunc-15",
            "empty(compare(\"a string\", \"a string\", \"http://www.example.com/COLLATION/NOT/SUPPORTED\"))",
            "F\tX:FOCH0002"
        ),
        ("fn-compare/compare-001", "compare(\"a\", \"b\")", "E:-1"),
        ("fn-compare/compare-002", "compare(\"b\", \"a\")", "E:1"),
        ("fn-compare/compare-003", "compare(\"b\", \"b\")", "E:0"),
        ("fn-compare/compare-004", "compare(\"b\", ())", "C:0"),
        ("fn-compare/compare-005", "compare((), \"b\")", "C:0"),
        ("fn-compare/compare-006", "compare((), ())", "C:0"),
        (
            "fn-compare/compare-007",
            "compare(\"\U00010001\", \"\U00010002\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "E:-1"
        ),
        (
            "fn-compare/compare-008",
            "compare(\"\U00010001\", \"\uFFF0\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "E:1"
        ),
        (
            "fn-compare/compare-009",
            "compare(\"\U00010001\", \"\uFFF0\", \"http://www.w3.org/2005/xpath-functions/collation/codepoint\")",
            "E:1"
        ),
        ("fn-compare/compare-011", "compare(123, 456)", "X:XPTY0004"),
        ("fn-compare/compare-012", "compare(xs:anyURI('http://www.example.com/'), 'http://www.example.com/')", "E:0"),
        (
            "fn-compare/compare-013",
            "compare(xs:untypedAtomic('http://www.example.com/'), 'http://www.example.com/')",
            "E:0"
        ),
        (
            "fn-compare/K2-CompareFunc-1",
            "compare(\"a\", \"a\", (\"http://www.w3.org/2005/xpath-functions/collation/codepoint\", ()))",
            "E:0"
        ),
        (
            "fn-compare/K2-CompareFunc-2",
            "compare(\"a\", \"a\", ((), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\"))",
            "E:0"
        ),
        (
            "fn-compare/K2-CompareFunc-3",
            "compare(\"a\", \"a\", ((), \"http://www.w3.org/2005/xpath-functions/collation/codepoint\", ()))",
            "E:0"
        ),
        ("fn-codepoint-equal/fn-codepoint-equal-1", "fn:codepoint-equal(\"a\")", "X:XPST0017"),
        ("fn-codepoint-equal/fn-codepoint-equal-2", "fn:codepoint-equal((),())", "C:0"),
        ("fn-codepoint-equal/fn-codepoint-equal-2a", "fn:codepoint-equal(\"abc\",())", "C:0"),
        ("fn-codepoint-equal/fn-codepoint-equal-2b", "fn:codepoint-equal((), \"\")", "C:0"),
        ("fn-codepoint-equal/fn-codepoint-equal-3", "fn:codepoint-equal(\"\",\"\")", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-4", "fn:codepoint-equal(\"a\",\"a\")", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-5", "fn:codepoint-equal(\"a\",\"b\")", "F"),
        ("fn-codepoint-equal/fn-codepoint-equal-6", "fn:not(fn:codepoint-equal(\"a\",\"b\"))", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-7", "fn:not(fn:codepoint-equal(\"a\",\"a\"))", "F"),
        ("fn-codepoint-equal/fn-codepoint-equal-8", "fn:codepoint-equal(fn:string(1),fn:string(1))", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-9", "fn:codepoint-equal(fn:string(\"aa\"),fn:string(\"aa\"))", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-10", "fn:codepoint-equal(xs:integer(1),xs:integer(1))", "X:XPTY0004"),
        ("fn-codepoint-equal/fn-codepoint-equal-11", "fn:codepoint-equal(\"aa\",xs:integer(1))", "X:XPTY0004"),
        ("fn-codepoint-equal/fn-codepoint-equal-12", "fn:codepoint-equal(\"aa\",\"AA\")", "F"),
        ("fn-codepoint-equal/fn-codepoint-equal-13", "fn:codepoint-equal(\"aa\",fn:lower-case(\"AA\"))", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-14", "fn:codepoint-equal(\"aa\",fn:upper-case(\"aa\"))", "F"),
        ("fn-codepoint-equal/fn-codepoint-equal-15", "xs:boolean(fn:codepoint-equal(\"aa\",\"aa\"))", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-16", "fn:codepoint-equal(\"aa\",\"aa\") and fn:true()", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-17", "fn:codepoint-equal(\"aa\",\"aa\") and fn:false()", "F"),
        ("fn-codepoint-equal/fn-codepoint-equal-18", "fn:codepoint-equal(\"aa\",\"aa\") or fn:true()", "T"),
        ("fn-codepoint-equal/fn-codepoint-equal-19", "fn:codepoint-equal(\"aa\",\"aa\") or fn:false()", "T"),
        (
            "fn-codepoint-equal/fn-codepoint-equal-20",
            "fn:codepoint-equal(\"aa\",\"aa\") and fn:codepoint-equal(\"aa\",\"aa\")",
            "T"
        ),
        (
            "fn-codepoint-equal/fn-codepoint-equal-21",
            "fn:codepoint-equal(\"aa\",\"aa\") or fn:codepoint-equal(\"aa\",\"aa\")",
            "T"
        ),
        (
            "fn-codepoint-equal/fn-codepoint-equal-22",
            "fn:codepoint-equal(normalize-unicode(\"gar\u00E7on\", \"NFC\"), normalize-unicode(\"gar\u00E7on\", \"NFD\"))",
            "F"
        ),
        (
            "fn-codepoint-equal/K2-CodepointEqual-1",
            "codepoint-equal(lower-case(concat(\"B STRING\", current-time())), lower-case(concat(\"b string\", current-time())))",
            "T"
        ),
        (
            "fn-codepoint-equal/K2-CodepointEqual-2",
            "codepoint-equal(upper-case(concat(\"B STRING\", current-time())), upper-case(concat(\"b string\", current-time())))",
            "T"
        ),
        (
            "fn-codepoint-equal/K2-CodepointEqual-3",
            "codepoint-equal(lower-case(concat(\"B STRING\", current-time())), lower-case(concat(\"no match\", current-time())))",
            "F"
        ),
        (
            "fn-codepoint-equal/K2-CodepointEqual-4",
            "codepoint-equal(upper-case(concat(\"B STRING\", current-time())), upper-case(concat(\"no match\", current-time())))",
            "F"
        ),
        (
            "fn-codepoint-equal/K2-CodepointEqual-5",
            "codepoint-equal(upper-case(concat(\"B STRING\", current-time())), lower-case(concat(\"no match\", current-time())))",
            "F"
        ),
        (
            "fn-codepoint-equal/K2-CodepointEqual-6",
            "codepoint-equal(lower-case(concat(\"B STRING\", current-time())), upper-case(concat(\"no match\", current-time())))",
            "F"
        ),
    ];

    /// <summary>The cases of the two sets that the XPath 2.0 rule of the QT3 runner does not select, with the work package that owns them.</summary>
    public static readonly (string Id, string Reason)[] NotApplicable =
    [
        ("fn-compare/K2-CompareFunc-4", "a let expression, which XPath 3.0 introduced"),
        ("fn-compare/K2-CompareFunc-5", "a let expression, which XPath 3.0 introduced"),
        ("fn-compare/K2-CompareFunc-6", "a let expression, which XPath 3.0 introduced"),
        ("fn-compare/K2-CompareFunc-7", "a let expression, which XPath 3.0 introduced"),
        ("fn-compare/K2-CompareFunc-8", "a let expression, which XPath 3.0 introduced"),
        ("fn-compare/K2-CompareFunc-9", "a let expression, which XPath 3.0 introduced"),
        (
            "fn-compare/compare-010",
            "needs the case-blind collation of the test catalog, a host collation: the dependency non_unicode_codepoint_collation"
        ),
        ("fn-compare/compare-014", "the HTML ASCII case-insensitive collation, XPath 3.1: DEF-76 (#88)"),
        ("fn-compare/compare-015", "the HTML ASCII case-insensitive collation, XPath 3.1: DEF-76 (#88)"),
        ("fn-compare/compare-016", "the HTML ASCII case-insensitive collation, XPath 3.1: DEF-76 (#88)"),
        ("fn-compare/compare-017", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-018", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-019", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-020", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-021", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-022", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-023", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-024", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-025", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-026", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-027", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-028", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-029", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-030", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-031", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-032", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-033", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-034", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-035", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-036", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-037", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-038", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-039", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-040", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-041", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-042", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-043", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-044", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        ("fn-compare/compare-045", "a Unicode Collation Algorithm collation, XPath 3.1: DEF-43 (#55)"),
        (
            "fn-codepoint-equal/cbcl-codepoint-equal-001",
            "a function declaration of the XQuery prolog, which XPath does not have"
        ),
        (
            "fn-codepoint-equal/cbcl-codepoint-equal-002",
            "a function declaration of the XQuery prolog, which XPath does not have"
        ),
        (
            "fn-codepoint-equal/cbcl-codepoint-equal-003",
            "a function declaration of the XQuery prolog, which XPath does not have"
        ),
        (
            "fn-codepoint-equal/cbcl-codepoint-equal-004",
            "a function declaration of the XQuery prolog, which XPath does not have"
        ),
        (
            "fn-codepoint-equal/cbcl-codepoint-equal-005",
            "a function declaration of the XQuery prolog, which XPath does not have"
        ),
        (
            "fn-codepoint-equal/cbcl-codepoint-equal-006",
            "a function declaration of the XQuery prolog, which XPath does not have"
        ),
    ];
}
