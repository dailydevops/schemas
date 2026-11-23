namespace NetEvolve.Schemas.Benchmarks.Xml.Pipeline;

using System.Globalization;
using System.Text;

/// <summary>
/// The input of the pipeline benchmarks: one invoice-sized UTF-8 document without a document type declaration and
/// without insignificant whitespace, so this library and <c>System.Xml</c> load and write the same infoset.
/// </summary>
internal static class PipelineBenchmarkInput
{
    private const int LineCount = 200;

    /// <summary>Gets the document as UTF-8.</summary>
    public static byte[] Document { get; } = Build();

    private static byte[] Build()
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        _ = sb.Append(
            "<invoice xmlns=\"http://example.org/billing\"><header><number>2026-0001</number><date>2026-11-22</date><customer>Example &amp; Corp</customer></header>"
        );
        for (var i = 0; i < LineCount; i++)
        {
            var n = i.ToString(CultureInfo.InvariantCulture);
            _ = sb.Append("<line id=\"l")
                .Append(n)
                .Append("\" kind=\"")
                .Append(i % 2 == 0 ? "goods" : "service")
                .Append("\"><description>Item ")
                .Append(n)
                .Append("</description><quantity>")
                .Append(i % 7 + 1)
                .Append("</quantity><price>9.99</price></line>");
        }

        return Encoding.UTF8.GetBytes(sb.Append("</invoice>").ToString());
    }
}
