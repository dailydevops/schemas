namespace NetEvolve.Schemas.Xml.Datatypes.Floating;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>
/// Exact conversion between decimal and binary floating point. Both directions use <see cref="BigInteger"/> arithmetic only, so the
/// results are identical on every target framework and never depend on the runtime's own parser or formatter.
/// </summary>
/// <remarks>Limitation: only <see cref="TryRound"/> is allocation-free; every other call uses arbitrary-precision arithmetic; the benchmarks are DT-58.</remarks>
internal static class BinaryFloat
{
    private const ulong HiddenBit = 1UL << 52;
    private const ulong LowHalf = 0xFFFFFFFFUL;

    // 10^19 is the largest power of ten that fits 64 bits; it bounds the decimal exponent of the allocation-free path.
    private const int MaxFastExponent = 19;

    // 10^22 is the largest power of ten that a double holds exactly.
    private const int MaxExactPower = 22;

    private static readonly double[] ExactPowersOfTen =
    [
        1e0,
        1e1,
        1e2,
        1e3,
        1e4,
        1e5,
        1e6,
        1e7,
        1e8,
        1e9,
        1e10,
        1e11,
        1e12,
        1e13,
        1e14,
        1e15,
        1e16,
        1e17,
        1e18,
        1e19,
        1e20,
        1e21,
        1e22,
    ];

    /// <summary>Rounds <c>mantissa x 10^exponent</c> to the nearest binary value, ties to even.</summary>
    /// <param name="mantissa">The positive mantissa.</param>
    /// <param name="exponent">The decimal exponent.</param>
    /// <param name="single">Round to 24 bits instead of 53; the result is still returned as the exactly equal double.</param>
    /// <returns>The rounded magnitude; infinity on overflow, zero on underflow.</returns>
    internal static double Round(BigInteger mantissa, int exponent, bool single)
    {
        var precision = single ? 24 : 53;
        var minExponent = single ? -149 : -1074;
        var maxExponent = single ? 104 : 971;

        // The decimal magnitude lies in [approx, approx + 1] above the exponent; this skips huge powers of ten for hopeless inputs.
        var approx = (long)((BitLength(mantissa) - 1) * 0.30102999566398);
        if (approx + exponent > (single ? 38 : 308))
        {
            return double.PositiveInfinity;
        }

        if (approx + 1 + exponent < (single ? -46 : -324))
        {
            return 0.0;
        }

        var numerator = mantissa;
        var denominator = BigInteger.One;
        if (exponent >= 0)
        {
            numerator *= BigInteger.Pow(10, exponent);
        }
        else
        {
            denominator = BigInteger.Pow(10, -exponent);
        }

        var limit = BigInteger.One << precision;
        var e = Math.Max(BitLength(numerator) - BitLength(denominator) - precision, minExponent);
        var q = Divide(numerator, denominator, e, out var remainder, out var divisor);
        if (q >= limit)
        {
            e++;
            q = Divide(numerator, denominator, e, out remainder, out divisor);
        }

        var comparison = (remainder << 1).CompareTo(divisor);
        if (comparison > 0 || (comparison == 0 && !q.IsEven))
        {
            q += 1;
        }

        if (q >= limit)
        {
            q >>= 1;
            e++;
        }

        return e > maxExponent ? double.PositiveInfinity : Build((ulong)q, e);
    }

    /// <summary>Rounds <c>mantissa x 10^exponent</c> to the nearest binary value, ties to even, in 64-bit integer arithmetic without allocating.</summary>
    /// <param name="mantissa">The positive mantissa.</param>
    /// <param name="exponent">The decimal exponent.</param>
    /// <param name="single">Round to 24 bits instead of 53; the result is still returned as the exactly equal double.</param>
    /// <param name="result">The rounded magnitude.</param>
    /// <returns><see langword="false"/> when the exponent is outside -19 to 19 and the exact-double shortcut does not apply; <see cref="Round"/> then decides.</returns>
    /// <remarks>
    /// Within that window the value lies between 10^-19 and 2^128, far from the subnormal and overflow ranges of both types.
    /// The mantissa is multiplied by 10^exponent exactly (128 bits), or shifted and divided by 10^-exponent so that the quotient has
    /// at least one bit more than the precision; a remainder only adds a sticky bit.
    /// </remarks>
    internal static bool TryRound(ulong mantissa, int exponent, bool single, out double result)
    {
        result = 0.0;

        // Both operands are exact doubles (a mantissa up to 2^53, a power of ten up to 10^22), so one IEEE operation is already the
        // correctly rounded result, ties to even; no 128-bit arithmetic is needed.
        if (!single && mantissa <= HiddenBit << 1 && exponent is >= -MaxExactPower and <= MaxExactPower)
        {
            result =
                exponent >= 0
                    ? (double)mantissa * ExactPowersOfTen[exponent]
                    : (double)mantissa / ExactPowersOfTen[-exponent];
            return true;
        }

        if (exponent is < -MaxFastExponent or > MaxFastExponent)
        {
            return false;
        }

        var precision = single ? 24 : 53;
        var scaled =
            exponent >= 0
                ? ScaleUp(mantissa, exponent, out var binaryExponent, out var sticky)
                : ScaleDown(mantissa, -exponent, precision, out binaryExponent, out sticky);

        var excess = Bits(scaled) - precision;
        if (excess > 0)
        {
            var roundBit = ((scaled >> (excess - 1)) & 1) != 0;
            sticky |= (scaled & ((1UL << (excess - 1)) - 1)) != 0;
            scaled >>= excess;
            binaryExponent += excess;
            if (roundBit && (sticky || (scaled & 1) != 0))
            {
                scaled++;
                if (scaled == 1UL << precision)
                {
                    scaled >>= 1;
                    binaryExponent++;
                }
            }
        }

        // The scaled value has at most 53 bits and the exponent stays far inside the normal range, so both factors are exact.
        result = (double)(long)scaled * BitConverter.Int64BitsToDouble((long)(binaryExponent + 1023) << 52);
        return true;
    }

    /// <summary>Gets the shortest decimal digits that round-trip a finite positive value.</summary>
    /// <param name="value">The positive finite value; for a float, its exactly equal double.</param>
    /// <param name="single">The value is a float.</param>
    /// <param name="exponent">The decimal exponent of the first digit.</param>
    /// <returns>The digits without trailing zeros; of the shortest candidates the one closest to the value.</returns>
    internal static string ShortestDigits(double value, bool single, out int exponent)
    {
        var x = Locate(value, out var numerator, out var denominator);

        var maxDigits = single ? 9 : 17;
        BigInteger chosen = default;
        var k = 0;
        for (var n = 1; n <= maxDigits; n++)
        {
            k = x - n + 1;
            var dividend = numerator;
            var divisor = denominator;
            if (k >= 0)
            {
                divisor *= BigInteger.Pow(10, k);
            }
            else
            {
                dividend *= BigInteger.Pow(10, -k);
            }

            var low = BigInteger.DivRem(dividend, divisor, out var remainder);
            var high = low + 1;
            var lowFits = !low.IsZero && Round(low, k, single) == value;
            var highFits = Round(high, k, single) == value;
            if (!lowFits && !highFits && n < maxDigits)
            {
                continue;
            }

            // Both candidates round-trip at the power-of-two boundaries: take the nearer one, a tie goes to the even digit.
            var comparison = (remainder << 1).CompareTo(divisor);
            chosen = highFits && (!lowFits || comparison > 0 || (comparison == 0 && !low.IsEven)) ? high : low;
            break;
        }

        var digits = chosen.ToString(CultureInfo.InvariantCulture);
        exponent = k + digits.Length - 1;
        var trimmed = digits.TrimEnd('0');
        return trimmed.Length > 0 ? trimmed : "0";
    }

    // Exact value as numerator / denominator and the decimal exponent x with 10^x <= value < 10^(x+1).
    private static int Locate(double value, out BigInteger numerator, out BigInteger denominator)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var biased = (int)((bits >> 52) & 0x7FF);
        var fraction = (ulong)bits & (HiddenBit - 1);
        var e = biased == 0 ? -1074 : biased - 1075;
        numerator = biased == 0 ? fraction : fraction | HiddenBit;
        denominator = BigInteger.One;
        if (e >= 0)
        {
            numerator <<= e;
        }
        else
        {
            denominator <<= -e;
        }

        // Math.Log10 is only an estimate; the exact comparisons below settle the decimal exponent.
        var x = (int)Math.Floor(Math.Log10(value));
        while (ComparePowerOfTen(x, numerator, denominator) > 0)
        {
            x--;
        }

        while (ComparePowerOfTen(x + 1, numerator, denominator) <= 0)
        {
            x++;
        }

        return x;
    }

    private static BigInteger Divide(
        BigInteger numerator,
        BigInteger denominator,
        int exponent,
        out BigInteger remainder,
        out BigInteger divisor
    )
    {
        if (exponent >= 0)
        {
            divisor = denominator << exponent;
            return BigInteger.DivRem(numerator, divisor, out remainder);
        }

        divisor = denominator;
        return BigInteger.DivRem(numerator << -exponent, divisor, out remainder);
    }

    private static double Build(ulong q, int e)
    {
        if (q == 0)
        {
            return 0.0;
        }

        // Normalize to a 53-bit significand; only a double subnormal stays below the hidden bit.
        while (q < HiddenBit && e > -1074)
        {
            q <<= 1;
            e--;
        }

        return BitConverter.Int64BitsToDouble((long)(q + ((ulong)(e + 1074) << 52)));
    }

    // Sign of 10^exponent - numerator / denominator.
    private static int ComparePowerOfTen(int exponent, BigInteger numerator, BigInteger denominator) =>
        exponent >= 0
            ? (BigInteger.Pow(10, exponent) * denominator).CompareTo(numerator)
            : denominator.CompareTo(numerator * BigInteger.Pow(10, -exponent));

    // mantissa x 10^exponent as scaled x 2^binaryExponent plus a sticky bit for the dropped low bits; scaled has at most 64 bits.
    private static ulong ScaleUp(ulong mantissa, int exponent, out int binaryExponent, out bool sticky)
    {
        Multiply(mantissa, XsdDecimal.Pow10[exponent], out var high, out var low);
        var highBits = Bits(high);
        if (highBits == 0)
        {
            binaryExponent = 0;
            sticky = false;
            return low;
        }

        // Keep the top 64 bits of the 64 + highBits bit product; the rest only decides ties.
        binaryExponent = highBits;
        sticky = (highBits == 64 ? low : low & ((1UL << highBits) - 1)) != 0;
        return highBits == 64 ? high : (high << (64 - highBits)) | (low >> highBits);
    }

    // mantissa / 10^exponent as scaled x 2^binaryExponent plus a sticky bit for the remainder; scaled has at least precision + 1 bits.
    private static ulong ScaleDown(ulong mantissa, int exponent, int precision, out int binaryExponent, out bool sticky)
    {
        var divisor = XsdDecimal.Pow10[exponent];
        var shift = Math.Max(0, precision + 1 - (Bits(mantissa) - Bits(divisor)));
        ulong high;
        ulong low;
        if (shift == 0)
        {
            high = 0;
            low = mantissa;
        }
        else if (shift < 64)
        {
            high = mantissa >> (64 - shift);
            low = mantissa << shift;
        }
        else
        {
            high = mantissa << (shift - 64);
            low = 0;
        }

        var quotient = Divide(high, low, divisor, out var remainder);
        binaryExponent = -shift;
        sticky = remainder != 0;
        return quotient;
    }

    // The 128-bit product of two 64-bit values.
    private static void Multiply(ulong a, ulong b, out ulong high, out ulong low)
    {
        var a0 = a & LowHalf;
        var a1 = a >> 32;
        var b0 = b & LowHalf;
        var b1 = b >> 32;
        var p00 = a0 * b0;
        var p01 = a0 * b1;
        var p10 = a1 * b0;
        var middle = (p00 >> 32) + (p01 & LowHalf) + (p10 & LowHalf);
        low = (p00 & LowHalf) | (middle << 32);
        high = (a1 * b1) + (p01 >> 32) + (p10 >> 32) + (middle >> 32);
    }

    // Divides the 128-bit value high:low by divisor; the quotient is known to fit 64 bits.
    private static ulong Divide(ulong high, ulong low, ulong divisor, out ulong remainder)
    {
        if (high == 0)
        {
            remainder = low % divisor;
            return low / divisor;
        }

        ulong quotient = 0;
        ulong rest = 0;
        for (var i = 63 + Bits(high); i >= 0; i--)
        {
            var bit = i >= 64 ? (high >> (i - 64)) & 1 : (low >> i) & 1;
            var carry = rest >> 63;
            rest = (rest << 1) | bit;
            quotient <<= 1;
            if (carry != 0 || rest >= divisor)
            {
                rest -= divisor;
                quotient |= 1;
            }
        }

        remainder = rest;
        return quotient;
    }

    private static int Bits(ulong value)
    {
        var bits = 0;
        if (value >= 1UL << 32)
        {
            bits = 32;
            value >>= 32;
        }

        if (value >= 1UL << 16)
        {
            bits += 16;
            value >>= 16;
        }

        if (value >= 1UL << 8)
        {
            bits += 8;
            value >>= 8;
        }

        while (value != 0)
        {
            bits++;
            value >>= 1;
        }

        return bits;
    }

    private static int BitLength(BigInteger value)
    {
        var bytes = value.ToByteArray();
        var length = bytes.Length;
        while (length > 1 && bytes[length - 1] == 0)
        {
            length--;
        }

        var top = bytes[length - 1];
        var bits = 0;
        while (top != 0)
        {
            bits++;
            top >>= 1;
        }

        return ((length - 1) * 8) + bits;
    }
}
