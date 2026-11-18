namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:timezone-from-date</c>: the timezone as an <c>xs:dayTimeDuration</c>, empty without one of an <c>xs:date</c> value, read from its local value.</summary>
internal sealed class TimezoneFromDateFunction : DateTimeComponentFunction
{
    /// <summary>Initializes a new instance of the <see cref="TimezoneFromDateFunction"/> class.</summary>
    public TimezoneFromDateFunction()
        : base("timezone-from-date", XPathItemType.Date) { }

    /// <inheritdoc />
    protected override XdmItem Extract(in XsdDateTime value) => DateTimeValues.Timezone(in value);
}
