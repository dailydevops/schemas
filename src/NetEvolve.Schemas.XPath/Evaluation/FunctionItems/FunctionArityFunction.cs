namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:function-arity</c> (Functions and Operators 3.0, 16.2.2): the number of parameters of a function item.</summary>
internal sealed class FunctionArityFunction : FunctionItemFunction
{
    /// <summary>Initializes a new instance of the <see cref="FunctionArityFunction"/> class.</summary>
    public FunctionArityFunction()
        : base("function-arity", [OneItem]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (!invocation.Arguments[0][0].TryGetFunction(out var function))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(function!.Arity));
        return XPathEvaluationStatus.Ok;
    }
}
