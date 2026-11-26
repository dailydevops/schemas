namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:round</c> (Functions and Operators 1.0, 6.4.4): the closest integer to the argument, in the type of the argument, a tie toward positive infinity: 2.5 gives 3 and -2.5 gives -2. A floating-point value from -0.5 up to 0 gives negative zero. The empty sequence gives the empty sequence.
/// </summary>
/// <remarks>The function takes one argument; the form with a precision is <see cref="RoundWithPrecisionFunction"/>, which XPath 3.0 and 3.1 add.</remarks>
internal sealed class RoundFunction : NumericFunction
{
    /// <summary>Initializes a new instance of the <see cref="RoundFunction"/> class.</summary>
    public RoundFunction()
        : base("round", 1, 1, [OptionalNumeric]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        invocation.Result = argument.IsEmpty ? argument : XdmSequence.Create(NumericRounding.Round(argument[0]));
        return XPathEvaluationStatus.Ok;
    }
}
