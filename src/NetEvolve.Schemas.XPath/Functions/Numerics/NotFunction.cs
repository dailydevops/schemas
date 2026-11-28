namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:not</c> (Functions and Operators 1.0, section 9): the negation of the effective boolean value of the argument, a wrapper over the
/// routine of <see cref="XPathAtomization"/>. A value without an effective boolean value is FORG0006.
/// </summary>
internal sealed class NotFunction : NumericFunction, IItemSpanFunction
{
    /// <summary>Initializes a new instance of the <see cref="NotFunction"/> class.</summary>
    public NotFunction()
        : base("not", 1, 1, [Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(
            XdmItem.FromBoolean(!XPathAtomization.EffectiveBooleanValue(in invocation.Arguments[0]))
        );
        return XPathEvaluationStatus.Ok;
    }

    /// <inheritdoc />
    public XdmSequence InvokeOver(XPathEvaluationContext context, ReadOnlySpan<XdmItem> items) =>
        XdmSequence.Create(XdmItem.FromBoolean(!XPathAtomization.EffectiveBooleanValue(items)));
}
