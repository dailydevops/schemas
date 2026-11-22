namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// What the pipeline does to a parsed schema document before any component is built: the attribute values of the QName types are
/// normalized (Datatypes 4.3.6: whitespace collapse for QName), the <c>xml</c> prefix is bound as the namespaces specification
/// demands, the <c>id</c> attributes are checked for the type <c>ID</c>, which is a name that no other <c>id</c> of the document has,
/// and the children of every element are checked against its content model (<see cref="SchemaStructure"/>).
/// </summary>
/// <remarks>
/// Limitation: an <c>id</c> or content violation is reported with the rule code of the element that carries it, because the specification
/// names no constraint for it; it is a violation of the schema for schemas (Structures 3.15.2). Uniqueness is checked per document,
/// not across the documents of a schema.
/// </remarks>
internal static class DocumentPreparation
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private static readonly string[] QNameAttributes =
    [
        "type",
        "ref",
        "base",
        "itemType",
        "refer",
        "substitutionGroup",
    ];

    /// <summary>Prepares the syntax of a document.</summary>
    /// <param name="root">The <c>schema</c> element.</param>
    /// <param name="version">The XSD version, which selects the datatype of <c>ID</c>.</param>
    /// <param name="reporter">The reporter.</param>
    internal static void Prepare(SchemaSyntaxNode root, XsdVersion version, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(reporter);

        if (!root.TryResolvePrefix("xml", out _))
        {
            root.AddNamespace("xml", XmlNamespace);
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var idType = BuiltInDatatypes.For(version).Get("ID");
        Visit(root, idType, ids, reporter);
    }

    private static void Visit(
        SchemaSyntaxNode node,
        IDatatypeDescriptor idType,
        HashSet<string> ids,
        XsdIssueReporter reporter
    )
    {
        var isSchema = string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal);
        if (isSchema)
        {
            Normalize(node);
            CheckId(node, idType, ids, reporter);
            CheckStructure(node, reporter);
        }

        for (var i = 0; i < node.ChildCount; i++)
        {
            Visit(node.GetChildNode(i), idType, ids, reporter);
        }
    }

    private static void Normalize(SchemaSyntaxNode node)
    {
        foreach (var name in QNameAttributes)
        {
            if (node.FindAttribute(name) is { } value && value.Length > 0 && !IsTrimmed(value))
            {
                node.SetAttribute(name, Trim(value));
            }
        }

        if (node.FindAttribute("memberTypes") is { } members)
        {
            var items = SyntaxValues.Split(members);
            var collapsed = string.Join(" ", items);
            if (!string.Equals(collapsed, members, StringComparison.Ordinal))
            {
                node.SetAttribute("memberTypes", collapsed);
            }
        }
    }

    // Whitespace collapse strips only #x20, #x9, #xA and #xD, not every Unicode whitespace character.
    private static bool IsTrimmed(string value) =>
        !WhitespaceNormalizer.IsXmlSpace(value[0]) && !WhitespaceNormalizer.IsXmlSpace(value[value.Length - 1]);

    private static string Trim(string value)
    {
        var start = 0;
        var end = value.Length;
        while (start < end && WhitespaceNormalizer.IsXmlSpace(value[start]))
        {
            start++;
        }

        while (end > start && WhitespaceNormalizer.IsXmlSpace(value[end - 1]))
        {
            end--;
        }

        return value.Substring(start, end - start);
    }

    private static void CheckId(
        SchemaSyntaxNode node,
        IDatatypeDescriptor idType,
        HashSet<string> ids,
        XsdIssueReporter reporter
    )
    {
        if (node.FindAttribute("id") is not { } id)
        {
            return;
        }

        var problem =
            !idType.Validate(id.AsSpan()).IsValid
                ? "The id '" + id + "' is not a valid ID, which is a name without a colon."
            : !ids.Add(id) ? "The id '" + id + "' is used by another element of the document."
            : null;
        if (problem is not null)
        {
            reporter.Report(XsdRuleLookup.Find(RuleOf(node)), node.Location, problem);
        }
    }

    private static void CheckStructure(SchemaSyntaxNode node, XsdIssueReporter reporter)
    {
        if (!SchemaStructure.IsValid(node))
        {
            reporter.Report(
                XsdRuleLookup.Find(RuleOf(node)),
                node.Location,
                "The content of the '"
                    + node.Name.LocalName
                    + "' element is not valid: its children must be "
                    + SchemaStructure.DescribeModel(node)
                    + "."
            );
        }
    }

    // The rule that the document grammar assigns to the element; an element that the grammar does not know is reported by the parser.
    private static DiagnosticCode RuleOf(SchemaSyntaxNode node)
    {
        var parent = node.Parent?.Name.LocalName ?? string.Empty;
        return SchemaGrammar.IsKnown(node.Name.LocalName)
            ? SchemaGrammar.Find(node.Name.LocalName, parent).Code
            : XsdRuleCodes.SchPropsCorrect;
    }
}
