namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary><c>fn:adjust-time-to-timezone</c>: moves an <c>xs:time</c> value to a timezone, or to the implicit timezone without a second argument.</summary>
internal sealed class AdjustTimeToTimezoneFunction : AdjustToTimezoneFunction
{
    /// <summary>Initializes a new instance of the <see cref="AdjustTimeToTimezoneFunction"/> class.</summary>
    public AdjustTimeToTimezoneFunction()
        : base("adjust-time-to-timezone", XPathItemType.Time) { }
}
