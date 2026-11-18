namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary><c>fn:adjust-dateTime-to-timezone</c>: moves an <c>xs:dateTime</c> value to a timezone, or to the implicit timezone without a second argument.</summary>
internal sealed class AdjustDateTimeToTimezoneFunction : AdjustToTimezoneFunction
{
    /// <summary>Initializes a new instance of the <see cref="AdjustDateTimeToTimezoneFunction"/> class.</summary>
    public AdjustDateTimeToTimezoneFunction()
        : base("adjust-dateTime-to-timezone", XPathItemType.DateTime) { }
}
