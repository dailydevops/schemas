namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

/// <summary>Validation against a rule set of <see cref="SchematronBenchmarkInput.Small"/> rule.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class SmallRuleSetValidationBenchmarks() : ValidationBenchmarks(SchematronBenchmarkInput.Small);
#pragma warning restore CA1515
