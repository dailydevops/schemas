namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:nilled($arg as node()?) as xs:boolean?</c> (Functions and Operators 1.0, 2.2): <see langword="false"/> for an element, the empty sequence
/// for the empty sequence and for the other kinds of node.
/// </summary>
/// <remarks>Limitation: a node carries no schema annotation here, so an element is never nilled (see <c>ItemTypeMatcher</c>).</remarks>
internal class NilledFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="NilledFunction"/> class.</summary>
    public NilledFunction()
        : this(1, [new XPathParameterType(XPathItemType.Node, Occurrence.ZeroOrOne)]) { }

    /// <summary>Initializes a new instance of the <see cref="NilledFunction"/> class with one fixed arity.</summary>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    private protected NilledFunction(int arity, XPathParameterType[] parameters)
        : base("nilled", arity, arity, parameters) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (TryGetNode(ref invocation, 0, out var node) && node.Kind == XdmNodeKind.Element)
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(false));
        }

        return XPathEvaluationStatus.Ok;
    }
}
