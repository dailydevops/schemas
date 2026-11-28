namespace NetEvolve.Schemas.Schematron.Xslt.Registry;

using System;
using System.Collections.Generic;

/// <summary>
/// The element names that the XSLT 2.0 and 3.0 Recommendations define, supported or not. An XSLT element outside this set is unknown
/// to the supported versions, so only it may be replaced by its <c>xsl:fallback</c> children; a known but deferred element is always
/// the unsupported construct error. The set stands in for per-element version tracking (SCH-58).
/// </summary>
internal static class XsltKnownElements
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "accept",
        "accumulator",
        "accumulator-rule",
        "analyze-string",
        "apply-imports",
        "apply-templates",
        "assert",
        "attribute",
        "attribute-set",
        "break",
        "call-template",
        "catch",
        "character-map",
        "choose",
        "comment",
        "context-item",
        "copy",
        "copy-of",
        "decimal-format",
        "document",
        "element",
        "evaluate",
        "expose",
        "fallback",
        "for-each",
        "for-each-group",
        "fork",
        "function",
        "global-context-item",
        "if",
        "import",
        "import-schema",
        "include",
        "iterate",
        "key",
        "map",
        "map-entry",
        "matching-substring",
        "merge",
        "merge-action",
        "merge-key",
        "merge-source",
        "message",
        "mode",
        "namespace",
        "namespace-alias",
        "next-iteration",
        "next-match",
        "non-matching-substring",
        "number",
        "on-completion",
        "on-empty",
        "on-non-empty",
        "otherwise",
        "output",
        "output-character",
        "override",
        "package",
        "param",
        "perform-sort",
        "preserve-space",
        "processing-instruction",
        "result-document",
        "sequence",
        "sort",
        "source-document",
        "strip-space",
        "stylesheet",
        "template",
        "text",
        "transform",
        "try",
        "use-package",
        "value-of",
        "variable",
        "when",
        "where-populated",
        "with-param",
    };

    /// <summary>Tells whether XSLT 2.0 or 3.0 defines an element with the local name.</summary>
    /// <param name="local">The local name.</param>
    /// <returns><see langword="true"/> when the name is defined.</returns>
    internal static bool Contains(string local) => Names.Contains(local);
}
