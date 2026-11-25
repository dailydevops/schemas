namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// Builds simple type definitions from the syntax of a <c>simpleType</c> (Structures 3.14.2): a restriction with its facets, a
/// list, or a union. The base, item, and member types are built first, so a definition may use one that the document declares
/// later; a cycle is reported once and cut.
/// </summary>
/// <remarks>
/// The step of a restriction goes through <see cref="SimpleTypeFacetCheck"/> before the type is created; a type that violates a
/// representation constraint still is built, as an unconstrained restriction of <c>anySimpleType</c>, so later phases see no
/// missing component and report no second error. Limitation: see <see cref="FacetFactory"/> for the facets the definition carries.
/// </remarks>
internal sealed class SimpleTypeBuilder : IComponentBuilder
{
    private const XsdDerivationMethod FinalMethods =
        XsdDerivationMethod.Restriction | XsdDerivationMethod.List | XsdDerivationMethod.Union;

    private readonly BuildContext _context;

    internal SimpleTypeBuilder(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.SimpleType;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not the syntax of a top-level <c>simpleType</c> of the build context.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        return
            element is SchemaSyntaxNode node
            && _context.DefinitionOf(node) is { } definition
            && SyntaxValues.IsSchemaElement(node, "simpleType")
            ? Build(definition)
            : throw new NotSupportedException("Only the syntax of a top-level simpleType can be built.");
    }

    /// <summary>Builds the global simple type of a definition and registers it.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The type.</returns>
    internal SimpleTypeDefinition Build(Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var node = definition.Node;
        var final = SyntaxValues.ParseMethods(
            node.FindAttribute("final") ?? definition.Unit.Root.FindAttribute("finalDefault"),
            FinalMethods
        );
        var type = Construct(node, definition.Unit, definition.Name, final);
        _context.Registry.Add(type);
        return type;
    }

    /// <summary>Builds the anonymous simple type of a <c>simpleType</c> child.</summary>
    /// <param name="node">The <c>simpleType</c> element.</param>
    /// <param name="unit">The unit that holds it.</param>
    /// <returns>The type.</returns>
    internal SimpleTypeDefinition BuildAnonymous(SchemaSyntaxNode node, UnitScope unit)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(unit);

        if (node.FindAttribute("name") is not null)
        {
            _context.Report(
                XsdRuleCodes.SrcSimpleType,
                node.Location,
                "A simple type definition that is not a child of schema must not have a name.",
                "src-simple-type.3"
            );
        }

        return Construct(node, unit, default, XsdDerivationMethod.None);
    }

    private SimpleTypeDefinition Construct(
        SchemaSyntaxNode node,
        UnitScope unit,
        XPathQName name,
        XsdDerivationMethod final
    )
    {
        SchemaSyntaxNode? content = null;
        var count = 0;
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChildNode(i);
            if (
                SyntaxValues.IsSchemaElement(child, "restriction")
                || SyntaxValues.IsSchemaElement(child, "list")
                || SyntaxValues.IsSchemaElement(child, "union")
            )
            {
                content ??= child;
                count++;
            }
        }

        // The content that is not exactly one of the three is reported with the children of every schema element (SchemaStructure).
        if (count != 1)
        {
            return _context.Placeholder(name, node.Location);
        }

        var type = content!.Name.LocalName switch
        {
            "restriction" => Restrict(content, node, unit, name, final),
            "list" => List(content, node, unit, name, final),
            _ => Union(content, node, unit, name, final),
        };
        return type ?? _context.Placeholder(name, node.Location);
    }

    private SimpleTypeDefinition? Restrict(
        SchemaSyntaxNode restriction,
        SchemaSyntaxNode node,
        UnitScope unit,
        XPathQName name,
        XsdDerivationMethod final
    )
    {
        var baseType = ReadType(restriction, unit, "base");
        if (baseType is null)
        {
            return null;
        }

        var step = ReadFacets(restriction);
        var check = SimpleTypeFacetCheck.Check(
            baseType,
            _context.FixedOf(baseType),
            step,
            restriction.Location,
            _context.Assessor,
            _context.Version,
            _context.Reporter
        );
        var parent = _context.AsSimple(baseType) ?? baseType;
        var type = SimpleTypeDefinition.Restrict(
            name,
            node.Location,
            final,
            parent,
            FacetFactory.Create(baseType, step, _context.Version),
            _context.Reporter
        );
        if (type is not null)
        {
            _context.SetFixed(type, check.Fixed);
            _context.NoteEnumeration(type, baseType, step);
        }

        return type;
    }

    private SimpleTypeDefinition? List(
        SchemaSyntaxNode list,
        SchemaSyntaxNode node,
        UnitScope unit,
        XPathQName name,
        XsdDerivationMethod final
    )
    {
        var item = ReadType(list, unit, "itemType");
        return item is null
            ? null
            : SimpleTypeDefinition.List(
                name,
                node.Location,
                final,
                _context.AsSimple(item) ?? item,
                [],
                _context.Reporter
            );
    }

    private SimpleTypeDefinition? Union(
        SchemaSyntaxNode union,
        SchemaSyntaxNode node,
        UnitScope unit,
        XPathQName name,
        XsdDerivationMethod final
    )
    {
        var members = new List<IXsdComponent>();
        var failed = false;
        var memberTypes = union.FindAttribute("memberTypes");
        if (memberTypes is not null)
        {
            foreach (var lexical in SyntaxValues.Split(memberTypes))
            {
                var member = _context.ResolveType(union, unit, lexical, union.Location);
                if (member is null)
                {
                    failed = true;
                }
                else
                {
                    members.Add(_context.AsSimple(member) ?? member);
                }
            }
        }

        var anonymous = 0;
        for (var i = 0; i < union.ChildCount; i++)
        {
            var child = union.GetChildNode(i);
            if (SyntaxValues.IsSchemaElement(child, "simpleType"))
            {
                members.Add(BuildAnonymous(child, unit));
                anonymous++;
            }
        }

        // The document parser reports a union without member types (src-union-memberTypes-or-simpleTypes).
        if (memberTypes is null && anonymous == 0)
        {
            return null;
        }

        return failed ? null : SimpleTypeDefinition.Union(name, node.Location, final, members, [], _context.Reporter);
    }

    // The base of a restriction, the item type of a list: a QName attribute or an anonymous simpleType child, never both.
    private IXsdComponent? ReadType(SchemaSyntaxNode parent, UnitScope unit, string attribute)
    {
        var lexical = parent.FindAttribute(attribute);
        var inline = SyntaxValues.FindChild(parent, "simpleType");
        // The document parser reports a choice that is not made exactly once (src-restriction-base-or-simpleType, src-list-itemType-or-simpleType).
        if ((lexical is null) == (inline is null))
        {
            return null;
        }

        return lexical is null
            ? BuildAnonymous(inline!, unit)
            : _context.ResolveType(parent, unit, lexical, parent.Location);
    }

    internal List<FacetDeclaration> ReadFacets(SchemaSyntaxNode restriction)
    {
        ArgumentNullException.ThrowIfNull(restriction);

        var step = new List<FacetDeclaration>();
        var seen = new HashSet<FacetKind>();
        for (var i = 0; i < restriction.ChildCount; i++)
        {
            var child = restriction.GetChildNode(i);
            var kind = KindOf(child);
            if (kind == FacetKind.None)
            {
                continue;
            }

            var value = child.FindAttribute("value");
            if (value is null)
            {
                _context.Report(
                    XsdRuleCodes.SrcSingleFacetValue,
                    child.Location,
                    "A facet must have a value attribute.",
                    "src-single-facet-value"
                );
                continue;
            }

            if (kind is not (FacetKind.Pattern or FacetKind.Enumeration) && !seen.Add(kind))
            {
                _context.Report(
                    XsdRuleCodes.SrcSingleFacetValue,
                    child.Location,
                    "A restriction must not state the " + child.Name.LocalName + " facet twice.",
                    "src-single-facet-value"
                );
                continue;
            }

            step.Add(
                new FacetDeclaration(kind, value, child.Location, SyntaxValues.IsTrue(child.FindAttribute("fixed")))
            );
        }

        return step;
    }

    private static FacetKind KindOf(SchemaSyntaxNode node) =>
        !string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            ? FacetKind.None
            : node.Name.LocalName switch
            {
                "length" => FacetKind.Length,
                "minLength" => FacetKind.MinLength,
                "maxLength" => FacetKind.MaxLength,
                "pattern" => FacetKind.Pattern,
                "enumeration" => FacetKind.Enumeration,
                "whiteSpace" => FacetKind.WhiteSpace,
                "maxInclusive" => FacetKind.MaxInclusive,
                "maxExclusive" => FacetKind.MaxExclusive,
                "minExclusive" => FacetKind.MinExclusive,
                "minInclusive" => FacetKind.MinInclusive,
                "totalDigits" => FacetKind.TotalDigits,
                "fractionDigits" => FacetKind.FractionDigits,
                "explicitTimezone" => FacetKind.ExplicitTimezone,
                _ => FacetKind.None,
            };
}
