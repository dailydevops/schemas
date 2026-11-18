namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:month-from-date</c>: the month of an <c>xs:date</c> value, read from its local value.</summary>
internal sealed class MonthFromDateFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="MonthFromDateFunction"/> class.</summary>
    public MonthFromDateFunction()
        : base("month-from-date", XPathItemType.Date) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Month);
}
