namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary><c>fn:adjust-date-to-timezone</c>: moves an <c>xs:date</c> value to a timezone, or to the implicit timezone without a second argument.</summary>
internal sealed class AdjustDateToTimezoneFunction : AdjustToTimezoneFunction
{
    /// <summary>Initializes a new instance of the <see cref="AdjustDateToTimezoneFunction"/> class.</summary>
    public AdjustDateToTimezoneFunction()
        : base("adjust-date-to-timezone", XPathItemType.Date) { }
}
