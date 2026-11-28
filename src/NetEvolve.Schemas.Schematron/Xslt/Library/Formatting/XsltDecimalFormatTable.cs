namespace NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The decimal formats of a compiled schema by expanded name, merged and checked: the provider that a run puts into the decimal format
/// slot of the XPath host (<see cref="XPathHostSlots.DecimalFormats"/>), which <c>format-number</c> reads. The unnamed format has the
/// default name of <see cref="XPathQName"/>; a schema without a declaration of it has no entry, and the XPath package applies the
/// default format. Immutable and thread-safe as long as nobody changes the formats, which the function does not do: it copies them.
/// </summary>
internal sealed class XsltDecimalFormatTable : IXPathDecimalFormatProvider
{
    private readonly Dictionary<XPathQName, XPathDecimalFormat> _formats;

    internal XsltDecimalFormatTable(Dictionary<XPathQName, XPathDecimalFormat> formats)
    {
        ArgumentNullException.ThrowIfNull(formats);

        _formats = formats;
    }

    /// <summary>Gets the number of formats, the unnamed one included.</summary>
    internal int Count => _formats.Count;

    /// <inheritdoc />
    public bool TryGetDecimalFormat(XPathQName name, out XPathDecimalFormat? format) =>
        _formats.TryGetValue(name, out format);
}
