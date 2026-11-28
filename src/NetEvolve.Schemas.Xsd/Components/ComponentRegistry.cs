namespace NetEvolve.Schemas.Xsd.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// The mutable registry used while components are built: one dictionary per symbol space, keyed by expanded name.
/// <see cref="Freeze"/> ends the build phase and returns the immutable <see cref="XsdSchemaSet"/>.
/// </summary>
/// <remarks>
/// Only named components of a kind that has a symbol space are registered; anonymous components and kinds such as particles
/// are reached through their owners. A duplicate is reported and the first component stays registered.
/// </remarks>
internal sealed class ComponentRegistry : IComponentRegistry
{
    private readonly Dictionary<XPathQName, IXsdComponent>[] _spaces;
    private readonly List<IXsdComponent> _components = [];
    private readonly XsdIssueReporter _reporter;
    private bool _isFrozen;

    internal ComponentRegistry(XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        _reporter = reporter;
        _spaces = new Dictionary<XPathQName, IXsdComponent>[XsdSymbolSpaces.Count];
        for (var i = 0; i < _spaces.Length; i++)
        {
            _spaces[i] = [];
        }
    }

    /// <inheritdoc />
    public int Count => _components.Count;

    /// <summary>Gets the registered components in registration order.</summary>
    internal IReadOnlyList<IXsdComponent> Components => _components;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The registry is frozen.</exception>
    public void Add(IXsdComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_isFrozen)
        {
            throw new InvalidOperationException("The component registry is frozen.");
        }

        if (!XsdSymbolSpaces.TryOf(component.Kind, out var space) || component.Name.LocalName.Length == 0)
        {
            return;
        }

        var map = _spaces[(int)space];
        if (map.TryGetValue(component.Name, out var first))
        {
            _reporter.Report(
                XsdRuleLookup.SchPropsCorrect,
                component.Location,
                $"The {space} component '{component.Name}' is already declared at {first.Location}.",
                "sch-props-correct.2"
            );
            return;
        }

        map.Add(component.Name, component);
        _components.Add(component);
    }

    /// <inheritdoc />
    public bool TryGet(XsdComponentKind kind, XPathQName name, out IXsdComponent? component)
    {
        if (XsdSymbolSpaces.TryOf(kind, out var space))
        {
            return TryGet(space, name, out component);
        }

        component = null;
        return false;
    }

    /// <summary>Looks a component up in a symbol space.</summary>
    /// <param name="space">The space.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="component">The component when found.</param>
    /// <returns><see langword="true"/> when found.</returns>
    internal bool TryGet(XsdSymbolSpace space, XPathQName name, out IXsdComponent? component)
    {
        var found = _spaces[(int)space].TryGetValue(name, out var value);
        component = value;
        return found;
    }

    /// <summary>Ends the build phase; the registry accepts no further component.</summary>
    /// <param name="version">The XSD version of the set.</param>
    /// <returns>The immutable schema set.</returns>
    internal XsdSchemaSet Freeze(XsdVersion version)
    {
        _isFrozen = true;
        return new XsdSchemaSet(version, _spaces, _components.Count);
    }
}
