namespace NetEvolve.Schemas.Xsd.Compilation;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Composition;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>One schema unit with what the builders need to read it: its namespace scope, its defaults, and the element builders of that scope.</summary>
internal sealed class UnitScope
{
    internal UnitScope(SchemaUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        Unit = unit;
        Scope = new SchemaNamespaceScope(
            unit.EffectiveNamespace,
            unit.EffectiveNamespace.Length > 0 && unit.Root.FindAttribute("targetNamespace") is null
        );
        Particles = new ParticleBuilder(Scope);
        Elements = new ElementDeclarationBuilder(Scope);
        Groups = new ModelGroupDefinitionBuilder(Scope);
    }

    /// <summary>Gets the unit.</summary>
    public SchemaUnit Unit { get; }

    /// <summary>Gets the namespace the unit contributes to and how it resolves QNames.</summary>
    public SchemaNamespaceScope Scope { get; }

    /// <summary>Gets the <c>schema</c> element of the unit.</summary>
    public SchemaSyntaxNode Root => Unit.Root;

    /// <summary>Gets the effective target namespace, empty for none.</summary>
    public string TargetNamespace => Unit.EffectiveNamespace;

    /// <summary>Gets the builder of model groups and particles.</summary>
    public ParticleBuilder Particles { get; }

    /// <summary>Gets the builder of global element declarations.</summary>
    public ElementDeclarationBuilder Elements { get; }

    /// <summary>Gets the builder of model group definitions.</summary>
    public ModelGroupDefinitionBuilder Groups { get; }
}
