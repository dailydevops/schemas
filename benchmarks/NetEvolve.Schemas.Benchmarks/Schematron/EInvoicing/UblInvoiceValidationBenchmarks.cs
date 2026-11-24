namespace NetEvolve.Schemas.Benchmarks.Schematron.EInvoicing;

/// <summary>Validation of a CEN UBL example invoice against the EN 16931 UBL rules.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class UblInvoiceValidationBenchmarks() : InvoiceValidationBenchmarks("ubl", "ubl-tc434-example1.xml");
#pragma warning restore CA1515
