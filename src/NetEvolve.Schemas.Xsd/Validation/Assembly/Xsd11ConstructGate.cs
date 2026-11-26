namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;

/// <summary>
/// Reports every XSD 1.1 construct of a schema document that the assembled validator does not support yet as a located schema
/// error under the default version, so a schema never silently loses a construct (design #146 DP-16). In strict 1.0 mode this gate
/// reports nothing, because the strict 1.0 check of the same constructs applies there.
/// </summary>
/// <remarks>
/// The attributes of the version control
/// namespace have no rule and are not reported; conditional inclusion is XSD-78.
/// </remarks>
internal static class Xsd11ConstructGate
{
    private const string NotSupportedYet = "not supported yet";

    /// <summary>Checks the syntax tree of one schema document.</summary>
    /// <param name="root">The <c>schema</c> element.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void Check(SchemaSyntaxNode root, XsdVersion version, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(reporter);

        if (version != XsdVersion.V11)
        {
            return;
        }

        Visit(root, reporter);
    }

    private static void Visit(SchemaSyntaxNode node, XsdIssueReporter reporter)
    {
        var local = node.Name.LocalName;
        var parent = node.Parent?.Name.LocalName ?? string.Empty;
        var inSchema = string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal);
        // Indexed, so the list enumerator is not boxed once per node.
        var constructs = Xsd11ConstructTable.Entries;
        for (var c = 0; c < constructs.Count; c++)
        {
            var construct = constructs[c];
            if (construct.IsAttribute)
            {
                ReportAttributes(node, construct, local, parent, reporter);
            }
            else if (inSchema && Array.IndexOf(construct.Names, local) >= 0)
            {
                Strict10Constructs.Report(
                    reporter,
                    construct.NotYetSupported,
                    node.Location,
                    "element",
                    local,
                    NotSupportedYet
                );
            }
        }

        for (var i = 0; i < node.ChildCount; i++)
        {
            Visit(node.GetChildNode(i), reporter);
        }
    }

    private static void ReportAttributes(
        SchemaSyntaxNode node,
        Xsd11Construct construct,
        string owner,
        string parent,
        XsdIssueReporter reporter
    )
    {
        if (!Strict10Constructs.Applies(construct.Owners, construct.LocalOnly, owner, parent))
        {
            return;
        }

        for (var i = 0; i < node.AttributeCount; i++)
        {
            var attribute = node.GetAttribute(i);
            if (
                attribute.Name.NamespaceName.Length == 0
                && Array.IndexOf(construct.Names, attribute.Name.LocalName) >= 0
                && (construct.ValueMatches is null || construct.ValueMatches(node, attribute.Value))
            )
            {
                Strict10Constructs.Report(
                    reporter,
                    construct.NotYetSupported,
                    attribute.Location,
                    "attribute",
                    attribute.Name.LocalName,
                    NotSupportedYet
                );
            }
        }
    }
}
