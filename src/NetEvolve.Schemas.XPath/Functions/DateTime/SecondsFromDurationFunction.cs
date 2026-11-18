namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:seconds-from-duration</c>: the seconds with their fraction of the canonical form of an <c>xs:duration</c> value, truncated toward zero and signed like the duration.</summary>
internal sealed class SecondsFromDurationFunction : DurationComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="SecondsFromDurationFunction"/> class.</summary>
    public SecondsFromDurationFunction()
        : base("seconds-from-duration") { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDuration value) => XdmItem.FromDecimal(DateTimeValues.Seconds(in value));
}
