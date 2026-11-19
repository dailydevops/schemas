namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:true</c> (Functions and Operators 1.0, section 9): the boolean value true. The function ignores the context item.</summary>
internal sealed class TrueFunction : NumericFunction
{
    private static readonly XdmItem True = XdmItem.FromBoolean(true);

    /// <summary>Initializes a new instance of the <see cref="TrueFunction"/> class.</summary>
    public TrueFunction()
        : base("true", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(True);
        return XPathEvaluationStatus.Ok;
    }
}
