namespace NetEvolve.Schemas.Xsd.Compilation;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.Assertions.Facet;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.TypeAlternatives;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The state that the component builders of one compilation share: the registry, the reporter, the index of the top-level
/// definitions by name, and what has been built so far. A definition is built when something first needs it, so the order of
/// the documents never matters.
/// </summary>
/// <remarks>
/// <para>
/// Types are built by a depth-first walk with an explicit stack over the base, item, and member types a definition names, so a
/// long derivation chain never nests calls. A definition that is reached while it is on the path is a cycle
/// (<c>st-props-correct.2</c>, <c>ct-props-correct.3</c>); it is reported once and the reference gets a placeholder, so every
/// definition of the cycle still is built and the schema errors that follow are not duplicated.
/// </para>
/// <para>
/// A built-in simple type is registered as an <see cref="XsdBuiltInType"/> that wraps its datatype; the definition factories
/// take a <see cref="SimpleTypeDefinition"/>, so <see cref="AsSimple"/> hands out one wrapper per built-in type.
/// </para>
/// </remarks>
internal sealed class BuildContext
{
    private readonly Dictionary<SchemaSyntaxNode, Definition> _definitions = [];
    private readonly Dictionary<XPathQName, Definition> _types = [];
    private readonly Dictionary<XPathQName, Definition> _attributes = [];
    private readonly Dictionary<XPathQName, Definition> _attributeGroups = [];
    private readonly Dictionary<SchemaSyntaxNode, AttributeGroupDefinition?> _defaultGroups = [];
    private readonly Dictionary<XsdBuiltInType, SimpleTypeDefinition> _wrappers = [];
    private readonly Dictionary<IXsdComponent, FacetKindSet> _fixed = [];
    private readonly HashSet<IXsdComponent> _enumeratedTypes = [];
    private readonly List<(XPathQName Name, DocumentLocation Location)> _notationValues = [];
    private readonly XsdBuiltInType _anySimpleType;

    internal BuildContext(XsdVersion version, ComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        Version = version;
        Registry = registry;
        Reporter = reporter;
        Assessor = new DescriptorValueAssessor(version);
        _ = registry.TryGet(
            XsdSymbolSpace.Types,
            new XPathQName(SchemaGrammar.Namespace, "anySimpleType"),
            out var any
        );
        _anySimpleType = (XsdBuiltInType)any!;
        SimpleTypes = new SimpleTypeBuilder(this);
        ComplexTypes = new ComplexTypeBuilder(this);
        Attributes = new AttributeBuilder(this);
        AttributeGroups = new AttributeGroupBuilder(this);
    }

    /// <summary>Gets the XSD version.</summary>
    public XsdVersion Version { get; }

    /// <summary>Gets the registry that receives the named components.</summary>
    public ComponentRegistry Registry { get; }

    /// <summary>Gets the reporter of the compilation.</summary>
    public XsdIssueReporter Reporter { get; }

    /// <summary>Gets the assessor that checks a lexical value against a simple type.</summary>
    public ISimpleValueAssessor Assessor { get; }

    /// <summary>Gets the builder of simple types.</summary>
    public SimpleTypeBuilder SimpleTypes { get; }

    /// <summary>Gets the builder of complex types.</summary>
    public ComplexTypeBuilder ComplexTypes { get; }

    /// <summary>Gets the builder of attribute declarations and uses.</summary>
    public AttributeBuilder Attributes { get; }

    /// <summary>Gets the builder of attribute groups.</summary>
    public AttributeGroupBuilder AttributeGroups { get; }

    /// <summary>Gets every top-level type, attribute, and attribute group definition in document order.</summary>
    public List<Definition> Definitions { get; } = [];

    /// <summary>Gets every complex type that was built, named and anonymous.</summary>
    public List<ComplexTypeDefinition> BuiltComplexTypes { get; } = [];

    /// <summary>Gets the assertions that each built complex type declares itself (XSD 1.1), in document order; a type without any is not listed.</summary>
    public Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>> ComplexTypeAssertions { get; } = [];

    /// <summary>Gets the assertion facets that each built simple type declares itself (XSD 1.1), in document order; a type without any is not listed.</summary>
    public Dictionary<SimpleTypeDefinition, IReadOnlyList<FacetAssertion>> SimpleTypeAssertions { get; } = [];

    /// <summary>Gets every attribute declaration that was built, global and local.</summary>
    public List<AttributeDeclaration> BuiltAttributes { get; } = [];

    /// <summary>Gets every attribute use that was built.</summary>
    public List<AttributeUse> BuiltUses { get; } = [];

    /// <summary>Gets every attribute group that was built.</summary>
    public List<AttributeGroupDefinition> BuiltAttributeGroups { get; } = [];

    /// <summary>Gets the components that no registry lists but that have references to bind: anonymous types.</summary>
    public List<IReferencingComponent> Anonymous { get; } = [];

    /// <summary>Gets the element declarations whose anonymous type is still to be built, with the unit that holds them.</summary>
    public Queue<KeyValuePair<ElementDeclaration, UnitScope>> PendingElements { get; } = new();

    /// <summary>Gets the type alternatives whose anonymous type is still to be built, with the unit that holds them.</summary>
    public Queue<KeyValuePair<TypeAlternative, UnitScope>> PendingAlternatives { get; } = new();

    /// <summary>Gets every element declaration that was found: global, local, and nested in an anonymous type.</summary>
    public List<ElementDeclaration> FoundElements { get; } = [];

    /// <summary>Gets the unit that holds the syntax of each found element declaration.</summary>
    public Dictionary<ElementDeclaration, UnitScope> ElementUnits { get; } = [];

    /// <summary>Indexes the top-level definitions of the units.</summary>
    /// <param name="units">The units.</param>
    internal void Index(IReadOnlyList<UnitScope> units)
    {
        ArgumentNullException.ThrowIfNull(units);

        for (var u = 0; u < units.Count; u++)
        {
            var unit = units[u];
            for (var i = 0; i < unit.Unit.Components.Count; i++)
            {
                var node = unit.Unit.Components[i];
                var index = node.Name.LocalName switch
                {
                    "simpleType" or "complexType" => _types,
                    "attribute" => _attributes,
                    "attributeGroup" => _attributeGroups,
                    _ => null,
                };
                if (index is null)
                {
                    continue;
                }

                var local = node.FindAttribute("name");
                var name = local is null || local.Length == 0 ? default : unit.Scope.Declare(local);
                var definition = new Definition(node, unit, name);
                _definitions.Add(node, definition);
                Definitions.Add(definition);
                if (name != default)
                {
                    if (!index.TryGetValue(name, out _))
                    {
                        index.Add(name, definition);
                    }
                }
            }
        }
    }

    /// <summary>Finds the definition of a top-level node.</summary>
    /// <param name="node">The syntax.</param>
    /// <returns>The definition, <see langword="null"/> when the node is not a top-level definition of the units.</returns>
    internal Definition? DefinitionOf(SchemaSyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return _definitions.TryGetValue(node, out var definition) ? definition : null;
    }

    /// <summary>Reports a violation of the rule with a code.</summary>
    /// <param name="code">The code.</param>
    /// <param name="location">The location.</param>
    /// <param name="message">The message.</param>
    /// <param name="clause">The clause path, or <see langword="null"/> for the rule id.</param>
    internal void Report(DiagnosticCode code, DocumentLocation location, string message, string? clause = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        Reporter.Report(XsdRuleLookup.Find(code), location, message, clause);
    }

    /// <summary>Gets the simple type that stands in for a component that could not be built.</summary>
    /// <param name="name">The expanded name, <see langword="default"/> when anonymous.</param>
    /// <param name="location">The location of the syntax.</param>
    /// <returns>A restriction-free type of <c>anySimpleType</c>.</returns>
    internal SimpleTypeDefinition Placeholder(XPathQName name, DocumentLocation location) =>
        SimpleTypeDefinition.FromDatatype(name, location, _anySimpleType.Descriptor!);

    /// <summary>Gets the simple type that an attribute without a type has.</summary>
    /// <returns><c>anySimpleType</c>.</returns>
    internal SimpleTypeDefinition AnySimpleType() => AsSimple(_anySimpleType)!;

    /// <summary>Views a type component as a simple type definition.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The definition; one wrapper per built-in type. <see langword="null"/> for a complex type.</returns>
    internal SimpleTypeDefinition? AsSimple(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        switch (type)
        {
            case SimpleTypeDefinition simple:
                return simple;
            case XsdBuiltInType { Kind: XsdComponentKind.SimpleType, Descriptor: { } descriptor } builtIn:
                if (!_wrappers.TryGetValue(builtIn, out var wrapper))
                {
                    wrapper = SimpleTypeDefinition.FromDatatype(builtIn.Name, builtIn.Location, descriptor);
                    _wrappers.Add(builtIn, wrapper);
                }

                return wrapper;
            default:
                return null;
        }
    }

    /// <summary>Gets the facets that a type fixes.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The set, empty for a type that was built without a restriction step.</returns>
    internal FacetKindSet FixedOf(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _fixed.TryGetValue(type, out var set) ? set : default;
    }

    /// <summary>Records the facets that a type fixes.</summary>
    /// <param name="type">The type.</param>
    /// <param name="facets">The facets.</param>
    internal void SetFixed(IXsdComponent type, FacetKindSet facets)
    {
        ArgumentNullException.ThrowIfNull(type);

        _fixed[type] = facets;
    }

    /// <summary>Records whether a type that a restriction step built carries an enumeration, which the facet objects do not model for NOTATION.</summary>
    /// <param name="type">The type that the step built.</param>
    /// <param name="baseType">The base type of the step.</param>
    /// <param name="step">The facets of the step.</param>
    internal void NoteEnumeration(IXsdComponent type, IXsdComponent baseType, IReadOnlyList<FacetDeclaration> step)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(step);

        var enumerated = _enumeratedTypes.Contains(baseType);
        for (var i = 0; i < step.Count && !enumerated; i++)
        {
            enumerated = step[i].Kind == FacetKind.Enumeration;
        }

        if (enumerated)
        {
            _ = _enumeratedTypes.Add(type);
        }
    }

    /// <summary>Gets the enumeration values of the types derived from NOTATION, resolved to names, in document order.</summary>
    internal IReadOnlyList<(XPathQName Name, DocumentLocation Location)> NotationValues => _notationValues;

    /// <summary>Records the resolved name of an enumeration value of a type derived from NOTATION, to check it against the declarations once they are built.</summary>
    /// <param name="name">The expanded name of the value.</param>
    /// <param name="location">The location of the <c>enumeration</c> element.</param>
    internal void NoteNotationValue(XPathQName name, DocumentLocation location) =>
        _notationValues.Add((name, location));

    /// <summary>Gets a value indicating whether a type that was built by restriction carries an enumeration.</summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> when the type or one of its bases states an enumeration.</returns>
    internal bool HasEnumeration(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _enumeratedTypes.Contains(type);
    }

    /// <summary>Resolves a type reference of a <c>type</c>, <see langword="base"/>, <c>itemType</c>, or <c>memberTypes</c> value.</summary>
    /// <param name="context">The element that carries the value, whose namespace declarations apply.</param>
    /// <param name="unit">The unit that holds the element.</param>
    /// <param name="lexical">The QName.</param>
    /// <param name="location">The location of the reference.</param>
    /// <returns>The type, <see langword="null"/> after the reference was reported as unresolved.</returns>
    internal IXsdComponent? ResolveType(
        SchemaSyntaxNode context,
        UnitScope unit,
        string lexical,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(lexical);

        if (!unit.Scope.TryResolve(context, lexical, out var name))
        {
            Report(
                XsdRuleCodes.SrcResolve,
                location,
                "The type '" + lexical + "' is not a QName with a declared prefix."
            );
            return null;
        }

        if (_types.TryGetValue(name, out var definition))
        {
            return EnsureType(definition);
        }

        if (Registry.TryGet(XsdSymbolSpace.Types, name, out var builtIn))
        {
            return builtIn;
        }

        Report(XsdRuleCodes.SrcResolve, location, "No type named '" + name + "' is declared.");
        return null;
    }

    /// <summary>Resolves the <see langword="ref"/> of an attribute to a global attribute declaration.</summary>
    /// <param name="context">The <c>attribute</c> element.</param>
    /// <param name="unit">The unit that holds it.</param>
    /// <param name="lexical">The QName.</param>
    /// <returns>The declaration, <see langword="null"/> after the reference was reported as unresolved.</returns>
    internal AttributeDeclaration? ResolveAttribute(SchemaSyntaxNode context, UnitScope unit, string lexical)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(lexical);

        if (!unit.Scope.TryResolve(context, lexical, out var name))
        {
            Report(
                XsdRuleCodes.SrcResolve,
                context.Location,
                "The attribute '" + lexical + "' is not a QName with a declared prefix."
            );
            return null;
        }

        if (_attributes.TryGetValue(name, out var definition))
        {
            return Attributes.Ensure(definition);
        }

        Report(XsdRuleCodes.SrcResolve, context.Location, "No attribute named '" + name + "' is declared.");
        return null;
    }

    /// <summary>Resolves the <see langword="ref"/> of an attribute group reference to its definition, built and resolved.</summary>
    /// <param name="context">The <c>attributeGroup</c> element.</param>
    /// <param name="unit">The unit that holds it.</param>
    /// <returns>The definition, <see langword="null"/> after the reference was reported as unresolved.</returns>
    internal AttributeGroupDefinition? ResolveAttributeGroup(SchemaSyntaxNode context, UnitScope unit)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(unit);

        var lexical = context.FindAttribute("ref");
        if (lexical is null)
        {
            Report(
                XsdRuleCodes.SrcAttributeGroup,
                context.Location,
                "An attribute group reference must have a ref attribute.",
                "src-attribute_group.1"
            );
            return null;
        }

        if (!unit.Scope.TryResolve(context, lexical, out var name))
        {
            Report(
                XsdRuleCodes.SrcResolve,
                context.Location,
                "The attribute group '" + lexical + "' is not a QName with a declared prefix."
            );
            return null;
        }

        if (_attributeGroups.TryGetValue(name, out var definition))
        {
            return AttributeGroups.Ensure(definition);
        }

        Report(XsdRuleCodes.SrcResolve, context.Location, "No attribute group named '" + name + "' is declared.");
        return null;
    }

    /// <summary>Resolves the attribute group that the <c>defaultAttributes</c> attribute of a unit names, once per unit.</summary>
    /// <param name="unit">The unit whose <c>schema</c> element is read.</param>
    /// <returns>The group, <see langword="null"/> when the schema names none or names one that is not declared (reported once).</returns>
    internal AttributeGroupDefinition? ResolveDefaultAttributeGroup(UnitScope unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var root = unit.Root;
        if (_defaultGroups.TryGetValue(root, out var cached))
        {
            return cached;
        }

        AttributeGroupDefinition? group = null;
        if (root.FindAttribute("defaultAttributes") is { } lexical)
        {
            if (
                unit.Scope.TryResolve(root, lexical, out var name)
                && _attributeGroups.TryGetValue(name, out var definition)
            )
            {
                group = AttributeGroups.Ensure(definition);
            }
            else
            {
                Report(
                    XsdRuleCodes.SrcResolve,
                    root.Location,
                    "The default attribute group '" + lexical + "' is not declared."
                );
            }
        }

        _defaultGroups[root] = group;
        return group;
    }

    /// <summary>Gets the name a reference to a top-level attribute group has, without building anything.</summary>
    /// <param name="context">The <c>attributeGroup</c> element.</param>
    /// <param name="unit">The unit that holds it.</param>
    /// <param name="name">The expanded name.</param>
    /// <returns><see langword="true"/> when the reference is a QName with a declared prefix.</returns>
    internal static bool TryNameOf(SchemaSyntaxNode context, UnitScope unit, out XPathQName name)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(unit);

        name = default;
        var lexical = context.FindAttribute("ref");
        return lexical is not null && unit.Scope.TryResolve(context, lexical, out name);
    }

    /// <summary>Builds a global type and everything it needs, in dependency order.</summary>
    /// <param name="definition">The definition of a <c>simpleType</c> or <c>complexType</c>.</param>
    /// <returns>The type, a placeholder when the definition is part of a cycle that is being built.</returns>
    internal IXsdComponent EnsureType(Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Component is not null)
        {
            return definition.Component;
        }

        if (definition.IsBuilding)
        {
            ReportCycle(definition);
            return PlaceholderOf(definition);
        }

        var path = new List<Frame> { Enter(definition) };
        while (path.Count > 0)
        {
            var top = path[path.Count - 1];
            if (top.Next < top.Dependencies.Count)
            {
                var dependency = top.Dependencies[top.Next++];
                if (dependency.Component is not null)
                {
                    continue;
                }

                if (dependency.IsBuilding)
                {
                    ReportCycle(dependency);
                }
                else
                {
                    path.Add(Enter(dependency));
                }

                continue;
            }

            path.RemoveAt(path.Count - 1);
            var built = IsSimple(top.Definition)
                ? SimpleTypes.Build(top.Definition)
                : (IXsdComponent)ComplexTypes.Build(top.Definition);
            top.Definition.Component = built;
            top.Definition.IsBuilding = false;
        }

        return definition.Component!;
    }

    private Frame Enter(Definition definition)
    {
        definition.IsBuilding = true;
        var dependencies = new List<Definition>();
        if (IsSimple(definition))
        {
            CollectSimple(definition.Node, definition.Unit, dependencies);
        }
        else
        {
            CollectComplex(definition.Node, definition.Unit, dependencies);
        }

        return new Frame(definition, dependencies);
    }

    private static bool IsSimple(Definition definition) => SyntaxValues.IsSchemaElement(definition.Node, "simpleType");

    private IXsdComponent PlaceholderOf(Definition definition) =>
        IsSimple(definition) ? Placeholder(definition.Name, definition.Node.Location) : ComplexTypeDefinition.AnyType;

    private void ReportCycle(Definition definition)
    {
        if (definition.IsCycleReported)
        {
            return;
        }

        definition.IsCycleReported = true;
        if (IsSimple(definition))
        {
            Report(
                XsdRuleCodes.StPropsCorrect,
                definition.Node.Location,
                "The definition of '"
                    + definition.Name
                    + "' is circular: it refers to itself directly or through other definitions.",
                "st-props-correct.2"
            );
        }
        else
        {
            Report(
                XsdRuleCodes.CtPropsCorrect,
                definition.Node.Location,
                "The definition of '"
                    + definition.Name
                    + "' is circular: it refers to itself directly or through other definitions.",
                "ct-props-correct.3"
            );
        }
    }

    // The types a simple type definition needs before it can be built: the base, item, and member types of its syntax.
    private void CollectSimple(SchemaSyntaxNode node, UnitScope unit, List<Definition> dependencies)
    {
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChildNode(i);
            if (SyntaxValues.IsSchemaElement(child, "restriction"))
            {
                Add(child, unit, child.FindAttribute("base"), dependencies);
            }
            else if (SyntaxValues.IsSchemaElement(child, "list"))
            {
                Add(child, unit, child.FindAttribute("itemType"), dependencies);
            }
            else if (SyntaxValues.IsSchemaElement(child, "union"))
            {
                var members = child.FindAttribute("memberTypes");
                if (members is not null)
                {
                    foreach (var member in SyntaxValues.Split(members))
                    {
                        Add(child, unit, member, dependencies);
                    }
                }
            }

            CollectSimple(child, unit, dependencies);
        }
    }

    // The type a complex type definition needs before it can be built: the base type of its content.
    private void CollectComplex(SchemaSyntaxNode node, UnitScope unit, List<Definition> dependencies)
    {
        for (var i = 0; i < node.ChildCount; i++)
        {
            var content = node.GetChildNode(i);
            if (
                !SyntaxValues.IsSchemaElement(content, "complexContent")
                && !SyntaxValues.IsSchemaElement(content, "simpleContent")
            )
            {
                continue;
            }

            for (var j = 0; j < content.ChildCount; j++)
            {
                var derivation = content.GetChildNode(j);
                if (
                    SyntaxValues.IsSchemaElement(derivation, "restriction")
                    || SyntaxValues.IsSchemaElement(derivation, "extension")
                )
                {
                    Add(derivation, unit, derivation.FindAttribute("base"), dependencies);
                }
            }
        }
    }

    private void Add(SchemaSyntaxNode context, UnitScope unit, string? lexical, List<Definition> dependencies)
    {
        if (
            lexical is not null
            && unit.Scope.TryResolve(context, lexical, out var name)
            && _types.TryGetValue(name, out var definition)
        )
        {
            dependencies.Add(definition);
        }
    }

    private sealed class Frame
    {
        internal Frame(Definition definition, List<Definition> dependencies)
        {
            Definition = definition;
            Dependencies = dependencies;
        }

        internal Definition Definition { get; }

        internal List<Definition> Dependencies { get; }

        internal int Next { get; set; }
    }
}
