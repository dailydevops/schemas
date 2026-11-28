namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a partial function application (XPath 3.0, 3.1.8), static or dynamic: a function item whose fixed arguments were
/// evaluated once, here, and whose arity is the number of placeholders.
/// </summary>
internal sealed class PartialApplicationNode : ExecutableNode
{
    private readonly ExecutableNode _function;
    private readonly ExecutableNode?[] _arguments;
    private readonly int[] _open;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="PartialApplicationNode"/> class.</summary>
    /// <param name="function">The function expression; for a static call, the named reference of the function.</param>
    /// <param name="arguments">The arguments; <see langword="null"/> stands for a placeholder.</param>
    /// <param name="location">The location of the call.</param>
    public PartialApplicationNode(ExecutableNode function, ExecutableNode?[] arguments, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);

        _function = function;
        _arguments = arguments;
        _location = location;

        var open = new System.Collections.Generic.List<int>();
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is null)
            {
                open.Add(i);
            }
        }

        _open = open.ToArray();
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var target = FunctionInvoker.Require(_function.Evaluate(context), _location);
        if (target.Arity != _arguments.Length)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004, _location.Offset, _location.Length);
        }

        var fixedArguments = new XdmSequence[_arguments.Length];
        for (var i = 0; i < fixedArguments.Length; i++)
        {
            if (_arguments[i] is { } argument)
            {
                // The conversion happens once here, so a wrong argument is an error of the application, not of its calls.
                fixedArguments[i] = FunctionConversion.Convert(
                    argument.Evaluate(context),
                    target.Signature.Parameter(i).Conversion,
                    context.Annotations
                );
            }
        }

        var signature = target.Signature.Keep(_open);
        return XdmSequence.Create(XdmItem.FromFunction(new PartialFunction(signature, target, fixedArguments, _open)));
    }
}
