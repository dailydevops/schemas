namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Schematron;

/// <summary>
/// Validation against a rule set whose rules declare rule-level <see langword="let"/> variables that several asserts read, as the
/// rule sets of PEPPOL BIS and XRechnung do. There is no <c>System.Xml</c> equivalent (XPath 1.0 has no <see langword="let"/>), so
/// the first measured value is the target and a 5 % regression limit applies. Every assert reads two variables, so the
/// figure shows the cost of handing a variable to the XPath engine. Fired rules per run are
/// <c>RuleCount * SchematronBenchmarkInput.LineCount</c>, each with <c>AssertsPerRule</c> asserts.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class LetRuleSetBenchmarks
#pragma warning restore CA1515
{
    private const int RuleCount = SchematronBenchmarkInput.Medium;
    private const int AssertsPerRule = 4;

    private readonly byte[] _document = SchematronBenchmarkInput.Document;
    private ISchematronValidator _validator = null!;

    /// <summary>Builds the validator and proves that every assert holds.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _validator = SchemaValidator.ForSchematron(BuildSchema()).Build();
        var counter = new FiredRuleCounter();
        var result = _validator.WithReportSink(counter).Validate(_document.AsSpan());
        if (!result.IsValid || counter.FiredRules != RuleCount * SchematronBenchmarkInput.LineCount)
        {
            throw new InvalidOperationException(
                $"The rule set did not fire as expected: {counter.FiredRules} fired rules, valid: {result.IsValid}."
            );
        }
    }

    /// <summary>Validates memory.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateMemory() => _validator.Validate(_document.AsSpan()).Issues.Count;

    private static byte[] BuildSchema()
    {
        var sb = new StringBuilder("<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"><ns prefix=\"i\" uri=\"")
            .Append(SchematronBenchmarkInput.Namespace)
            .Append("\"/>");
        for (var i = 0; i < RuleCount; i++)
        {
            _ = sb.Append("<pattern id=\"p")
                .Append(i.ToString(CultureInfo.InvariantCulture))
                .Append("\"><rule context=\"")
                .Append(SchematronBenchmarkInput.Context)
                .Append("\"><let name=\"p\" value=\"number(i:price)\"/><let name=\"q\" value=\"number(i:qty)\"/>");
            for (var a = 0; a < AssertsPerRule; a++)
            {
                _ = sb.Append("<assert test=\"$p &gt; -")
                    .Append((a + 1).ToString(CultureInfo.InvariantCulture))
                    .Append(" and $q &gt; 0\">The price is too low.</assert>");
            }

            _ = sb.Append("</rule></pattern>");
        }

        return Encoding.UTF8.GetBytes(sb.Append("</schema>").ToString());
    }
}
