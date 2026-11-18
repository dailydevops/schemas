namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:hours-from-time</c>: the hours of an <c>xs:time</c> value, read from its local value.</summary>
internal sealed class HoursFromTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="HoursFromTimeFunction"/> class.</summary>
    public HoursFromTimeFunction()
        : base("hours-from-time", XPathItemType.Time) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Hour);
}
