namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:day-from-date</c>: the day of an <c>xs:date</c> value, read from its local value.</summary>
internal sealed class DayFromDateFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="DayFromDateFunction"/> class.</summary>
    public DayFromDateFunction()
        : base("day-from-date", XPathItemType.Date) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Day);
}
