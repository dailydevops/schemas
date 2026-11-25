namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// One step of a path expression (XPath 2.0, 3.2). A step delivers the raw results for its context items: an axis step in the order its
/// axis delivers, so that positions count right. The enclosing <see cref="PathNode"/> brings the combined results of a step into
/// document order, and only when the static ordering flag says it is needed.
/// </summary>
internal abstract class PathStep
{
    /// <summary>Gets a value indicating whether the step can stop after a number of results: it delivers nodes in the order of its axis, with nothing left to filter by position.</summary>
    public virtual bool CanLimit => false;

    /// <summary>Evaluates the step for the context item of the evaluation, the first step of a relative path.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="results">The list that receives the results.</param>
    /// <exception cref="XPathException">The context item is absent (XPDY0002), or an axis step finds an atomic value (XPTY0020).</exception>
    public abstract void EvaluateFromContext(XPathEvaluationContext context, ref XdmItemList results);

    /// <summary>Evaluates the step for each item of the previous step, with that item, its position, and the count as the focus.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="inputs">The result of the previous step; nodes in document order without duplicates.</param>
    /// <param name="results">The list that receives the results of all items, one item after the other.</param>
    /// <exception cref="XPathException">An axis step finds an atomic value (XPTY0020).</exception>
    public abstract void EvaluateOver(
        XPathEvaluationContext context,
        ReadOnlySpan<XdmItem> inputs,
        ref XdmItemList results
    );

    /// <summary>Evaluates the step for the context item of the evaluation and stops once a number of results exist.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="results">The list that receives the results.</param>
    /// <param name="limit">The number of results that is enough, as <see cref="CanLimit"/> allows.</param>
    public virtual void EvaluateFromContext(XPathEvaluationContext context, ref XdmItemList results, int limit) =>
        EvaluateFromContext(context, ref results);

    /// <summary>Evaluates the step for each item of the previous step and stops once a number of results exist.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="inputs">The result of the previous step.</param>
    /// <param name="results">The list that receives the results of the items, one item after the other.</param>
    /// <param name="limit">The number of results that is enough, as <see cref="CanLimit"/> allows.</param>
    public virtual void EvaluateOver(
        XPathEvaluationContext context,
        ReadOnlySpan<XdmItem> inputs,
        ref XdmItemList results,
        int limit
    ) => EvaluateOver(context, inputs, ref results);

    /// <summary>Gets what is known about the combined results of this step, given what is known about its inputs.</summary>
    /// <param name="input">The properties of the sequence of inputs.</param>
    /// <returns>The properties of the combined results, before any sorting.</returns>
    public abstract XPathOrderProperties Compose(XPathOrderProperties input);
}
