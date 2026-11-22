namespace NetEvolve.Schemas.Benchmarks.Xml.Dtd;

using System.Globalization;
using System.Text;

/// <summary>
/// The input of the DTD validation benchmarks: one invoice-sized document, valid against its declarations, that is
/// served with the declarations in the internal subset or in an external subset. Both scenarios and both readers
/// (this library and <c>System.Xml</c>) read the same bytes, so the work is the same.
/// </summary>
internal static class DtdBenchmarkInput
{
    /// <summary>The absolute URI of the document; the base of the relative system identifier of the external subset.</summary>
    public const string DocumentUri = "http://example.org/billing/invoice.xml";

    /// <summary>The URI the system identifier <c>invoice.dtd</c> resolves to.</summary>
    public const string SubsetUri = "http://example.org/billing/invoice.dtd";

    private const int LineCount = 200;

    private const string Declarations =
        "<!ELEMENT invoice (header, line+)>\n"
        + "<!ELEMENT header (number, date, customer)>\n"
        + "<!ELEMENT number (#PCDATA)>\n"
        + "<!ELEMENT date (#PCDATA)>\n"
        + "<!ELEMENT customer (#PCDATA)>\n"
        + "<!ELEMENT line (description, quantity, price)>\n"
        + "<!ATTLIST line id ID #REQUIRED kind (goods | service) \"goods\" currency CDATA #FIXED \"EUR\">\n"
        + "<!ELEMENT description (#PCDATA)>\n"
        + "<!ELEMENT quantity (#PCDATA)>\n"
        + "<!ELEMENT price (#PCDATA)>\n";

    /// <summary>Gets the document with the declarations in its internal subset, as UTF-8.</summary>
    public static byte[] InternalSubsetDocument { get; } = Utf8("<!DOCTYPE invoice [\n" + Declarations + "]>");

    /// <summary>Gets the document that refers to <c>invoice.dtd</c> as its external subset, as UTF-8.</summary>
    public static byte[] ExternalSubsetDocument { get; } = Utf8("<!DOCTYPE invoice SYSTEM \"invoice.dtd\">");

    /// <summary>Gets the external subset: the same declarations as the internal subset, as UTF-8.</summary>
    public static byte[] ExternalSubset { get; } = Encoding.UTF8.GetBytes(Declarations);

    private static byte[] Utf8(string doctype)
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n")
            .Append(doctype)
            .Append(
                "\n<invoice>\n<header><number>2026-0001</number><date>2026-11-22</date><customer>Example Corp</customer></header>\n"
            );
        for (var i = 0; i < LineCount; i++)
        {
            // Every other line leaves out kind and currency, so the default and the fixed value apply.
            _ = sb.Append("<line id=\"l").Append(i.ToString(CultureInfo.InvariantCulture)).Append('"');
            if (i % 2 == 1)
            {
                _ = sb.Append(" kind=\"service\" currency=\"EUR\"");
            }

            _ = sb.Append("><description>Item ")
                .Append(i.ToString(CultureInfo.InvariantCulture))
                .Append("</description>");
            _ = sb.Append("<quantity>").Append(i % 7 + 1).Append("</quantity><price>9.99</price></line>\n");
        }

        return Encoding.UTF8.GetBytes(sb.Append("</invoice>\n").ToString());
    }
}
