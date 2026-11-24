namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

using System.Buffers;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Svrl;

/// <summary>
/// A validation run that streams its events to an <see cref="SvrlReportSink"/> (ISO/IEC 19757-3:2016, Annex D). There is no
/// <c>System.Xml</c> equivalent, so the first measured value is the target and a 5 % regression limit applies. Limitation:
/// the events are produced by the run, so the figure includes the evaluation of the medium rule set; the report carries
/// one finding per rule and line with a quantity above five.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class SvrlWriteBenchmarks
#pragma warning restore CA1515
{
    private readonly byte[] _document = SchematronBenchmarkInput.Document;
    private readonly ArrayBufferWriter<byte> _output = new(1 << 20);
    private ISchematronValidator _validator = null!;

    /// <summary>Builds the validator and proves that a report is written.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _validator = SchemaValidator
            .ForSchematron(SchematronBenchmarkInput.Schema(SchematronBenchmarkInput.Medium, withReports: true))
            .Build();
        if (WriteSvrl() == 0)
        {
            throw new InvalidOperationException("The sink wrote no SVRL.");
        }
    }

    /// <summary>Validates and writes the SVRL report into a reused buffer.</summary>
    /// <returns>The number of bytes written.</returns>
    [Benchmark]
    public int WriteSvrl()
    {
        _output.Clear();
        using var sink = new SvrlReportSink(_output);
        _ = _validator.WithReportSink(sink).Validate(_document.AsSpan());

        return _output.WrittenCount;
    }
}
