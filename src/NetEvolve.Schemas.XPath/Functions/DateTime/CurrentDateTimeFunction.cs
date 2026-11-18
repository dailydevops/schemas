namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:current-dateTime()</c>: the <c>xs:dateTime</c> value that the dynamic context fixed once for the evaluation.</summary>
internal sealed class CurrentDateTimeFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="CurrentDateTimeFunction"/> class.</summary>
    public CurrentDateTimeFunction()
        : base("current-dateTime", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(
            XdmItem.FromDateTime(DateTimeValues.Current(invocation.Context.CurrentDateTime, XsdDateTimeKind.DateTime))
        );
        return XPathEvaluationStatus.Ok;
    }
}
