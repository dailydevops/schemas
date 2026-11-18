namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:hours-from-dateTime</c>: the hours of an <c>xs:dateTime</c> value, read from its local value.</summary>
internal sealed class HoursFromDateTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="HoursFromDateTimeFunction"/> class.</summary>
    public HoursFromDateTimeFunction()
        : base("hours-from-dateTime", XPathItemType.DateTime) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Hour);
}
