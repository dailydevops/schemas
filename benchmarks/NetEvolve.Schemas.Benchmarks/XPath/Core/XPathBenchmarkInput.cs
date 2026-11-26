namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using System.Globalization;
using System.Text;

/// <summary>
/// The input of the XPath core benchmarks: one invoice document of EN 16931 size and shape, generated
/// deterministically from fixed values, and a fixed list of expressions typical for validation rules.
/// Limitation: the document has no namespaces, so both engines need no static context; only size and shape follow EN 16931.
/// </summary>
internal static class XPathBenchmarkInput
{
    private const int LineCount = 100;

    /// <summary>Gets the XPath 1.0 compatible expressions that are dominated by predicates.</summary>
    public static string[] Predicates { get; } =
    [
        "/Invoice/InvoiceLine[Quantity > 5]",
        "/Invoice/InvoiceLine[position() > 10 and position() < 20]/LineAmount",
        "//InvoiceLine[Item/TaxCategory/Code = 'S']",
        "/Invoice/TaxTotal/TaxSubtotal[Rate = 19]/TaxAmount",
    ];

    /// <summary>Gets the XPath 1.0 compatible expressions whose predicate selects a range of positions.</summary>
    public static string[] PositionalRanges { get; } =
    [
        "/Invoice/InvoiceLine[position() > 10 and position() < 20]/LineAmount",
        "/Invoice/InvoiceLine[position() >= 40 and position() <= 60 and position() > 45]",
        "/Invoice/InvoiceLine[position() > 90 and position() <= 95]/LineAmount",
    ];

    /// <summary>Gets the XPath 1.0 compatible expressions that are dominated by comparisons.</summary>
    public static string[] Comparisons { get; } =
    [
        "/Invoice/ID != ''",
        "count(/Invoice/InvoiceLine) > 0",
        "sum(/Invoice/InvoiceLine/LineAmount) > 0",
        "/Invoice/InvoiceLine/LineAmount[@currency = 'EUR']",
        "string-length(/Invoice/ID) <= 20",
    ];

    /// <summary>Gets the XPath 1.0 compatible tests whose truth is all a rule needs: paths, comparisons, and logical combinations.</summary>
    public static string[] Booleans { get; } =
    [
        "/Invoice/InvoiceLine",
        "//InvoiceLine/Item/TaxCategory",
        "/Invoice/InvoiceLine/LineAmount",
        "/Invoice/MissingElement",
        "/Invoice/ID != ''",
        "count(/Invoice/InvoiceLine) > 0",
        "/Invoice/InvoiceLine/Quantity > 5",
        "/Invoice/InvoiceLine[Quantity > 5] and /Invoice/TaxTotal",
        "/Invoice/MissingElement or /Invoice/InvoiceLine/Item",
        "//InvoiceLine[Item/TaxCategory/Code = 'S']",
    ];

    /// <summary>Gets the expressions that are valid XPath 1.0 as well, so <c>System.Xml.XPath</c> can run them as the baseline.</summary>
    public static string[] Compatible { get; } = [.. Predicates, .. Comparisons];

    /// <summary>Gets the expressions that need XPath 2.0; they have no baseline.</summary>
    public static string[] Extended { get; } =
    [
        "every $l in /Invoice/InvoiceLine satisfies number($l/Quantity) gt 0",
        "for $l in /Invoice/InvoiceLine return number($l/LineAmount)",
        "if (/Invoice/InvoiceTypeCode = ('380', '381')) then 1 else 0",
        "count(distinct-values(/Invoice/InvoiceLine/Item/TaxCategory/Code)) le 3",
        "/Invoice/InvoiceLine[number(LineAmount) ge 100]",
        "exists(/Invoice/InvoiceLine[Item/Name eq 'Item 7'])",
    ];

    /// <summary>Creates the invoice: header, parties, payment, tax, totals, and 100 invoice lines.</summary>
    /// <returns>The document text; the same text on every call and every runtime.</returns>
    public static string CreateInvoice()
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Invoice>\n");
        _ = sb.Append("<ID>2026-0001</ID><IssueDate>2026-11-22</IssueDate><DueDate>2026-12-22</DueDate>\n")
            .Append("<InvoiceTypeCode>380</InvoiceTypeCode><DocumentCurrencyCode>EUR</DocumentCurrencyCode>")
            .Append("<BuyerReference>04011000-12345-34</BuyerReference>\n");
        Party(sb, "Seller", "Example Seller GmbH", "DE123456789");
        Party(sb, "Buyer", "Example Buyer AG", "DE987654321");
        _ = sb.Append("<PaymentMeans><Code>58</Code><Iban>DE89370400440532013000</Iban></PaymentMeans>\n");
        _ = sb.Append("<TaxTotal><TaxAmount currency=\"EUR\">1900.00</TaxAmount>\n");
        foreach (var rate in new[] { 19, 7, 0 })
        {
            _ = sb.Append("<TaxSubtotal><Category>")
                .Append(rate == 0 ? 'Z' : 'S')
                .Append("</Category><Rate>")
                .Append(rate.ToString(CultureInfo.InvariantCulture))
                .Append("</Rate><Taxable currency=\"EUR\">10000.00</Taxable><TaxAmount currency=\"EUR\">")
                .Append((rate * 100).ToString(CultureInfo.InvariantCulture))
                .Append(".00</TaxAmount></TaxSubtotal>\n");
        }

        _ = sb.Append("</TaxTotal>\n")
            .Append("<LegalMonetaryTotal><LineExtension>10000.00</LineExtension><TaxExclusive>10000.00</TaxExclusive>")
            .Append("<TaxInclusive>11900.00</TaxInclusive><Payable>11900.00</Payable></LegalMonetaryTotal>\n");
        for (var i = 0; i < LineCount; i++)
        {
            Line(sb, i);
        }

        return sb.Append("</Invoice>\n").ToString();
    }

    private static void Party(StringBuilder sb, string role, string name, string vatId) =>
        _ = sb.Append('<')
            .Append(role)
            .Append("><Name>")
            .Append(name)
            .Append("</Name><Country>DE</Country><VatId>")
            .Append(vatId)
            .Append(
                "</VatId><Address><Street>Example Street 1</Street><City>Example City</City><Postcode>12345</Postcode></Address></"
            )
            .Append(role)
            .Append(">\n");

    private static void Line(StringBuilder sb, int i)
    {
        var id = i.ToString(CultureInfo.InvariantCulture);
        var quantity = (i % 9 + 1).ToString(CultureInfo.InvariantCulture);
        var amount = ((i % 9 + 1) * 10).ToString(CultureInfo.InvariantCulture);
        _ = sb.Append("<InvoiceLine><ID>")
            .Append(id)
            .Append("</ID><Quantity unit=\"C62\">")
            .Append(quantity)
            .Append("</Quantity><LineAmount currency=\"EUR\">")
            .Append(amount)
            .Append(".00</LineAmount><Item><Name>Item ")
            .Append(id)
            .Append("</Name><Description>Example item number ")
            .Append(id)
            .Append("</Description><TaxCategory><Code>")
            .Append(i % 5 == 4 ? 'Z' : 'S')
            .Append(
                "</Code><Rate>19</Rate></TaxCategory></Item><Price><Amount currency=\"EUR\">10.00</Amount></Price></InvoiceLine>\n"
            );
    }
}
