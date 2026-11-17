namespace NetEvolve.Schemas.Xsd.Versioning.Constructs;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Reports every XSD 1.1 construct of a schema document as a located schema error in strict 1.0 mode.</summary>
/// <remarks>
/// Limitation: an attribute is located at the start tag of its element (design #108 AP9). Attributes in a namespace, such
/// as the version control attributes, are ignored in 1.0 mode, not rejected; their evaluation is XSD-22. In 1.1 mode this
/// check reports nothing.
/// </remarks>
internal static class Strict10Constructs
{
    /// <summary>Checks the syntax tree of one schema document.</summary>
    /// <param name="root">The <c>schema</c> element.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void Check(ISchemaSyntaxElement root, XsdVersion version, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(root);
        Argument.ThrowIfNull(reporter);

        if (version != XsdVersion.V10)
        {
            return;
        }

        Visit(root, string.Empty, reporter);
    }

    private static void Visit(ISchemaSyntaxElement element, string parent, XsdIssueReporter reporter)
    {
        var name = element.Name.LocalName;
        if (string.Equals(element.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal))
        {
            var entry = Strict10ConstructTable.Find(false, name);
            if (entry is not null)
            {
                Report(reporter, entry, element.Location, "element", name);
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
            if (entry is not null && Applies(entry, name, parent))
            {
                Report(reporter, entry, attribute.Location, "attribute", attribute.Name.LocalName);
            }
        }

        for (var i = 0; i < element.ChildCount; i++)
        {
            Visit(element.GetChild(i), name, reporter);
        }
    }

    private static bool Applies(Strict10Construct entry, string owner, string parent) =>
        (entry.Owners.Length == 0 || Array.IndexOf(entry.Owners, owner) >= 0)
        && (!entry.LocalOnly || !(parent is "schema" or "redefine" or "override"));

    private static void Report(
        XsdIssueReporter reporter,
        Strict10Construct entry,
        DocumentLocation location,
        string kind,
        string name
    ) =>
        reporter.Report(
            entry.Rule,
            location,
            "The XSD 1.1 " + kind + " '" + name + "' is not allowed in strict 1.0 mode."
        );
}
