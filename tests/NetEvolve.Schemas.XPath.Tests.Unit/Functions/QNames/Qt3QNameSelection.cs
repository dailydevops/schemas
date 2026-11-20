namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

/// <summary>
/// The selection of the QT3 test sets <c>fn-resolve-QName</c>, <c>fn-QName</c>, <c>fn-prefix-from-QName</c>, <c>fn-local-name-from-QName</c>,
/// <c>fn-namespace-uri-from-QName</c>, <c>fn-namespace-uri-for-prefix</c>, <c>fn-in-scope-prefixes</c>, <c>fn-resolve-uri</c>, <c>fn-base-uri</c>,
/// <c>fn-static-base-uri</c>, <c>fn-encode-for-uri</c>, and <c>fn-iri-to-uri</c>. The cases are the ones the XPath 2.0 rule of the QT3 runner
/// selects (the baselines of <c>tests/conformance/baselines/qt3</c>), taken from the pinned commit of the suite
/// (tests/conformance/suites/qt3.json): every one of them is either in <see cref="Cases"/>, where the compiled expression must satisfy its
/// assertion, or in <see cref="Exclusions"/> with the reason and the issue that closes it.
/// </summary>
/// <remarks>
/// <para>
/// Limitation: the QT3 runner of the integration tests still has no evaluator behind it (#295), so these cases run through the lowerings of
/// <see cref="Sequences.Qt3SequenceFixture"/> and not through the runner, and the baselines are unchanged. The end-to-end run stays open
/// until #295.
/// </para>
/// <para>
/// Rewrite that keeps the value: a case of the environments that only bind a prefix writes <c>xs:QName("foo:name")</c>, which the
/// constructor function resolves against no namespaces; the case is written with <c>fn:QName("http://example.org", "foo:name")</c>, which
/// builds the same value, because the lowerings of the fixture have no static namespaces.
/// </para>
/// <para>
/// The same holds for the case <c>fn-resolve-uri-33</c>, which asserts the string value of an <c>xs:anyURI</c> result: the fixture renders
/// no <c>xs:anyURI</c> item, so the call is wrapped in <c>fn:string</c>, which gives the same string value.
/// </para>
/// <para>
/// An assertion is written for <see cref="Numerics.Qt3NumericFixture.Satisfies"/>: <c>T</c> or <c>F</c> for a boolean, <c>S:</c> and <c>N:</c> for
/// a string value (<c>N:</c> with normalized space), <c>E:</c> for one value equal to an expression, <c>D:</c> for a sequence deep-equal
/// to an expression, <c>C:</c> for an item count, <c>Y:</c> for an item of a type, and <c>X:</c> for an error code. Alternatives
/// (<c>any-of</c>) are separated by a tab, and assertions that must all hold (<c>all-of</c>) by the unit separator (U+001F).
/// </para>
/// </remarks>
internal static class Qt3QNameSelection
{
    public static readonly (string Id, string Expression, string Expectation)[] Cases =
    [
        ("fn-resolve-QName/K-ResolveQNameConstructFunc-1", "resolve-QName()", "X:XPST0017"),
        ("fn-resolve-QName/K-ResolveQNameConstructFunc-2", "resolve-QName(\"wrongparam\")", "X:XPST0017"),
        (
            "fn-resolve-QName/K-ResolveQNameConstructFunc-3",
            "resolve-QName(\"wrongparam\", \"takes a node\", \"wrong\")",
            "X:XPST0017"
        ),
        ("fn-resolve-QName/K-ResolveQNameConstructFunc-4", "empty(resolve-QName((), \"a string\"))", "T\tX:XPTY0004"),
        ("fn-QName/ExpandedQNameConstructFunc005", "fn:QName(\"\", \"ht:person\")", "X:FOCA0002"),
        ("fn-QName/ExpandedQNameConstructFunc006", "fn:QName((), \"ht:person\")", "X:FOCA0002"),
        (
            "fn-QName/ExpandedQNameConstructFunc007",
            "fn:QName(\"http://www.example.com/example\", \"1person\")",
            "X:FOCA0002"
        ),
        (
            "fn-QName/ExpandedQNameConstructFunc008",
            "fn:QName(\"http://www.example.com/example\", \"@person\")",
            "X:FOCA0002"
        ),
        (
            "fn-QName/ExpandedQNameConstructFunc009",
            "fn:QName(\"http://www.example.com/example\", \"-person\")",
            "X:FOCA0002"
        ),
        (
            "fn-QName/ExpandedQNameConstructFunc010",
            "fn:QName(\"http://www.example.com/example\", \"<person>\")",
            "X:FOCA0002"
        ),
        (
            "fn-QName/ExpandedQNameConstructFunc011",
            "fn:QName(\"http://www.example.com/example\", \":person\")",
            "X:FOCA0002"
        ),
        (
            "fn-QName/ExpandedQNameConstructFunc012",
            "fn:QName(\"http://www.example.com/example\", \"person:\")",
            "X:FOCA0002"
        ),
        ("fn-QName/ExpandedQNameConstructFunc013", "fn:QName(\"person\")", "X:XPST0017"),
        ("fn-QName/ExpandedQNameConstructFunc014", "fn:QName(\"http://www.example.com/example\")", "X:XPST0017"),
        (
            "fn-QName/ExpandedQNameConstructFunc015",
            "fn:QName(\"http://www.example.com/example\", xs:integer(\"100\"))",
            "X:XPTY0004"
        ),
        ("fn-QName/ExpandedQNameConstructFunc016", "fn:QName( xs:integer(\"100\"), \"person\" )", "X:XPTY0004"),
        ("fn-QName/K-ExpandedQNameConstructFunc-1", "QName()", "X:XPST0017"),
        ("fn-QName/K-ExpandedQNameConstructFunc-2", "QName(\"http://www.example.com/\")", "X:XPST0017"),
        (
            "fn-QName/K-ExpandedQNameConstructFunc-3",
            "QName(\"http://www.example.com/\", \"ncname\", \"error\")",
            "X:XPST0017"
        ),
        ("fn-QName/K-ExpandedQNameConstructFunc-5", "QName(\"http://www.example.com/\", \"1asd:error\")", "X:FOCA0002"),
        ("fn-QName/K-ExpandedQNameConstructFunc-6", "QName(\"\", \"error:ncname\")", "X:FOCA0002"),
        ("fn-QName/K-ExpandedQNameConstructFunc-7", "QName((), \"error:ncname\")", "X:FOCA0002"),
        (
            "fn-QName/K-ExpandedQNameConstructFunc-8",
            "QName(\"my:qName\", \"http://example.com/MyErrorNS\")",
            "X:FOCA0002"
        ),
        ("fn-QName/K2-ExpandedQNameConstructFunc-1", "fn:QName((), ())", "X:XPTY0004"),
        ("fn-QName/cbcl-qname-001", "fn:QName('', ' ')", "X:FOCA0002"),
        ("fn-QName/qName-1", "xs:QName(20)", "X:XPTY0004"),
        ("fn-prefix-from-QName/K-PrefixFromQName-1", "prefix-from-QName()", "X:XPST0017"),
        ("fn-prefix-from-QName/K-PrefixFromQName-2", "prefix-from-QName(1, 2)", "X:XPST0017"),
        ("fn-prefix-from-QName/K-PrefixFromQName-3", "empty(prefix-from-QName( () ))", "T"),
        (
            "fn-prefix-from-QName/K-PrefixFromQName-4",
            "prefix-from-QName( QName(\"example.com/\", \"pre:lname\")) eq \"pre\"",
            "T"
        ),
        ("fn-prefix-from-QName/fn-prefix-from-qname-1", "fn:prefix-from-QName(\"arg1\",\"arg2\")", "X:XPST0017"),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-10",
            "fn:lower-case(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"FOO:bar\")))",
            "S:foo"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-11",
            "fn:concat(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),\":bar\")",
            "S:foo:bar"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-12",
            "fn:substring-before(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),\"oo\")",
            "S:f"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-13",
            "fn:substring-after(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),\"f\")",
            "S:oo"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-14",
            "fn:string-length(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")))",
            "S:3"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-15",
            "fn:substring(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),2)",
            "S:oo"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-16",
            "fn:string-join((fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),\":bar\"),\"\")",
            "S:foo:bar"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-17",
            "fn:starts-with(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),\"f\")",
            "T"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-18",
            "fn:ends-with(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),\"f\")",
            "F"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-19",
            "fn:contains(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")),\"f\")",
            "T"
        ),
        ("fn-prefix-from-QName/fn-prefix-from-qname-2", "fn:prefix-from-QName(xs:integer(1))", "X:XPTY0004"),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-20",
            "fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\"))",
            "S:foo"
        ),
        ("fn-prefix-from-QName/fn-prefix-from-qname-3", "fn:count(fn:prefix-from-QName(xs:QName(\"name\")))", "S:0"),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-4",
            "fn:string-length(xs:string(fn:prefix-from-QName(xs:QName(\"name\"))))",
            "S:0"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-5",
            "xs:string(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:name\")))",
            "S:foo"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-6",
            "fn:string(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:name\")))",
            "S:foo"
        ),
        ("fn-prefix-from-QName/fn-prefix-from-qname-7", "fn:count(fn:prefix-from-QName(()))", "S:0"),
        ("fn-prefix-from-QName/fn-prefix-from-qname-8", "fn:prefix-from-QName(xs:QName(\"foo:bar\"))", "X:FONS0004"),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-9",
            "fn:upper-case(fn:prefix-from-QName(fn:QName(\"http://example.org\",\"foo:bar\")))",
            "S:FOO"
        ),
        ("fn-local-name-from-QName/K-LocalNameFromQNameFunc-1", "local-name-from-QName()", "X:XPST0017"),
        ("fn-local-name-from-QName/K-LocalNameFromQNameFunc-2", "local-name-from-QName(1, 2)", "X:XPST0017"),
        ("fn-local-name-from-QName/K-LocalNameFromQNameFunc-3", "empty(local-name-from-QName( () ))", "T"),
        (
            "fn-local-name-from-QName/K-LocalNameFromQNameFunc-4",
            "local-name-from-QName( QName(\"example.com/\", \"pre:lname\")) eq \"lname\"",
            "T"
        ),
        ("fn-local-name-from-QName/LocalNameFromQNameFunc006", "fn:local-name-from-QName(())", "C:0"),
        ("fn-local-name-from-QName/LocalNameFromQNameFunc007", "fn:local-name-from-QName(((),()))", "C:0"),
        ("fn-local-name-from-QName/LocalNameFromQNameFunc009", "fn:local-name-from-QName(\"\")", "X:XPTY0004"),
        ("fn-local-name-from-QName/LocalNameFromQNameFunc011", "fn:local-name-from-QName()", "X:XPST0017"),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc012",
            "fn:local-name-from-Qname((/root/elemQN)[1])",
            "X:XPST0017"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc013",
            "fn:local-name-from-qname((/root/elemQN)[1])",
            "X:XPST0017"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc014",
            "fn:localnamefromQName((/root/elemQN)[1])",
            "X:XPST0017"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc016",
            "fn:local-name-from-QName(xs:integer(\"100\"))",
            "X:XPTY0004"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc017",
            "fn:local-name-from-QName(xs:time(\"12:00:00Z\"))",
            "X:XPTY0004"
        ),
        ("fn-namespace-uri-from-QName/K-NamespaceURIFromQNameFunc-1", "namespace-uri-from-QName()", "X:XPST0017"),
        ("fn-namespace-uri-from-QName/K-NamespaceURIFromQNameFunc-2", "namespace-uri-from-QName(1, 2)", "X:XPST0017"),
        ("fn-namespace-uri-from-QName/K-NamespaceURIFromQNameFunc-3", "empty(namespace-uri-from-QName( () ))", "T"),
        (
            "fn-namespace-uri-from-QName/K-NamespaceURIFromQNameFunc-4",
            "namespace-uri-from-QName( QName(\"example.com/\", \"pre:lname\")) eq xs:anyURI(\"example.com/\")",
            "T"
        ),
        (
            "fn-namespace-uri-from-QName/K-NamespaceURIFromQNameFunc-5",
            "namespace-uri-from-QName( QName(\"example.com/\", \"pre:lname\")) instance of xs:anyURI",
            "T"
        ),
        ("fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc006", "fn:namespace-uri-from-QName(())", "C:0"),
        ("fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc007", "fn:namespace-uri-from-QName(((),()))", "C:0"),
        ("fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc009", "fn:namespace-uri-from-QName(\"\")", "X:XPTY0004"),
        ("fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc011", "fn:namespace-uri-from-QName()", "X:XPST0017"),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc012",
            "fn:namespace-uri-from-qname((/root/elemQN)[1])",
            "X:XPST0017"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc013",
            "fn:namespace-uri-from-Qname((/root/elemQN)[1])",
            "X:XPST0017"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc014",
            "fn:namespaceURIfromQName((/root/elemQN)[1])",
            "X:XPST0017"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc016",
            "fn:namespace-uri-from-QName(xs:integer(\"100\"))",
            "X:XPTY0004"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc017",
            "fn:namespace-uri-from-QName(xs:time(\"12:00:00Z\"))",
            "X:XPTY0004"
        ),
        ("fn-namespace-uri-for-prefix/K-NamespaceURIForPrefixFunc-1", "namespace-uri-for-prefix()", "X:XPST0017"),
        (
            "fn-namespace-uri-for-prefix/K-NamespaceURIForPrefixFunc-2",
            "namespace-uri-for-prefix(\"string\", (), \"wrong param\")",
            "X:XPST0017"
        ),
        ("fn-in-scope-prefixes/K-InScopePrefixesFunc-1", "in-scope-prefixes()", "X:XPST0017"),
        (
            "fn-in-scope-prefixes/K-InScopePrefixesFunc-2",
            "in-scope-prefixes(\"string\", \"nodetest\", \"wrong param\")",
            "X:XPST0017"
        ),
        ("fn-in-scope-prefixes/fn-in-scope-prefixes-2", "fn:in-scope-prefixes(200)", "X:XPTY0004"),
        ("fn-resolve-uri/K-ResolveURIFunc-1", "resolve-uri()", "X:XPST0017"),
        (
            "fn-resolve-uri/K-ResolveURIFunc-2",
            "resolve-uri(\"http://www.example.com/\", \"relative/uri.ext\", \"wrong param\")",
            "X:XPST0017"
        ),
        (
            "fn-resolve-uri/K-ResolveURIFunc-3",
            "resolve-uri(\"relative/uri.ext\", \"http://www.example.com/\") eq xs:anyURI(\"http://www.example.com/relative/uri.ext\")",
            "T"
        ),
        (
            "fn-resolve-uri/K-ResolveURIFunc-4",
            "resolve-uri(\"\", \"http://www.example.com/\") eq xs:anyURI(\"http://www.example.com/\")",
            "T"
        ),
        ("fn-resolve-uri/K-ResolveURIFunc-5", "empty(resolve-uri((), \"http://www.example.com/\"))", "T"),
        (
            "fn-resolve-uri/K-ResolveURIFunc-6",
            "resolve-uri(\"http://www.example.com/absolute\", \"http://www.example.com/\") eq xs:anyURI(\"http://www.example.com/absolute\")",
            "T"
        ),
        ("fn-resolve-uri/fn-resolve-uri-1", "fn:count(fn:resolve-uri((),\"BaseValue\"))", "E:0"),
        (
            "fn-resolve-uri/fn-resolve-uri-10",
            "fn:string(fn:resolve-uri(fn:lower-case(\"EXAMPLES\"),fn:lower-case(\"HTTP://www.examples.com/\")))",
            "S:http://www.examples.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-11",
            "fn:string(fn:resolve-uri(\"examples\",fn:substring(\"1234http://www.examples.com/\",5)))",
            "S:http://www.examples.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-12",
            "fn:string(fn:resolve-uri(\"examples\",fn:string-join(('http://www.example','.com/'),'')))",
            "S:http://www.example.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-13",
            "fn:string(fn:resolve-uri(\"examples\",fn:concat(\"http://www.example\",\".com/\")))",
            "S:http://www.example.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-14",
            "fn:string(fn:resolve-uri(\"examples\",fn:substring-before(\"http://www.example.com/123\",\"123\")))",
            "S:http://www.example.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-15",
            "fn:string(fn:resolve-uri(\"examples\",fn:substring-after(\"123http://www.example.com/\",\"123\")))",
            "S:http://www.example.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-16",
            "fn:string(fn:resolve-uri(fn:string(\"http://www.examples.com/\"),\"\"))",
            "S:http://www.examples.com/"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-17",
            "fn:string(fn:resolve-uri(fn:upper-case(\"http://www.examples.com\"),\"\"))",
            "S:HTTP://WWW.EXAMPLES.COM"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-18",
            "fn:string(fn:resolve-uri(fn:lower-case(\"http://www.examples.com\"),\"\"))",
            "S:http://www.examples.com"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-19",
            "fn:string(fn:resolve-uri(fn:substring(\"123http://www.examples.com\",4),\"\"))",
            "S:http://www.examples.com"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-20",
            "fn:string(fn:resolve-uri(fn:string-join((\"http://www.examples\",\".com\"),''),\"\"))",
            "S:http://www.examples.com"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-21",
            "fn:string(fn:resolve-uri(fn:concat(\"http://www.examples\",\".com\"),\"\"))",
            "S:http://www.examples.com"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-22",
            "fn:string(fn:resolve-uri(fn:substring-before(\"http://www.example.com123\",\"123\"),\"\"))",
            "S:http://www.example.com"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-23",
            "fn:string(fn:resolve-uri(fn:substring-after(\"123http://www.example.com\",\"123\"),\"\"))",
            "S:http://www.example.com"
        ),
        ("fn-resolve-uri/fn-resolve-uri-24", "fn:string(fn:resolve-uri(\"a.html\",\"b.html\"))", "X:FORG0002"),
        (
            "fn-resolve-uri/fn-resolve-uri-25",
            "string(resolve-uri(\"http://www.example.com/a.html\",\"b.html\"))",
            "S:http://www.example.com/a.html"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-26",
            "resolve-uri(\"b.html\", \"http://www.example.com/a.html#fragment\")",
            "X:FORG0002"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-27",
            "string(resolve-uri(\"b.html\", \"http://www.example.com/a.html?foo=bar\"))",
            "S:http://www.example.com/b.html"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-29",
            "string(resolve-uri(\"urn:isbn:01234567890X\", \"http://www.example.com/\"))",
            "S:urn:isbn:01234567890X"
        ),
        ("fn-resolve-uri/fn-resolve-uri-3", "fn:resolve-uri(\":\",\"http://www.example.com/\")", "X:FORG0002"),
        (
            "fn-resolve-uri/fn-resolve-uri-31",
            "resolve-uri(\"%C3%A0.html\", \"http://www.example.com/%C3%A7.html\")\n         = \"http://www.example.com/%C3%A0.html\"",
            "T"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-32",
            "resolve-uri(\"this doc.html\", \"http://www.example.com/that doc.html\")",
            "S:http://www.example.com/this doc.html\tX:FORG0002"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-33",
            "fn:string(resolve-uri(\"http://www.example.com/books.xml\", \"urn:doi:234567\"))",
            "S:http://www.example.com/books.xml\tX:FORG0002"
        ),
        ("fn-resolve-uri/fn-resolve-uri-4", "fn:resolve-uri(\"examples\",\"http:%%\")", "X:FORG0002"),
        (
            "fn-resolve-uri/fn-resolve-uri-5",
            "fn:string(fn:resolve-uri(\"http://www.examples.com\",\"\"))",
            "S:http://www.examples.com"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-6",
            "fn:string(fn:resolve-uri(\"examples\",\"http://www.examples.com/\"))",
            "S:http://www.examples.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-7",
            "fn:string(fn:resolve-uri(\"examples\",xs:string(\"http://www.examples.com/\")))",
            "S:http://www.examples.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-8",
            "fn:string(fn:resolve-uri(fn:string(\"examples\"),fn:string(\"http://www.examples.com/\")))",
            "S:http://www.examples.com/examples"
        ),
        (
            "fn-resolve-uri/fn-resolve-uri-9",
            "fn:string(fn:resolve-uri(fn:upper-case(\"examples\"),fn:upper-case(\"http://www.examples.com/\")))",
            "S:HTTP://WWW.EXAMPLES.COM/EXAMPLES\tS:http://www.examples.com/EXAMPLES"
        ),
        ("fn-base-uri/K-BaseURIFunc-1", "base-uri((), \"wrong param\")", "X:XPST0017"),
        ("fn-base-uri/K-BaseURIFunc-2", "empty(base-uri(()))", "T"),
        ("fn-base-uri/fn-base-uri-3", "fn:count(fn:base-uri(()))", "E:0"),
        ("fn-static-base-uri/K-StaticBaseURIFunc-1", "static-base-uri(.)", "X:XPST0017"),
        ("fn-static-base-uri/K-StaticBaseURIFunc-2", "static-base-uri(1, 2)", "X:XPST0017"),
        ("fn-static-base-uri/fn-static-base-1", "fn:static-base-uri(\"A argument\")", "X:XPST0017"),
        ("fn-encode-for-uri/K-EncodeURIfunc-1", "encode-for-uri()", "X:XPST0017"),
        (
            "fn-encode-for-uri/K-EncodeURIfunc-2",
            "encode-for-uri(\"http://example.com/\", \"wrong param\")",
            "X:XPST0017"
        ),
        ("fn-encode-for-uri/K-EncodeURIfunc-3", "encode-for-uri(()) eq \"\"", "T"),
        (
            "fn-encode-for-uri/K-EncodeURIfunc-4",
            "concat(\"http://www.example.com/\", encode-for-uri(\"~b\u00E9b\u00E9\")) eq \"http://www.example.com/~b%C3%A9b%C3%A9\"",
            "T"
        ),
        (
            "fn-encode-for-uri/K-EncodeURIfunc-5",
            "concat(\"http://www.example.com/\", encode-for-uri(\"100% organic\")) eq \"http://www.example.com/100%25%20organic\"",
            "T"
        ),
        ("fn-encode-for-uri/fn-encode-for-uri-1", "(fn:encode-for-uri(\"example\"))", "S:example"),
        ("fn-encode-for-uri/fn-encode-for-uri-10", "(fn:encode-for-uri(\"examples(example\"))", "S:examples%28example"),
        ("fn-encode-for-uri/fn-encode-for-uri-11", "(fn:encode-for-uri(\"examples)example\"))", "S:examples%29example"),
        (
            "fn-encode-for-uri/fn-encode-for-uri-12",
            "(fn:encode-for-uri(\"examples0123456789example\"))",
            "S:examples0123456789example"
        ),
        ("fn-encode-for-uri/fn-encode-for-uri-13", "(fn:encode-for-uri(\"examples example\"))", "S:examples%20example"),
        ("fn-encode-for-uri/fn-encode-for-uri-14", "(fn:encode-for-uri(\"examples/example\"))", "S:examples%2Fexample"),
        ("fn-encode-for-uri/fn-encode-for-uri-15", "(fn:encode-for-uri(\"http:examples\"))", "S:http%3Aexamples"),
        ("fn-encode-for-uri/fn-encode-for-uri-16", "(fn:encode-for-uri(\"http%20examples\"))", "S:http%2520examples"),
        ("fn-encode-for-uri/fn-encode-for-uri-2", "(fn:encode-for-uri(\"examples#example\"))", "S:examples%23example"),
        ("fn-encode-for-uri/fn-encode-for-uri-3", "(fn:encode-for-uri(\"examples-example\"))", "S:examples-example"),
        ("fn-encode-for-uri/fn-encode-for-uri-4", "(fn:encode-for-uri(\"examples_example\"))", "S:examples_example"),
        ("fn-encode-for-uri/fn-encode-for-uri-5", "(fn:encode-for-uri(\"examples.example\"))", "S:examples.example"),
        ("fn-encode-for-uri/fn-encode-for-uri-6", "(fn:encode-for-uri(\"examples!example\"))", "S:examples%21example"),
        ("fn-encode-for-uri/fn-encode-for-uri-7", "(fn:encode-for-uri(\"examples~example\"))", "S:examples~example"),
        ("fn-encode-for-uri/fn-encode-for-uri-8", "(fn:encode-for-uri(\"examples*example\"))", "S:examples%2Aexample"),
        ("fn-encode-for-uri/fn-encode-for-uri-9", "(fn:encode-for-uri(\"examples'example\"))", "S:examples%27example"),
        (
            "fn-encode-for-uri/fn-encode-for-uri1args-1",
            "fn:encode-for-uri (\"http://www.example.com/00/Weather/CA/Los%20Angeles#ocean\")",
            "S:http%3A%2F%2Fwww.example.com%2F00%2FWeather%2FCA%2FLos%2520Angeles%23ocean"
        ),
        ("fn-encode-for-uri/fn-encode-for-uri1args-2", "encode-for-uri(\"~b\u00E9b\u00E9\")", "S:~b%C3%A9b%C3%A9"),
        ("fn-encode-for-uri/fn-encode-for-uri1args-3", "encode-for-uri(\"100% organic\")", "S:100%25%20organic"),
        ("fn-encode-for-uri/fn-encode-for-uri1args-4", "encode-for-uri('')", "S:"),
        ("fn-encode-for-uri/fn-encode-for-uri1args-5", "encode-for-uri(())", "S:"),
        ("fn-encode-for-uri/fn-encode-for-uri1args-6", "encode-for-uri(12)", "X:XPTY0004"),
        ("fn-encode-for-uri/fn-encode-for-uri1args-7", "encode-for-uri('',())", "X:XPST0017"),
        ("fn-iri-to-uri/K-IRIToURIfunc-1", "iri-to-uri()", "X:XPST0017"),
        ("fn-iri-to-uri/K-IRIToURIfunc-2", "iri-to-uri(\"http://example.com/\", \"wrong param\")", "X:XPST0017"),
        ("fn-iri-to-uri/K-IRIToURIfunc-3", "iri-to-uri(()) eq \"\"", "T"),
        ("fn-iri-to-uri/K2-IRIToURIfunc-1", "iri-to-uri()", "X:XPST0017"),
        (
            "fn-iri-to-uri/K2-IRIToURIfunc-10",
            "iri-to-uri(codepoints-to-string(15000 to 16000))",
            "S:%E3%AA%98%E3%AA%99%E3%AA%9A%E3%AA%9B%E3%AA%9C%E3%AA%9D%E3%AA%9E%E3%AA%9F%E3%AA%A0%E3%AA%A1%E3%AA%A2%E3%AA%A3%E3%AA%A4%E3%AA%A5%E3%AA%A6%E3%AA%A7%E3%AA%A8%E3%AA%A9%E3%AA%AA%E3%AA%AB%E3%AA%AC%E3%AA%AD%E3%AA%AE%E3%AA%AF%E3%AA%B0%E3%AA%B1%E3%AA%B2%E3%AA%B3%E3%AA%B4%E3%AA%B5%E3%AA%B6%E3%AA%B7%E3%AA%B8%E3%AA%B9%E3%AA%BA%E3%AA%BB%E3%AA%BC%E3%AA%BD%E3%AA%BE%E3%AA%BF%E3%AB%80%E3%AB%81%E3%AB%82%E3%AB%83%E3%AB%84%E3%AB%85%E3%AB%86%E3%AB%87%E3%AB%88%E3%AB%89%E3%AB%8A%E3%AB%8B%E3%AB%8C%E3%AB%8D%E3%AB%8E%E3%AB%8F%E3%AB%90%E3%AB%91%E3%AB%92%E3%AB%93%E3%AB%94%E3%AB%95%E3%AB%96%E3%AB%97%E3%AB%98%E3%AB%99%E3%AB%9A%E3%AB%9B%E3%AB%9C%E3%AB%9D%E3%AB%9E%E3%AB%9F%E3%AB%A0%E3%AB%A1%E3%AB%A2%E3%AB%A3%E3%AB%A4%E3%AB%A5%E3%AB%A6%E3%AB%A7%E3%AB%A8%E3%AB%A9%E3%AB%AA%E3%AB%AB%E3%AB%AC%E3%AB%AD%E3%AB%AE%E3%AB%AF%E3%AB%B0%E3%AB%B1%E3%AB%B2%E3%AB%B3%E3%AB%B4%E3%AB%B5%E3%AB%B6%E3%AB%B7%E3%AB%B8%E3%AB%B9%E3%AB%BA%E3%AB%BB%E3%AB%BC%E3%AB%BD%E3%AB%BE%E3%AB%BF%E3%AC%80%E3%AC%81%E3%AC%82%E3%AC%83%E3%AC%84%E3%AC%85%E3%AC%86%E3%AC%87%E3%AC%88%E3%AC%89%E3%AC%8A%E3%AC%8B%E3%AC%8C%E3%AC%8D%E3%AC%8E%E3%AC%8F%E3%AC%90%E3%AC%91%E3%AC%92%E3%AC%93%E3%AC%94%E3%AC%95%E3%AC%96%E3%AC%97%E3%AC%98%E3%AC%99%E3%AC%9A%E3%AC%9B%E3%AC%9C%E3%AC%9D%E3%AC%9E%E3%AC%9F%E3%AC%A0%E3%AC%A1%E3%AC%A2%E3%AC%A3%E3%AC%A4%E3%AC%A5%E3%AC%A6%E3%AC%A7%E3%AC%A8%E3%AC%A9%E3%AC%AA%E3%AC%AB%E3%AC%AC%E3%AC%AD%E3%AC%AE%E3%AC%AF%E3%AC%B0%E3%AC%B1%E3%AC%B2%E3%AC%B3%E3%AC%B4%E3%AC%B5%E3%AC%B6%E3%AC%B7%E3%AC%B8%E3%AC%B9%E3%AC%BA%E3%AC%BB%E3%AC%BC%E3%AC%BD%E3%AC%BE%E3%AC%BF%E3%AD%80%E3%AD%81%E3%AD%82%E3%AD%83%E3%AD%84%E3%AD%85%E3%AD%86%E3%AD%87%E3%AD%88%E3%AD%89%E3%AD%8A%E3%AD%8B%E3%AD%8C%E3%AD%8D%E3%AD%8E%E3%AD%8F%E3%AD%90%E3%AD%91%E3%AD%92%E3%AD%93%E3%AD%94%E3%AD%95%E3%AD%96%E3%AD%97%E3%AD%98%E3%AD%99%E3%AD%9A%E3%AD%9B%E3%AD%9C%E3%AD%9D%E3%AD%9E%E3%AD%9F%E3%AD%A0%E3%AD%A1%E3%AD%A2%E3%AD%A3%E3%AD%A4%E3%AD%A5%E3%AD%A6%E3%AD%A7%E3%AD%A8%E3%AD%A9%E3%AD%AA%E3%AD%AB%E3%AD%AC%E3%AD%AD%E3%AD%AE%E3%AD%AF%E3%AD%B0%E3%AD%B1%E3%AD%B2%E3%AD%B3%E3%AD%B4%E3%AD%B5%E3%AD%B6%E3%AD%B7%E3%AD%B8%E3%AD%B9%E3%AD%BA%E3%AD%BB%E3%AD%BC%E3%AD%BD%E3%AD%BE%E3%AD%BF%E3%AE%80%E3%AE%81%E3%AE%82%E3%AE%83%E3%AE%84%E3%AE%85%E3%AE%86%E3%AE%87%E3%AE%88%E3%AE%89%E3%AE%8A%E3%AE%8B%E3%AE%8C%E3%AE%8D%E3%AE%8E%E3%AE%8F%E3%AE%90%E3%AE%91%E3%AE%92%E3%AE%93%E3%AE%94%E3%AE%95%E3%AE%96%E3%AE%97%E3%AE%98%E3%AE%99%E3%AE%9A%E3%AE%9B%E3%AE%9C%E3%AE%9D%E3%AE%9E%E3%AE%9F%E3%AE%A0%E3%AE%A1%E3%AE%A2%E3%AE%A3%E3%AE%A4%E3%AE%A5%E3%AE%A6%E3%AE%A7%E3%AE%A8%E3%AE%A9%E3%AE%AA%E3%AE%AB%E3%AE%AC%E3%AE%AD%E3%AE%AE%E3%AE%AF%E3%AE%B0%E3%AE%B1%E3%AE%B2%E3%AE%B3%E3%AE%B4%E3%AE%B5%E3%AE%B6%E3%AE%B7%E3%AE%B8%E3%AE%B9%E3%AE%BA%E3%AE%BB%E3%AE%BC%E3%AE%BD%E3%AE%BE%E3%AE%BF%E3%AF%80%E3%AF%81%E3%AF%82%E3%AF%83%E3%AF%84%E3%AF%85%E3%AF%86%E3%AF%87%E3%AF%88%E3%AF%89%E3%AF%8A%E3%AF%8B%E3%AF%8C%E3%AF%8D%E3%AF%8E%E3%AF%8F%E3%AF%90%E3%AF%91%E3%AF%92%E3%AF%93%E3%AF%94%E3%AF%95%E3%AF%96%E3%AF%97%E3%AF%98%E3%AF%99%E3%AF%9A%E3%AF%9B%E3%AF%9C%E3%AF%9D%E3%AF%9E%E3%AF%9F%E3%AF%A0%E3%AF%A1%E3%AF%A2%E3%AF%A3%E3%AF%A4%E3%AF%A5%E3%AF%A6%E3%AF%A7%E3%AF%A8%E3%AF%A9%E3%AF%AA%E3%AF%AB%E3%AF%AC%E3%AF%AD%E3%AF%AE%E3%AF%AF%E3%AF%B0%E3%AF%B1%E3%AF%B2%E3%AF%B3%E3%AF%B4%E3%AF%B5%E3%AF%B6%E3%AF%B7%E3%AF%B8%E3%AF%B9%E3%AF%BA%E3%AF%BB%E3%AF%BC%E3%AF%BD%E3%AF%BE%E3%AF%BF%E3%B0%80%E3%B0%81%E3%B0%82%E3%B0%83%E3%B0%84%E3%B0%85%E3%B0%86%E3%B0%87%E3%B0%88%E3%B0%89%E3%B0%8A%E3%B0%8B%E3%B0%8C%E3%B0%8D%E3%B0%8E%E3%B0%8F%E3%B0%90%E3%B0%91%E3%B0%92%E3%B0%93%E3%B0%94%E3%B0%95%E3%B0%96%E3%B0%97%E3%B0%98%E3%B0%99%E3%B0%9A%E3%B0%9B%E3%B0%9C%E3%B0%9D%E3%B0%9E%E3%B0%9F%E3%B0%A0%E3%B0%A1%E3%B0%A2%E3%B0%A3%E3%B0%A4%E3%B0%A5%E3%B0%A6%E3%B0%A7%E3%B0%A8%E3%B0%A9%E3%B0%AA%E3%B0%AB%E3%B0%AC%E3%B0%AD%E3%B0%AE%E3%B0%AF%E3%B0%B0%E3%B0%B1%E3%B0%B2%E3%B0%B3%E3%B0%B4%E3%B0%B5%E3%B0%B6%E3%B0%B7%E3%B0%B8%E3%B0%B9%E3%B0%BA%E3%B0%BB%E3%B0%BC%E3%B0%BD%E3%B0%BE%E3%B0%BF%E3%B1%80%E3%B1%81%E3%B1%82%E3%B1%83%E3%B1%84%E3%B1%85%E3%B1%86%E3%B1%87%E3%B1%88%E3%B1%89%E3%B1%8A%E3%B1%8B%E3%B1%8C%E3%B1%8D%E3%B1%8E%E3%B1%8F%E3%B1%90%E3%B1%91%E3%B1%92%E3%B1%93%E3%B1%94%E3%B1%95%E3%B1%96%E3%B1%97%E3%B1%98%E3%B1%99%E3%B1%9A%E3%B1%9B%E3%B1%9C%E3%B1%9D%E3%B1%9E%E3%B1%9F%E3%B1%A0%E3%B1%A1%E3%B1%A2%E3%B1%A3%E3%B1%A4%E3%B1%A5%E3%B1%A6%E3%B1%A7%E3%B1%A8%E3%B1%A9%E3%B1%AA%E3%B1%AB%E3%B1%AC%E3%B1%AD%E3%B1%AE%E3%B1%AF%E3%B1%B0%E3%B1%B1%E3%B1%B2%E3%B1%B3%E3%B1%B4%E3%B1%B5%E3%B1%B6%E3%B1%B7%E3%B1%B8%E3%B1%B9%E3%B1%BA%E3%B1%BB%E3%B1%BC%E3%B1%BD%E3%B1%BE%E3%B1%BF%E3%B2%80%E3%B2%81%E3%B2%82%E3%B2%83%E3%B2%84%E3%B2%85%E3%B2%86%E3%B2%87%E3%B2%88%E3%B2%89%E3%B2%8A%E3%B2%8B%E3%B2%8C%E3%B2%8D%E3%B2%8E%E3%B2%8F%E3%B2%90%E3%B2%91%E3%B2%92%E3%B2%93%E3%B2%94%E3%B2%95%E3%B2%96%E3%B2%97%E3%B2%98%E3%B2%99%E3%B2%9A%E3%B2%9B%E3%B2%9C%E3%B2%9D%E3%B2%9E%E3%B2%9F%E3%B2%A0%E3%B2%A1%E3%B2%A2%E3%B2%A3%E3%B2%A4%E3%B2%A5%E3%B2%A6%E3%B2%A7%E3%B2%A8%E3%B2%A9%E3%B2%AA%E3%B2%AB%E3%B2%AC%E3%B2%AD%E3%B2%AE%E3%B2%AF%E3%B2%B0%E3%B2%B1%E3%B2%B2%E3%B2%B3%E3%B2%B4%E3%B2%B5%E3%B2%B6%E3%B2%B7%E3%B2%B8%E3%B2%B9%E3%B2%BA%E3%B2%BB%E3%B2%BC%E3%B2%BD%E3%B2%BE%E3%B2%BF%E3%B3%80%E3%B3%81%E3%B3%82%E3%B3%83%E3%B3%84%E3%B3%85%E3%B3%86%E3%B3%87%E3%B3%88%E3%B3%89%E3%B3%8A%E3%B3%8B%E3%B3%8C%E3%B3%8D%E3%B3%8E%E3%B3%8F%E3%B3%90%E3%B3%91%E3%B3%92%E3%B3%93%E3%B3%94%E3%B3%95%E3%B3%96%E3%B3%97%E3%B3%98%E3%B3%99%E3%B3%9A%E3%B3%9B%E3%B3%9C%E3%B3%9D%E3%B3%9E%E3%B3%9F%E3%B3%A0%E3%B3%A1%E3%B3%A2%E3%B3%A3%E3%B3%A4%E3%B3%A5%E3%B3%A6%E3%B3%A7%E3%B3%A8%E3%B3%A9%E3%B3%AA%E3%B3%AB%E3%B3%AC%E3%B3%AD%E3%B3%AE%E3%B3%AF%E3%B3%B0%E3%B3%B1%E3%B3%B2%E3%B3%B3%E3%B3%B4%E3%B3%B5%E3%B3%B6%E3%B3%B7%E3%B3%B8%E3%B3%B9%E3%B3%BA%E3%B3%BB%E3%B3%BC%E3%B3%BD%E3%B3%BE%E3%B3%BF%E3%B4%80%E3%B4%81%E3%B4%82%E3%B4%83%E3%B4%84%E3%B4%85%E3%B4%86%E3%B4%87%E3%B4%88%E3%B4%89%E3%B4%8A%E3%B4%8B%E3%B4%8C%E3%B4%8D%E3%B4%8E%E3%B4%8F%E3%B4%90%E3%B4%91%E3%B4%92%E3%B4%93%E3%B4%94%E3%B4%95%E3%B4%96%E3%B4%97%E3%B4%98%E3%B4%99%E3%B4%9A%E3%B4%9B%E3%B4%9C%E3%B4%9D%E3%B4%9E%E3%B4%9F%E3%B4%A0%E3%B4%A1%E3%B4%A2%E3%B4%A3%E3%B4%A4%E3%B4%A5%E3%B4%A6%E3%B4%A7%E3%B4%A8%E3%B4%A9%E3%B4%AA%E3%B4%AB%E3%B4%AC%E3%B4%AD%E3%B4%AE%E3%B4%AF%E3%B4%B0%E3%B4%B1%E3%B4%B2%E3%B4%B3%E3%B4%B4%E3%B4%B5%E3%B4%B6%E3%B4%B7%E3%B4%B8%E3%B4%B9%E3%B4%BA%E3%B4%BB%E3%B4%BC%E3%B4%BD%E3%B4%BE%E3%B4%BF%E3%B5%80%E3%B5%81%E3%B5%82%E3%B5%83%E3%B5%84%E3%B5%85%E3%B5%86%E3%B5%87%E3%B5%88%E3%B5%89%E3%B5%8A%E3%B5%8B%E3%B5%8C%E3%B5%8D%E3%B5%8E%E3%B5%8F%E3%B5%90%E3%B5%91%E3%B5%92%E3%B5%93%E3%B5%94%E3%B5%95%E3%B5%96%E3%B5%97%E3%B5%98%E3%B5%99%E3%B5%9A%E3%B5%9B%E3%B5%9C%E3%B5%9D%E3%B5%9E%E3%B5%9F%E3%B5%A0%E3%B5%A1%E3%B5%A2%E3%B5%A3%E3%B5%A4%E3%B5%A5%E3%B5%A6%E3%B5%A7%E3%B5%A8%E3%B5%A9%E3%B5%AA%E3%B5%AB%E3%B5%AC%E3%B5%AD%E3%B5%AE%E3%B5%AF%E3%B5%B0%E3%B5%B1%E3%B5%B2%E3%B5%B3%E3%B5%B4%E3%B5%B5%E3%B5%B6%E3%B5%B7%E3%B5%B8%E3%B5%B9%E3%B5%BA%E3%B5%BB%E3%B5%BC%E3%B5%BD%E3%B5%BE%E3%B5%BF%E3%B6%80%E3%B6%81%E3%B6%82%E3%B6%83%E3%B6%84%E3%B6%85%E3%B6%86%E3%B6%87%E3%B6%88%E3%B6%89%E3%B6%8A%E3%B6%8B%E3%B6%8C%E3%B6%8D%E3%B6%8E%E3%B6%8F%E3%B6%90%E3%B6%91%E3%B6%92%E3%B6%93%E3%B6%94%E3%B6%95%E3%B6%96%E3%B6%97%E3%B6%98%E3%B6%99%E3%B6%9A%E3%B6%9B%E3%B6%9C%E3%B6%9D%E3%B6%9E%E3%B6%9F%E3%B6%A0%E3%B6%A1%E3%B6%A2%E3%B6%A3%E3%B6%A4%E3%B6%A5%E3%B6%A6%E3%B6%A7%E3%B6%A8%E3%B6%A9%E3%B6%AA%E3%B6%AB%E3%B6%AC%E3%B6%AD%E3%B6%AE%E3%B6%AF%E3%B6%B0%E3%B6%B1%E3%B6%B2%E3%B6%B3%E3%B6%B4%E3%B6%B5%E3%B6%B6%E3%B6%B7%E3%B6%B8%E3%B6%B9%E3%B6%BA%E3%B6%BB%E3%B6%BC%E3%B6%BD%E3%B6%BE%E3%B6%BF%E3%B7%80%E3%B7%81%E3%B7%82%E3%B7%83%E3%B7%84%E3%B7%85%E3%B7%86%E3%B7%87%E3%B7%88%E3%B7%89%E3%B7%8A%E3%B7%8B%E3%B7%8C%E3%B7%8D%E3%B7%8E%E3%B7%8F%E3%B7%90%E3%B7%91%E3%B7%92%E3%B7%93%E3%B7%94%E3%B7%95%E3%B7%96%E3%B7%97%E3%B7%98%E3%B7%99%E3%B7%9A%E3%B7%9B%E3%B7%9C%E3%B7%9D%E3%B7%9E%E3%B7%9F%E3%B7%A0%E3%B7%A1%E3%B7%A2%E3%B7%A3%E3%B7%A4%E3%B7%A5%E3%B7%A6%E3%B7%A7%E3%B7%A8%E3%B7%A9%E3%B7%AA%E3%B7%AB%E3%B7%AC%E3%B7%AD%E3%B7%AE%E3%B7%AF%E3%B7%B0%E3%B7%B1%E3%B7%B2%E3%B7%B3%E3%B7%B4%E3%B7%B5%E3%B7%B6%E3%B7%B7%E3%B7%B8%E3%B7%B9%E3%B7%BA%E3%B7%BB%E3%B7%BC%E3%B7%BD%E3%B7%BE%E3%B7%BF%E3%B8%80%E3%B8%81%E3%B8%82%E3%B8%83%E3%B8%84%E3%B8%85%E3%B8%86%E3%B8%87%E3%B8%88%E3%B8%89%E3%B8%8A%E3%B8%8B%E3%B8%8C%E3%B8%8D%E3%B8%8E%E3%B8%8F%E3%B8%90%E3%B8%91%E3%B8%92%E3%B8%93%E3%B8%94%E3%B8%95%E3%B8%96%E3%B8%97%E3%B8%98%E3%B8%99%E3%B8%9A%E3%B8%9B%E3%B8%9C%E3%B8%9D%E3%B8%9E%E3%B8%9F%E3%B8%A0%E3%B8%A1%E3%B8%A2%E3%B8%A3%E3%B8%A4%E3%B8%A5%E3%B8%A6%E3%B8%A7%E3%B8%A8%E3%B8%A9%E3%B8%AA%E3%B8%AB%E3%B8%AC%E3%B8%AD%E3%B8%AE%E3%B8%AF%E3%B8%B0%E3%B8%B1%E3%B8%B2%E3%B8%B3%E3%B8%B4%E3%B8%B5%E3%B8%B6%E3%B8%B7%E3%B8%B8%E3%B8%B9%E3%B8%BA%E3%B8%BB%E3%B8%BC%E3%B8%BD%E3%B8%BE%E3%B8%BF%E3%B9%80%E3%B9%81%E3%B9%82%E3%B9%83%E3%B9%84%E3%B9%85%E3%B9%86%E3%B9%87%E3%B9%88%E3%B9%89%E3%B9%8A%E3%B9%8B%E3%B9%8C%E3%B9%8D%E3%B9%8E%E3%B9%8F%E3%B9%90%E3%B9%91%E3%B9%92%E3%B9%93%E3%B9%94%E3%B9%95%E3%B9%96%E3%B9%97%E3%B9%98%E3%B9%99%E3%B9%9A%E3%B9%9B%E3%B9%9C%E3%B9%9D%E3%B9%9E%E3%B9%9F%E3%B9%A0%E3%B9%A1%E3%B9%A2%E3%B9%A3%E3%B9%A4%E3%B9%A5%E3%B9%A6%E3%B9%A7%E3%B9%A8%E3%B9%A9%E3%B9%AA%E3%B9%AB%E3%B9%AC%E3%B9%AD%E3%B9%AE%E3%B9%AF%E3%B9%B0%E3%B9%B1%E3%B9%B2%E3%B9%B3%E3%B9%B4%E3%B9%B5%E3%B9%B6%E3%B9%B7%E3%B9%B8%E3%B9%B9%E3%B9%BA%E3%B9%BB%E3%B9%BC%E3%B9%BD%E3%B9%BE%E3%B9%BF%E3%BA%80"
        ),
        ("fn-iri-to-uri/K2-IRIToURIfunc-2", "iri-to-uri(\"a string\", \"a string\")", "X:XPST0017"),
        ("fn-iri-to-uri/K2-IRIToURIfunc-3", "iri-to-uri(1)", "X:XPTY0004"),
        ("fn-iri-to-uri/K2-IRIToURIfunc-4", "iri-to-uri((\"a string\", \"a string\"))", "X:XPTY0004"),
        ("fn-iri-to-uri/K2-IRIToURIfunc-5", "iri-to-uri(xs:anyURI(\"a string\"))", "S:a%20string"),
        ("fn-iri-to-uri/K2-IRIToURIfunc-6", "iri-to-uri(xs:untypedAtomic(\"a string\"))", "S:a%20string"),
        ("fn-iri-to-uri/K2-IRIToURIfunc-7", "iri-to-uri(\"<> \"\"{}|\\^`\")", "S:%3C%3E%20%22%7B%7D%7C%5C%5E%60"),
        ("fn-iri-to-uri/K2-IRIToURIfunc-8", "iri-to-uri(\"\n\")", "S:%0A"),
        (
            "fn-iri-to-uri/K2-IRIToURIfunc-9",
            "iri-to-uri(codepoints-to-string(32 to 294))",
            "S:%20!%22#$%&'()*+,-./0123456789:;%3C=%3E?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[%5C]%5E_%60abcdefghijklmnopqrstuvwxyz%7B%7C%7D~%7F%C2%80%C2%81%C2%82%C2%83%C2%84%C2%85%C2%86%C2%87%C2%88%C2%89%C2%8A%C2%8B%C2%8C%C2%8D%C2%8E%C2%8F%C2%90%C2%91%C2%92%C2%93%C2%94%C2%95%C2%96%C2%97%C2%98%C2%99%C2%9A%C2%9B%C2%9C%C2%9D%C2%9E%C2%9F%C2%A0%C2%A1%C2%A2%C2%A3%C2%A4%C2%A5%C2%A6%C2%A7%C2%A8%C2%A9%C2%AA%C2%AB%C2%AC%C2%AD%C2%AE%C2%AF%C2%B0%C2%B1%C2%B2%C2%B3%C2%B4%C2%B5%C2%B6%C2%B7%C2%B8%C2%B9%C2%BA%C2%BB%C2%BC%C2%BD%C2%BE%C2%BF%C3%80%C3%81%C3%82%C3%83%C3%84%C3%85%C3%86%C3%87%C3%88%C3%89%C3%8A%C3%8B%C3%8C%C3%8D%C3%8E%C3%8F%C3%90%C3%91%C3%92%C3%93%C3%94%C3%95%C3%96%C3%97%C3%98%C3%99%C3%9A%C3%9B%C3%9C%C3%9D%C3%9E%C3%9F%C3%A0%C3%A1%C3%A2%C3%A3%C3%A4%C3%A5%C3%A6%C3%A7%C3%A8%C3%A9%C3%AA%C3%AB%C3%AC%C3%AD%C3%AE%C3%AF%C3%B0%C3%B1%C3%B2%C3%B3%C3%B4%C3%B5%C3%B6%C3%B7%C3%B8%C3%B9%C3%BA%C3%BB%C3%BC%C3%BD%C3%BE%C3%BF%C4%80%C4%81%C4%82%C4%83%C4%84%C4%85%C4%86%C4%87%C4%88%C4%89%C4%8A%C4%8B%C4%8C%C4%8D%C4%8E%C4%8F%C4%90%C4%91%C4%92%C4%93%C4%94%C4%95%C4%96%C4%97%C4%98%C4%99%C4%9A%C4%9B%C4%9C%C4%9D%C4%9E%C4%9F%C4%A0%C4%A1%C4%A2%C4%A3%C4%A4%C4%A5%C4%A6"
        ),
        ("fn-iri-to-uri/fn-iri-to-uri-1", "fn:iri-to-uri(\"example\")", "S:example"),
        ("fn-iri-to-uri/fn-iri-to-uri-10", "fn:iri-to-uri(\"example'example\")", "S:example'example"),
        ("fn-iri-to-uri/fn-iri-to-uri-11", "fn:iri-to-uri(\"example(example\")", "S:example(example"),
        ("fn-iri-to-uri/fn-iri-to-uri-12", "fn:iri-to-uri(\"example)example\")", "S:example)example"),
        ("fn-iri-to-uri/fn-iri-to-uri-13", "fn:iri-to-uri(\"example;example\")", "S:example;example"),
        ("fn-iri-to-uri/fn-iri-to-uri-14", "fn:iri-to-uri(\"example/example\")", "S:example/example"),
        ("fn-iri-to-uri/fn-iri-to-uri-15", "fn:iri-to-uri(\"example?example\")", "S:example?example"),
        ("fn-iri-to-uri/fn-iri-to-uri-16", "fn:iri-to-uri(\"example:example\")", "S:example:example"),
        ("fn-iri-to-uri/fn-iri-to-uri-17", "fn:iri-to-uri(\"example@example\")", "S:example@example"),
        ("fn-iri-to-uri/fn-iri-to-uri-18A", "fn:iri-to-uri(\"example&amp;example\")", "S:example&amp;example"),
        ("fn-iri-to-uri/fn-iri-to-uri-19", "fn:iri-to-uri(\"example=example\")", "S:example=example"),
        ("fn-iri-to-uri/fn-iri-to-uri-2", "fn:iri-to-uri(\"example0123456789\")", "S:example0123456789"),
        ("fn-iri-to-uri/fn-iri-to-uri-20", "fn:iri-to-uri(\"example+example\")", "S:example+example"),
        ("fn-iri-to-uri/fn-iri-to-uri-21", "fn:iri-to-uri(\"example$example\")", "S:example$example"),
        ("fn-iri-to-uri/fn-iri-to-uri-22", "fn:iri-to-uri(\"example,example\")", "S:example,example"),
        ("fn-iri-to-uri/fn-iri-to-uri-23", "fn:iri-to-uri(\"example[example\")", "S:example[example"),
        ("fn-iri-to-uri/fn-iri-to-uri-24", "fn:iri-to-uri(\"example]example\")", "S:example]example"),
        ("fn-iri-to-uri/fn-iri-to-uri-25", "fn:iri-to-uri(\"example%example\")", "S:example%example"),
        ("fn-iri-to-uri/fn-iri-to-uri-26", "fn:iri-to-uri(\"example example\")", "S:example%20example"),
        ("fn-iri-to-uri/fn-iri-to-uri-3", "fn:iri-to-uri(\"example#example\")", "S:example#example"),
        ("fn-iri-to-uri/fn-iri-to-uri-4", "fn:iri-to-uri(\"example-example\")", "S:example-example"),
        ("fn-iri-to-uri/fn-iri-to-uri-5", "fn:iri-to-uri(\"example_example\")", "S:example_example"),
        ("fn-iri-to-uri/fn-iri-to-uri-6", "fn:iri-to-uri(\"example.example\")", "S:example.example"),
        ("fn-iri-to-uri/fn-iri-to-uri-7", "fn:iri-to-uri(\"example!example\")", "S:example!example"),
        ("fn-iri-to-uri/fn-iri-to-uri-8", "fn:iri-to-uri(\"example~example\")", "S:example~example"),
        ("fn-iri-to-uri/fn-iri-to-uri-9", "fn:iri-to-uri(\"example*example\")", "S:example*example"),
        (
            "fn-iri-to-uri/fn-iri-to-uri1args-1",
            "iri-to-uri(\"http://www.example.com/00/Weather/CA/Los%20Angeles#ocean\")",
            "S:http://www.example.com/00/Weather/CA/Los%20Angeles#ocean"
        ),
        (
            "fn-iri-to-uri/fn-iri-to-uri1args-2",
            "iri-to-uri(\"http://www.example.com/~b\u00E9b\u00E9\")",
            "S:http://www.example.com/~b%C3%A9b%C3%A9"
        ),
        ("fn-iri-to-uri/fn-iri-to-uri1args-3", "iri-to-uri('')", "S:"),
        ("fn-iri-to-uri/fn-iri-to-uri1args-4", "iri-to-uri(())", "S:"),
        ("fn-iri-to-uri/fn-iri-to-uri1args-5", "iri-to-uri(12)", "X:XPTY0004"),
        ("fn-iri-to-uri/fn-iri-to-uri1args-6", "iri-to-uri('',())", "X:XPST0017"),
    ];

    public static readonly (string Id, string Reason)[] Exclusions =
    [
        (
            "fn-resolve-QName/fn-resolve-qname-23",
            "the case reads the document of the environment auction, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-resolve-QName/fn-resolve-qname-24",
            "the case reads the document of the environment auction, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-resolve-QName/fn-resolve-qname-25",
            "the case reads the document of the environment auction, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-resolve-QName/fn-resolve-qname-26",
            "the case reads the document of the environment auction, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-QName/K-ExpandedQNameConstructFunc-10",
            "the case compares two QNames by eq, which has no entry in the comparison table until the QName value type (#545)"
        ),
        (
            "fn-QName/K-ExpandedQNameConstructFunc-4",
            "the case compares two QNames by eq, which has no entry in the comparison table until the QName value type (#545), and writes xs:QName(\"fn:local\"), which the constructor resolves against no namespaces (#644)"
        ),
        (
            "fn-QName/K-ExpandedQNameConstructFunc-9",
            "the case compares two QNames by eq, which has no entry in the comparison table until the QName value type (#545)"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-21",
            "the case reads the document of the environment atomic, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-22",
            "the case reads the document of the environment atomic, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-prefix-from-QName/fn-prefix-from-qname-23",
            "the case reads the document of the environment works-mod, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc001",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc002",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc003",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc004",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc005",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc010",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc015",
            "the case reads the document of the environment fsx, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc018",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc019",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/LocalNameFromQNameFunc020",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/fn-local-name-from-qname-022",
            "the case reads the document of the environment atomic, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/fn-local-name-from-qname-023",
            "the case reads the document of the environment atomic, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-local-name-from-QName/fn-local-name-from-qname-024",
            "the case reads the document of the environment works-mod, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc001",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc002",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc003",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc004",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc005",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc010",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc015",
            "the case reads the document of the environment fsx, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc018",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc019",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-namespace-uri-from-QName/NamespaceURIFromQNameFunc020",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-in-scope-prefixes/fn-in-scope-prefixes-21",
            "the case reads the document of the environment auction, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-in-scope-prefixes/fn-in-scope-prefixes-22",
            "the case reads the document of the environment auction, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-in-scope-prefixes/fn-in-scope-prefixes-23",
            "the case reads the document of the environment auction, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-in-scope-prefixes/fn-in-scope-prefixes-24",
            "the case reads the document of the environment qname, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-in-scope-prefixes/fn-in-scope-prefixes-25",
            "the case reads the document of the environment NamespaceSuppliedInternally, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-in-scope-prefixes/fn-in-scope-prefixes-26",
            "the case reads the document of the environment pathdata, which the fixture of the sequence group cannot load; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-resolve-uri/cbcl-fn-resolve-uri-001",
            "the fixture of the sequence group has no lowering for the for expression; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-base-uri/fn-base-uri-2",
            "the fixture of the sequence group has no lowering for a predicate; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-static-base-uri/K-StaticBaseURIFunc-3",
            "the fixture of the sequence group has no lowering for the conditional expression; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-static-base-uri/fn-static-base-15",
            "the case needs the static base URI http://www.example.com of its static context, which a call does not reach; the function of the default catalog holds none: #806, #752"
        ),
        (
            "fn-encode-for-uri/K-EncodeURIfunc-6",
            "the fixture of the sequence group has no lowering for a predicate; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
        (
            "fn-iri-to-uri/K-IRIToURIfunc-4",
            "the fixture of the sequence group has no lowering for a predicate; the evaluator-backed runner runs the case: XP-14 (#295)"
        ),
    ];
}
