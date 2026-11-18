namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:month-from-dateTime</c>: the month of an <c>xs:dateTime</c> value, read from its local value.</summary>
internal sealed class MonthFromDateTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="MonthFromDateTimeFunction"/> class.</summary>
    public MonthFromDateTimeFunction()
        : base("month-from-dateTime", XPathItemType.DateTime) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Month);
}
