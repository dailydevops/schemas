namespace NetEvolve.Schemas.Xsd.Elements;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// Builds global element declarations (Structures section 3.3.2) from the syntax of an <c>element</c> child of <c>schema</c>
/// and registers them; local declarations are built through <see cref="BuildDeclaration"/> by the particle builder. References
/// stay unresolved until the resolver runs.
/// </summary>
/// <remarks>
/// The syntax must come from <see cref="SchemaDocumentParser"/>, whose grammar already restricted the attribute values. The
/// constraints that depend on several attributes (<c>src-element.1</c>, <c>.2.1</c>, <c>.3</c>) are checked here. A declaration
/// that violates one is still built, so later phases see no missing component; the schema error is reported once.
/// </remarks>
internal sealed class ElementDeclarationBuilder : IComponentBuilder
{
    private const XsdDerivationMethod BlockMethods =
        XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction | XsdDerivationMethod.Substitution;
    private const XsdDerivationMethod FinalMethods = XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;

    private readonly SchemaNamespaceScope _scope;

    /// <summary>Initializes a new instance of the <see cref="ElementDeclarationBuilder"/> class.</summary>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    internal ElementDeclarationBuilder(SchemaNamespaceScope scope) => _scope = scope;

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ElementDeclaration;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not a <see cref="SchemaSyntaxNode"/>, the only syntax with parent and namespace scope.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(reporter);

        var node =
            element as SchemaSyntaxNode
            ?? throw new NotSupportedException("Only a SchemaSyntaxNode can be built into a component.");
        var declaration = BuildDeclaration(node, isGlobal: true, reporter);
        registry.Add(declaration);
        return declaration;
    }

    /// <summary>Builds a declaration from an <c>element</c> syntax node that has a <c>name</c>.</summary>
    /// <param name="node">The syntax.</param>
    /// <param name="isGlobal">Whether the node is a child of <c>schema</c>.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The declaration.</returns>
    internal ElementDeclaration BuildDeclaration(SchemaSyntaxNode node, bool isGlobal, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(node);
        Argument.ThrowIfNull(reporter);

        var name = node.FindAttribute("name");
        if (isGlobal && (name is null || node.FindAttribute("ref") is not null))
        {
            ReportElement(
                reporter,
                node,
                "src-element.2.1",
                "A global element declaration must have a name and no ref."
            );
        }

        var fixedValue = node.FindAttribute("fixed");
        var defaultValue = node.FindAttribute("default");
        if (fixedValue is not null && defaultValue is not null)
        {
            ReportElement(
                reporter,
                node,
                "src-element.1",
                "An element declaration must not have both default and fixed."
            );
            defaultValue = null;
        }

        var (typeName, inlineType) = ReadType(node, reporter);
        var root = Root(node);
        var form = node.FindAttribute("form") ?? root.FindAttribute("elementFormDefault");
        var qualifiedName =
            name is null || name.Length == 0 ? default
            : isGlobal || string.Equals(form, "qualified", StringComparison.Ordinal) ? _scope.Declare(name)
            : new XPathQName(string.Empty, name);

        return ElementDeclaration.Create(
            qualifiedName,
            node.Location,
            isGlobal,
            typeName,
            inlineType,
            IsTrue(node.FindAttribute("nillable")),
            fixedValue is not null ? new ElementValueConstraint(ElementValueConstraintKind.Fixed, fixedValue)
                : defaultValue is not null
                    ? new ElementValueConstraint(ElementValueConstraintKind.Default, defaultValue)
                : default,
            IsTrue(node.FindAttribute("abstract")),
            ParseMethods(node.FindAttribute("block") ?? root.FindAttribute("blockDefault"), BlockMethods),
            ParseMethods(node.FindAttribute("final") ?? root.FindAttribute("finalDefault"), FinalMethods)
        );
    }

    /// <summary>Reports a violation of Element Declaration Representation OK.</summary>
    /// <param name="reporter">The reporter.</param>
    /// <param name="node">The element declaration syntax.</param>
    /// <param name="clause">The clause path.</param>
    /// <param name="message">The message.</param>
    internal static void ReportElement(
        XsdIssueReporter reporter,
        SchemaSyntaxNode node,
        string clause,
        string message
    ) => reporter.Report(XsdRuleLookup.SrcElement, node.Location, message, clause);

    private (XPathQName TypeName, ISchemaSyntaxElement? Inline) ReadType(
        SchemaSyntaxNode node,
        XsdIssueReporter reporter
    )
    {
        ISchemaSyntaxElement? inline = null;
        for (var i = 0; i < node.ChildCount && inline is null; i++)
        {
            var child = node.GetChildNode(i);
            if (child.Name.LocalName is "simpleType" or "complexType")
            {
                inline = child;
            }
        }

        var type = node.FindAttribute("type");
        if (type is null)
        {
            return inline is null ? (new XPathQName(SchemaGrammar.Namespace, "anyType"), null) : (default, inline);
        }

        if (inline is not null)
        {
            ReportElement(
                reporter,
                node,
                "src-element.3",
                "An element declaration must not have both a type attribute and an anonymous type."
            );
        }

        if (_scope.TryResolve(node, type, out var typeName))
        {
            return (typeName, null);
        }

        reporter.Report(
            XsdRuleLookup.SrcResolve,
            node.Location,
            $"The type '{type}' is not a QName with a declared prefix."
        );
        return (default, null);
    }

    private static SchemaSyntaxNode Root(SchemaSyntaxNode node)
    {
        while (node.Parent is not null)
        {
            node = node.Parent;
        }

        return node;
    }

    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.Ordinal) || string.Equals(value, "1", StringComparison.Ordinal);

    // The grammar guarantees "#all" or a list of method names; names outside the mask (list, union) do not apply to elements.
    private static XsdDerivationMethod ParseMethods(string? value, XsdDerivationMethod mask)
    {
        if (value is null)
        {
            return XsdDerivationMethod.None;
        }

        if (string.Equals(value.Trim(), "#all", StringComparison.Ordinal))
        {
            return mask;
        }

        var methods = XsdDerivationMethod.None;
        foreach (var token in value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            methods |= token switch
            {
                "extension" => XsdDerivationMethod.Extension,
                "restriction" => XsdDerivationMethod.Restriction,
                "substitution" => XsdDerivationMethod.Substitution,
                _ => XsdDerivationMethod.None,
            };
        }

        return methods & mask;
    }
}
