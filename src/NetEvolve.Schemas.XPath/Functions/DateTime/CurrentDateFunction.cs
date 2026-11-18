namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:current-date()</c>: the <c>xs:date</c> value that the dynamic context fixed once for the evaluation.</summary>
internal sealed class CurrentDateFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="CurrentDateFunction"/> class.</summary>
    public CurrentDateFunction()
        : base("current-date", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(
            XdmItem.FromDateTime(DateTimeValues.Current(invocation.Context.CurrentDateTime, XsdDateTimeKind.Date))
        );
        return XPathEvaluationStatus.Ok;
    }
}
