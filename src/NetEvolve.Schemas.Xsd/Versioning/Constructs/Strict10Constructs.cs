namespace NetEvolve.Schemas.Xsd.Versioning.Constructs;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Reports every XSD 1.1 construct of a schema document as a located schema error in strict 1.0 mode.</summary>
/// <remarks>
/// Attributes in a namespace, such
/// as the version control attributes, are ignored in 1.0 mode, not rejected; their evaluation is XSD-22. In 1.1 mode this
/// check reports nothing.
/// </remarks>
internal static class Strict10Constructs
{
    private const string NotAllowedInStrict10 = "not allowed in strict 1.0 mode";

    /// <summary>Checks the syntax tree of one schema document.</summary>
    /// <param name="root">The <c>schema</c> element.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void Check(ISchemaSyntaxElement root, XsdVersion version, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(reporter);

        if (version != XsdVersion.V10)
        {
            return;
        }

        Visit(root, string.Empty, reporter);
    }

    /// <summary>Rejects an <c>xml:lang</c> of <c>appinfo</c> or <c>documentation</c> that is not an <c>xs:language</c> in strict 1.0 mode (#1179); XSD 1.1 accepts any value.</summary>
    /// <param name="document">The schema document.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void CheckLanguages(SchemaDocument document, XsdVersion version, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reporter);

        if (version != XsdVersion.V10)
        {
            return;
        }

        foreach (var location in document.InvalidLanguages)
        {
            reporter.Report(
                XsdRuleCodes.SrcAnnotation,
                location,
                "The value of the attribute 'xml:lang' is not a language."
            );
        }
    }

    private static void Visit(ISchemaSyntaxElement element, string parent, XsdIssueReporter reporter)
    {
        var name = element.Name.LocalName;
        if (string.Equals(element.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal))
        {
            var entry = Strict10ConstructTable.Find(false, name);
            if (entry is not null)
            {
                Report(reporter, entry.Rule, element.Location, "element", name, NotAllowedInStrict10);
            }
        }

        for (var i = 0; i < element.AttributeCount; i++)
        {
            var attribute = element.GetAttribute(i);
            if (attribute.Name.NamespaceName.Length != 0)
            {
                continue;
            }

            var entry = Strict10ConstructTable.Find(true, attribute.Name.LocalName);
            if (entry is not null && Applies(entry.Owners, false, name, parent))
            {
                Report(
                    reporter,
                    entry.Rule,
                    attribute.Location,
                    "attribute",
                    attribute.Name.LocalName,
                    NotAllowedInStrict10
                );
            }
        }

        for (var i = 0; i < element.ChildCount; i++)
        {
            Visit(element.GetChild(i), name, reporter);
        }
    }

    /// <summary>Tells whether an attribute construct applies to an element, shared with the not-yet-supported gate of XSD 1.1 (design #146 DP-16).</summary>
    /// <param name="owners">The local names of the elements that carry the attribute; empty for any.</param>
    /// <param name="localOnly">Whether the construct is rejected only on a local declaration.</param>
    /// <param name="owner">The local name of the element that carries the attribute.</param>
    /// <param name="parent">The local name of the parent of that element, empty for the root.</param>
    /// <returns><see langword="true"/> when the construct applies.</returns>
    internal static bool Applies(string[] owners, bool localOnly, string owner, string parent) =>
        (owners.Length == 0 || Array.IndexOf(owners, owner) >= 0)
        && (!localOnly || !(parent is "schema" or "redefine" or "override"));

    /// <summary>Reports one XSD 1.1 construct, shared with the not-yet-supported gate of XSD 1.1 (design #146 DP-16).</summary>
    /// <param name="reporter">Receives the error.</param>
    /// <param name="rule">The rule.</param>
    /// <param name="location">The location.</param>
    /// <param name="kind">Either <c>element</c> or <c>attribute</c>.</param>
    /// <param name="name">The local name of the construct.</param>
    /// <param name="outcome">What the message says about the construct, such as <c>not supported yet</c>.</param>
    internal static void Report(
        XsdIssueReporter reporter,
        XsdRule rule,
        DocumentLocation location,
        string kind,
        string name,
        string outcome
    ) => reporter.Report(rule, location, "The XSD 1.1 " + kind + " '" + name + "' is " + outcome + ".");
}
