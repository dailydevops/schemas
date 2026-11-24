namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:dateTime</c> (Functions and Operators 1.0, 10.4.1): combines an <c>xs:date</c> and an <c>xs:time</c> into an
/// <c>xs:dateTime</c>. An empty argument gives the empty sequence; the timezone is that of the argument that has one, and
/// two different timezones raise FORG0008. The time <c>24:00:00</c> is already <c>00:00:00</c> after parsing.
/// </summary>
internal sealed class DateTimeConstructorFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeConstructorFunction"/> class.</summary>
    public DateTimeConstructorFunction()
        : base(
            "dateTime",
            2,
            2,
            [
                new XPathParameterType(XPathItemType.Date, Occurrence.ZeroOrOne),
                new XPathParameterType(XPathItemType.Time, Occurrence.ZeroOrOne),
            ]
        ) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">Both arguments have a timezone and the timezones differ (FORG0008).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (arguments[0].IsEmpty || arguments[1].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        _ = arguments[0][0].TryGetDateTime(out var date);
        _ = arguments[1][0].TryGetDateTime(out var time);
        var timezone = date.TimezoneOffsetMinutes;
        if (time.TimezoneOffsetMinutes is { } other)
        {
            if (timezone is { } own && own != other)
            {
                throw XPathErrors.Exception(XPathErrorCodes.FORG0008);
            }

            timezone = other;
        }

        invocation.Result = XdmSequence.Create(
            XdmItem.FromDateTime(
                XsdDateTime.Create(
                    XsdDateTimeKind.DateTime,
                    date.Year,
                    date.Month,
                    date.Day,
                    time.Hour,
                    time.Minute,
                    time.Second,
                    timezone
                )
            )
        );
        return XPathEvaluationStatus.Ok;
    }
}
