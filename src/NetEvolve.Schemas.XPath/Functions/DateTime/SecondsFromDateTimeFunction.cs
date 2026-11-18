namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:seconds-from-dateTime</c>: the seconds with their fraction of an <c>xs:dateTime</c> value, read from its local value.</summary>
internal sealed class SecondsFromDateTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="SecondsFromDateTimeFunction"/> class.</summary>
    public SecondsFromDateTimeFunction()
        : base("seconds-from-dateTime", XPathItemType.DateTime) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromDecimal(value.Second);
}
