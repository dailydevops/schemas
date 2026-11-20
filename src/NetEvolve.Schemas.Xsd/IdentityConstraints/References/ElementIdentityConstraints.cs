namespace NetEvolve.Schemas.Xsd.IdentityConstraints.References;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The identity constraints of one element declaration in document order: its own definitions and the constraints it reuses
/// through <see langword="ref"/>, the latter as the same instances the registry holds.
/// </summary>
/// <remarks>
/// Two phases: <see cref="Create"/> runs for every element of the schema set, so every named constraint is registered;
/// <see cref="Resolve"/> then binds the references, so a reference may name a constraint of a later element or of another
/// document. Limitation: the <c>refer</c> of a <c>keyref</c> is resolved by <see cref="IdentityConstraintDefinition.ResolveRefer"/>
/// of the pipeline that wires the definitions (XSD-78), not here, so a shared constraint is never resolved twice.
/// </remarks>
internal sealed class ElementIdentityConstraints
{
    private readonly List<IdentityConstraintDefinition?> _constraints = [];
    private readonly List<IdentityConstraintReference?> _references = [];

    private ElementIdentityConstraints() { }

    /// <summary>Gets the constraints in document order; a reference that did not resolve is left out.</summary>
    public IReadOnlyList<IdentityConstraintDefinition> Constraints { get; private set; } = [];

    /// <summary>Reads the <c>unique</c>, <c>key</c>, and <c>keyref</c> children of an element declaration and registers the named ones.</summary>
    /// <param name="element">The <c>element</c> declaration.</param>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    /// <param name="registry">The registry that receives the named constraints.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The constraints with their references still unresolved.</returns>
    internal static ElementIdentityConstraints Create(
        SchemaSyntaxNode element,
        SchemaNamespaceScope scope,
        IComponentRegistry registry,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(reporter);

        var result = new ElementIdentityConstraints();
        for (var i = 0; i < element.ChildCount; i++)
        {
            var child = element.GetChildNode(i);
            if (child.Name.LocalName is not ("unique" or "key" or "keyref"))
            {
                continue;
            }

            if (child.FindAttribute("ref") is null)
            {
                var definition = IdentityConstraintDefinition.Create(child, scope, registry, reporter);
                if (definition is not null)
                {
                    result._constraints.Add(definition);
                    result._references.Add(null);
                }
            }
            else
            {
                var reference = IdentityConstraintReference.Create(child, scope, version, reporter);
                if (reference is not null)
                {
                    result._constraints.Add(null);
                    result._references.Add(reference);
                }
            }
        }

        return result;
    }

    /// <summary>Binds the references and publishes <see cref="Constraints"/>.</summary>
    /// <param name="registry">The registry holding the identity constraints of every schema document of the set.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Resolve(IComponentRegistry registry, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(reporter);

        var resolved = new List<IdentityConstraintDefinition>(_constraints.Count);
        for (var i = 0; i < _constraints.Count; i++)
        {
            var own = _constraints[i];
            if (own is not null)
            {
                resolved.Add(own);
            }
            else if (_references[i]!.Resolve(registry, reporter))
            {
                resolved.Add(_references[i]!.Target!);
            }
        }

        Constraints = resolved;
    }
}
