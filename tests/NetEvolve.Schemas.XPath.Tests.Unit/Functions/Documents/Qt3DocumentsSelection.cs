namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Documents;

/// <summary>
/// The QT3 test sets <c>fn-doc</c>, <c>fn-doc-available</c>, <c>fn-id</c>, <c>fn-idref</c>, and <c>fn-collection</c>, from the pinned commit of
/// the suite (tests/conformance/suites/qt3.json). The integration runner decides every case it can in the baselines of
/// <c>tests/conformance/baselines/qt3</c>; the cases that it cannot decide stay in them and are <see cref="Exclusions"/> here, one by one with
/// the reason and the issue that closes it. The cases that need no environment and that the runner skips only because their assertion is a
/// compound one are decided here as well (<see cref="Cases"/>): the expression must end in one of the accepted outcomes.
/// </summary>
/// <remarks>
/// The sets <c>fn-unparsed-text</c>, <c>fn-unparsed-text-available</c>, and <c>fn-unparsed-text-lines</c> have no selected case, because the
/// functions belong to XPath 3.0 and to the XSLT 2.0 host and the runner selects XPath 2.0 cases only (#317); they are covered by
/// <see cref="UnparsedTextTests"/>, which pins the behavior that the cases name: the encodings, the byte order marks, the line endings, and the
/// error codes.
/// </remarks>
internal static class Qt3DocumentsSelection
{
    private const string AnyOf = "the QT3 runner skips compound any-of assertions (#901)";
    private const string Resolver =
        "the QT3 engine binds no resolver to the sources and resources of the environment (#946)";
    private const string Structural = "the QT3 runner skips structural assertions (#315)";
    private const string XmlId = "ID typing through xml:id is DEF-67 (#79)";
    private const string Collections = "working fn:collection is DEF-49 (#61)";
    private const string NodeName = "fn:node-name and fn:nilled are not in the library yet (#886)";

    /// <summary>The cases that the runner skips for their compound assertion: the expression, and the accepted outcomes separated by <c>|</c>.</summary>
    public static readonly (string Id, string Expression, string Accepted)[] Cases =
    [
        ("fn-doc/fn-doc-1", @"fn:doc(""http:\\invalid>URI\someURI"")", "error:FODC0005|error:FODC0002"),
        ("fn-doc/fn-doc-17", @"fn:doc(""%gg"")", "error:FODC0002|error:FODC0005"),
        (
            "fn-doc/K2-SeqDocFunc-7",
            @"doc(""example.com\example.org\does\not\exist\doesNotExist\works-mod.xml"")",
            "error:FODC0002|error:FODC0005"
        ),
        (
            "fn-doc/K2-SeqDocFunc-8",
            @"doc(""\example.com\example.org\does\not\exist\doesNotExist\works-mod.xml"")",
            "error:FODC0002|error:FODC0005"
        ),
        (
            "fn-doc/K2-SeqDocFunc-9",
            @"doc(""C:\example.com\example.org\does\not\exist\doesNotExist\works-mod.xml"")",
            "error:FODC0002|error:FODC0005"
        ),
        ("fn-doc-available/K2-SeqDocAvailableFunc-1", "doc-available(':/')", "false|error:FODC0005"),
        ("fn-doc-available/cbcl-doc-available-002", "fn:doc-available( '%gg' )", "error:FODC0005|false"),
        ("fn-collection/fn-collection-3", @"fn:collection(""invalidURI%gg"")", "error:FODC0002|error:FODC0004"),
        (
            "fn-collection/K2-SeqCollectionFunc-1",
            @"collection(""http:\\invalidURI\someURI%gg"")",
            "error:FODC0004|error:FODC0002"
        ),
        ("fn-collection/K2-SeqCollectionFunc-2", @"collection("":/"")", "error:FODC0004|error:FODC0002"),
        ("fn-collection/collection-902", @"collection(""##invalid"")", "error:FODC0004|error:FODC0002"),
        ("fn-collection/cbcl-collection-001", "fn:collection('%gg')", "error:FODC0004|error:FODC0002"),
    ];

    /// <summary>The cases of the five test sets that stay in the baselines of the runner, each with its reason.</summary>
    public static readonly (string Id, string Reason)[] Exclusions =
    [
        ("fn-doc/K2-SeqDocFunc-7", AnyOf),
        ("fn-doc/K2-SeqDocFunc-8", AnyOf),
        ("fn-doc/K2-SeqDocFunc-9", AnyOf),
        ("fn-doc/fn-doc-1", AnyOf),
        ("fn-doc/fn-doc-17", AnyOf),
        ("fn-doc/fn-doc-15", Resolver + "; " + NodeName),
        ("fn-doc/fn-doc-16", Resolver + "; " + NodeName),
        ("fn-doc/fn-doc-24", Resolver + "; " + Structural),
        ("fn-doc/fn-doc-25", Resolver + "; " + Structural),
        ("fn-doc/fn-doc-26", Resolver + "; " + Structural),
        ("fn-doc/fn-doc-27", Resolver),
        ("fn-doc/fn-doc-28", Resolver),
        ("fn-doc/fn-doc-29", Resolver + "; " + Structural),
        ("fn-doc/fn-doc-30", Resolver + "; " + AnyOf),
        ("fn-doc/fn-doc-31", Resolver + "; " + AnyOf),
        ("fn-doc/fn-doc-32", Resolver + "; " + Structural),
        ("fn-doc/fn-doc-34", Resolver + "; " + Structural),
        ("fn-doc/fn-doc-36", Resolver),
        ("fn-doc-available/K2-SeqDocAvailableFunc-1", AnyOf),
        ("fn-doc-available/cbcl-doc-available-002", AnyOf),
        ("fn-doc-available/fn-doc-available-3", Resolver + "; " + AnyOf),
        ("fn-doc-available/fn-doc-available-4", Resolver + "; " + AnyOf),
        ("fn-doc-available/fn-doc-available-5", Resolver),
        ("fn-id/K2-SeqIDFunc-10", XmlId),
        ("fn-id/K2-SeqIDFunc-11", XmlId),
        ("fn-id/K2-SeqIDFunc-12", XmlId),
        ("fn-id/K2-SeqIDFunc-13", XmlId),
        ("fn-id/K2-SeqIDFunc-14", XmlId),
        ("fn-id/K2-SeqIDFunc-15", XmlId),
        ("fn-idref/fn-idref-dtd-12", NodeName),
        ("fn-collection/K2-SeqCollectionFunc-1", AnyOf),
        ("fn-collection/K2-SeqCollectionFunc-2", AnyOf),
        ("fn-collection/cbcl-collection-001", AnyOf),
        ("fn-collection/collection-001", AnyOf + "; " + Collections),
        ("fn-collection/collection-002", AnyOf + "; " + Collections),
        ("fn-collection/collection-003", AnyOf + "; " + Collections),
        ("fn-collection/collection-004", AnyOf + "; " + Collections),
        ("fn-collection/collection-005", AnyOf + "; " + Collections),
        ("fn-collection/collection-006", AnyOf + "; " + Collections),
        ("fn-collection/collection-007", AnyOf + "; " + Collections),
        ("fn-collection/collection-008", AnyOf + "; " + Collections),
        ("fn-collection/collection-010", AnyOf + "; " + Collections),
        ("fn-collection/collection-902", AnyOf),
        ("fn-collection/fn-collection-3", AnyOf),
        ("fn-collection/fn-collection-4", Collections),
        ("fn-collection/fn-collection-4d", Collections),
        ("fn-collection/fn-collection-5", Collections),
        ("fn-collection/fn-collection-5d", Collections),
        ("fn-collection/fn-collection-7", AnyOf + "; " + Collections),
        ("fn-collection/fn-collection-8", AnyOf + "; " + Collections),
    ];
}
