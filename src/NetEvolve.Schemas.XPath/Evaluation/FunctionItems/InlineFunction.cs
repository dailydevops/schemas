namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The function item of an inline function expression (XPath 3.0, 3.1.7): the body, the parameter names, and the closure, the values that
/// the free variables of the body had when the function was created. The body runs without a focus.
/// </summary>
/// <remarks>
/// The call binds the closure and the parameters in the variable table of the context and restores what they shadowed afterwards,
/// the way the binding of a <see langword="for"/> expression does, so the variables of the caller are not visible to the body.
/// </remarks>
internal sealed class InlineFunction : XdmFunction
{
    private readonly XPathQName[] _parameters;
    private readonly ExecutableNode _body;
    private readonly XPathQName[] _closureNames;
    private readonly XdmSequence[] _closureValues;

    /// <summary>Initializes a new instance of the <see cref="InlineFunction"/> class.</summary>
    /// <param name="signature">The signature; its arity is the number of parameters.</param>
    /// <param name="parameters">The expanded names of the parameters.</param>
    /// <param name="body">The body.</param>
    /// <param name="closureNames">The names of the captured variables.</param>
    /// <param name="closureValues">The values of the captured variables, in the order of the names.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public InlineFunction(
        FunctionSignature signature,
        XPathQName[] parameters,
        ExecutableNode body,
        XPathQName[] closureNames,
        XdmSequence[] closureValues
    )
        : base(null, signature)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(closureNames);
        ArgumentNullException.ThrowIfNull(closureValues);

        _parameters = parameters;
        _body = body;
        _closureNames = closureNames;
        _closureValues = closureValues;
    }

    /// <inheritdoc />
    public override XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The function conversion comes first, so that a bad argument leaves the variables untouched.
        var converted = new XdmSequence[_parameters.Length];
        for (var i = 0; i < converted.Length; i++)
        {
            converted[i] = FunctionConversion.Convert(
                arguments[i],
                Signature.Parameter(i).Conversion,
                context.Annotations
            );
        }

        var variables = context.Variables;
        var count = _closureNames.Length + _parameters.Length;
        var names = new XPathQName[count];
        var previous = new XdmSequence[count];
        var shadowed = new bool[count];
        var focus = context.Focus;
        for (var i = 0; i < count; i++)
        {
            names[i] = i < _closureNames.Length ? _closureNames[i] : _parameters[i - _closureNames.Length];
            shadowed[i] = variables.TryGet(names[i], out previous[i]);
        }

        try
        {
            for (var i = 0; i < _closureNames.Length; i++)
            {
                variables.Set(_closureNames[i], _closureValues[i]);
            }

            for (var i = 0; i < _parameters.Length; i++)
            {
                variables.Set(_parameters[i], converted[i]);
            }

            context.RestoreFocus(default);
            return FunctionConversion.Convert(
                _body.Evaluate(context),
                Signature.Result.Conversion,
                context.Annotations
            );
        }
        finally
        {
            context.RestoreFocus(focus);
            for (var i = count - 1; i >= 0; i--)
            {
                if (shadowed[i])
                {
                    variables.Set(names[i], previous[i]);
                }
                else
                {
                    variables.Remove(names[i]);
                }
            }
        }
    }
}
