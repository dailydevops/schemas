namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:round-half-to-even</c> (Functions and Operators 1.0, 6.4.5): the argument rounded to a precision, a tie to the even neighbor,
/// in the type of the argument: 2.5 gives 2 and 3.5 gives 4. The precision is the number of fraction digits kept and defaults to 0; a
/// negative precision rounds to a power of ten, so 12350 with a precision of -2 gives 12400. The empty sequence gives the empty sequence.
/// </summary>
/// <remarks>Limitation: a precision of an <c>xs:integer</c> beyond 64 bits raises XPTY0004 until the function conversion accepts it (#719).</remarks>
internal sealed class RoundHalfToEvenFunction : NumericFunction
{
    /// <summary>Initializes a new instance of the <see cref="RoundHalfToEvenFunction"/> class.</summary>
    public RoundHalfToEvenFunction()
        : base("round-half-to-even", 1, 2, [OptionalNumeric, OneInteger]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.IsEmpty)
        {
            invocation.Result = argument;
            return XPathEvaluationStatus.Ok;
        }

        // The function conversion has matched the precision against xs:integer exactly once; one beyond 64 bits is saturated.
        var precision = invocation.Arguments.Length > 1 ? invocation.Arguments[1][0].GetSaturatedInteger() : 0;

        invocation.Result = XdmSequence.Create(NumericRounding.RoundHalfToEven(argument[0], precision));
        return XPathEvaluationStatus.Ok;
    }
}
