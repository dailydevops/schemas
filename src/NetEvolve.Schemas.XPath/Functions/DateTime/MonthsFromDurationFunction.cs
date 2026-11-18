namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:months-from-duration</c>: the months of the canonical form of an <c>xs:duration</c> value, truncated toward zero and signed like the duration.</summary>
internal sealed class MonthsFromDurationFunction : DurationComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="MonthsFromDurationFunction"/> class.</summary>
    public MonthsFromDurationFunction()
        : base("months-from-duration") { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDuration value) => XdmItem.FromInteger(value.Months % 12);
}
