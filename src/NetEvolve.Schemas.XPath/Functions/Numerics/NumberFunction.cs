namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:number</c> (Functions and Operators 1.0, section 2): the argument as an <c>xs:double</c>, by the cast of the casting table, and NaN
/// for the empty sequence and for a value that cannot be converted: a string that is not a lexical form of <c>xs:double</c>, or a value
/// of a type without a cast to <c>xs:double</c>. Without an argument the function converts the context item. Leading and trailing
/// white space of a string is ignored by the cast, and a boolean gives 1 or 0.
/// </summary>
/// <remarks>
/// Limitation: a lexical form beyond the digit cap of the datatype layer (4,096 digits) is an implementation limit and not an invalid
/// form, so it raises FOCA0001 as the function conversion does (#621, #660, #271), where the specification gives a number or an infinity.
/// </remarks>
internal sealed class NumberFunction : NumericFunction
{
    private static readonly XdmItem NaN = XdmItem.FromDouble(double.NaN);

    /// <summary>Initializes a new instance of the <see cref="NumberFunction"/> class.</summary>
    public NumberFunction()
        : base("number", 0, 1, [OptionalAtomic]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument =
            invocation.Arguments.Length == 0
                ? XPathAtomization.Atomize(
                    XdmSequence.Create(
                        invocation.Context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002)
                    )
                )
                : invocation.Arguments[0];
        if (argument.Count > 1)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        invocation.Result = XdmSequence.Create(argument.IsEmpty ? NaN : ToDouble(argument[0]));
        return XPathEvaluationStatus.Ok;
    }

    private static XdmItem ToDouble(in XdmItem item)
    {
        if (item.Kind == XdmItemKind.XsDouble)
        {
            return item;
        }

        var status = CastingCatalog.Default.Table.TryCast(item, XdmTypeId.XsDouble, out var result);
        return status switch
        {
            CastStatus.Ok => result,
            CastStatus.NotAllowed or CastStatus.InvalidLexical or CastStatus.InvalidValue => NaN,
            _ => throw XPathErrors.Exception(status.ToErrorCode()),
        };
    }
}
