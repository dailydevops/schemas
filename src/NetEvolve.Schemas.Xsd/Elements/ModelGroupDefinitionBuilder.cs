namespace NetEvolve.Schemas.Xsd.Elements;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// Builds named model group definitions (Structures section 3.7.2) from the syntax of a <see langword="group"/> child of <c>schema</c> and
/// registers them. Group references inside stay unresolved until the resolver runs; it also reports circular definitions.
/// </summary>
/// <remarks>
/// A definition that violates <c>src-model_group_defn</c> is still built, with an empty sequence when it has no compositor, so
/// later phases see no missing component.
/// </remarks>
internal sealed class ModelGroupDefinitionBuilder : IComponentBuilder
{
    private readonly SchemaNamespaceScope _scope;
    private readonly ParticleBuilder _particles;

    /// <summary>Initializes a new instance of the <see cref="ModelGroupDefinitionBuilder"/> class.</summary>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    internal ModelGroupDefinitionBuilder(SchemaNamespaceScope scope)
    {
        _scope = scope;
        _particles = new ParticleBuilder(scope);
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ModelGroupDefinition;

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
        if (name is null || name.Length == 0)
        {
            Report(reporter, node, "A model group definition must have a name.");
        }

        SchemaSyntaxNode? compositor = null;
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChildNode(i);
            if (!ParticleBuilder.IsCompositor(child.Name.LocalName))
            {
                continue;
            }

            if (compositor is null)
            {
                compositor = child;
            }
            else
            {
                Report(reporter, node, "A model group definition must have exactly one of all, choice, and sequence.");
            }
        }

        if (compositor is null)
        {
            Report(reporter, node, "A model group definition must have one of all, choice, and sequence.");
        }

        if (compositor?.FindAttribute("minOccurs") is not null || compositor?.FindAttribute("maxOccurs") is not null)
        {
            Report(reporter, node, "The model group of a model group definition must not have minOccurs or maxOccurs.");
        }

        var group = compositor is null
            ? ModelGroup.Create(ModelGroupCompositor.Sequence, node.Location, [])
            : _particles.BuildGroup(compositor, reporter);
        var definition = ModelGroupDefinition.Create(
            name is null || name.Length == 0 ? default : _scope.Declare(name),
            node.Location,
            group
        );
        registry.Add(definition);
        return definition;
    }

    private static void Report(XsdIssueReporter reporter, SchemaSyntaxNode node, string message) =>
        reporter.Report(XsdRuleLookup.SrcModelGroupDefn, node.Location, message, "src-model_group_defn.1");
}
