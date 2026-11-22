namespace NetEvolve.Schemas.Xsd.Declarations11;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The <c>targetNamespace</c> attribute of element and attribute declarations (Structures 1.1, 3.3.2 and 3.2.2, constraints
/// <c>src-element.4</c> and <c>src-attribute.6</c>): the declaration is named in that namespace, and the attribute is
/// limited to a restriction of a complex type, where it lets the restriction restate a declaration of its base in the
/// namespace of the base.
/// </summary>
/// <remarks>
/// <para>
/// The attribute needs a <c>name</c> and excludes <c>form</c>. A namespace that differs from the target namespace of the schema
/// element of the document, or a schema without one, needs a <c>complexType</c> ancestor and, between the declaration and its
/// nearest complex type, a <c>restriction</c> whose <see langword="base"/> attribute does not resolve to <c>xs:anyType</c>. The nearest enclosing
/// complex type decides, so a declaration in an anonymous type inside a restriction is outside it. The namespace of the schema
/// is allowed anywhere. All violated clauses of one declaration are reported at its start tag (design #108 AP9).
/// </para>
/// <para>
/// Strict 1.0 mode is not reported here: <see cref="Versioning.Constructs.Strict10Constructs"/> rejects the attribute on a local
/// declaration, so that one construct has one report. Limitations: a base with an undeclared prefix does not match
/// <c>xs:anyType</c> and is the <c>src-resolve</c> error of the type; the schema element of an included chameleon document
/// is the one of its own document, as the attribute reads.
/// </para>
/// </remarks>
internal static class LocalTargetNamespace
{
    private const string TargetNamespace = "targetNamespace";

    private static readonly XPathQName AnyType = new(SchemaGrammar.Namespace, "anyType");

    /// <summary>Checks the declarations of one schema document.</summary>
    /// <param name="root">The <c>schema</c> element.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void Check(SchemaSyntaxNode root, XsdVersion version, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(reporter);

        if (version == XsdVersion.V10)
        {
            return;
        }

        Visit(root, root.FindAttribute(TargetNamespace), reporter);
    }

    /// <summary>Gets the expanded name that the <c>targetNamespace</c> attribute gives a declaration.</summary>
    /// <param name="declaration">The <c>element</c> or <c>attribute</c> syntax.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="name">Receives the expanded name; for a declaration that is not valid too, which is built all the same.</param>
    /// <returns><see langword="true"/> when the declaration has a name and the attribute, and the version knows it.</returns>
    internal static bool TryGetName(SchemaSyntaxNode declaration, XsdVersion version, out XPathQName name)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        name = default;
        var targetNamespace = declaration.FindAttribute(TargetNamespace);
        var localName = declaration.FindAttribute("name");
        if (version == XsdVersion.V10 || targetNamespace is null || localName is null)
        {
            return false;
        }

        name = new XPathQName(targetNamespace, localName);
        return true;
    }

    private static void Visit(SchemaSyntaxNode node, string? schemaNamespace, XsdIssueReporter reporter)
    {
        if (
            (Is(node, "element") || Is(node, "attribute")) && node.FindAttribute(TargetNamespace) is { } targetNamespace
        )
        {
            CheckDeclaration(node, targetNamespace, schemaNamespace, reporter);
        }

        for (var i = 0; i < node.ChildCount; i++)
        {
            Visit(node.GetChildNode(i), schemaNamespace, reporter);
        }
    }

    private static void CheckDeclaration(
        SchemaSyntaxNode node,
        string targetNamespace,
        string? schemaNamespace,
        XsdIssueReporter reporter
    )
    {
        var isAttribute = Is(node, "attribute");
        var rule = XsdRuleLookup.Find(isAttribute ? XsdRuleCodes.SrcAttribute : XsdRuleCodes.SrcElement);
        var clause = isAttribute ? "src-attribute.6" : "src-element.4";
        var kind = isAttribute ? "attribute" : "element";

        if (node.FindAttribute("name") is null)
        {
            reporter.Report(
                rule,
                node.Location,
                "An " + kind + " declaration with a targetNamespace must have a name.",
                clause + ".1"
            );
        }

        if (node.FindAttribute("form") is not null)
        {
            reporter.Report(
                rule,
                node.Location,
                "An " + kind + " declaration with a targetNamespace must not have a form.",
                clause + ".2"
            );
        }

        if (!string.Equals(schemaNamespace, targetNamespace, StringComparison.Ordinal))
        {
            CheckPlacement(node, rule, clause, kind, reporter);
        }
    }

    // The declaration names a namespace other than that of the schema: it must sit in the restriction of a complex type.
    private static void CheckPlacement(
        SchemaSyntaxNode node,
        XsdRule rule,
        string clause,
        string kind,
        XsdIssueReporter reporter
    )
    {
        SchemaSyntaxNode? restriction = null;
        var inComplexType = false;
        for (var ancestor = node.Parent; ancestor is not null && !inComplexType; ancestor = ancestor.Parent)
        {
            inComplexType = Is(ancestor, "complexType");
            if (restriction is null && Is(ancestor, "restriction"))
            {
                restriction = ancestor;
            }
        }

        if (!inComplexType)
        {
            reporter.Report(
                rule,
                node.Location,
                "An "
                    + kind
                    + " declaration with a targetNamespace other than that of the schema must be inside a complex type.",
                clause + ".3.1"
            );
        }
        else if (restriction is null || BaseIsAnyType(restriction))
        {
            reporter.Report(
                rule,
                node.Location,
                "An "
                    + kind
                    + " declaration with a targetNamespace other than that of the schema must be inside the restriction of a type other than xs:anyType.",
                clause + ".3.2"
            );
        }
    }

    private static bool BaseIsAnyType(SchemaSyntaxNode restriction) =>
        restriction.FindAttribute("base") is { } value
        && new SchemaNamespaceScope(string.Empty, false).TryResolve(restriction, value, out var name)
        && name == AnyType;

    private static bool Is(SchemaSyntaxNode node, string localName) =>
        string.Equals(node.Name.LocalName, localName, StringComparison.Ordinal)
        && string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal);
}
