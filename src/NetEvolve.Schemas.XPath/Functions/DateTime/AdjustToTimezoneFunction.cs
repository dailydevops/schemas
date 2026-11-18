namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A timezone adjustment function in both arities (Functions and Operators 1.0, 10.7): the first argument is an <c>xs:dateTime</c>,
/// <c>xs:date</c>, or <c>xs:time</c> value that may be empty, the optional second argument an <c>xs:dayTimeDuration</c> that may be
/// empty. The empty value gives the empty sequence before the timezone is looked at; without a second argument the implicit
/// timezone of the dynamic context applies, and an empty second argument removes the timezone.
/// </summary>
internal abstract class AdjustToTimezoneFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="AdjustToTimezoneFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="valueType"><see cref="XPathItemType.DateTime"/>, <see cref="XPathItemType.Date"/>, or <see cref="XPathItemType.Time"/>.</param>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="valueType"/> is not a defined type.</exception>
    protected AdjustToTimezoneFunction(string localName, XPathItemType valueType)
        : base(
            localName,
            1,
            2,
            [
                new XPathParameterType(valueType, Occurrence.ZeroOrOne),
                new XPathParameterType(XPathItemType.DayTimeDuration, Occurrence.ZeroOrOne),
            ]
        ) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The timezone lies outside -PT14H to PT14H or is not a whole number of minutes (FODT0003).</exception>
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        int? timezone;
        if (arguments.Length == 1)
        {
            timezone = invocation.Context.ImplicitTimezoneMinutes;
        }
        else if (arguments[1].IsEmpty)
        {
            timezone = null;
        }
        else
        {
            _ = arguments[1][0].TryGetDuration(out var duration);
            timezone = DateTimeValues.TryGetTimezoneMinutes(in duration, out var minutes)
                ? minutes
                : throw XPathErrors.Exception(XPathErrorCodes.FODT0003);
        }

        _ = arguments[0][0].TryGetDateTime(out var value);
        invocation.Result = XdmSequence.Create(XdmItem.FromDateTime(DateTimeValues.Adjust(in value, timezone)));
        return XPathEvaluationStatus.Ok;
    }
}
