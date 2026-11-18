namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Registry;

using System.Collections.Generic;

/// <summary>
/// The element names of the XSLT namespace as listed by the element syntax summaries of the W3C Recommendations XSLT 2.0
/// (23 January 2007) and XSLT 3.0 (8 June 2017). Elements that only the 4.0 drafts add are not listed.
/// </summary>
internal static class XsltElementNames
{
    private static readonly string[] Xslt20 =
    [
        "analyze-string",
        "apply-imports",
        "apply-templates",
        "attribute",
        "attribute-set",
        "call-template",
        "character-map",
        "choose",
        "comment",
        "copy",
        "copy-of",
        "decimal-format",
        "document",
        "element",
        "fallback",
        "for-each",
        "for-each-group",
        "function",
        "if",
        "import",
        "import-schema",
        "include",
        "key",
        "matching-substring",
        "message",
        "namespace",
        "namespace-alias",
        "next-match",
        "non-matching-substring",
        "number",
        "otherwise",
        "output",
        "output-character",
        "param",
        "perform-sort",
        "preserve-space",
        "processing-instruction",
        "result-document",
        "sequence",
        "sort",
        "strip-space",
        "stylesheet",
        "template",
        "text",
        "transform",
        "value-of",
        "variable",
        "when",
        "with-param",
    ];

    private static readonly string[] Xslt30Additions =
    [
        "accept",
        "accumulator",
        "accumulator-rule",
        "assert",
        "break",
        "catch",
        "context-item",
        "evaluate",
        "expose",
        "fork",
        "global-context-item",
        "iterate",
        "map",
        "map-entry",
        "merge",
        "merge-action",
        "merge-key",
        "merge-source",
        "mode",
        "next-iteration",
        "on-completion",
        "on-empty",
        "on-non-empty",
        "override",
        "package",
        "source-document",
        "try",
        "use-package",
        "where-populated",
    ];

    /// <summary>Gets every name of XSLT 2.0 and 3.0, each once.</summary>
    public static IEnumerable<string> All()
    {
        foreach (var name in Xslt20)
        {
            yield return name;
        }

        foreach (var name in Xslt30Additions)
        {
            yield return name;
        }
    }
}
