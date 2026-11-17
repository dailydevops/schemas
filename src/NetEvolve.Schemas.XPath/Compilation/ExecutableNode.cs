namespace NetEvolve.Schemas.XPath.Compilation;

using NetEvolve.Schemas.XPath.Context;
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
}
