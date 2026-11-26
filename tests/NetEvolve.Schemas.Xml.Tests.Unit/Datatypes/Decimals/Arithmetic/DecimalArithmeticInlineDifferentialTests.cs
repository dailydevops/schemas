namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Decimals.Arithmetic;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;

/// <summary>Compares the 64-bit paths of rounding and conversion with a BigInteger and string reference over boundary values.</summary>
public class DecimalArithmeticInlineDifferentialTests
{
    private static readonly int[] Scales = BuildRange(-20, 25);
    private static readonly int[] Precisions = BuildRange(-5, 25);
    private static readonly BigInteger[] Mantissas = BuildMantissas();

    private enum Mode
    {
        HalfUp,
        HalfEven,
        Floor,
        Ceiling,
    }

    [Test]
    public async Task Round_AllModes_MatchBigIntegerReference()
    {
        var mismatch = FirstMismatch(
            (v, p) => (DecimalArithmetic.Round(v, p), Reference(v, p, Mode.HalfUp)),
            (v, p) => (DecimalArithmetic.RoundHalfToEven(v, p), Reference(v, p, Mode.HalfEven)),
            (v, _) => (DecimalArithmetic.Floor(v), Reference(v, 0, Mode.Floor)),
            (v, _) => (DecimalArithmetic.Ceiling(v), Reference(v, 0, Mode.Ceiling))
        );

        await Assert.That(mismatch).IsNull();
    }

    [Test]
    public async Task ToDouble_MatchesParsedString_BitForBit()
    {
        string? mismatch = null;
        foreach (var value in Values())
        {
            var actual = DecimalArithmetic.ToDouble(value);
            var expected =
                value.Sign == 0 ? 0d : double.Parse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
            if (BitConverter.DoubleToInt64Bits(actual) != BitConverter.DoubleToInt64Bits(expected))
            {
                mismatch = $"{value}: {actual:R} vs {expected:R}";
                break;
            }
        }

        await Assert.That(mismatch).IsNull();
    }

    [Test]
    public async Task ToBigInteger_MatchesBigIntegerReference()
    {
        string? mismatch = null;
        foreach (var value in Values())
        {
            var scale = value.Scale;
            var expected =
                scale <= 0
                    ? value.Mantissa * BigInteger.Pow(10, -scale)
                    : BigInteger.Divide(value.Mantissa, BigInteger.Pow(10, scale));
            var actual = DecimalArithmetic.ToBigInteger(value).Value;
            if (actual != expected)
            {
                mismatch = $"{value}: {actual} vs {expected}";
                break;
            }
        }

        await Assert.That(mismatch).IsNull();
    }

    [Test]
    public async Task Round_ResultScaleBelowIntRange_ThrowsOverflow()
    {
        var value = new XsdDecimal(new BigInteger(95), int.MinValue + 1);

        await Assert.That(() => DecimalArithmetic.Round(value, int.MinValue)).Throws<OverflowException>();
    }

    [Test]
    public async Task ToDouble_InlineValue_DoesNotAllocate()
    {
        var value = XsdDecimal.Parse("12345.6789".AsSpan()).Value;

        await Assert.That(MeasureToDoubleAllocation(value)).IsEqualTo(0L);
    }

    private static long MeasureToDoubleAllocation(XsdDecimal value)
    {
        _ = DecimalArithmetic.ToDouble(value);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            _ = DecimalArithmetic.ToDouble(value);
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static string? FirstMismatch(
        params Func<XsdDecimal, int, (XsdDecimal Actual, XsdDecimal Expected)>[] checks
    )
    {
        foreach (var value in Values())
        {
            foreach (var precision in Precisions)
            {
                foreach (var check in checks)
                {
                    var (actual, expected) = check(value, precision);
                    if (actual != expected || actual.Scale != expected.Scale)
                    {
                        return $"{value} @ {precision}: {actual} (scale {actual.Scale}) vs {expected} (scale {expected.Scale})";
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<XsdDecimal> Values()
    {
        foreach (var mantissa in Mantissas)
        {
            foreach (var scale in Scales)
            {
                yield return new XsdDecimal(mantissa, scale);
                yield return new XsdDecimal(-mantissa, scale);
            }
        }
    }

    // The old implementation: BigInteger quotient and remainder at a power of ten.
    private static XsdDecimal Reference(XsdDecimal value, int precision, Mode mode)
    {
        if (value.Sign == 0 || value.Scale <= precision)
        {
            return value;
        }

        var drop = value.Scale - precision;
        var magnitude = BigInteger.Abs(value.Mantissa);
        var unit = BigInteger.Pow(10, drop);
        var quotient = BigInteger.DivRem(magnitude, unit, out var remainder);
        var half = remainder.IsZero ? -2 : (remainder * 2).CompareTo(unit);
        var increment = mode switch
        {
            Mode.HalfUp => half > 0 || (half == 0 && value.Sign > 0),
            Mode.HalfEven => half > 0 || (half == 0 && !quotient.IsEven),
            Mode.Floor => value.Sign < 0 && half != -2,
            _ => value.Sign > 0 && half != -2,
        };

        if (increment)
        {
            quotient += BigInteger.One;
        }

        return new XsdDecimal(value.Sign < 0 ? -quotient : quotient, precision);
    }

    private static int[] BuildRange(int from, int to)
    {
        var range = new int[to - from + 1];
        for (var i = 0; i < range.Length; i++)
        {
            range[i] = from + i;
        }

        return range;
    }

    private static BigInteger[] BuildMantissas()
    {
        var set = new HashSet<BigInteger> { 0, 1, 2, 3, 5, 15, 25, 35, 95, 99, 999, 12345, 125, 135 };
        var anchors = new List<BigInteger> { BigInteger.One << 53, BigInteger.One << 63, BigInteger.One << 64 };
        for (var power = 1; power <= 21; power++)
        {
            anchors.Add(BigInteger.Pow(10, power));
            anchors.Add(BigInteger.Pow(10, power) * 5);
        }

        foreach (var anchor in anchors)
        {
            set.Add(anchor);
            set.Add(anchor - 1);
            set.Add(anchor + 1);
            set.Add(anchor + (anchor / 2));
        }

        set.Remove(-1);
        var result = new List<BigInteger>(set);
        result.Sort();
        return [.. result];
    }
}
