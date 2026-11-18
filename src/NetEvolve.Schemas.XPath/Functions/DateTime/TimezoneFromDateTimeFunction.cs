namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:timezone-from-dateTime</c>: the timezone as an <c>xs:dayTimeDuration</c>, empty without one of an <c>xs:dateTime</c> value, read from its local value.</summary>
internal sealed class TimezoneFromDateTimeFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="TimezoneFromDateTimeFunction"/> class.</summary>
    public TimezoneFromDateTimeFunction()
        : base("timezone-from-dateTime", XPathItemType.DateTime) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => DateTimeValues.Timezone(in value);
}
