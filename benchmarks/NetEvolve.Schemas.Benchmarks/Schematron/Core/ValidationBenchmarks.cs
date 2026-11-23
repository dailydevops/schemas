namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Schematron;

/// <summary>
/// Validation of the document against a synthetic rule set, from a stream and from memory, against <c>System.Xml</c>
/// evaluating the same contexts and tests on the same document. Fired rules per run are
/// <c>ruleCount * SchematronBenchmarkInput.LineCount</c>; divide the allocated bytes of the report by that number for the
/// allocation per fired rule. Subclasses fix the size, so every size is its own benchmark class with its own baseline.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public abstract class ValidationBenchmarks(int ruleCount)
#pragma warning restore CA1515
{
    private readonly byte[] _document = SchematronBenchmarkInput.Document;
    private readonly SystemXmlXPathEvaluation _systemXml = new(ruleCount);
    private ISchematronValidator _validator = null!;

    /// <summary>Builds the validator and proves that the library and the baseline fire the same rules.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _validator = SchemaValidator.ForSchematron(SchematronBenchmarkInput.Schema(ruleCount)).Build();
        var counter = new FiredRuleCounter();
        var result = _validator.WithReportSink(counter).Validate(_document.AsSpan());
        var expected = ruleCount * SchematronBenchmarkInput.LineCount;
        var baseline = _systemXml.Evaluate(_document);
        if (!result.IsValid || counter.FiredRules != expected || baseline != expected)
        {
            throw new InvalidOperationException(
                $"The engines disagree: expected {expected} fired rules, the library fired {counter.FiredRules}, System.Xml {baseline}."
            );
        }
    }

    /// <summary>Baseline: <c>System.Xml</c> parses and evaluates the contexts and tests.</summary>
    /// <returns>The number of fired rules.</returns>
    [Benchmark(Baseline = true)]
    public int ValidateSystemXml() => _systemXml.Evaluate(_document);

    /// <summary>Validates a stream.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateStream()
    {
        using var stream = new MemoryStream(_document, writable: false);

        return _validator.Validate(stream).Issues.Count;
    }

    /// <summary>Validates memory.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateMemory() => _validator.Validate(_document.AsSpan()).Issues.Count;
}
