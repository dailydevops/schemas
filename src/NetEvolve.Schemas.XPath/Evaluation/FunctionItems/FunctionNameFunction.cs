namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:function-name</c> (Functions and Operators 3.0, 16.2.1): the name of a function item; the empty sequence for an anonymous function.</summary>
internal sealed class FunctionNameFunction : FunctionItemFunction
{
    /// <summary>Initializes a new instance of the <see cref="FunctionNameFunction"/> class.</summary>
    public FunctionNameFunction()
        : base("function-name", [OneItem]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (!invocation.Arguments[0][0].TryGetFunction(out var function))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        if (function!.Name is { } name)
        {
            var qname = new XsdQName(name.NamespaceName.Length == 0 ? null : name.NamespaceName, name.LocalName);
            invocation.Result = XdmSequence.Create(XdmItem.FromQName(new XdmQName(qname, null)));
        }

        return XPathEvaluationStatus.Ok;
    }
}
