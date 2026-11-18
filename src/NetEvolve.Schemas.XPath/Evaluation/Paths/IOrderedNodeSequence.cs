namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

/// <summary>An executable node whose result, when it holds nodes, is in document order without duplicates; the set operators then skip the sort of that operand.</summary>
internal interface IOrderedNodeSequence
{
    /// <summary>Gets a value indicating whether the nodes of the result are in document order without duplicates.</summary>
    bool IsDocumentOrdered { get; }
}
