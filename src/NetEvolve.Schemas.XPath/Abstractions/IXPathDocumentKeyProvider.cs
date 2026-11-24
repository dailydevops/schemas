namespace NetEvolve.Schemas.XPath.Abstractions;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A key provider that knows the document of a lookup (XSLT 2.0, 16.3): the key index is per document, so the lookup names the node
/// from which the document is reached. The function <c>key</c> needs this contract; the host supplies it through
/// <see cref="XPathHostSlots.Keys"/>.
/// </summary>
public interface IXPathDocumentKeyProvider : IXPathKeyProvider
{
    /// <summary>Looks up the nodes of a key: <c>key(name, values)</c> with the context node, and <c>key(name, values, top)</c> with the top node.</summary>
    /// <param name="name">The expanded key name.</param>
    /// <param name="values">The atomized key values; the result is the union of the nodes of every value.</param>
    /// <param name="node">The context node, whose document is searched, or the top node of the three-argument form.</param>
    /// <param name="subtree"><see langword="true"/> to keep only the nodes that are <paramref name="node"/> or its descendants (the three-argument form).</param>
    /// <param name="nodes">The matching nodes in document order, without duplicates.</param>
    /// <returns><see langword="true"/> when the key exists; <see langword="false"/> when the provider declares no key of that name.</returns>
    /// <exception cref="XPathException">The tree of the node has no document node (XTDE1270), or the lookup failed otherwise.</exception>
    bool TryLookup(in XPathQName name, in XdmSequence values, in XPathNode node, bool subtree, out XdmSequence nodes);
}
