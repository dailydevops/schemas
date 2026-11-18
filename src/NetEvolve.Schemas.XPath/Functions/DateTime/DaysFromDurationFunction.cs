namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:days-from-duration</c>: the days of the canonical form of an <c>xs:duration</c> value, truncated toward zero and signed like the duration.</summary>
internal sealed class DaysFromDurationFunction : DurationComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="DaysFromDurationFunction"/> class.</summary>
    public DaysFromDurationFunction()
        : base("days-from-duration") { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDuration value) => DateTimeValues.Days(in value);
}
