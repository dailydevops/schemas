namespace NetEvolve.Schemas.XPath.Context;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The variables bound for an evaluation. Not thread-safe; one instance per evaluation context.</summary>
public sealed class XPathVariables
{
    private readonly Dictionary<XPathQName, XdmSequence> _values = [];

    internal XPathVariables() { }

    /// <summary>Binds a variable to one item.</summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value.</param>
    public void Set(XPathQName name, in XdmItem value) => _values[name] = XdmSequence.Create(value);

    /// <summary>Binds a variable to a sequence.</summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value.</param>
    public void Set(XPathQName name, XdmSequence value) => _values[name] = value;

    internal bool TryGet(XPathQName name, out XdmSequence value) => _values.TryGetValue(name, out value);

    internal void Clear() => _values.Clear();
}
