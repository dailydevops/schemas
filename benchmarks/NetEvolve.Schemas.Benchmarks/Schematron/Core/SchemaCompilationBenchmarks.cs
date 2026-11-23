namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Schematron;

/// <summary>
/// Compilation of a synthetic schema (parse, expand, compile) at three sizes. There is no <c>System.Xml</c> equivalent,
/// so the first measured value is the target and a 5 % regression limit applies.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class SchemaCompilationBenchmarks
#pragma warning restore CA1515
{
    private readonly byte[] _small = SchematronBenchmarkInput.Schema(SchematronBenchmarkInput.Small);
    private readonly byte[] _medium = SchematronBenchmarkInput.Schema(SchematronBenchmarkInput.Medium);
    private readonly byte[] _large = SchematronBenchmarkInput.Schema(SchematronBenchmarkInput.Large);

    /// <summary>Compiles the small rule set.</summary>
    /// <returns>The validator.</returns>
    [Benchmark]
    public ISchematronValidator CompileSmall() => SchemaValidator.ForSchematron(_small).Build();

    /// <summary>Compiles the medium rule set.</summary>
    /// <returns>The validator.</returns>
    [Benchmark]
    public ISchematronValidator CompileMedium() => SchemaValidator.ForSchematron(_medium).Build();

    /// <summary>Compiles the large rule set.</summary>
    /// <returns>The validator.</returns>
    [Benchmark]
    public ISchematronValidator CompileLarge() => SchemaValidator.ForSchematron(_large).Build();
}
