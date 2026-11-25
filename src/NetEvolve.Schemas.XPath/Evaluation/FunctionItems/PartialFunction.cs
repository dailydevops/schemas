namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The function item of a partial function application (XPath 3.0, 3.1.8): a function with some arguments fixed. The fixed arguments were
/// evaluated and converted once, when the item was created; the item is anonymous and its arity is the number of placeholders.
/// </summary>
internal sealed class PartialFunction : XdmFunction
{
    private readonly XdmFunction _target;
    private readonly XdmSequence[] _fixed;
    private readonly int[] _open;

    /// <summary>Initializes a new instance of the <see cref="PartialFunction"/> class.</summary>
    /// <param name="signature">The signature of the item: the parameters of the target at the open positions, and its result.</param>
    /// <param name="target">The function that the application calls.</param>
    /// <param name="fixedArguments">One value per parameter of the target; the values at the open positions are ignored.</param>
    /// <param name="open">The zero-based positions of the placeholders, in order.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public PartialFunction(FunctionSignature signature, XdmFunction target, XdmSequence[] fixedArguments, int[] open)
        : base(null, signature)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(fixedArguments);
        ArgumentNullException.ThrowIfNull(open);

        _target = target;
        _fixed = fixedArguments;
        _open = open;
    }

    /// <inheritdoc />
    public override XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);

        var all = (XdmSequence[])_fixed.Clone();
        for (var i = 0; i < _open.Length; i++)
        {
            all[_open[i]] = arguments[i];
        }

        return FunctionInvoker.Call(_target, context, all, default(SourceSpan));
    }
}
