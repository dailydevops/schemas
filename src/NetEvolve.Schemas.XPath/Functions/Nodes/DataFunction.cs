namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// <c>fn:data</c>: the atomized argument. The parameter is declared <c>xs:anyAtomicType*</c>, so the function conversion rules
/// atomize it (the typed value of each node) before the call, and the function returns the converted argument.
/// </summary>
/// <remarks>Limitation: the zero-argument form of XPath 3.0 is not registered (#92).</remarks>
internal sealed class DataFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="DataFunction"/> class.</summary>
    public DataFunction()
        : base("data", 1, 1, [new XPathParameterType(XPathItemType.AnyAtomic, Occurrence.ZeroOrMore)]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = invocation.Arguments[0];
        return XPathEvaluationStatus.Ok;
    }
}
