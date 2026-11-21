namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// Builds attribute declarations (Structures 3.2.2) and attribute uses (3.5.2) from the syntax of an <c>attribute</c> element:
/// a global declaration, a local declaration, or a reference to a global declaration. The type is built first, so a declaration
/// may use a simple type that the document declares later.
/// </summary>
/// <remarks>
/// A declaration that violates <c>src-attribute</c> is not built; the issue is reported once and a use of it is left out, so
/// the type that holds it still is built and reports no second error.
/// </remarks>
internal sealed class AttributeBuilder : IComponentBuilder
{
    private readonly BuildContext _context;

    internal AttributeBuilder(BuildContext context)
    {
        Argument.ThrowIfNull(context);

        _context = context;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.AttributeDeclaration;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not the syntax of a top-level <c>attribute</c> of the build context.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(reporter);

        return
            element is SchemaSyntaxNode node
            && _context.DefinitionOf(node) is { } definition
            && SyntaxValues.IsSchemaElement(node, "attribute")
            ? Ensure(definition) ?? throw new NotSupportedException("The attribute declaration has an error.")
            : throw new NotSupportedException("Only the syntax of a top-level attribute can be built.");
    }

    /// <summary>Builds the global attribute declaration of a definition once and registers it.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The declaration, <see langword="null"/> when it has an error that was reported.</returns>
    internal AttributeDeclaration? Ensure(Definition definition)
    {
        Argument.ThrowIfNull(definition);

        if (definition.Component is null && !definition.IsFailed)
        {
            var declaration = Declare(definition.Node, definition.Unit, isGlobal: true);
            if (declaration is null)
            {
                definition.IsFailed = true;
            }
            else
            {
                definition.Component = declaration;
                _context.Registry.Add(declaration);
            }
        }

        return definition.Component as AttributeDeclaration;
    }

    /// <summary>Builds an attribute use: from a local declaration or from a reference to a global one.</summary>
    /// <param name="node">The <c>attribute</c> child of a type or an attribute group.</param>
    /// <param name="unit">The unit that holds it.</param>
    /// <returns>The use, <see langword="null"/> after an issue was reported.</returns>
    internal AttributeUse? BuildUse(SchemaSyntaxNode node, UnitScope unit)
    {
        Argument.ThrowIfNull(node);
        Argument.ThrowIfNull(unit);

        var constraint = ReadConstraint(node);
        var reference = node.FindAttribute("ref");
        AttributeDeclaration? declaration;
        if (reference is null)
        {
            declaration = Declare(node, unit, isGlobal: false);
        }
        else
        {
            if (
                node.FindAttribute("name") is not null
                || node.FindAttribute("type") is not null
                || node.FindAttribute("form") is not null
                || SyntaxValues.FindChild(node, "simpleType") is not null
            )
            {
                _context.Report(
                    XsdRuleCodes.SrcAttribute,
                    node.Location,
                    "An attribute with a ref must not have a name, type, form, or an anonymous type.",
                    "src-attribute.3.2"
                );
            }

            declaration = _context.ResolveAttribute(node, unit, reference);
        }

        if (declaration is null)
        {
            return null;
        }

        var use = AttributeUse.Create(node.Location, ReadUse(node), declaration, constraint, _context.Reporter);
        if (use is not null)
        {
            _context.BuiltUses.Add(use);
        }

        return use;
    }

    private AttributeDeclaration? Declare(SchemaSyntaxNode node, UnitScope unit, bool isGlobal)
    {
        var name = node.FindAttribute("name");
        if (name is null || name.Length == 0)
        {
            _context.Report(
                XsdRuleCodes.SrcAttribute,
                node.Location,
                "An attribute declaration must have a name or a ref.",
                "src-attribute.3.1"
            );
            return null;
        }

        CheckRepresentation(node, isGlobal);
        var type = ReadType(node, unit);
        if (type is null)
        {
            return null;
        }

        var constraint = ReadConstraint(node);
        var declaration = isGlobal
            ? AttributeDeclaration.Global(
                name,
                unit.TargetNamespace,
                node.Location,
                type,
                constraint,
                _context.Reporter
            )
            : AttributeDeclaration.Local(
                name,
                unit.TargetNamespace,
                FormOf(node),
                node.Location,
                type,
                constraint,
                _context.Reporter
            );
        if (declaration is not null)
        {
            _context.BuiltAttributes.Add(declaration);
        }

        return declaration;
    }

    // The form of a local declaration: its own attribute, else the default of the schema (Structures 3.2.2).
    private static AttributeForm FormOf(SchemaSyntaxNode node) =>
        string.Equals(
            node.FindAttribute("form") ?? SyntaxValues.Root(node).FindAttribute("attributeFormDefault"),
            "qualified",
            StringComparison.Ordinal
        )
            ? AttributeForm.Qualified
            : AttributeForm.Unqualified;

    private void CheckRepresentation(SchemaSyntaxNode node, bool isGlobal)
    {
        if (isGlobal && (node.FindAttribute("form") is not null || node.FindAttribute("use") is not null))
        {
            _context.Report(
                XsdRuleCodes.SrcAttribute,
                node.Location,
                "A global attribute declaration must not have a form or a use attribute.",
                "src-attribute.3.2"
            );
        }

        if (node.FindAttribute("default") is not null && node.FindAttribute("fixed") is not null)
        {
            _context.Report(
                XsdRuleCodes.SrcAttribute,
                node.Location,
                "An attribute declaration must not have both default and fixed.",
                "src-attribute.1"
            );
        }
    }

    private SimpleTypeDefinition? ReadType(SchemaSyntaxNode node, UnitScope unit)
    {
        var lexical = node.FindAttribute("type");
        var inline = SyntaxValues.FindChild(node, "simpleType");
        if (lexical is not null && inline is not null)
        {
            _context.Report(
                XsdRuleCodes.SrcAttribute,
                node.Location,
                "An attribute declaration must not have both a type attribute and an anonymous type.",
                "src-attribute.4"
            );
        }

        if (lexical is null)
        {
            return inline is null ? _context.AnySimpleType() : _context.SimpleTypes.BuildAnonymous(inline, unit);
        }

        var type = _context.ResolveType(node, unit, lexical, node.Location);
        if (type is null)
        {
            return null;
        }

        var simple = _context.AsSimple(type);
        if (simple is null)
        {
            _context.Report(
                XsdRuleCodes.SrcAttribute,
                node.Location,
                "The type of an attribute must be a simple type.",
                "src-attribute"
            );
        }

        return simple;
    }

    private static AttributeValueConstraint ReadConstraint(SchemaSyntaxNode node)
    {
        var fixedValue = node.FindAttribute("fixed");
        if (fixedValue is not null)
        {
            return new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, fixedValue);
        }

        var defaultValue = node.FindAttribute("default");
        return defaultValue is not null
            ? new AttributeValueConstraint(AttributeValueConstraintKind.Default, defaultValue)
            : default;
    }

    private static AttributeUseKind ReadUse(SchemaSyntaxNode node) =>
        node.FindAttribute("use") switch
        {
            "required" => AttributeUseKind.Required,
            "prohibited" => AttributeUseKind.Prohibited,
            _ => AttributeUseKind.Optional,
        };
}
