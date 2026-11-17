namespace NetEvolve.Schemas.Xml.Datatypes.Floating;

using System;
using System.Globalization;
using System.Numerics;

/// <summary>
/// Exact conversion between decimal and binary floating point. Both directions use <see cref="BigInteger"/> arithmetic only, so the
/// results are identical on every target framework and never depend on the runtime's own parser or formatter.
/// </summary>
/// <remarks>Limitation: every call uses arbitrary-precision arithmetic, there is no fast path for short inputs; the benchmarks are DT-58.</remarks>
internal static class BinaryFloat
{
    private const ulong HiddenBit = 1UL << 52;

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
