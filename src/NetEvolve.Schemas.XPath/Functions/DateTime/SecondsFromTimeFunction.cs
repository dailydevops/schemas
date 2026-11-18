namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:seconds-from-time</c>: the seconds with their fraction of an <c>xs:time</c> value, read from its local value.</summary>
internal sealed class SecondsFromTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="SecondsFromTimeFunction"/> class.</summary>
    public SecondsFromTimeFunction()
        : base("seconds-from-time", XPathItemType.Time) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromDecimal(value.Second);
}
