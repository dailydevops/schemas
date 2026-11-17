namespace NetEvolve.Schemas.XPath.Evaluation.Ordering;

using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A pull source of nodes; implement it on a struct and pass it generically so that draining it allocates nothing.</summary>
internal interface IXPathNodeSource
{
    /// <summary>Gets the next node.</summary>
    /// <param name="node">The node.</param>
    /// <returns><see langword="true"/> when there is one.</returns>
    bool TryGetNext(out XPathNode node);
}
