namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:abs</c> (Functions and Operators 1.0, 6.4.1): the absolute value of a number, in the type of the argument: an <c>xs:integer</c> stays an <c>xs:integer</c>, and an <c>xs:untypedAtomic</c> value is read as an <c>xs:double</c>. The empty sequence gives the empty sequence.
/// </summary>
internal sealed class AbsFunction : NumericFunction
{
    /// <summary>Initializes a new instance of the <see cref="AbsFunction"/> class.</summary>
    public AbsFunction()
        : base("abs", 1, 1, [OptionalNumeric]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        invocation.Result = argument.IsEmpty ? argument : XdmSequence.Create(NumericRounding.Abs(argument[0]));
        return XPathEvaluationStatus.Ok;
    }
}
