namespace NetEvolve.Schemas.Benchmarks.Datatypes;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;

/// <summary>
/// Add, multiply, divide, and compare on <see cref="XsdDecimal"/> operands of three sizes, the largest beyond the range of
/// <see cref="decimal"/>. There is no <c>System.Xml</c> counterpart, so the recorded values gate regressions (limitation).
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DecimalArithmeticBenchmarks
#pragma warning restore CA1515
{
    private XsdDecimal _left;
    private XsdDecimal _right;

    /// <summary>Gets or sets the operand set: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the operands.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var left =
            Size == DatatypeBenchmarkInput.Long
                ? DatatypeBenchmarkInput.WideDecimal(Size)
                : DatatypeBenchmarkInput.Decimal(Size);
        _left = XsdDecimal.Parse(left.AsSpan()).Value;
        _right = XsdDecimal.Parse("3.75".AsSpan()).Value;
    }

    /// <summary>Adds two values.</summary>
    /// <returns>The sum.</returns>
    [Benchmark]
    public XsdDecimal Add() => _left.Add(_right);

    /// <summary>Multiplies two values.</summary>
    /// <returns>The product.</returns>
    [Benchmark]
    public XsdDecimal Multiply() => _left.Multiply(_right);

    /// <summary>Divides two values at the default number of fraction digits.</summary>
    /// <returns>The quotient.</returns>
    [Benchmark]
    public XsdDecimal Divide() => DecimalArithmetic.Divide(_left, _right).Value;

    /// <summary>Compares two values.</summary>
    /// <returns>The sign of the comparison.</returns>
    [Benchmark]
    public int Compare() => _left.CompareTo(_right);
}
