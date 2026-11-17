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

    internal XsdSchemaSet(XsdVersion version, Dictionary<XPathQName, IXsdComponent>[] spaces, int count)
    {
        Version = version;
        Count = count;
        _spaces = spaces;

        // Publishes everything wired before the freeze to the threads that receive the set (ARM64 memory model, design #146 DP-4).
        Interlocked.MemoryBarrier();
    }

    /// <summary>Gets the XSD version the set was compiled for.</summary>
    public XsdVersion Version { get; }

    /// <summary>Gets the number of registered components.</summary>
    public int Count { get; }

    /// <summary>Looks a component up.</summary>
    /// <param name="kind">The kind, which selects the symbol space.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="component">The component when found.</param>
    /// <returns><see langword="true"/> when a component is registered under the name.</returns>
    public bool TryGet(XsdComponentKind kind, XPathQName name, out IXsdComponent? component)
    {
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
