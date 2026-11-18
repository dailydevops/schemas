namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:remove</c> (Functions and Operators 1.0, 15.1.3): the target without the item at the position; a position outside the sequence removes nothing.</summary>
internal sealed class RemoveFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="RemoveFunction"/> class.</summary>
    public RemoveFunction()
        : base("remove", 2, 2, [Items, OneInteger]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var target = invocation.Arguments[0];
        var count = target.Count;
        _ = invocation.Arguments[1][0].TryGetInteger(out var position);
        if (position < 1 || position > count)
        {
            invocation.Result = target;
            return XPathEvaluationStatus.Ok;
        }

        var result = default(XdmItemList);
        try
        {
            for (var i = 0; i < count; i++)
            {
                if (i != position - 1)
                {
                    result.Add(target[i]);
                }
            }

            invocation.Result = result.ToSequence();
        }
        finally
        {
            result.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
