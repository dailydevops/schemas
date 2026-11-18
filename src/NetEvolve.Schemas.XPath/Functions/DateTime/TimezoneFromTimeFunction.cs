namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:timezone-from-time</c>: the timezone as an <c>xs:dayTimeDuration</c>, empty without one of an <c>xs:time</c> value, read from its local value.</summary>
internal sealed class TimezoneFromTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="TimezoneFromTimeFunction"/> class.</summary>
    public TimezoneFromTimeFunction()
        : base("timezone-from-time", XPathItemType.Time) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => DateTimeValues.Timezone(in value);
}
