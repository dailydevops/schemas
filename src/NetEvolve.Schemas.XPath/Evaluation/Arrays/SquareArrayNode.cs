namespace NetEvolve.Schemas.XPath.Evaluation.Arrays;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Arrays;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of a square array constructor (XPath 3.1, 3.11.2.1): each member expression gives one member, however many items it yields.</summary>
internal sealed class SquareArrayNode : ExecutableNode
{
    private readonly ExecutableNode[] _members;

    /// <summary>Initializes a new instance of the <see cref="SquareArrayNode"/> class.</summary>
    /// <param name="members">The member expressions, in order.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="members"/> is <see langword="null"/>.</exception>
    public SquareArrayNode(ExecutableNode[] members)
    {
        ArgumentNullException.ThrowIfNull(members);

        _members = members;
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var members = new XdmSequence[_members.Length];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = _members[i].Evaluate(context).Materialize();
        }

        return XdmSequence.Create(XdmArray.Create(members).ToItem());
    }
}
