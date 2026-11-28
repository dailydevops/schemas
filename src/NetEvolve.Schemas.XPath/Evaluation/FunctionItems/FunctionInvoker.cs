namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The shared steps of every call of a function item: reading the function from a value, checking the arity, and bounding the depth of
/// nested calls. A function item can call itself, so the depth of the calls is bounded by the recursion depth limit of the context.
/// </summary>
internal static class FunctionInvoker
{
    /// <summary>Reads the function from the value of the function expression of a dynamic call.</summary>
    /// <param name="value">The value.</param>
    /// <param name="location">The location reported for the error.</param>
    /// <returns>The function.</returns>
    /// <exception cref="XPathException">The value is not exactly one function item (XPTY0004).</exception>
    public static XdmFunction Require(in XdmSequence value, in SourceSpan location)
    {
        if (value.Count == 1 && value[0].TryGetFunction(out var function))
        {
            return function!;
        }

        throw XPathErrors.Exception(XPathErrorCodes.XPTY0004, location.Offset, location.Length);
    }

    /// <summary>Calls a function item.</summary>
    /// <param name="function">The function.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="location">The location reported for an error of the call itself.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The number of arguments is not the arity (XPTY0004), the calls are nested deeper than the recursion depth limit, or the function raises an error.</exception>
    public static XdmSequence Call(
        XdmFunction function,
        XPathEvaluationContext context,
        ReadOnlySpan<XdmSequence> arguments,
        in SourceSpan location
    )
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(context);

        if (arguments.Length != function.Arity)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004, location.Offset, location.Length);
        }

        var depth = ++context.CallDepth;
        try
        {
            context.Limits.ThrowIfTooDeep(depth, location);
            return function.Invoke(context, arguments);
        }
        finally
        {
            context.CallDepth--;
        }
    }
}
