namespace NetEvolve.Schemas.Xsd.Contracts;

using NetEvolve.Schemas.XPath.Api;

/// <summary>The registry of one symbol space per component kind, keyed by expanded name.</summary>
internal interface IComponentRegistry
{
    /// <summary>Gets the number of registered components.</summary>
    int Count { get; }

    /// <summary>Registers a component.</summary>
    /// <param name="component">The component.</param>
    void Add(IXsdComponent component);

    /// <summary>Looks a component up.</summary>
    /// <param name="kind">The kind, which selects the symbol space.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="component">The component when found.</param>
    /// <returns><see langword="true"/> when a component is registered under the name.</returns>
    bool TryGet(XsdComponentKind kind, XPathQName name, out IXsdComponent? component);
}
