namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:years-from-duration</c>: the years of the canonical form of an <c>xs:duration</c> value, truncated toward zero and signed like the duration.</summary>
internal sealed class YearsFromDurationFunction : DurationComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="YearsFromDurationFunction"/> class.</summary>
    public YearsFromDurationFunction()
        : base("years-from-duration") { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDuration value) => XdmItem.FromInteger(value.Months / 12);
}
