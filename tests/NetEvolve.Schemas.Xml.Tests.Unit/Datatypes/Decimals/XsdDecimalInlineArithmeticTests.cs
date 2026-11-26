namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Decimals;

using System;
using System.Collections.Generic;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>Differential tests of the 64-bit fast paths of add, subtract, multiply, and compare against a BigInteger reference.</summary>
public class XsdDecimalInlineArithmeticTests
{
    private static readonly BigInteger[] Mantissas =
    [
        0,
        1,
        5,
        99,
        uint.MaxValue,
        (BigInteger)uint.MaxValue + 1,
        BigInteger.Pow(10, 18) - 1,
        BigInteger.Pow(10, 18),
        BigInteger.Pow(2, 63) - 1,
        BigInteger.Pow(2, 63),
        BigInteger.Pow(10, 19) - 1,
        BigInteger.Pow(10, 19),
        BigInteger.Pow(2, 64) - 1,
        BigInteger.Pow(2, 64),
        BigInteger.Pow(10, 20) + 7,
    ];

    private static readonly int[] Scales = [-20, -1, 0, 1, 2, 18, 19, 20, 25];

    private static List<XsdDecimal> Operands()
    {
        var operands = new List<XsdDecimal>();
        foreach (var mantissa in Mantissas)
        {
            foreach (var scale in Scales)
            {
                operands.Add(new XsdDecimal(mantissa, scale));
                operands.Add(new XsdDecimal(-mantissa, scale));
            }
        }

        return operands;
    }

    private static BigInteger Align(XsdDecimal value, int scale) =>
        value.Mantissa * BigInteger.Pow(10, scale - value.Scale);

    private static XsdDecimal ReferenceAdd(XsdDecimal a, XsdDecimal b)
    {
        var scale = Math.Max(a.Scale, b.Scale);
        return new XsdDecimal(Align(a, scale) + Align(b, scale), scale);
    }

    [Test]
    public async Task Add_MatchesBigIntegerReference()
    {
        var operands = Operands();
        var mismatches = 0;
        foreach (var a in operands)
        {
            foreach (var b in operands)
            {
                if (a.Add(b) != ReferenceAdd(a, b))
                {
                    mismatches++;
                }
            }
        }

        await Assert.That(mismatches).IsEqualTo(0);
    }

    [Test]
    public async Task Subtract_MatchesBigIntegerReference()
    {
        var operands = Operands();
        var mismatches = 0;
        foreach (var a in operands)
        {
            foreach (var b in operands)
            {
                if (a.Subtract(b) != ReferenceAdd(a, new XsdDecimal(-b.Mantissa, b.Scale)))
                {
                    mismatches++;
                }
            }
        }

        await Assert.That(mismatches).IsEqualTo(0);
    }

    [Test]
    public async Task Multiply_MatchesBigIntegerReference()
    {
        var operands = Operands();
        var mismatches = 0;
        foreach (var a in operands)
        {
            foreach (var b in operands)
            {
                if (a.Multiply(b) != new XsdDecimal(a.Mantissa * b.Mantissa, a.Scale + b.Scale))
                {
                    mismatches++;
                }
            }
        }

        await Assert.That(mismatches).IsEqualTo(0);
    }

    [Test]
    public async Task CompareTo_MatchesBigIntegerReference()
    {
        var operands = Operands();
        var mismatches = 0;
        foreach (var a in operands)
        {
            foreach (var b in operands)
            {
                var scale = Math.Max(a.Scale, b.Scale);
                var expected = Align(a, scale).CompareTo(Align(b, scale));
                if (Math.Sign(a.CompareTo(b)) != expected)
                {
                    mismatches++;
                }
            }
        }

        await Assert.That(mismatches).IsEqualTo(0);
    }

    [Test]
    public async Task Add_OverflowToWideMagnitude_FallsBack()
    {
        var max = new XsdDecimal(ulong.MaxValue, 0);
        await Assert.That(max.Add(max).ToString()).IsEqualTo("36893488147419103230");
        await Assert.That(max.Add(new XsdDecimal(1, 0)).ToString()).IsEqualTo("18446744073709551616");
        await Assert.That(max.Add(new XsdDecimal(1, 1)).ToString()).IsEqualTo("18446744073709551615.1");
        await Assert.That(max.Multiply(max).ToString()).IsEqualTo("340282366920938463426481119284349108225");
    }

    [Test]
    public async Task Add_ScaleUnderflowAfterStripping_Throws()
    {
        var five = new XsdDecimal(5, int.MinValue);
        await Assert.That(() => five.Add(five)).Throws<OverflowException>();
        await Assert.That(() => five.Multiply(new XsdDecimal(2, int.MinValue))).Throws<OverflowException>();
        await Assert
            .That(() => new XsdDecimal(3, int.MaxValue).Multiply(new XsdDecimal(3, 1)))
            .Throws<OverflowException>();
    }
}
