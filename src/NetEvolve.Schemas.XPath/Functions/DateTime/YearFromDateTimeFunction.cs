namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:year-from-dateTime</c>: the year of an <c>xs:dateTime</c> value, read from its local value.</summary>
internal sealed class YearFromDateTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="YearFromDateTimeFunction"/> class.</summary>
    public YearFromDateTimeFunction()
        : base("year-from-dateTime", XPathItemType.DateTime) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Year);
}
