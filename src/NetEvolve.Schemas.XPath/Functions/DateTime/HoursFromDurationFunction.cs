namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:hours-from-duration</c>: the hours of the canonical form of an <c>xs:duration</c> value, truncated toward zero and signed like the duration.</summary>
internal sealed class HoursFromDurationFunction : DurationComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="HoursFromDurationFunction"/> class.</summary>
    public HoursFromDurationFunction()
        : base("hours-from-duration") { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDuration value) => DateTimeValues.Hours(in value);
}
