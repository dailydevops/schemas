namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:false</c> (Functions and Operators 1.0, section 9): the boolean value false. The function ignores the context item.</summary>
internal sealed class FalseFunction : NumericFunction
{
    private static readonly XdmItem False = XdmItem.FromBoolean(false);

    /// <summary>Initializes a new instance of the <see cref="FalseFunction"/> class.</summary>
    public FalseFunction()
        : base("false", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(False);
        return XPathEvaluationStatus.Ok;
    }
}
