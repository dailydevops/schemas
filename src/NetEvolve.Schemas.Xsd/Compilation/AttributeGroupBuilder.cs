namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// Builds attribute group definitions (Structures 3.6.2) from the syntax of a top-level <c>attributeGroup</c>: the uses of its
/// <c>attribute</c> children, the references of its <c>attributeGroup</c> children by name, and its <c>anyAttribute</c>. The
/// references are bound by <see cref="AttributeGroupDefinition.Resolve"/> once every group is registered.
/// </summary>
internal sealed class AttributeGroupBuilder : IComponentBuilder
{
    private readonly BuildContext _context;

    internal AttributeGroupBuilder(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.AttributeGroup;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not the syntax of a top-level <c>attributeGroup</c> of the build context.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        return
            element is SchemaSyntaxNode node
            && _context.DefinitionOf(node) is { } definition
            && SyntaxValues.IsSchemaElement(node, "attributeGroup")
            ? Ensure(definition)
            : throw new NotSupportedException("Only the syntax of a top-level attributeGroup can be built.");
    }

    /// <summary>Builds the group of a definition once and registers it.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The group.</returns>
    internal AttributeGroupDefinition Ensure(Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Component is AttributeGroupDefinition built)
        {
            return built;
        }

        var node = definition.Node;
        var unit = definition.Unit;
        if (definition.Name == default)
        {
            _context.Report(
                XsdRuleCodes.SrcAttributeGroup,
                node.Location,
                "An attribute group definition must have a name.",
                "src-attribute_group.1"
            );
        }

        var uses = new List<AttributeUse>();
        var references = new List<AttributeGroupReference>();
        IWildcard? wildcard = null;
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChildNode(i);
            if (SyntaxValues.IsSchemaElement(child, "attribute"))
            {
                var use = _context.Attributes.BuildUse(child, unit);
                if (use is not null)
                {
                    uses.Add(use);
                }
            }
            else if (SyntaxValues.IsSchemaElement(child, "attributeGroup"))
            {
                AddReference(child, unit, references);
            }
            else if (SyntaxValues.IsSchemaElement(child, "anyAttribute"))
            {
                wildcard = ReadWildcard(child, unit);
            }
        }

        var group = AttributeGroupDefinition.Create(definition.Name, node.Location, uses, references, wildcard);
        definition.Component = group;
        _context.Registry.Add(group);
        _context.BuiltAttributeGroups.Add(group);
        return group;
    }

    /// <summary>Reads an <c>anyAttribute</c> element.</summary>
    /// <param name="node">The element.</param>
    /// <param name="unit">The unit that holds it.</param>
    /// <returns>The wildcard, <see langword="null"/> after an issue was reported.</returns>
    internal IWildcard? ReadWildcard(SchemaSyntaxNode node, UnitScope unit)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(unit);

        if (
            Wildcard.TryCreate(
                node.FindAttribute("namespace"),
                node.FindAttribute("processContents"),
                unit.TargetNamespace,
                node.Location,
                out var wildcard
            )
        )
        {
            return wildcard;
        }

        _context.Report(XsdRuleCodes.SrcWildcard, node.Location, "The wildcard has an invalid value.");
        return null;
    }

    private void AddReference(SchemaSyntaxNode node, UnitScope unit, List<AttributeGroupReference> references)
    {
        if (node.FindAttribute("ref") is null)
        {
            _context.Report(
                XsdRuleCodes.SrcAttributeGroup,
                node.Location,
                "An attribute group reference must have a ref attribute.",
                "src-attribute_group.1"
            );
        }
        else if (BuildContext.TryNameOf(node, unit, out var name))
        {
            references.Add(new AttributeGroupReference(name, node.Location));
        }
        else
        {
            _context.Report(
                XsdRuleCodes.SrcResolve,
                node.Location,
                "The attribute group reference is not a QName with a declared prefix."
            );
        }
    }
}
