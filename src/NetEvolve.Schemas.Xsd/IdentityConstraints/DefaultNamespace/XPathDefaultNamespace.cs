namespace NetEvolve.Schemas.Xsd.IdentityConstraints.DefaultNamespace;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The <c>xpathDefaultNamespace</c> attribute of <c>selector</c> and <c>field</c> (XSD 1.1, Structures section 3.11.2 and the
/// XPath expression mapping of section 3.13.2). <see cref="Resolve"/> gives the namespace of the unprefixed element name tests of
/// one path; <see cref="Apply"/> binds it before the identity constraint definition is created, so the definition and the
/// evaluation of XSD-15 see a path whose names all carry a prefix.
/// </summary>
/// <remarks>
/// The value of the path element wins, then the value of the <c>schema</c> element of the same document, then no namespace. The
/// value is a namespace name or one of the special values <c>##local</c> (no namespace), <c>##targetNamespace</c> (the effective
/// target namespace, the including one for a chameleon), and <c>##defaultNamespace</c> (the default namespace in scope at the path
/// element). The default namespace applies to element name tests only: attribute names, wildcards, and prefixed names keep
/// their meaning. Any other value is read as a namespace name, as the lexical space of <c>xs:anyURI</c> admits it.
/// Limitations: the namespace is bound by giving each unprefixed element name test a prefix (<c>xdn</c>, or <c>xdn1</c> and on when
/// that is taken) declared on the path element and written into its <c>xpath</c> attribute, so <c>Selector</c> and <c>Fields</c> of
/// the definition show the prefixed text; <see cref="Apply"/> has to run before
/// <c>IdentityConstraintDefinition.Create</c>, which the compilation pipeline of XSD-78 does. In strict 1.0 mode the attribute is
/// ignored here because <c>Strict10Constructs</c> reports it as a schema error.
/// </remarks>
internal static class XPathDefaultNamespace
{
    private const string AttributeName = "xpathDefaultNamespace";
    private const string BasePrefix = "xdn";

    /// <summary>Resolves the namespace of the unprefixed element names of a selector or field.</summary>
    /// <param name="path">The <c>selector</c> or <c>field</c> element.</param>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    /// <param name="version">The configured XSD version; the attribute has no meaning in <see cref="XsdVersion.V10"/>.</param>
    /// <returns>The namespace name, empty for no namespace.</returns>
    internal static string Resolve(SchemaSyntaxNode path, SchemaNamespaceScope scope, XsdVersion version)
    {
        Argument.ThrowIfNull(path);
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        if (version == XsdVersion.V10)
        {
            return string.Empty;
        }

        var root = path;
        while (root.Parent is { } parent)
        {
            root = parent;
        }

        var value = path.FindAttribute(AttributeName) ?? root.FindAttribute(AttributeName);
        if (value is null)
        {
            return string.Empty;
        }

        value = value.Trim(' ', '\t', '\r', '\n');
        switch (value)
        {
            case "##local":
                return string.Empty;
            case "##targetNamespace":
                return scope.TargetNamespace;
            case "##defaultNamespace":
                _ = path.TryResolvePrefix(string.Empty, out var defaultNamespace);
                return defaultNamespace;
            default:
                return value;
        }
    }

    /// <summary>Gives the unprefixed element names of the selector and the fields of a constraint the prefix of their default namespace.</summary>
    /// <param name="constraint">The <c>unique</c>, <c>key</c>, or <c>keyref</c> element.</param>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    /// <param name="version">The configured XSD version.</param>
    internal static void Apply(SchemaSyntaxNode constraint, SchemaNamespaceScope scope, XsdVersion version)
    {
        Argument.ThrowIfNull(constraint);
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        for (var i = 0; i < constraint.ChildCount; i++)
        {
            var child = constraint.GetChildNode(i);
            if (
                string.Equals(child.Name.LocalName, "selector", StringComparison.Ordinal)
                || string.Equals(child.Name.LocalName, "field", StringComparison.Ordinal)
            )
            {
                Qualify(child, scope, version);
            }
        }
    }

    private static void Qualify(SchemaSyntaxNode path, SchemaNamespaceScope scope, XsdVersion version)
    {
        var namespaceName = Resolve(path, scope, version);
        var xpath = path.FindAttribute("xpath");

        // A path that does not parse is the definition's to report.
        if (namespaceName.Length == 0 || xpath is null || !XPathParser.TryParse(xpath.AsSpan(), out var tree, out _))
        {
            return;
        }

        var offsets = new List<int>();
        CollectElementNames(tree!, offsets);
        if (offsets.Count == 0)
        {
            return;
        }

        offsets.Sort();
        var prefix = BasePrefix;
        for (
            var n = 1;
            path.TryResolvePrefix(prefix, out _) || xpath.Contains(prefix + ":", StringComparison.Ordinal);
            n++
        )
        {
            prefix = BasePrefix + n;
        }

        var text = new StringBuilder(xpath.Length + (offsets.Count * (prefix.Length + 1)));
        var last = 0;
        foreach (var offset in offsets)
        {
            text.Append(xpath, last, offset - last).Append(prefix).Append(':');
            last = offset;
        }

        text.Append(xpath, last, xpath.Length - last);
        path.AddNamespace(prefix, namespaceName);
        path.SetAttribute("xpath", text.ToString());
    }

    // The restricted subset has no other place for a name than the child axis; the attribute axis keeps its names.
    private static void CollectElementNames(SyntaxNode node, List<int> offsets)
    {
        if (
            node is AxisStep
            {
                Axis: Axis.Child,
                Test: NameTest { Kind: NameTestKind.QualifiedName, Prefix: null } test,
            }
        )
        {
            offsets.Add(test.Location.Offset);
        }

        var children = node.Children;
        for (var i = 0; i < children.Count; i++)
        {
            CollectElementNames(children[i], offsets);
        }
    }
}
