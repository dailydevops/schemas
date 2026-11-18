namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:implicit-timezone()</c>: the <c>xs:dayTimeDuration</c> value that the dynamic context fixed once for the evaluation.</summary>
internal sealed class ImplicitTimezoneFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="ImplicitTimezoneFunction"/> class.</summary>
    public ImplicitTimezoneFunction()
        : base("implicit-timezone", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(DateTimeValues.TimezoneItem(invocation.Context.ImplicitTimezoneMinutes));
        return XPathEvaluationStatus.Ok;
    }
}
