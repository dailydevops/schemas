namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:tail</c> (Functions and Operators 3.1, 14.1.2): every item of the sequence but the first; the empty sequence for a sequence of fewer than two items.</summary>
internal sealed class TailFunction : Delta3Function
{
    /// <summary>Initializes a new instance of the <see cref="TailFunction"/> class.</summary>
    public TailFunction()
        : base("tail", 1, 1, [Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.Count > 1)
        {
            var rest = new XdmItem[argument.Count - 1];
            for (var i = 0; i < rest.Length; i++)
            {
                rest[i] = argument[i + 1];
            }

            invocation.Result = XdmSequence.Create(rest);
        }

        return XPathEvaluationStatus.Ok;
    }
}
