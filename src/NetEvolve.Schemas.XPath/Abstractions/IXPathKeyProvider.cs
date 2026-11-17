namespace NetEvolve.Schemas.XPath.Abstractions;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Resolves a key by name and value to nodes; the host supplies it through <see cref="XPathHostSlots.Keys"/>.</summary>
public interface IXPathKeyProvider
{
    /// <summary>Resolves a key.</summary>
    /// <param name="name">The key name.</param>
    /// <param name="value">The key value to look up.</param>
    /// <param name="nodes">The matching nodes in document order.</param>
    /// <returns><see langword="true"/> when the key exists.</returns>
    bool TryGetNodes(XPathQName name, in XdmItem value, out XdmSequence nodes);
}
