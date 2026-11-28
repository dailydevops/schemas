namespace NetEvolve.Schemas.Xsd.Components;

using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The immutable set of components of a compiled schema. It exposes lookups only and is safe to read from many threads.
/// </summary>
/// <remarks>The dictionaries are handed over by <see cref="ComponentRegistry.Freeze"/>, which never writes to them again.</remarks>
internal sealed class XsdSchemaSet
{
    private readonly Dictionary<XPathQName, IXsdComponent>[] _spaces;
    private readonly HashSet<string> _namespaces;

    internal XsdSchemaSet(XsdVersion version, Dictionary<XPathQName, IXsdComponent>[] spaces, int count)
        : this(version, spaces, count, null, NamespacesOf(spaces)) { }

    private XsdSchemaSet(
        XsdVersion version,
        Dictionary<XPathQName, IXsdComponent>[] spaces,
        int count,
        XsdSchemaSet? parent,
        HashSet<string> namespaces
    )
    {
        Version = version;
        Count = count + (parent?.Count ?? 0);
        _spaces = spaces;
        _namespaces = namespaces;
        Parent = parent;

        // Publishes everything wired before the freeze to the threads that receive the set (ARM64 memory model, design #146 DP-4).
        Interlocked.MemoryBarrier();
    }

    /// <summary>Gets the XSD version the set was compiled for.</summary>
    public XsdVersion Version { get; }

    /// <summary>Gets the number of registered components, those of the parent set included.</summary>
    public int Count { get; }

    /// <summary>Gets the set that is consulted first, <see langword="null"/> for a set without a parent.</summary>
    /// <remarks>A parent makes a set an overlay: the lookups find the components of the parent before the own ones (design #146 DP-22).</remarks>
    public XsdSchemaSet? Parent { get; }

    /// <summary>Returns a set with the components of this one that falls back to a parent set; neither set is changed.</summary>
    /// <param name="parent">The set that the lookups consult first.</param>
    /// <returns>The overlay set.</returns>
    internal XsdSchemaSet WithParent(XsdSchemaSet parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        return new XsdSchemaSet(Version, _spaces, Count, parent, _namespaces);
    }

    /// <summary>Tests whether the set, or its parent, has a component in a namespace.</summary>
    /// <param name="namespaceName">The namespace name, empty for none.</param>
    /// <returns><see langword="true"/> when a registered component has the namespace.</returns>
    /// <remarks>The built-in types make the XML Schema namespace always defined. The answer is computed when the set is built.</remarks>
    internal bool DefinesNamespace(string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);

        return _namespaces.Contains(namespaceName) || (Parent?.DefinesNamespace(namespaceName) ?? false);
    }

    private static HashSet<string> NamespacesOf(Dictionary<XPathQName, IXsdComponent>[] spaces)
    {
        var namespaces = new HashSet<string>(System.StringComparer.Ordinal);
        for (var i = 0; i < spaces.Length; i++)
        {
            foreach (var name in spaces[i].Keys)
            {
                _ = namespaces.Add(name.NamespaceName);
            }
        }

        return namespaces;
    }

    /// <summary>Looks a component up.</summary>
    /// <param name="kind">The kind, which selects the symbol space.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="component">The component when found.</param>
    /// <returns><see langword="true"/> when a component is registered under the name.</returns>
    public bool TryGet(XsdComponentKind kind, XPathQName name, out IXsdComponent? component)
    {
        if (Parent is not null && Parent.TryGet(kind, name, out component))
        {
            return true;
        }

        if (XsdSymbolSpaces.TryOf(kind, out var space))
        {
            var found = _spaces[(int)space].TryGetValue(name, out var value);
            component = value;
            return found;
        }

        component = null;
        return false;
    }
}
