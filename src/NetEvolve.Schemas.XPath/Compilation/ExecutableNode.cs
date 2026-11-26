namespace NetEvolve.Schemas.XPath.Compilation;

using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Base of the executable tree: immutable operator objects that hold no evaluation state, so one tree is safe for
/// concurrent evaluation. Everything an evaluation changes lives in the <see cref="XPathEvaluationContext"/>.
/// </summary>
internal abstract class ExecutableNode
{
    /// <summary>Evaluates the node.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The value.</returns>
    public abstract XdmSequence Evaluate(XPathEvaluationContext context);

    /// <summary>Evaluates the node to the effective boolean value of its value.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The effective boolean value.</returns>
    /// <remarks>The default reduces <see cref="Evaluate"/>; a node that knows its truth without a sequence overrides it and must raise the same errors.</remarks>
    /// <exception cref="XPathException">The evaluation fails, or the value has no effective boolean value (FORG0006).</exception>
    public virtual bool EvaluateBoolean(XPathEvaluationContext context) =>
        XPathAtomization.EffectiveBooleanValue(Evaluate(context));
}
