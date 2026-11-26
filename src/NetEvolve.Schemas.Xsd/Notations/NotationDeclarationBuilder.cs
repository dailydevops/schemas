namespace NetEvolve.Schemas.Xsd.Notations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// Builds notation declarations (Structures section 3.12.2) from the syntax of a <c>notation</c> child of <c>schema</c> and
/// registers them in the notation symbol space. A declaration that violates <c>src-notation</c> is reported and still built.
/// </summary>
internal sealed class NotationDeclarationBuilder : IComponentBuilder
{
    private readonly SchemaNamespaceScope _scope;

    /// <summary>Initializes a new instance of the <see cref="NotationDeclarationBuilder"/> class.</summary>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    internal NotationDeclarationBuilder(SchemaNamespaceScope scope) => _scope = scope;

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.Notation;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not a <see cref="SchemaSyntaxNode"/>, the only syntax with parent and namespace scope.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        var node =
            element as SchemaSyntaxNode
            ?? throw new NotSupportedException("Only a SchemaSyntaxNode can be built into a component.");

        var name = node.FindAttribute("name");
        var publicId = node.FindAttribute("public");
        var systemId = node.FindAttribute("system");
        if (publicId is null && systemId is null)
        {
            reporter.Report(
                XsdRuleLookup.SrcNotation,
                node.Location,
                "A notation declaration must have a public or a system identifier.",
                "src-notation"
            );
        }

        var notation = new NotationDeclaration(
            name is null || name.Length == 0 ? default : _scope.Declare(name),
            node.Location,
            publicId,
            systemId
        );
        registry.Add(notation);
        return notation;
    }
}
