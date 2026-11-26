namespace NetEvolve.Schemas.Benchmarks.Schematron.Functions;

using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Benchmarks.Schematron.Core;
using NetEvolve.Schemas.Schematron;

/// <summary>
/// Validation against a rule set under <c>queryBinding="xslt2"</c> whose every rule calls one stylesheet function and looks up one
/// <c>xsl:key</c> per line, so the cost of function calls and key lookups shows. Fired rules per run are
/// <c>RuleCount * SchematronBenchmarkInput.LineCount</c>; the document is the one of the core benchmarks.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class FunctionAndKeyBenchmarks
#pragma warning restore CA1515
{
    private const int RuleCount = 10;

    private readonly byte[] _document = SchematronBenchmarkInput.Document;
    private ISchematronValidator _validator = null!;

    /// <summary>Builds the validator and proves that every rule fires for every line without a finding.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _validator = SchemaValidator.ForSchematron(Schema()).Build();
        var counter = new FiredRuleCounter();
        var result = _validator.WithReportSink(counter).Validate(_document.AsSpan());
        var expected = RuleCount * SchematronBenchmarkInput.LineCount;
        if (!result.IsValid || counter.FiredRules != expected)
        {
            throw new InvalidOperationException(
                $"Expected {expected} fired rules without findings, got {counter.FiredRules} fired rules and {result.Issues.Count} issues."
            );
        }
    }

    /// <summary>Validates memory.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateMemory() => _validator.Validate(_document.AsSpan()).Issues.Count;

    private static byte[] Schema()
    {
        var sb = new StringBuilder(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" "
                + "xmlns:f=\"urn:f\" queryBinding=\"xslt2\" schematronEdition=\"2025\"><ns prefix=\"i\" uri=\""
        )
            .Append(SchematronBenchmarkInput.Namespace)
            .Append("\"/><ns prefix=\"f\" uri=\"urn:f\"/>")
            .Append("<xsl:function name=\"f:above\"><xsl:param name=\"value\"/><xsl:param name=\"limit\"/>")
            .Append("<xsl:sequence select=\"number($value) &gt; $limit\"/></xsl:function>")
            .Append("<xsl:key name=\"byQty\" match=\"i:line\" use=\"i:qty\"/>");
        for (var i = 0; i < RuleCount; i++)
        {
            _ = sb.Append("<pattern id=\"p")
                .Append(i.ToString(CultureInfo.InvariantCulture))
                .Append("\"><rule context=\"")
                .Append(SchematronBenchmarkInput.Context)
                .Append("\"><assert test=\"f:above(i:price, -")
                .Append((i + 1).ToString(CultureInfo.InvariantCulture))
                .Append(
                    ") and count(key('byQty', string(i:qty))) &gt; 0\">The line is invalid.</assert></rule></pattern>"
                );
        }

        return Encoding.UTF8.GetBytes(sb.Append("</schema>").ToString());
    }
}
