namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:round</c> with a precision (Functions and Operators 3.1, 4.4.4): the argument rounded to a number of fraction digits, in the type
/// of the argument, a tie toward positive infinity: 1.125 with a precision of 2 gives 1.13 and -1.125 gives -1.12; a negative precision rounds
/// to a power of ten, so 8455 with a precision of -1 gives 8460. The empty sequence gives the empty sequence. The form is visible for XPath 3.0
/// and 3.1 only, so XPath 2.0 reports XPST0017 for it.
/// </summary>
/// <remarks>A precision of an <c>xs:integer</c> beyond 64 bits is saturated to the 64-bit range: a positive one keeps every digit and a negative one clears every digit, as for <c>fn:round-half-to-even</c> (#719).</remarks>
internal sealed class RoundWithPrecisionFunction : NumericFunction
{
    /// <summary>Initializes a new instance of the <see cref="RoundWithPrecisionFunction"/> class.</summary>
    public RoundWithPrecisionFunction()
        : base("round", 2, 2, [OptionalNumeric, OneInteger]) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        invocation.Result = argument.IsEmpty
            ? argument
            : XdmSequence.Create(NumericRounding.Round(argument[0], invocation.Arguments[1][0].GetSaturatedInteger()));
        return XPathEvaluationStatus.Ok;
    }
}
