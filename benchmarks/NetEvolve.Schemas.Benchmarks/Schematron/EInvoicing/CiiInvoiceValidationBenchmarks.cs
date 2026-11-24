namespace NetEvolve.Schemas.Benchmarks.Schematron.EInvoicing;

/// <summary>Validation of a CEN CII example invoice against the EN 16931 CII rules.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class CiiInvoiceValidationBenchmarks() : InvoiceValidationBenchmarks("cii", "CII_example1.xml");
#pragma warning restore CA1515
