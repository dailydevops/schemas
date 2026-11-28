namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of a reference to a variable that the host keeps in a slot: no name is hashed at the reference.</summary>
internal sealed class HostVariableNode : ExecutableNode
{
    private readonly int _slot;

    /// <summary>Initializes a new instance of the <see cref="HostVariableNode"/> class.</summary>
    /// <param name="slot">The host slot.</param>
    public HostVariableNode(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);

        _slot = slot;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The slot has no value (XPDY0002).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Variables.GetSlot(_slot);
    }
}
