namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

/// <summary>Validation against a rule set of <see cref="SchematronBenchmarkInput.Medium"/> rules.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class MediumRuleSetValidationBenchmarks() : ValidationBenchmarks(SchematronBenchmarkInput.Medium);
#pragma warning restore CA1515
