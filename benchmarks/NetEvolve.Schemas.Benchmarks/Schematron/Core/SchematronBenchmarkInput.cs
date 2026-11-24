namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

using System.Globalization;
using System.Text;

/// <summary>
/// The input of the Schematron core benchmarks: one invoice-sized document and synthetic rule sets of growing size. A rule
/// set of size <c>n</c> has <c>n</c> patterns with one rule each; every rule has the context <c>i:line</c> and one assert
/// that always holds, so each rule fires once per line and <c>n * LineCount</c> rules fire per run. One rule per pattern
/// keeps every rule firing (within a pattern only the first matching rule fires).
/// </summary>
internal static class SchematronBenchmarkInput
{
    /// <summary>The namespace of the document.</summary>
    public const string Namespace = "urn:invoice";

    /// <summary>The context of every rule.</summary>
    public const string Context = "i:line";

    /// <summary>The number of <c>line</c> elements of the document.</summary>
    public const int LineCount = 100;

    /// <summary>The size of the small rule set.</summary>
    public const int Small = 1;

    /// <summary>The size of the medium rule set.</summary>
    public const int Medium = 10;

    /// <summary>The size of the large rule set.</summary>
    public const int Large = 100;

    /// <summary>Gets the document as UTF-8.</summary>
    public static byte[] Document { get; } = BuildDocument();

    /// <summary>Gets the XPath test of the rule with the given index; it always holds for the document.</summary>
    /// <param name="index">The index of the rule.</param>
    /// <returns>The XPath test, not escaped.</returns>
    public static string Test(int index) =>
        string.Create(CultureInfo.InvariantCulture, $"number(i:price) > -{index + 1}");

    /// <summary>Builds the schema of a rule set.</summary>
    /// <param name="ruleCount">The number of patterns, each with one rule.</param>
    /// <param name="withReports">Whether every rule also reports lines with a quantity above five, so that findings exist.</param>
    /// <returns>The schema as UTF-8.</returns>
    public static byte[] Schema(int ruleCount, bool withReports = false)
    {
        var sb = new StringBuilder("<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"><ns prefix=\"i\" uri=\"")
            .Append(Namespace)
            .Append("\"/>");
        for (var i = 0; i < ruleCount; i++)
        {
            _ = sb.Append("<pattern id=\"p")
                .Append(i.ToString(CultureInfo.InvariantCulture))
                .Append("\"><rule context=\"")
                .Append(Context)
                .Append("\"><assert test=\"")
                .Append(Test(i).Replace(">", "&gt;", StringComparison.Ordinal))
                .Append("\">The price is too low.</assert>");
            if (withReports)
            {
                _ = sb.Append("<report test=\"i:qty &gt; 5\">Large quantity: <value-of select=\"i:qty\"/></report>");
            }

            _ = sb.Append("</rule></pattern>");
        }

        return Encoding.UTF8.GetBytes(sb.Append("</schema>").ToString());
    }

    private static byte[] BuildDocument()
    {
        var sb = new StringBuilder("<invoice xmlns=\"").Append(Namespace).Append("\">\n");
        for (var i = 0; i < LineCount; i++)
        {
            _ = sb.Append("<line><qty>").Append(i % 7 + 1).Append("</qty><price>9.99</price></line>\n");
        }

        return Encoding.UTF8.GetBytes(sb.Append("</invoice>\n").ToString());
    }
}
