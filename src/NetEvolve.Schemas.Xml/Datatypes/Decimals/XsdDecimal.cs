namespace NetEvolve.Schemas.Xml.Datatypes.Decimals;

using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// The value space of <c>xs:decimal</c>: a sign, a mantissa, and a scale, value = mantissa x 10^-scale, exact and always normalized
/// (no trailing zeros in the mantissa, zero has scale 0), so equality and the hash code ignore trailing zeros.
/// </summary>
/// <remarks>
/// A magnitude of up to 64 bits is kept inline in a <see cref="ulong"/>; only a larger magnitude uses a <see cref="BigInteger"/>.
/// Limitations: parsing caps the digit count (<see cref="DefaultMaxDigits"/> by default, DT0010); the scale of a result is a
/// 32-bit integer and an overflow throws <see cref="OverflowException"/>; division, rounding, and conversions are not part of this type yet.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XsdDecimal : IEquatable<XsdDecimal>, IComparable<XsdDecimal>
{
    /// <summary>The default maximum number of digits of a lexical form.</summary>
    public const int DefaultMaxDigits = 4096;

    private const int ChunkDigits = 18;

    internal static readonly ulong[] Pow10 =
    [
        1UL,
        10UL,
        100UL,
        1_000UL,
        10_000UL,
        100_000UL,
        1_000_000UL,
        10_000_000UL,
        100_000_000UL,
        1_000_000_000UL,
        10_000_000_000UL,
        100_000_000_000UL,
        1_000_000_000_000UL,
        10_000_000_000_000UL,
        100_000_000_000_000UL,
        1_000_000_000_000_000UL,
        10_000_000_000_000_000UL,
        100_000_000_000_000_000UL,
        1_000_000_000_000_000_000UL,
        10_000_000_000_000_000_000UL,
    ];

    private readonly int _sign;
    private readonly ulong _mag;
    private readonly BigInteger _big;

    private XsdDecimal(int sign, ulong mag, BigInteger big, int scale)
    {
        _sign = sign;
        _mag = mag;
        _big = big;
        Scale = scale;
    }

    /// <summary>Initializes a new instance of the <see cref="XsdDecimal"/> struct as <paramref name="mantissa"/> x 10^-<paramref name="scale"/>, normalized.</summary>
    /// <param name="mantissa">The mantissa.</param>
    /// <param name="scale">The scale, negative for a multiple of a power of ten.</param>
    /// <remarks>Arithmetic aligns operands by scaling with a power of ten, so the cost grows with the scale difference; callers handling untrusted scales must bound them.</remarks>
    /// <exception cref="OverflowException">The normalized scale does not fit a 32-bit integer.</exception>
    public XsdDecimal(BigInteger mantissa, int scale)
    {
        if (mantissa.IsZero)
        {
            _sign = 0;
            _mag = 0;
            _big = default;
            Scale = 0;
            return;
        }

        _sign = mantissa.Sign;
        var magnitude = BigInteger.Abs(mantissa);
        if (magnitude <= ulong.MaxValue)
        {
            var small = (ulong)magnitude;
            while (small % 10 == 0)
            {
                small /= 10;
                scale = checked(scale - 1);
            }

            _mag = small;
            _big = default;
        }
        else
        {
            while (true)
            {
                var quotient = BigInteger.DivRem(magnitude, 10, out var remainder);
                if (!remainder.IsZero)
                {
                    break;
                }

                magnitude = quotient;
                scale = checked(scale - 1);
            }

            // The magnitude can drop to 64 bits after stripping; keep the inline-iff-fits invariant.
            if (magnitude <= ulong.MaxValue)
            {
                _mag = (ulong)magnitude;
                _big = default;
            }
            else
            {
                _mag = 0;
                _big = magnitude;
            }
        }

        Scale = scale;
    }

    /// <summary>Creates a normalized value from a magnitude kept inline, without a <see cref="BigInteger"/>.</summary>
    /// <param name="negative"><see langword="true"/> for a negative value.</param>
    /// <param name="magnitude">The magnitude of the mantissa.</param>
    /// <param name="scale">The scale before normalization, between 0 and 19.</param>
    /// <returns>The value, normalized as the public constructor does.</returns>
    internal static XsdDecimal FromInline(bool negative, ulong magnitude, int scale)
    {
        if (magnitude == 0)
        {
            return default;
        }

        while (magnitude % 10 == 0)
        {
            magnitude /= 10;
            scale--;
        }

        return new XsdDecimal(negative ? -1 : 1, magnitude, default, scale);
    }

    /// <summary>Gets the value zero.</summary>
    public static XsdDecimal Zero => default;

    /// <summary>Gets the scale of the normalized value; negative when the value is a multiple of a power of ten.</summary>
    public int Scale { get; }

    /// <summary>Gets the sign: -1, 0, or 1.</summary>
    public int Sign => _sign;

    /// <summary>Gets the signed mantissa of the normalized value.</summary>
    public BigInteger Mantissa => _sign < 0 ? -Magnitude : Magnitude;

    /// <summary>Gets the number of digits, defined by the <c>totalDigits</c> facet; zero counts as one digit.</summary>
    public int TotalDigits
    {
        get
        {
            if (_sign == 0)
            {
                return 1;
            }

            var digits = MantissaDigits();
            return Scale <= 0 ? digits - Scale : Math.Max(digits, Scale);
        }
    }

    /// <summary>Gets the number of fraction digits, defined by the <c>fractionDigits</c> facet.</summary>
    public int FractionDigits => Scale > 0 ? Scale : 0;

    private BigInteger Magnitude => _big.IsZero ? new BigInteger(_mag) : _big;

    /// <summary>Gets the magnitude of the mantissa without creating a <see cref="BigInteger"/>.</summary>
    /// <param name="magnitude">The magnitude, when it is kept inline.</param>
    /// <returns><see langword="true"/> when the magnitude fits 64 bits.</returns>
    internal bool TryGetInlineMagnitude(out ulong magnitude)
    {
        magnitude = _mag;
        return _big.IsZero;
    }

    /// <summary>Parses the <c>xs:decimal</c> lexical space: an optional sign, digits, and at most one decimal point with at least one digit; no exponent and no whitespace.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="maxDigits">The maximum number of digits, leading and trailing zeros included.</param>
    /// <returns>The value, or <see cref="DatatypeError.InvalidLexicalForm"/>, <see cref="DatatypeError.InvalidCharacter"/>, or <see cref="DatatypeError.DigitCap"/> with the offset.</returns>
    public static ParseResult<XsdDecimal> Parse(ReadOnlySpan<char> lexical, int maxDigits = DefaultMaxDigits)
    {
        var negative = lexical.Length > 0 && lexical[0] == '-';
        var start = lexical.Length > 0 && (lexical[0] == '+' || negative) ? 1 : 0;
        var error = Scan(lexical, start, maxDigits, out var digits, out var errorOffset);
        if (error != DatatypeError.None)
        {
            return new ParseResult<XsdDecimal>(error, errorOffset);
        }

        if (digits.Total == 0)
        {
            return new ParseResult<XsdDecimal>(DatatypeError.InvalidLexicalForm, lexical.Length);
        }

        if (digits.First < 0)
        {
            return new ParseResult<XsdDecimal>(default);
        }

        // Digits after the last nonzero digit are trailing zeros of the mantissa: dropping them lowers the scale.
        var scale = digits.Fraction - (digits.Total - 1 - digits.Last);
        var sign = negative ? -1 : 1;
        if (digits.Last - digits.First < 19)
        {
            return new ParseResult<XsdDecimal>(
                new XsdDecimal(sign, BuildInline(lexical, start, digits), default, scale)
            );
        }

#if NET
        // 38 digits cannot overflow 128 bits: one conversion replaces a BigInteger multiply and add per chunk.
        if (digits.Last - digits.First < MaxWideDigits)
        {
            var wide = BuildWide(lexical, start, digits);
            return new ParseResult<XsdDecimal>(
                wide <= ulong.MaxValue
                    ? new XsdDecimal(sign, (ulong)wide, default, scale)
                    : new XsdDecimal(sign, 0, (BigInteger)wide, scale)
            );
        }
#endif

        var big = BuildBig(lexical, start, digits);

        // A 20-digit magnitude can still fit 64 bits.
        return new ParseResult<XsdDecimal>(
            big <= ulong.MaxValue
                ? new XsdDecimal(sign, (ulong)big, default, scale)
                : new XsdDecimal(sign, 0, big, scale)
        );
    }

    /// <summary>Adds two values exactly.</summary>
    /// <param name="other">The other value.</param>
    /// <returns>The sum.</returns>
    public XsdDecimal Add(XsdDecimal other)
    {
        if (_big.IsZero && other._big.IsZero && TryAddInline(other, out var inline))
        {
            return inline;
        }

        var scale = Math.Max(Scale, other.Scale);
        return new XsdDecimal(Aligned(scale) + other.Aligned(scale), scale);
    }

    /// <summary>Subtracts a value exactly.</summary>
    /// <param name="other">The value to subtract.</param>
    /// <returns>The difference.</returns>
    public XsdDecimal Subtract(XsdDecimal other) => Add(other.Negate());

    /// <summary>Multiplies two values exactly.</summary>
    /// <param name="other">The other value.</param>
    /// <returns>The product.</returns>
    /// <exception cref="OverflowException">The scale of the product does not fit a 32-bit integer.</exception>
    public XsdDecimal Multiply(XsdDecimal other) =>
        _big.IsZero && other._big.IsZero && TryMultiplyInline(other, out var inline)
            ? inline
            : new(Mantissa * other.Mantissa, checked(Scale + other.Scale));

    /// <summary>Negates the value.</summary>
    /// <returns>The value with the opposite sign; zero stays zero.</returns>
    public XsdDecimal Negate() => new(-_sign, _mag, _big, Scale);

    /// <summary>Compares two values in the value space; scale and trailing zeros do not matter.</summary>
    /// <param name="other">The other value.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareTo(XsdDecimal other)
    {
        if (_sign != other._sign)
        {
            return _sign < other._sign ? -1 : 1;
        }

        if (_sign == 0)
        {
            return 0;
        }

        var magnitude = CompareMagnitude(other);
        return _sign < 0 ? -magnitude : magnitude;
    }

    /// <inheritdoc />
    public bool Equals(XsdDecimal other) =>
        _sign == other._sign && Scale == other.Scale && _mag == other._mag && _big == other._big;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdDecimal other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked((((((_sign * 397) ^ Scale) * 397) ^ _mag.GetHashCode()) * 397) ^ _big.GetHashCode());

    /// <summary>Gets the canonical lexical form: no plus sign, no leading or trailing zeros except <c>0</c> before the point, and no point for an integer.</summary>
    /// <returns>The canonical form.</returns>
    public override string ToString()
    {
        if (_sign == 0)
        {
            return "0";
        }

        var digits = _big.IsZero
            ? _mag.ToString(CultureInfo.InvariantCulture)
            : _big.ToString(CultureInfo.InvariantCulture);
        var builder = new StringBuilder(digits.Length + Math.Abs(Scale) + 3);
        if (_sign < 0)
        {
            _ = builder.Append('-');
        }

        if (Scale <= 0)
        {
            _ = builder.Append(digits).Append('0', -Scale);
        }
        else if (Scale < digits.Length)
        {
            _ = builder
                .Append(digits, 0, digits.Length - Scale)
                .Append('.')
                .Append(digits, digits.Length - Scale, Scale);
        }
        else
        {
            _ = builder.Append("0.").Append('0', Scale - digits.Length).Append(digits);
        }

        return builder.ToString();
    }

    /// <summary>Determines whether two values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XsdDecimal left, XsdDecimal right) => left.Equals(right);

    /// <summary>Determines whether two values differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XsdDecimal left, XsdDecimal right) => !left.Equals(right);

    /// <summary>Determines whether the first value is less than the second.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when less.</returns>
    public static bool operator <(XsdDecimal left, XsdDecimal right) => left.CompareTo(right) < 0;

    /// <summary>Determines whether the first value is less than or equal to the second.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when less or equal.</returns>
    public static bool operator <=(XsdDecimal left, XsdDecimal right) => left.CompareTo(right) <= 0;

    /// <summary>Determines whether the first value is greater than the second.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when greater.</returns>
    public static bool operator >(XsdDecimal left, XsdDecimal right) => left.CompareTo(right) > 0;

    /// <summary>Determines whether the first value is greater than or equal to the second.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when greater or equal.</returns>
    public static bool operator >=(XsdDecimal left, XsdDecimal right) => left.CompareTo(right) >= 0;

    // Both magnitudes are inline. Anything that does not fit 64 bits, or could overflow the 32-bit scale while stripping zeros, takes the BigInteger path.
    private bool TryAddInline(XsdDecimal other, out XsdDecimal result)
    {
        result = default;
        if (_sign == 0 || other._sign == 0)
        {
            result = _sign == 0 ? other : this;
            return true;
        }

        var scale = Math.Max(Scale, other.Scale);
        if (
            scale < int.MinValue + Pow10.Length
            || !TryScaleUp(_mag, (long)scale - Scale, out var left)
            || !TryScaleUp(other._mag, (long)scale - other.Scale, out var right)
        )
        {
            return false;
        }

        if (_sign == other._sign)
        {
            var sum = left + right;
            if (sum < left)
            {
                return false;
            }

            result = FromInline(_sign < 0, sum, scale);
        }
        else
        {
            result =
                left >= right
                    ? FromInline(_sign < 0, left - right, scale)
                    : FromInline(other._sign < 0, right - left, scale);
        }

        return true;
    }

    private bool TryMultiplyInline(XsdDecimal other, out XsdDecimal result)
    {
        result = default;
        var scale = (long)Scale + other.Scale;
        if (scale < int.MinValue + Pow10.Length || scale > int.MaxValue)
        {
            return false;
        }

        if (_sign == 0 || other._sign == 0)
        {
            return true;
        }

#if NET
        var high = Math.BigMul(_mag, other._mag, out var low);
#else
        var a0 = _mag & 0xFFFFFFFFUL;
        var a1 = _mag >> 32;
        var b0 = other._mag & 0xFFFFFFFFUL;
        var b1 = other._mag >> 32;
        var p00 = a0 * b0;
        var p01 = a0 * b1;
        var p10 = a1 * b0;
        var middle = (p00 >> 32) + (p01 & 0xFFFFFFFFUL) + (p10 & 0xFFFFFFFFUL);
        var low = (p00 & 0xFFFFFFFFUL) | (middle << 32);
        var high = (a1 * b1) + (p01 >> 32) + (p10 >> 32) + (middle >> 32);
#endif
        if (high != 0)
        {
            return false;
        }

        result = FromInline(_sign != other._sign, low, (int)scale);
        return true;
    }

    // Multiplies by 10^power in 64 bits; false when the power exceeds 19 or the result does not fit.
    private static bool TryScaleUp(ulong magnitude, long power, out ulong scaled)
    {
        if (power >= Pow10.Length || magnitude > ulong.MaxValue / Pow10[power])
        {
            scaled = 0;
            return false;
        }

        scaled = magnitude * Pow10[power];
        return true;
    }

    private BigInteger Aligned(int scale) => Scale == scale ? Mantissa : Mantissa * BigInteger.Pow(10, scale - Scale);

    /// <summary>Gets the number of digits of the mantissa magnitude; zero counts as one digit.</summary>
    /// <returns>The digit count.</returns>
    internal int MantissaDigits()
    {
        if (!_big.IsZero)
        {
            // ponytail: formats the number to count digits; a log10 estimate with a correction would avoid the string.
            return _big.ToString(CultureInfo.InvariantCulture).Length;
        }

        var digits = 1;
        while (digits < Pow10.Length && _mag >= Pow10[digits])
        {
            digits++;
        }

        return digits;
    }

    private int CompareMagnitude(XsdDecimal other)
    {
        if (Scale == other.Scale)
        {
            return _big.IsZero && other._big.IsZero ? _mag.CompareTo(other._mag) : Magnitude.CompareTo(other.Magnitude);
        }

        // value = mantissa x 10^-scale lies in [10^(digits-scale-1), 10^(digits-scale)); decide by that exponent first.
        var exponent = MantissaDigits() - Scale;
        var otherExponent = other.MantissaDigits() - other.Scale;
        if (exponent != otherExponent)
        {
            return exponent < otherExponent ? -1 : 1;
        }

        // Equal exponents mean the digit counts differ by the scale difference only, so the alignment is bounded.
        if (_big.IsZero && other._big.IsZero)
        {
            // A magnitude scaled past 64 bits exceeds the other side, which fits.
            return Scale > other.Scale
                ? (
                    TryScaleUp(other._mag, (long)Scale - other.Scale, out var scaledOther)
                        ? _mag.CompareTo(scaledOther)
                        : -1
                )
                : (
                    TryScaleUp(_mag, (long)other.Scale - Scale, out var scaledThis)
                        ? scaledThis.CompareTo(other._mag)
                        : 1
                );
        }

        return Scale > other.Scale
            ? Magnitude.CompareTo(other.Magnitude * BigInteger.Pow(10, Scale - other.Scale))
            : (Magnitude * BigInteger.Pow(10, other.Scale - Scale)).CompareTo(other.Magnitude);
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct DigitScan
    {
        public DigitScan(int total, int fraction, int first, int last)
        {
            Total = total;
            Fraction = fraction;
            First = first;
            Last = last;
        }

        public int Total { get; }

        public int Fraction { get; }

        public int First { get; }

        public int Last { get; }
    }

    private static DatatypeError Scan(
        ReadOnlySpan<char> lexical,
        int start,
        int maxDigits,
        out DigitScan digits,
        out int errorOffset
    )
    {
        var total = 0;
        var fraction = 0;
        var seenPoint = false;
        var first = -1;
        var last = -1;
        digits = default;
        errorOffset = 0;
        for (var i = start; i < lexical.Length; i++)
        {
            var c = lexical[i];
            if (c == '.')
            {
                if (seenPoint)
                {
                    errorOffset = i;
                    return DatatypeError.InvalidLexicalForm;
                }

                seenPoint = true;
            }
            else if (c is >= '0' and <= '9')
            {
                if (total == maxDigits)
                {
                    errorOffset = i;
                    return DatatypeError.DigitCap;
                }

                if (c != '0')
                {
                    if (first < 0)
                    {
                        first = total;
                    }

                    last = total;
                }

                if (seenPoint)
                {
                    fraction++;
                }

                total++;
            }
            else
            {
                errorOffset = i;
                return DatatypeError.InvalidCharacter;
            }
        }

        digits = new DigitScan(total, fraction, first, last);
        return DatatypeError.None;
    }

    // Both builders read the digits between the first and the last nonzero digit and skip the point.
    private static ulong BuildInline(ReadOnlySpan<char> lexical, int start, DigitScan digits)
    {
        ulong value = 0;
        var ordinal = -1;
        for (var i = start; i < lexical.Length && ordinal < digits.Last; i++)
        {
            if (lexical[i] != '.' && ++ordinal >= digits.First)
            {
                value = (value * 10) + (ulong)(lexical[i] - '0');
            }
        }

        return value;
    }

#if NET
    private const int MaxWideDigits = 38;

    private static UInt128 BuildWide(ReadOnlySpan<char> lexical, int start, DigitScan digits)
    {
        UInt128 value = 0;
        var ordinal = -1;
        for (var i = start; i < lexical.Length && ordinal < digits.Last; i++)
        {
            if (lexical[i] != '.' && ++ordinal >= digits.First)
            {
                value = (value * 10) + (ulong)(lexical[i] - '0');
            }
        }

        return value;
    }
#endif

    // Chunks of 18 digits: no string and no culture-aware parse.
    private static BigInteger BuildBig(ReadOnlySpan<char> lexical, int start, DigitScan digits)
    {
        var accumulator = BigInteger.Zero;
        ulong chunk = 0;
        var inChunk = 0;
        var ordinal = -1;
        for (var i = start; i < lexical.Length && ordinal < digits.Last; i++)
        {
            if (lexical[i] == '.' || ++ordinal < digits.First)
            {
                continue;
            }

            chunk = (chunk * 10) + (ulong)(lexical[i] - '0');
            if (++inChunk == ChunkDigits)
            {
                accumulator = (accumulator * Pow10[ChunkDigits]) + chunk;
                chunk = 0;
                inChunk = 0;
            }
        }

        return inChunk > 0 ? (accumulator * Pow10[inChunk]) + chunk : accumulator;
    }
}
