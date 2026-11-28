namespace NetEvolve.Schemas.Benchmarks.Schematron.EInvoicing;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron;

/// <summary>
/// Compilation of the CEN EN 16931 UBL rule set, which instantiates abstract patterns with several hundred parameters. The
/// archive is fetched by its pinned checksum (see <see cref="CenArchive"/>). There is no <c>System.Xml</c> equivalent.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class UblSchemaBuildBenchmarks
#pragma warning restore CA1515
{
    private byte[] _schema = [];
    private string _schemaPath = string.Empty;

    /// <summary>Fetches the archive and reads the rule set.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _schemaPath = Path.Combine(CenArchive.Fetch(), "ubl", "schematron", "EN16931-UBL-validation.sch");
        _schema = File.ReadAllBytes(_schemaPath);
    }

    /// <summary>Compiles the UBL rule set.</summary>
    /// <returns>The validator.</returns>
    [Benchmark]
    public ISchematronValidator BuildUbl() =>
        SchemaValidator
            .ForSchematron(_schema, new Uri(_schemaPath).AbsoluteUri)
            .WithResolver(new SandboxedFileSystemResourceResolver(Path.GetDirectoryName(_schemaPath)!))
            .Build();
}
