namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// The exclusion list of the XPath 3.0 and 3.1 selections: the deferred features of the issue (DEF-40 to DEF-49, DEF-68, DEF-69,
/// DEF-73, DEF-74, and DEF-76) and the three rules the XPath 2.0 selection already has. The list is preliminary until XP-73 closes it.
/// </summary>
/// <remarks>
/// Limitation: the cases that apply to XQuery or XSLT only are not applicable to an XPath processor. The spec dependency excludes them,
/// so no entry and no deferred feature covers them.
/// </remarks>
internal static class Qt3Xp3Exclusions
{
    private const string Feature = "feature ";
    private const string Schema = "schema-aware processing";
    private const string Uca = "Unicode Collation Algorithm collations";
    private const string UcaOne = "a Unicode Collation Algorithm collation";
    private const string Collection = "fn:collection and fn:uri-collection";
    private const string Transform = "fn:transform";
    private const string XQueryModule = "fn:load-xquery-module";
    private const string OtherSet = "a case of another test set that uses ";

    private static readonly char[] Separators = [' ', '\t', '\r', '\n'];

    /// <summary>Gets every entry: first the dependencies, then the test sets, then the patterns.</summary>
    public static IReadOnlyList<Qt3ExclusionEntry> All { get; } =
    [
        new(Qt3ExclusionKind.Dependency, Feature + "schemaImport", Schema, "DEF-40", 52),
        new(Qt3ExclusionKind.Dependency, Feature + "schemaValidation", Schema, "DEF-40", 52),
        new(Qt3ExclusionKind.Dependency, Feature + "typedData", Schema, "DEF-40", 52),
        new(Qt3ExclusionKind.Dependency, Feature + "schema-location-hint", Schema, "DEF-40", 52),
        new(Qt3ExclusionKind.Dependency, Feature + "staticTyping", "static typing", "DEF-41", 53),
        new(Qt3ExclusionKind.Dependency, Feature + "namespace-axis", "the namespace axis", "DEF-42", 54),
        new(Qt3ExclusionKind.Dependency, Feature + "simple-uca-fallback", Uca, "DEF-43", 55),
        new(Qt3ExclusionKind.Dependency, Feature + "advanced-uca-fallback", Uca, "DEF-43", 55),
        new(Qt3ExclusionKind.Dependency, Feature + "serialization", "serialization", "DEF-46", 58),
        new(Qt3ExclusionKind.Dependency, Feature + "fn-transform-XSLT", Transform, "DEF-47", 59),
        new(Qt3ExclusionKind.Dependency, Feature + "fn-transform-XSLT30", Transform, "DEF-47", 59),
        new(Qt3ExclusionKind.Dependency, Feature + "fn-load-xquery-module", XQueryModule, "DEF-48", 60),
        new(Qt3ExclusionKind.Dependency, Feature + "non_empty_sequence_collection", Collection, "DEF-49", 61),
        new(Qt3ExclusionKind.Dependency, Feature + "directory-as-collection-uri", Collection, "DEF-49", 61),
        new(Qt3ExclusionKind.Dependency, Feature + "collection-stability", Collection, "DEF-49", 61),
        new(Qt3ExclusionKind.Dependency, "xml-version 1.1", "an XML 1.1 case; the documents of the runner are XML 1.0"),
        new(Qt3ExclusionKind.Dependency, "xsd-version 1.1", "an XSD 1.1 case; the library targets XSD 1.0"),
        new(
            Qt3ExclusionKind.Dependency,
            Feature + "non_unicode_codepoint_collation",
            "a host collation of the test catalog other than the codepoint collation"
        ),
        new(Qt3ExclusionKind.TestSet, "fn-parse-json", "the JSON function fn:parse-json", "DEF-44", 56),
        new(Qt3ExclusionKind.TestSet, "fn-json-doc", "the JSON function fn:json-doc", "DEF-44", 56),
        new(Qt3ExclusionKind.TestSet, "fn-json-to-xml", "the JSON function fn:json-to-xml", "DEF-45", 57),
        new(Qt3ExclusionKind.TestSet, "fn-xml-to-json", "the JSON function fn:xml-to-json", "DEF-45", 57),
        new(Qt3ExclusionKind.TestSet, "fn-serialize", "serialization with fn:serialize", "DEF-46", 58),
        new(Qt3ExclusionKind.TestSet, "misc-UCACollation", Uca, "DEF-43", 55),
        new(Qt3ExclusionKind.TestSet, "fn-transform", Transform, "DEF-47", 59),
        new(Qt3ExclusionKind.TestSet, "fn-load-xquery-module", XQueryModule, "DEF-48", 60),
        new(Qt3ExclusionKind.TestSet, "fn-collection", "fn:collection", "DEF-49", 61),
        new(Qt3ExclusionKind.TestSet, "fn-uri-collection", "fn:uri-collection", "DEF-49", 61),
        new(Qt3ExclusionKind.TestSet, "fn-collation-key", "fn:collation-key", "DEF-68", 80),
        new(Qt3ExclusionKind.TestSet, "fn-default-language", "fn:default-language", "DEF-69", 81),
        new(Qt3ExclusionKind.TestSet, "fn-parse-ietf-date", "fn:parse-ietf-date", "DEF-73", 85),
        new(Qt3ExclusionKind.TestSet, "fn-random-number-generator", "fn:random-number-generator", "DEF-74", 86),
        new(
            Qt3ExclusionKind.Pattern,
            @"\b(?:parse-json|json-doc)\b",
            OtherSet + "fn:parse-json or fn:json-doc",
            "DEF-44",
            56
        ),
        new(
            Qt3ExclusionKind.Pattern,
            @"\b(?:json-to-xml|xml-to-json)\b",
            OtherSet + "fn:json-to-xml or fn:xml-to-json",
            "DEF-45",
            57
        ),
        new(Qt3ExclusionKind.Pattern, @"\bserialize\b", OtherSet + "fn:serialize", "DEF-46", 58),
        new(Qt3ExclusionKind.Pattern, "2013/collation/UCA", OtherSet + UcaOne, "DEF-43", 55),
        new(Qt3ExclusionKind.Pattern, @"\btransform\b", OtherSet + Transform, "DEF-47", 59),
        new(Qt3ExclusionKind.Pattern, @"\bload-xquery-module\b", OtherSet + XQueryModule, "DEF-48", 60),
        new(
            Qt3ExclusionKind.Pattern,
            @"\b(?:uri-)?collection\b",
            OtherSet + "fn:collection or fn:uri-collection",
            "DEF-49",
            61
        ),
        new(Qt3ExclusionKind.Pattern, @"\bcollation-key\b", OtherSet + "fn:collation-key", "DEF-68", 80),
        new(Qt3ExclusionKind.Pattern, @"\bdefault-language\b", OtherSet + "fn:default-language", "DEF-69", 81),
        new(Qt3ExclusionKind.Pattern, @"\bparse-ietf-date\b", OtherSet + "fn:parse-ietf-date", "DEF-73", 85),
        new(
            Qt3ExclusionKind.Pattern,
            @"\brandom-number-generator\b",
            OtherSet + "fn:random-number-generator",
            "DEF-74",
            86
        ),
        new(
            Qt3ExclusionKind.Pattern,
            "html-ascii-case-insensitive",
            OtherSet + "the HTML ASCII case-insensitive collation",
            "DEF-76",
            88
        ),
    ];

    /// <summary>Determines whether a dependency of the catalog is the one of a <see cref="Qt3ExclusionKind.Dependency"/> entry.</summary>
    /// <param name="entry">The entry.</param>
    /// <param name="dependency">The dependency of a test set or a case.</param>
    /// <returns>
    /// <see langword="true"/> when the case needs what the entry excludes: a satisfied dependency with the type and the value of the
    /// key, where <c>xml-version</c> matches when every version is 1.1 and <c>xsd-version</c> when 1.0 is not one of them.
    /// </returns>
    public static bool Matches(Qt3ExclusionEntry entry, Qt3Dependency dependency)
    {
        if (entry.Kind != Qt3ExclusionKind.Dependency || !dependency.Satisfied)
        {
            return false;
        }

        var space = entry.Key.IndexOf(' ', StringComparison.Ordinal);
        if (!string.Equals(entry.Key[..space], dependency.Type, StringComparison.Ordinal))
        {
            return false;
        }

        var value = entry.Key[(space + 1)..];
        var tokens = dependency.Value.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        return dependency.Type switch
        {
            "xml-version" => Array.TrueForAll(tokens, t => string.Equals(t, value, StringComparison.Ordinal)),
            "xsd-version" => Array.IndexOf(tokens, "1.0") < 0,
            _ => string.Equals(dependency.Value, value, StringComparison.Ordinal),
        };
    }
}
