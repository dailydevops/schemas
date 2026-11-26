namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using NetEvolve.Schemas.XPath.Context;

/// <summary>An executable node that can append its result to a pooled list, so that a caller that only reads the items does not need the result copied into a sequence array.</summary>
internal interface IItemListProducer
{
    /// <summary>Evaluates the node and appends the items of the result.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="results">The list that receives the items.</param>
    void EvaluateInto(XPathEvaluationContext context, ref XdmItemList results);
}
