namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:floor</c> (Functions and Operators 1.0, 6.4.3): the largest integer that is not greater than the argument, in the type of the argument. The empty sequence gives the empty sequence.
/// </summary>
internal sealed class FloorFunction : NumericFunction
{
    /// <summary>Initializes a new instance of the <see cref="FloorFunction"/> class.</summary>
    public FloorFunction()
        : base("floor", 1, 1, [OptionalNumeric]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        invocation.Result = argument.IsEmpty ? argument : XdmSequence.Create(NumericRounding.Floor(argument[0]));
        return XPathEvaluationStatus.Ok;
    }
}
