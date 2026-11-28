namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of a dynamic function call (XPath 3.0, 3.2.2): the function expression is evaluated, then the arguments.</summary>
internal sealed class DynamicCallNode : ExecutableNode
{
    private readonly ExecutableNode _function;
    private readonly ExecutableNode[] _arguments;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="DynamicCallNode"/> class.</summary>
    /// <param name="function">The function expression.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="location">The location of the call.</param>
    public DynamicCallNode(ExecutableNode function, ExecutableNode[] arguments, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);

        _function = function;
        _arguments = arguments;
        _location = location;
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var function = FunctionInvoker.Require(_function.Evaluate(context), _location);
        var arguments = new XdmSequence[_arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            arguments[i] = _arguments[i].Evaluate(context);
        }

        return FunctionInvoker.Call(function, context, arguments, _location);
    }
}
