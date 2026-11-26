namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using System.Numerics;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;

/// <summary>
/// Round, floor, and the conversions to <see cref="double"/> and <see cref="BigInteger"/> on <see cref="XsdDecimal"/> operands of
/// three sizes, the largest beyond 64 bits (the <see cref="BigInteger"/> fallback).
/// There is no <c>System.Xml</c> counterpart, so the recorded values gate regressions (limitation).
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DecimalRoundingBenchmarks
#pragma warning restore CA1515
{
    private XsdDecimal _value;

    /// <summary>Gets or sets the operand: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the operand.</summary>
    [GlobalSetup]
    public void Setup() =>
        _value = XsdDecimal
            .Parse(
                (
                    Size == DatatypeBenchmarkInput.Long
                        ? DatatypeBenchmarkInput.WideDecimal(Size)
                        : DatatypeBenchmarkInput.Decimal(Size)
                ).AsSpan()
            )
            .Value;

    /// <summary>Rounds to an integer, ties toward positive infinity.</summary>
    /// <returns>The rounded value.</returns>
    [Benchmark]
    public XsdDecimal Round() => DecimalArithmetic.Round(_value);

    /// <summary>Gets the floor.</summary>
    /// <returns>The floor.</returns>
    [Benchmark]
    public XsdDecimal Floor() => DecimalArithmetic.Floor(_value);

    /// <summary>Converts to a double.</summary>
    /// <returns>The double.</returns>
    [Benchmark]
    public double ToDouble() => DecimalArithmetic.ToDouble(_value);

    /// <summary>Truncates to an integer.</summary>
    /// <returns>The integer.</returns>
    [Benchmark]
    public BigInteger ToBigInteger() => DecimalArithmetic.ToBigInteger(_value).Value;
}
