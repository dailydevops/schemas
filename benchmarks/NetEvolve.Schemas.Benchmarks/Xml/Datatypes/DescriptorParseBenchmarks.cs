namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>Parses decimal, dateTime, and double forms through the atomic descriptor, the path of instance validation.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DescriptorParseBenchmarks
#pragma warning restore CA1515
{
    private readonly AtomicDescriptor<XsdDecimal> _decimal =
        (AtomicDescriptor<XsdDecimal>)BuiltInDatatypes.For(XsdVersion.V11).Get("decimal");

    private readonly AtomicDescriptor<XsdDateTime> _dateTime =
        (AtomicDescriptor<XsdDateTime>)BuiltInDatatypes.For(XsdVersion.V11).Get("dateTime");

    private readonly AtomicDescriptor<double> _double =
        (AtomicDescriptor<double>)BuiltInDatatypes.For(XsdVersion.V11).Get("double");

    private string _decimalLexical = "";
    private string _dateTimeLexical = "";
    private string _doubleLexical = "";

    /// <summary>Gets or sets the input set: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the lexical forms and proves that the descriptors accept them.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _decimalLexical = DatatypeBenchmarkInput.Decimal(Size);
        _dateTimeLexical = DatatypeBenchmarkInput.DateTime(Size);
        _doubleLexical = DatatypeBenchmarkInput.Double(Size);
        if (
            !_decimal.Parse(_decimalLexical.AsSpan()).Success
            || !_dateTime.Parse(_dateTimeLexical.AsSpan()).Success
            || !_double.Parse(_doubleLexical.AsSpan()).Success
        )
        {
            throw new InvalidOperationException("The library rejects an input form.");
        }
    }

    /// <summary>Parses a decimal through its descriptor.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseDecimal() => _decimal.Parse(_decimalLexical.AsSpan()).Success;

    /// <summary>Parses a dateTime through its descriptor.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseDateTime() => _dateTime.Parse(_dateTimeLexical.AsSpan()).Success;

    /// <summary>Parses a double through its descriptor.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseDouble() => _double.Parse(_doubleLexical.AsSpan()).Success;
}
