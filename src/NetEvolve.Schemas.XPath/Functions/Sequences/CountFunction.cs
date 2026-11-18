namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:count</c>: the number of items of the argument. A lazy argument that nobody has read is counted as it is read, without keeping the items.</summary>
/// <remarks>Limitation: such a lazy source is enumerated again when the sequence is read afterwards, because the count keeps nothing.</remarks>
internal sealed class CountFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="CountFunction"/> class.</summary>
    public CountFunction()
        : base("count", 1, 1, [Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(invocation.Arguments[0].CountItems()));
        return XPathEvaluationStatus.Ok;
    }
}
