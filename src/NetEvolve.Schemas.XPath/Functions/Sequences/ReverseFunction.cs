namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:reverse</c> (Functions and Operators 1.0, 15.1.4): the items of the argument in reverse order.</summary>
internal sealed class ReverseFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="ReverseFunction"/> class.</summary>
    public ReverseFunction()
        : base("reverse", 1, 1, [Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var source = invocation.Arguments[0];
        var count = source.Count;
        if (count <= 1)
        {
            invocation.Result = source;
            return XPathEvaluationStatus.Ok;
        }

        var result = default(XdmItemList);
        try
        {
            for (var i = count - 1; i >= 0; i--)
            {
                result.Add(source[i]);
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
