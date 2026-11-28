namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of a named function reference (XPath 3.0, 3.1.6): a function item that keeps the focus of the reference.</summary>
internal sealed class NamedReferenceNode : ExecutableNode
{
    private readonly XPathFunction _function;
    private readonly FunctionSignature _signature;

    /// <summary>Initializes a new instance of the <see cref="NamedReferenceNode"/> class.</summary>
    /// <param name="function">The function the reference names.</param>
    /// <param name="arity">The arity of the reference.</param>
    public NamedReferenceNode(XPathFunction function, int arity)
    {
        ArgumentNullException.ThrowIfNull(function);

        _function = function;
        _signature = NamedFunction.SignatureOf(function, arity);
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return XdmSequence.Create(XdmItem.FromFunction(new NamedFunction(_function, _signature, context.Focus)));
    }
}
