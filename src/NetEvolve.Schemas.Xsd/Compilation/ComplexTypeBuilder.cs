namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// Builds complex type definitions (Structures 3.4.2) from the syntax of a <c>complexType</c>: the implicit form, <c>complexContent</c>,
/// and <c>simpleContent</c>, each with its content particle, attribute uses, and attribute wildcard. The base type is built first,
/// so a definition may derive from a type that the document declares later.
/// </summary>
/// <remarks>
/// A type that violates a representation constraint still is built, as an empty restriction of <c>anyType</c>, so later phases see
/// no missing component and report no second error. Limitations: <c>openContent</c> and <c>defaultOpenContent</c> (XSD 1.1) are not
/// read (XSD-78, #360), and neither are the XSD 1.1 <c>assert</c> children and <c>defaultAttributesApply</c>.
/// </remarks>
internal sealed class ComplexTypeBuilder : IComponentBuilder
{
    private const XsdDerivationMethod DerivationMethods =
        XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;

    private readonly BuildContext _context;

    internal ComplexTypeBuilder(BuildContext context)
    {
        Argument.ThrowIfNull(context);

        _context = context;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ComplexType;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not the syntax of a top-level <c>complexType</c> of the build context.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(reporter);

        return
            element is SchemaSyntaxNode node
            && _context.DefinitionOf(node) is { } definition
            && SyntaxValues.IsSchemaElement(node, "complexType")
            ? Build(definition)
            : throw new NotSupportedException("Only the syntax of a top-level complexType can be built.");
    }

    /// <summary>Builds the global complex type of a definition and registers it.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The type.</returns>
    internal ComplexTypeDefinition Build(Definition definition)
    {
        Argument.ThrowIfNull(definition);

        var node = definition.Node;
        var root = definition.Unit.Root;
        var header = new ComplexTypeHeader(
            definition.Name,
            node.Location,
            SyntaxValues.IsTrue(node.FindAttribute("abstract")),
            SyntaxValues.ParseMethods(
                node.FindAttribute("block") ?? root.FindAttribute("blockDefault"),
                DerivationMethods
            ),
            SyntaxValues.ParseMethods(
                node.FindAttribute("final") ?? root.FindAttribute("finalDefault"),
                DerivationMethods
            )
        );
        var type = Construct(node, definition.Unit, header);
        _context.Registry.Add(type);
        return type;
    }

    /// <summary>Builds the anonymous complex type of a <c>complexType</c> child.</summary>
    /// <param name="node">The <c>complexType</c> element.</param>
    /// <param name="unit">The unit that holds it.</param>
    /// <returns>The type, which the caller binds to its element.</returns>
    internal ComplexTypeDefinition BuildAnonymous(SchemaSyntaxNode node, UnitScope unit)
    {
        Argument.ThrowIfNull(node);
        Argument.ThrowIfNull(unit);

        var header = new ComplexTypeHeader(
            default,
            node.Location,
            SyntaxValues.IsTrue(node.FindAttribute("abstract")),
            SyntaxValues.ParseMethods(node.FindAttribute("block"), DerivationMethods),
            SyntaxValues.ParseMethods(node.FindAttribute("final"), DerivationMethods)
        );
        var type = Construct(node, unit, header);
        _context.Anonymous.Add(type);
        return type;
    }

    /// <summary>Finds the element declarations of a particle that need an anonymous type and queues them.</summary>
    /// <param name="particle">The particle, <see langword="null"/> for none.</param>
    /// <param name="unit">The unit that holds the syntax.</param>
    internal void Scan(Particle? particle, UnitScope unit)
    {
        Argument.ThrowIfNull(unit);

        switch (particle?.Term)
        {
            case ElementDeclaration element:
                Scan(element, unit);
                break;
            case ModelGroup group:
                Scan(group, unit);
                break;
            default:
                break;
        }
    }

    /// <summary>Finds the element declarations of a model group that need an anonymous type and queues them.</summary>
    /// <param name="group">The group.</param>
    /// <param name="unit">The unit that holds the syntax.</param>
    internal void Scan(ModelGroup group, UnitScope unit)
    {
        Argument.ThrowIfNull(group);
        Argument.ThrowIfNull(unit);

        for (var i = 0; i < group.Particles.Count; i++)
        {
            Scan(group.Particles[i], unit);
        }
    }

    /// <summary>Records an element declaration and queues it when its type is anonymous.</summary>
    /// <param name="element">The declaration.</param>
    /// <param name="unit">The unit that holds the syntax.</param>
    internal void Scan(ElementDeclaration element, UnitScope unit)
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(unit);

        _context.FoundElements.Add(element);
        if (element.InlineTypeSyntax is not null)
        {
            _context.PendingElements.Enqueue(new KeyValuePair<ElementDeclaration, UnitScope>(element, unit));
        }
    }

    private ComplexTypeDefinition Construct(SchemaSyntaxNode node, UnitScope unit, ComplexTypeHeader header)
    {
        var type = Create(node, unit, header, out var content) ?? Fallback(header);
        _context.BuiltComplexTypes.Add(type);
        Scan(content, unit);
        return type;
    }

    private ComplexTypeDefinition Fallback(ComplexTypeHeader header) =>
        ComplexTypeDefinition.Implicit(header, false, null, [], null, _context.Reporter);

    private ComplexTypeDefinition? Create(
        SchemaSyntaxNode node,
        UnitScope unit,
        ComplexTypeHeader header,
        out Particle? content
    )
    {
        content = null;
        var typeMixed = SyntaxValues.IsTrue(node.FindAttribute("mixed"));
        var simple = SyntaxValues.FindChild(node, "simpleContent");
        if (simple is not null)
        {
            return FromSimpleContent(simple, unit, header);
        }

        var complex = SyntaxValues.FindChild(node, "complexContent");
        if (complex is not null)
        {
            return FromComplexContent(complex, unit, header, typeMixed, out content);
        }

        content = ReadContent(node, unit);
        var uses = ReadUses(node, unit, out var wildcard);
        return ComplexTypeDefinition.Implicit(header, typeMixed, content, uses, wildcard, _context.Reporter);
    }

    private ComplexTypeDefinition? FromComplexContent(
        SchemaSyntaxNode complexContent,
        UnitScope unit,
        ComplexTypeHeader header,
        bool typeMixed,
        out Particle? content
    )
    {
        content = null;
        var derivation = ReadDerivation(complexContent, header, out var method);
        if (derivation is null)
        {
            return null;
        }

        var baseType = ReadBase(derivation, unit);
        if (baseType is null)
        {
            return null;
        }

        var contentMixed = complexContent.FindAttribute("mixed") is { } value
            ? SyntaxValues.IsTrue(value)
            : (bool?)null;
        content = ReadContent(derivation, unit);
        var uses = ReadUses(derivation, unit, out var wildcard);
        return ComplexTypeDefinition.FromComplexContent(
            header,
            method,
            baseType,
            ComplexTypeDefinition.EffectiveMixed(typeMixed, contentMixed),
            content,
            uses,
            wildcard,
            _context.Reporter
        );
    }

    private ComplexTypeDefinition? FromSimpleContent(
        SchemaSyntaxNode simpleContent,
        UnitScope unit,
        ComplexTypeHeader header
    )
    {
        var derivation = ReadDerivation(simpleContent, header, out var method);
        if (derivation is null)
        {
            return null;
        }

        var baseType = ReadBase(derivation, unit);
        if (baseType is null)
        {
            return null;
        }

        var simpleType = method == XsdDerivationMethod.Restriction ? RestrictContent(derivation, unit, baseType) : null;
        var uses = ReadUses(derivation, unit, out var wildcard);
        return ComplexTypeDefinition.FromSimpleContent(
            header,
            method,
            baseType,
            simpleType,
            uses,
            wildcard,
            _context.Reporter
        );
    }

    // The simple type of a simpleContent restriction: the anonymous simpleType child and the facets that restrict it, or the
    // simple content type of the base restricted by the facets; null keeps the simple content type of the base.
    private SimpleTypeDefinition? RestrictContent(SchemaSyntaxNode restriction, UnitScope unit, IXsdComponent baseType)
    {
        var inline = SyntaxValues.FindChild(restriction, "simpleType");
        var anonymous = inline is null ? null : _context.SimpleTypes.BuildAnonymous(inline, unit);
        var step = _context.SimpleTypes.ReadFacets(restriction);
        if (anonymous is null && step.Count == 0)
        {
            return null;
        }

        IXsdComponent? restricted = anonymous ?? (baseType as ComplexTypeDefinition)?.SimpleContentType;
        if (restricted is null)
        {
            return null;
        }

        if (step.Count == 0)
        {
            return anonymous;
        }

        var check = SimpleTypeFacetCheck.Check(
            restricted,
            _context.FixedOf(restricted),
            step,
            restriction.Location,
            _context.Assessor,
            _context.Version,
            _context.Reporter
        );
        var type = SimpleTypeDefinition.Restrict(
            default,
            restriction.Location,
            XsdDerivationMethod.None,
            restricted,
            FacetFactory.Create(restricted, step, _context.Version),
            _context.Reporter
        );
        if (type is not null)
        {
            _context.SetFixed(type, check.Fixed);
        }

        return type;
    }

    private SchemaSyntaxNode? ReadDerivation(
        SchemaSyntaxNode content,
        ComplexTypeHeader header,
        out XsdDerivationMethod method
    )
    {
        method = XsdDerivationMethod.Restriction;
        for (var i = 0; i < content.ChildCount; i++)
        {
            var child = content.GetChildNode(i);
            if (SyntaxValues.IsSchemaElement(child, "restriction"))
            {
                return child;
            }

            if (SyntaxValues.IsSchemaElement(child, "extension"))
            {
                method = XsdDerivationMethod.Extension;
                return child;
            }
        }

        _context.Report(
            XsdRuleCodes.SrcCt,
            header.Location,
            "A " + content.Name.LocalName + " must have a restriction or an extension.",
            "src-ct.1"
        );
        return null;
    }

    private IXsdComponent? ReadBase(SchemaSyntaxNode derivation, UnitScope unit)
    {
        var lexical = derivation.FindAttribute("base");
        if (lexical is null)
        {
            _context.Report(
                XsdRuleCodes.SrcCt,
                derivation.Location,
                "A " + derivation.Name.LocalName + " must have a base attribute.",
                "src-ct.1"
            );
            return null;
        }

        return _context.ResolveType(derivation, unit, lexical, derivation.Location);
    }

    // The particle of the group, all, choice, or sequence child.
    private Particle? ReadContent(SchemaSyntaxNode owner, UnitScope unit)
    {
        for (var i = 0; i < owner.ChildCount; i++)
        {
            var child = owner.GetChildNode(i);
            if (
                SyntaxValues.IsSchemaElement(child, "group")
                || ParticleBuilder.IsCompositor(child.Name.LocalName)
                    && string.Equals(child.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            )
            {
                return unit.Particles.BuildParticle(child, _context.Reporter);
            }
        }

        return null;
    }

    // The attribute uses of the attribute and attributeGroup children, and the wildcard that anyAttribute and the groups give.
    private List<AttributeUse> ReadUses(SchemaSyntaxNode owner, UnitScope unit, out IWildcard? wildcard)
    {
        var uses = new List<AttributeUse>();
        wildcard = null;
        var failed = false;
        for (var i = 0; i < owner.ChildCount; i++)
        {
            var child = owner.GetChildNode(i);
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
                var group = _context.ResolveAttributeGroup(child, unit);
                if (group is not null)
                {
                    uses.AddRange(group.Uses);
                    Intersect(ref wildcard, ref failed, group.AttributeWildcard, child);
                }
            }
            else if (SyntaxValues.IsSchemaElement(child, "anyAttribute"))
            {
                Intersect(ref wildcard, ref failed, _context.AttributeGroups.ReadWildcard(child, unit), child);
            }
        }

        return uses;
    }

    private void Intersect(ref IWildcard? wildcard, ref bool failed, IWildcard? next, SchemaSyntaxNode at)
    {
        if (next is null || failed)
        {
            return;
        }

        if (wildcard is null)
        {
            wildcard = next;
            return;
        }

        wildcard = wildcard.Intersect(next);
        if (wildcard is null)
        {
            _context.Report(
                XsdRuleCodes.CosAwIntersect,
                at.Location,
                "The attribute wildcards of the type have no expressible intersection."
            );
            failed = true;
        }
    }
}
