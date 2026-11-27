namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The result of function coercion (XPath 3.0, 3.1.5.2): a function item of the right arity whose type is not a subtype of an expected
/// function test is wrapped, so that the arguments are converted to the parameter types of the test, and the result to its result type.
/// </summary>
internal sealed class CoercedFunction : XdmFunction
{
    private readonly XdmFunction _inner;

    /// <summary>Initializes a new instance of the <see cref="CoercedFunction"/> class.</summary>
    /// <param name="inner">The function that is wrapped.</param>
    /// <param name="expected">The signature of the function test; it becomes the signature of the item.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> or <paramref name="expected"/> is <see langword="null"/>.</exception>
    public CoercedFunction(XdmFunction inner, FunctionSignature expected)
        : base(inner?.Name, expected)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <inheritdoc />
    public override XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);

        var converted = new XdmSequence[arguments.Length];
        for (var i = 0; i < converted.Length; i++)
        {
            converted[i] = FunctionConversion.Convert(
                arguments[i],
                Signature.Parameter(i).Conversion,
                context.Annotations
            );
        }

        var result = FunctionInvoker.Call(_inner, context, converted, default(SourceSpan));
        return FunctionConversion.Convert(result, Signature.Result.Conversion, context.Annotations);
    }
}
