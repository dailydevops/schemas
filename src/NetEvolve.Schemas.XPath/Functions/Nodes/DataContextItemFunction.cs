namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:data</c> without an argument (XPath 3.0): the atomized context item. An absent context item is XPDY0002, and an item
/// without a typed value, a function item, raises the error of atomization.
/// </summary>
internal sealed class DataContextItemFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="DataContextItemFunction"/> class.</summary>
    public DataContextItemFunction()
        : base("data", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var item = invocation.Context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        invocation.Result = XPathAtomization.Atomize(XdmSequence.Create(item), invocation.Context.Annotations);
        return XPathEvaluationStatus.Ok;
    }
}
