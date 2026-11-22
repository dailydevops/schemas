namespace NetEvolve.Schemas.Benchmarks.Xml.Dtd;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

/// <summary>
/// Validating parse of a document with an internal subset, against the <c>System.Xml</c> validating reader on the same
/// bytes. Limitation: it measures the internal validating pipeline until the public entry point of XML-69 lands.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class InternalSubsetValidationBenchmarks
#pragma warning restore CA1515
{
    private readonly byte[] _document = DtdBenchmarkInput.InternalSubsetDocument;
    private readonly SystemXmlDtdValidation _systemXml = new(resolver: null);

    /// <summary>Proves that both readers do the same work: each accepts the document as valid.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var events = _systemXml.Validate(_document);
        var result = DtdValidatingPipeline.Validate(_document, documentUri: DtdBenchmarkInput.DocumentUri);
        if (events != 0 || !result.IsWellFormed || result.Issues.Issues.Count != 0)
        {
            throw new InvalidOperationException(
                $"The readers disagree: System.Xml reports {events} events, this library {result.Issues.Issues.Count} issues."
            );
        }
    }

    /// <summary>Baseline: the <c>System.Xml</c> reader validates against the internal subset.</summary>
    /// <returns>The number of validation events.</returns>
    [Benchmark(Baseline = true)]
    public int ValidateInternalSubsetSystemXml() => _systemXml.Validate(_document);

    /// <summary>Validates against the internal subset with the validating reader mode.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateInternalSubset() =>
        DtdValidatingPipeline.Validate(_document, documentUri: DtdBenchmarkInput.DocumentUri).Issues.Issues.Count;
}
