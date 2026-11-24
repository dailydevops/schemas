namespace NetEvolve.Schemas.Benchmarks.Schematron.EInvoicing;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron;

/// <summary>
/// Validation of one CEN EN 16931 example invoice against the CEN rule set, from a stream and from memory, against
/// <c>System.Xml</c> evaluating the XPath 1.0 subset of the same rules on the same invoice. The archive is fetched by its
/// pinned checksum (see <see cref="CenArchive"/>); the rule set is compiled once in the setup, outside the measured run.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public abstract class InvoiceValidationBenchmarks(string syntax, string example)
#pragma warning restore CA1515
{
    private byte[] _document = [];
    private SystemXmlRuleEvaluation _systemXml = null!;
    private ISchematronValidator _validator = null!;

    /// <summary>Fetches the archive, compiles the rule set, and proves that the invoice is valid.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var root = CenArchive.Fetch();
        var schemaPath = Path.Combine(
            root,
            syntax,
            "schematron",
            $"EN16931-{syntax.ToUpperInvariant()}-validation.sch"
        );
        _document = File.ReadAllBytes(Path.Combine(root, syntax, "examples", example));
        _validator = SchemaValidator
            .ForSchematron(File.ReadAllBytes(schemaPath), new Uri(schemaPath).AbsoluteUri)
            .WithResolver(new SandboxedFileSystemResourceResolver(Path.GetDirectoryName(schemaPath)!))
            .Build();
        _systemXml = new SystemXmlRuleEvaluation(_validator.Schema, _document);
        if (!_validator.Validate(_document.AsSpan()).IsValid || _systemXml.RuleCount == 0)
        {
            throw new InvalidOperationException(
                $"The example '{example}' must be valid and the baseline must evaluate rules."
            );
        }
    }

    /// <summary>Baseline: <c>System.Xml</c> parses the invoice and evaluates the XPath 1.0 subset of the rules.</summary>
    /// <returns>The number of evaluated tests.</returns>
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
