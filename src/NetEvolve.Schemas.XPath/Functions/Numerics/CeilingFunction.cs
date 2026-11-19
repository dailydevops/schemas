namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:ceiling</c> (Functions and Operators 1.0, 6.4.2): the smallest integer that is not less than the argument, in the type of the argument. A floating-point value between -1 and 0 gives negative zero. The empty sequence gives the empty sequence.
/// </summary>
internal sealed class CeilingFunction : NumericFunction
{
    /// <summary>Initializes a new instance of the <see cref="CeilingFunction"/> class.</summary>
    public CeilingFunction()
        : base("ceiling", 1, 1, [OptionalNumeric]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        invocation.Result = argument.IsEmpty ? argument : XdmSequence.Create(NumericRounding.Ceiling(argument[0]));
        return XPathEvaluationStatus.Ok;
    }
}
