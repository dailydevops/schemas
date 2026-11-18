namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:minutes-from-time</c>: the minutes of an <c>xs:time</c> value, read from its local value.</summary>
internal sealed class MinutesFromTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="MinutesFromTimeFunction"/> class.</summary>
    public MinutesFromTimeFunction()
        : base("minutes-from-time", XPathItemType.Time) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => XdmItem.FromInteger(value.Minute);
}
