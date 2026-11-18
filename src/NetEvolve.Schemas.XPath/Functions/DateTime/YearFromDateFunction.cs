namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:year-from-date</c>: the year of an <c>xs:date</c> value, read from its local value.</summary>
internal sealed class YearFromDateFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="YearFromDateFunction"/> class.</summary>
    public YearFromDateFunction()
        : base("year-from-date", XPathItemType.Date) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Year);
}
