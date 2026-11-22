namespace NetEvolve.Schemas.Benchmarks.Xml.Dtd;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

/// <summary>
/// Validating parse of a document with an external subset that a resolver serves from memory, against the
/// <c>System.Xml</c> validating reader with an in-memory resolver on the same bytes. Neither side touches the network
/// or the filesystem. Limitation: it measures the internal validating pipeline until the public entry point of XML-69 lands.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class ExternalSubsetValidationBenchmarks
#pragma warning restore CA1515
{
    private readonly byte[] _document = DtdBenchmarkInput.ExternalSubsetDocument;
    private readonly InMemoryResolver _resolver = new(DtdBenchmarkInput.ExternalSubset);
    private readonly InMemoryXmlResolver _xmlResolver = new(DtdBenchmarkInput.ExternalSubset);
    private readonly SystemXmlDtdValidation _systemXml;
    private readonly DtdValidationOptions _options;

    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetValidationBenchmarks"/> class.</summary>
    public ExternalSubsetValidationBenchmarks()
    {
        _systemXml = new SystemXmlDtdValidation(_xmlResolver);
        _options = DtdValidationOptions.Default.WithResolver(_resolver).WithAllowExternalSubset(true);
    }

    /// <summary>Proves that both readers do the same work: each requests the subset once and accepts the document as valid.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _resolver.Reset();
        _xmlResolver.Reset();
        var events = _systemXml.Validate(_document);
        var result = DtdValidatingPipeline.Validate(_document, _options, documentUri: DtdBenchmarkInput.DocumentUri);
        if (
            events != 0
            || !result.IsWellFormed
            || result.Issues.Issues.Count != 0
            || _resolver.Requests != 1
            || _xmlResolver.Requests != 1
        )
        {
            throw new InvalidOperationException(
                $"The readers disagree: System.Xml reports {events} events after {_xmlResolver.Requests} requests, this library {result.Issues.Issues.Count} issues after {_resolver.Requests} requests."
            );
        }
    }

    /// <summary>Baseline: the <c>System.Xml</c> reader validates against the external subset.</summary>
    /// <returns>The number of validation events.</returns>
    [Benchmark(Baseline = true)]
    public int ValidateExternalSubsetSystemXml() => _systemXml.Validate(_document);

    /// <summary>Validates against the external subset, requested through the resolver, with the validating reader mode.</summary>
    /// <returns>The number of issues.</returns>
    [Benchmark]
    public int ValidateExternalSubset() =>
        DtdValidatingPipeline
            .Validate(_document, _options, documentUri: DtdBenchmarkInput.DocumentUri)
            .Issues.Issues.Count;
}
