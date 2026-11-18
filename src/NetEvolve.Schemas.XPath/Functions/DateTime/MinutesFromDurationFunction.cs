namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:minutes-from-duration</c>: the minutes of the canonical form of an <c>xs:duration</c> value, truncated toward zero and signed like the duration.</summary>
internal sealed class MinutesFromDurationFunction : DurationComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="MinutesFromDurationFunction"/> class.</summary>
    public MinutesFromDurationFunction()
        : base("minutes-from-duration") { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDuration value) => DateTimeValues.Minutes(in value);
}
