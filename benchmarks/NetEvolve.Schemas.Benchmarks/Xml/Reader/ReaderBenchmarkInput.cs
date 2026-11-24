namespace NetEvolve.Schemas.Benchmarks.Xml.Reader;

using System.Globalization;
using System.Text;

/// <summary>
/// The input of the reader benchmarks: a small, an invoice-sized, and a large document. Every input is ASCII, so its
/// length in characters equals its length in UTF-8 bytes, and both readers (this library and <c>System.Xml</c>) read
/// the same content as characters and as bytes.
/// </summary>
internal static class ReaderBenchmarkInput
{
    private const int InvoiceLines = 200;
    private const int LargeLines = 20_000;

    /// <summary>Gets the small document: a declaration, a few elements, an attribute, and a character reference.</summary>
    public static string Small { get; } =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<note id=\"n1\"><to>Example</to><body>Hello &amp; welcome</body></note>\n";

    /// <summary>Gets the invoice-sized document (about 25 KB).</summary>
    public static string Invoice { get; } = BuildInvoice(InvoiceLines);

    /// <summary>Gets the large document (about 2.5 MB).</summary>
    public static string Large { get; } = BuildInvoice(LargeLines);

    /// <summary>Gets the document as UTF-8.</summary>
    /// <param name="document">The document.</param>
    /// <returns>The bytes.</returns>
    public static byte[] Utf8(string document) => Encoding.UTF8.GetBytes(document);

    /// <summary>Gets the input length for a benchmark class named <c>&lt;Size&gt;&lt;Encoding&gt;ReaderBenchmarks</c>.</summary>
    /// <param name="className">The class name.</param>
    /// <returns>The length in bytes, or <see langword="null"/> for any other class.</returns>
    public static int? LengthFor(string className) =>
        className.StartsWith("Small", StringComparison.Ordinal) ? Small.Length
        : className.StartsWith("Invoice", StringComparison.Ordinal) ? Invoice.Length
        : className.StartsWith("Large", StringComparison.Ordinal) ? Large.Length
        : null;

    private static string BuildInvoice(int lines)
    {
        var sb = new StringBuilder(
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<invoice xmlns=\"http://example.org/billing\">\n"
        ).Append(
            "<header><number>2026-0001</number><date>2026-11-22</date><customer>Example Corp</customer></header>\n"
        );
        for (var i = 0; i < lines; i++)
        {
            var n = i.ToString(CultureInfo.InvariantCulture);
            _ = sb.Append("<line id=\"l")
                .Append(n)
                .Append("\" kind=\"goods\" currency=\"EUR\"><description>Item ")
                .Append(n)
                .Append(" &amp; accessories</description><quantity>")
                .Append(i % 7 + 1)
                .Append("</quantity><price>9.99</price><!-- line ")
                .Append(n)
                .Append(" --></line>\n");
        }

        return sb.Append("</invoice>\n").ToString();
    }
}
