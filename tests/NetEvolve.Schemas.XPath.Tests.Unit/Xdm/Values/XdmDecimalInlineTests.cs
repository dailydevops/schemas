namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Values;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A decimal of at most 19 digits lives in the item itself: creating it and reading it back allocates nothing (#123 section 9).</summary>
public class XdmDecimalInlineTests
{
    private const int Rounds = 64;

    private static long BytesPerRound(XsdDecimal value)
    {
        for (var i = 0; i < 8; i++)
        {
            RoundTrip(value);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Rounds; i++)
        {
            RoundTrip(value);
        }

        return (GC.GetAllocatedBytesForCurrentThread() - before) / Rounds;
    }

    private static bool RoundTrip(XsdDecimal value)
    {
        var item = XdmItem.FromDecimal(value);
        return item.TryGetDecimal(out var read) && read.Equals(value) && item.Equals(XdmItem.FromDecimal(read));
    }

    [Test]
    [Arguments("2.5")]
    [Arguments("-2.5")]
    [Arguments("0")]
    [Arguments("1234567890123456789")]
    [Arguments("-0.0000000000000000001")]
    [Arguments("12345678901234567890123")]
    public async Task XdmItem_Decimal_UpTo19Digits_DoesNotAllocate(string lexical)
    {
        var value = XsdDecimal.Parse(lexical.AsSpan()).Value;

        await Assert.That(RoundTrip(value)).IsTrue();
        var bytes = BytesPerRound(value);
        if (value.TotalDigits <= 19)
        {
            await Assert.That(bytes).IsEqualTo(0);
        }
    }

    [Test]
    [Arguments("100000000000000000000")]
    [Arguments("18446744073709551615")]
    [Arguments("9223372036854775808")]
    public async Task XdmItem_Decimal_BeyondInlineRange_StaysExact(string lexical)
    {
        var value = new XsdDecimal(BigInteger.Parse(lexical, CultureInfo.InvariantCulture), -3);

        await Assert.That(RoundTrip(value)).IsTrue();
        await Assert.That(XdmItem.FromDecimal(value).TryGetDecimal(out var read)).IsTrue();
        await Assert.That(read.Mantissa).IsEqualTo(value.Mantissa);
        await Assert.That(read.Scale).IsEqualTo(value.Scale);
    }

    [Test]
    [Arguments(int.MinValue)]
    [Arguments(-1000)]
    [Arguments(1000)]
    [Arguments(int.MaxValue)]
    public async Task XdmItem_Decimal_ExtremeScale_StaysExact(int scale)
    {
        var value = new XsdDecimal(7, scale);

        await Assert.That(RoundTrip(value)).IsTrue();
        await Assert.That(XdmItem.FromDecimal(value).TryGetDecimal(out var read)).IsTrue();
        await Assert.That(read.Scale).IsEqualTo(value.Scale);
    }
}
