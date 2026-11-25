namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Subsets;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// An identity-constraint definition: <c>unique</c>, <c>key</c>, or <c>keyref</c> with its selector and ordered fields
/// (Structures section 3.11). The selector and field expressions are checked against the restricted XPath subset of the
/// XPath package when the definition is created; the check of a <c>keyref</c> against its target runs in
/// <see cref="ResolveRefer"/>.
/// </summary>
/// <remarks>
/// Limitations: the <c>xpathDefaultNamespace</c> attribute (XSD 1.1, XSD-85) and the <see langword="ref"/> attribute (XSD 1.1, XSD-84)
/// are not handled, so an unprefixed name test selects no-namespace names and a definition without <c>name</c> is an error.
/// Evaluation against instances is XSD-15.
/// </remarks>
internal sealed class IdentityConstraintDefinition : IXsdComponent
{
    private const string Xml = "xml";

    private readonly string[] _fields;

    private IdentityConstraintDefinition(
        IdentityConstraintKind kind,
        XPathQName name,
        DocumentLocation location,
        Parts parts,
        XPathQName refer
    )
    {
        ConstraintKind = kind;
        Name = name;
        Location = location;
        Selector = parts.Selector;
        _fields = parts.Fields;
        SelectorBindings = parts.SelectorBindings;
        FieldBindings = parts.FieldBindings;
        Refer = refer;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.IdentityConstraint;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets the category.</summary>
    public IdentityConstraintKind ConstraintKind { get; }

    /// <summary>Gets the selector expression.</summary>
    public string Selector { get; }

    /// <summary>Gets the field expressions in document order.</summary>
    public IReadOnlyList<string> Fields => _fields;

    /// <summary>Gets the namespace bindings of the prefixes of the selector in order of first use; the prefix <c>xml</c> is not listed.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> SelectorBindings { get; }

    /// <summary>Gets the namespace bindings of the prefixes of every field, in the order of <see cref="Fields"/>.</summary>
    public IReadOnlyList<IReadOnlyList<KeyValuePair<string, string>>> FieldBindings { get; }

    /// <summary>Gets the expanded name of the referenced definition of a keyref, <see langword="default"/> otherwise.</summary>
    public XPathQName Refer { get; }

    /// <summary>Gets the key or unique definition a keyref refers to, <see langword="null"/> until <see cref="ResolveRefer"/> succeeded.</summary>
    public IdentityConstraintDefinition? ReferencedConstraint { get; private set; }

    /// <summary>Creates a definition from its declaration and registers it.</summary>
    /// <param name="element">The <c>unique</c>, <c>key</c>, or <c>keyref</c> element.</param>
    /// <param name="scope">The namespace the schema document contributes to: the name and the <c>refer</c> of a chameleon-included document are in the including namespace.</param>
    /// <param name="registry">The registry that receives the definition.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The definition, <see langword="null"/> after an issue was reported.</returns>
    internal static IdentityConstraintDefinition? Create(
        SchemaSyntaxNode element,
        SchemaNamespaceScope scope,
        IComponentRegistry registry,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        var kind = element.Name.LocalName switch
        {
            "key" => IdentityConstraintKind.Key,
            "keyref" => IdentityConstraintKind.Keyref,
            _ => IdentityConstraintKind.Unique,
        };

        var localName = element.FindAttribute("name");
        if (localName is null or { Length: 0 })
        {
            Report(
                reporter,
                XsdRuleCodes.SrcIdentityConstraint,
                element.Location,
                "An identity constraint needs a non-empty name."
            );
            return null;
        }

        var parts = TryReadParts(element, reporter);
        if (parts is null)
        {
            return null;
        }

        var refer = default(XPathQName);
        if (
            kind == IdentityConstraintKind.Keyref
            && !scope.TryResolve(element, element.FindAttribute("refer") ?? string.Empty, out refer)
        )
        {
            Report(reporter, XsdRuleCodes.SrcResolve, element.Location, "The prefix of refer is not declared.");
            return null;
        }

        var name = scope.Declare(localName);
        if (registry.TryGet(XsdComponentKind.IdentityConstraint, name, out _))
        {
            Report(
                reporter,
                XsdRuleCodes.SchPropsCorrect,
                element.Location,
                "Two identity constraints have the expanded name " + name + "."
            );
            return null;
        }

        var definition = new IdentityConstraintDefinition(kind, name, element.Location, parts, refer);
        registry.Add(definition);
        return definition;
    }

    /// <summary>Resolves the refer of a keyref and checks it against the target; does nothing for unique and key.</summary>
    /// <param name="registry">The registry holding the identity constraints.</param>
    /// <param name="reporter">The reporter.</param>
    internal void ResolveRefer(IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        if (ConstraintKind != IdentityConstraintKind.Keyref)
        {
            return;
        }

        if (
            !registry.TryGet(XsdComponentKind.IdentityConstraint, Refer, out var found)
            || found is not IdentityConstraintDefinition target
        )
        {
            Report(
                reporter,
                XsdRuleCodes.SrcResolve,
                Location,
                "The identity constraint " + Refer + " is not declared."
            );
        }
        else if (target.ConstraintKind == IdentityConstraintKind.Keyref)
        {
            Report(
                reporter,
                XsdRuleCodes.CPropsCorrect,
                Location,
                "A keyref must refer to a key or unique, not to a keyref."
            );
        }
        else if (target._fields.Length != _fields.Length)
        {
            Report(
                reporter,
                XsdRuleCodes.CPropsCorrect,
                Location,
                "A keyref must have as many fields as the constraint it refers to."
            );
        }
        else
        {
            ReferencedConstraint = target;
        }
    }

    private static Parts? TryReadParts(SchemaSyntaxNode element, XsdIssueReporter reporter)
    {
        SchemaSyntaxNode? selectorNode = null;
        var fieldNodes = new List<SchemaSyntaxNode>();
        var selectors = 0;
        for (var i = 0; i < element.ChildCount; i++)
        {
            var child = element.GetChildNode(i);
            if (string.Equals(child.Name.LocalName, "selector", StringComparison.Ordinal))
            {
                selectorNode = child;
                selectors++;
            }
            else if (string.Equals(child.Name.LocalName, "field", StringComparison.Ordinal))
            {
                fieldNodes.Add(child);
            }
        }

        if (selectors != 1 || fieldNodes.Count == 0)
        {
            Report(
                reporter,
                XsdRuleCodes.SrcIdentityConstraint,
                element.Location,
                "An identity constraint needs exactly one selector and at least one field."
            );
            return null;
        }

        return ReadPaths(selectorNode!, fieldNodes, reporter);
    }

    private static Parts? ReadPaths(
        SchemaSyntaxNode selectorNode,
        List<SchemaSyntaxNode> fieldNodes,
        XsdIssueReporter reporter
    )
    {
        if (
            !IsValid(
                selectorNode,
                XPathSubsetChecker.Selector,
                XsdRuleCodes.CSelectorXpath,
                reporter,
                out var selectorBindings
            )
        )
        {
            return null;
        }

        var fields = new string[fieldNodes.Count];
        var fieldBindings = new IReadOnlyList<KeyValuePair<string, string>>[fields.Length];
        for (var i = 0; i < fields.Length; i++)
        {
            if (
                !IsValid(
                    fieldNodes[i],
                    XPathSubsetChecker.Field,
                    XsdRuleCodes.CFieldsXpaths,
                    reporter,
                    out fieldBindings[i]
                )
            )
            {
                return null;
            }

            fields[i] = fieldNodes[i].FindAttribute("xpath")!;
        }

        return new Parts(selectorNode.FindAttribute("xpath")!, fields, selectorBindings, fieldBindings);
    }

    private static bool IsValid(
        SchemaSyntaxNode node,
        XPathSubsetChecker profile,
        DiagnosticCode code,
        XsdIssueReporter reporter,
        out IReadOnlyList<KeyValuePair<string, string>> bindings
    )
    {
        bindings = [];

        // The grammar of the schema document requires the xpath attribute on selector and field.
        var xpath = node.FindAttribute("xpath") ?? string.Empty;
        if (!XPathParser.TryParse(xpath.AsSpan(), out var tree, out var diagnostic))
        {
            Report(reporter, code, node.Location, diagnostic.Message + " (offset " + diagnostic.Offset + ")");
            return false;
        }

        if (!profile.Accepts(tree!, out diagnostic))
        {
            Report(reporter, code, node.Location, diagnostic.Message + " (offset " + diagnostic.Offset + ")");
            return false;
        }

        var found = new List<KeyValuePair<string, string>>();
        var prefix = CollectBindings(tree!, node, found);
        if (prefix is not null)
        {
            Report(reporter, code, node.Location, "The prefix '" + prefix + "' is not declared.");
            return false;
        }

        bindings = found;
        return true;
    }

    // Walks the shapes the subset accepts: unions, paths, and axis steps with a name test. Collects the namespace of every prefix
    // in order of first use and returns the first prefix that is not declared.
    private static string? CollectBindings(
        Expression expression,
        SchemaSyntaxNode scope,
        List<KeyValuePair<string, string>> bindings
    )
    {
        switch (expression)
        {
            case SetExpression set:
                return CollectBindings(set.Left, scope, bindings) ?? CollectBindings(set.Right, scope, bindings);
            case PathExpression path:
                for (var i = 0; i < path.Steps.Count; i++)
                {
                    var found = CollectBindings(path.Steps[i], scope, bindings);
                    if (found is not null)
                    {
                        return found;
                    }
                }

                return null;
            case AxisStep { Test: NameTest { Prefix: { } prefix } }
                when !string.Equals(prefix, Xml, StringComparison.Ordinal):
                if (!scope.TryResolvePrefix(prefix, out var uri))
                {
                    return prefix;
                }

                if (!bindings.Exists(binding => string.Equals(binding.Key, prefix, StringComparison.Ordinal)))
                {
                    bindings.Add(new KeyValuePair<string, string>(prefix, uri));
                }

                return null;
            default:
                return null;
        }
    }

    private sealed class Parts
    {
        internal Parts(
            string selector,
            string[] fields,
            IReadOnlyList<KeyValuePair<string, string>> selectorBindings,
            IReadOnlyList<KeyValuePair<string, string>>[] fieldBindings
        )
        {
            Selector = selector;
            Fields = fields;
            SelectorBindings = selectorBindings;
            FieldBindings = fieldBindings;
        }

        internal string Selector { get; }

        internal string[] Fields { get; }

        internal IReadOnlyList<KeyValuePair<string, string>> SelectorBindings { get; }

        internal IReadOnlyList<KeyValuePair<string, string>>[] FieldBindings { get; }
    }

    private static void Report(
        XsdIssueReporter reporter,
        DiagnosticCode code,
        DocumentLocation location,
        string message
    )
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == code)
            {
                reporter.Report(rule, location, message);
                return;
            }
        }
    }
}
