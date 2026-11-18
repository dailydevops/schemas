namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:minutes-from-dateTime</c>: the minutes of an <c>xs:dateTime</c> value, read from its local value.</summary>
internal sealed class MinutesFromDateTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="MinutesFromDateTimeFunction"/> class.</summary>
    public MinutesFromDateTimeFunction()
        : base("minutes-from-dateTime", XPathItemType.DateTime) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Minute);
}
