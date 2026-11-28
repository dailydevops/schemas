namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Attributes.DefaultAttributes;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.OpenContent;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.TypeAlternatives;

/// <summary>
/// Builds complex type definitions (Structures 3.4.2) from the syntax of a <c>complexType</c>: the implicit form, <c>complexContent</c>,
/// and <c>simpleContent</c>, each with its content particle, attribute uses, and attribute wildcard. The base type is built first,
/// so a definition may derive from a type that the document declares later.
/// </summary>
/// <remarks>
/// A type that violates a representation constraint still is built, as an empty restriction of <c>anyType</c>, so later phases see
/// no missing component and report no second error. The <c>openContent</c> child and the <c>defaultOpenContent</c> child of the schema
/// document are read under XSD 1.1 (XSD-78, #1424). The <c>defaultAttributes</c> of a schema is read from the document that holds the type (Structures 1.1, 3.4.2.3.1); the <c>assert</c> children are collected under XSD 1.1.
/// </remarks>
internal sealed class ComplexTypeBuilder : IComponentBuilder
{
    private const XsdDerivationMethod DerivationMethods =
        XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;

    private readonly BuildContext _context;

    internal ComplexTypeBuilder(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ComplexType;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not the syntax of a top-level <c>complexType</c> of the build context.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

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
        ArgumentNullException.ThrowIfNull(definition);

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
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(unit);

        if (node.FindAttribute("name") is not null)
        {
            _context.Report(
                XsdRuleCodes.SrcCt,
                node.Location,
                "A complex type definition that is not a child of schema must not have a name.",
                "src-ct.5"
            );
        }

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
        ArgumentNullException.ThrowIfNull(unit);

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
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(unit);

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
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(unit);

        _context.FoundElements.Add(element);
        _context.ElementUnits[element] = unit;
        if (element.InlineTypeSyntax is not null)
        {
            _context.PendingElements.Enqueue(new KeyValuePair<ElementDeclaration, UnitScope>(element, unit));
        }

        if (element.TypeTable is { } table)
        {
            for (var i = 0; i < table.Count; i++)
            {
                Register(table[i], unit);
            }

            if (table.Default is { } fallback)
            {
                Register(fallback, unit);
            }
        }
    }

    // An alternative has references to bind, and an anonymous type to build when it states one.
    private void Register(TypeAlternative alternative, UnitScope unit)
    {
        _context.Anonymous.Add(alternative);
        if (alternative.InlineTypeSyntax is not null)
        {
            _context.PendingAlternatives.Enqueue(new KeyValuePair<TypeAlternative, UnitScope>(alternative, unit));
        }
    }

    private ComplexTypeDefinition Construct(SchemaSyntaxNode node, UnitScope unit, ComplexTypeHeader header)
    {
        var type = Create(node, unit, header, out var content) ?? Fallback(header);
        _context.BuiltComplexTypes.Add(type);
        if (_context.Version == XsdVersion.V11)
        {
            var assertions = ComplexTypeAssertion.Collect(node, unit.Scope, _context.Version, _context.Reporter);
            if (assertions.Count > 0)
            {
                _context.ComplexTypeAssertions[type] = assertions;
            }
        }

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
            return FromSimpleContent(node, simple, unit, header);
        }

        var complex = SyntaxValues.FindChild(node, "complexContent");
        if (complex is not null)
        {
            return FromComplexContent(node, complex, unit, header, typeMixed, out content);
        }

        content = ReadContent(node, unit);
        var uses = ReadUses(node, unit, null, out var wildcard);
        uses = WithDefaults(node, unit, uses, ref wildcard);
        return ComplexTypeDefinition.Implicit(
            header,
            typeMixed,
            content,
            uses,
            wildcard,
            _context.Reporter,
            ReadOpenContent(node, unit),
            ReadDefaultOpenContent(unit)
        );
    }

    private ComplexTypeDefinition? FromComplexContent(
        SchemaSyntaxNode typeNode,
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
        var uses = ReadUses(derivation, unit, baseType, out var wildcard);
        uses = WithDefaults(typeNode, unit, uses, ref wildcard);
        return ComplexTypeDefinition.FromComplexContent(
            header,
            method,
            baseType,
            ComplexTypeDefinition.EffectiveMixed(typeMixed, contentMixed),
            content,
            uses,
            wildcard,
            _context.Reporter,
            ReadOpenContent(derivation, unit),
            ReadDefaultOpenContent(unit),
            _context.Version
        );
    }

    private ComplexTypeDefinition? FromSimpleContent(
        SchemaSyntaxNode typeNode,
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
        var uses = ReadUses(derivation, unit, baseType, out var wildcard);
        uses = WithDefaults(typeNode, unit, uses, ref wildcard);
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
        var asserted = HasAssertionFacet(restriction);
        if (anonymous is null && step.Count == 0 && !asserted)
        {
            return null;
        }

        IXsdComponent? restricted = anonymous ?? (baseType as ComplexTypeDefinition)?.SimpleContentType;
        if (restricted is null)
        {
            return null;
        }

        if (step.Count == 0 && !asserted)
        {
            return anonymous;
        }

        var facts = FacetBase.Resolve(restricted, _context.Version);
        if (facts is null)
        {
            RejectFacetsOfAnySimpleType(restricted, step);
        }

        var check = SimpleTypeFacetCheck.Check(
            restricted,
            facts,
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
            FacetFactory.Create(step, facts, _context.Version, check.CompiledPattern, _context.Assessor),
            _context.Reporter
        );
        if (type is not null)
        {
            _context.SetFixed(type, check.Fixed);
            _context.NoteEnumeration(type, restricted, step);
            _context.SimpleTypes.NoteNotationValues(restriction, unit, facts);
            _context.SimpleTypes.NoteAssertions(type, restriction, unit);
        }

        return type;
    }

    // Datatypes 4.1.5 (cos-applicable-facets): anySimpleType has no primitive, so a length, digits, or bounds facet applies to
    // nothing. Structures 1.0 only; XSD 1.1 leaves the facets of such a restriction to the datatype check.
    private void RejectFacetsOfAnySimpleType(IXsdComponent restricted, List<FacetDeclaration> step)
    {
        if (
            _context.Version != XsdVersion.V10
            || _context.AsSimple(restricted) is not { Name: { LocalName: "anySimpleType" } name }
            || !string.Equals(name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
        )
        {
            return;
        }

        for (var i = 0; i < step.Count; i++)
        {
            if (
                !FacetApplicability.IsApplicable(
                    step[i].Kind,
                    DatatypeVariety.Atomic,
                    PrimitiveKind.None,
                    _context.Version
                )
            )
            {
                _context.Report(
                    XsdRuleCodes.CosApplicableFacets,
                    step[i].Location,
                    "The facet does not apply to the simple content type anySimpleType of the base type."
                );
            }
        }
    }

    private static bool HasAssertionFacet(SchemaSyntaxNode restriction)
    {
        for (var i = 0; i < restriction.ChildCount; i++)
        {
            if (SyntaxValues.IsSchemaElement(restriction.GetChildNode(i), "assertion"))
            {
                return true;
            }
        }

        return false;
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

    // The openContent child of a complexType, an extension, or a restriction; null for none, and always null under XSD 1.0.
    private OpenContentElement? ReadOpenContent(SchemaSyntaxNode owner, UnitScope unit)
    {
        if (_context.Version != XsdVersion.V11 || SyntaxValues.FindChild(owner, "openContent") is not { } node)
        {
            return null;
        }

        var mode = ReadMode(node);
        return OpenContentElement.Create(
            mode,
            mode == OpenContentMode.None ? null : ReadAny(node, unit),
            node.Location,
            _context.Reporter
        );
    }

    // The defaultOpenContent child of the schema document of the unit; null for none or without a usable any child.
    private OpenContentElement? ReadDefaultOpenContent(UnitScope unit)
    {
        if (
            _context.Version != XsdVersion.V11
            || SyntaxValues.FindChild(unit.Root, "defaultOpenContent") is not { } node
        )
        {
            return null;
        }

        var mode = ReadMode(node);
        return mode != OpenContentMode.None && ReadAny(node, unit) is { } wildcard
            ? OpenContentElement.CreateDefault(
                mode,
                wildcard,
                SyntaxValues.IsTrue(node.FindAttribute("appliesToEmpty")),
                node.Location
            )
            : null;
    }

    // The schema grammar reports an invalid mode, so an unknown value reads as the default.
    private static OpenContentMode ReadMode(SchemaSyntaxNode node) =>
        node.FindAttribute("mode") switch
        {
            "none" => OpenContentMode.None,
            "suffix" => OpenContentMode.Suffix,
            _ => OpenContentMode.Interleave,
        };

    private IWildcard? ReadAny(SchemaSyntaxNode openContent, UnitScope unit) =>
        SyntaxValues.FindChild(openContent, "any") is { } any
            ? unit.Particles.BuildParticle(any, _context.Reporter)?.Term as IWildcard
            : null;

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
    private List<AttributeUse> ReadUses(
        SchemaSyntaxNode owner,
        UnitScope unit,
        IXsdComponent? baseType,
        out IWildcard? wildcard
    )
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
                    uses.Add(KeepsAsOptional(use, baseType) ? OptionalOf(use) : use);
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

    // Structures 1.1, 3.4.2.3.1: the default attribute group of the schema acts as one more attribute group reference at the end of
    // the attribute children of a type, unless defaultAttributesApply is false.
    private List<AttributeUse> WithDefaults(
        SchemaSyntaxNode typeNode,
        UnitScope unit,
        List<AttributeUse> uses,
        ref IWildcard? wildcard
    )
    {
        var group = _context.ResolveDefaultAttributeGroup(unit);
        if (group is null)
        {
            return uses;
        }

        _ = DefaultAttributeRules.TryApply(
            group,
            DefaultAttributeRules.Applies(typeNode.FindAttribute("defaultAttributesApply")),
            uses,
            wildcard,
            typeNode.Location,
            _context.Reporter,
            out var all,
            out wildcard
        );
        return [.. all];
    }

    // Structures 1.0: a prohibited use with a value constraint that removes no use of the base does not forbid the attribute.
    private bool KeepsAsOptional(AttributeUse use, IXsdComponent? baseType)
    {
        if (
            _context.Version != XsdVersion.V10
            || !use.Prohibited
            || use.ValueConstraint.Kind == AttributeValueConstraintKind.None
        )
        {
            return false;
        }

        if (baseType is ComplexTypeDefinition complex)
        {
            for (var i = 0; i < complex.AttributeUses.Count; i++)
            {
                if (complex.AttributeUses[i].Name == use.Name)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private AttributeUse OptionalOf(AttributeUse use) =>
        AttributeUse.Create(
            use.Location,
            AttributeUseKind.Optional,
            use.Declaration,
            use.ValueConstraint,
            _context.Reporter
        ) ?? use;

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
