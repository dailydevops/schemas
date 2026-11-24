namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

/// <summary>Validation against a rule set of <see cref="SchematronBenchmarkInput.Large"/> rules.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class LargeRuleSetValidationBenchmarks() : ValidationBenchmarks(SchematronBenchmarkInput.Large);
#pragma warning restore CA1515
