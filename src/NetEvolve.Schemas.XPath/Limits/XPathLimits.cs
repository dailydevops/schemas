namespace NetEvolve.Schemas.XPath.Limits;

using System;
using System.Numerics;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The budgets that stop an evaluation before it exhausts the stack or memory: the recursion depth, the size of a sequence, the size of
/// a range (<c>a to b</c>), the length of a string, the bit width of an integer, and the scale of a decimal. Expressions can come from untrusted schemas, so every path that grows without
/// bound is charged against one of them, and a breach ends in a coded diagnostic (<see cref="XPathLimitErrors"/>) with the limit name,
/// the configured value, and the expression location. Immutable and thread-safe.
/// </summary>
/// <remarks>
/// <para>
/// The defaults flow from the core <see cref="ValidationLimits"/> (<see cref="From"/>): the recursion depth is
/// <see cref="ValidationLimits.MaxDepth"/>, because both are nesting depths; the five growth budgets (sequence size, range size, string
/// length, integer width, decimal scale) are <see cref="ValidationLimits.MaxEntityExpansion"/>, because each bounds the output of a construct that produces more than it
/// reads. A caller overrides each budget with its <c>With</c> method. Every budget is finite and at least one; there is no unlimited value.
/// </para>
/// <para>
/// Limitation: the limits are implementation limits allowed by XPath 2.0 (2.3.1) and Functions and Operators (FOAR0002); a valid
/// expression that exceeds a budget fails with a library error code. A budget is per value (one sequence, one range, one string), not a
/// total over an evaluation; a work budget with cancellation is #641. A range is charged by its size even when its items are
/// produced lazily. The integer budget is the bit width of the magnitude of an <c>xs:integer</c> result beyond 64 bits, and the decimal
/// budget is the absolute scale of a decimal operand or product; both are checked where the arithmetic computes a result (#639). A
/// decimal <c>div</c> yields 18 fraction digits (#100 AP-7), so a scale budget below 18 does not suit a query that divides.
/// </para>
/// </remarks>
internal sealed class XPathLimits
{
    /// <summary>The highest allowed <see cref="MaxRecursionDepth"/>, the ceiling of <see cref="ValidationLimits.MaxDepth"/>.</summary>
    internal const int MaxRecursionDepthCeiling = ValidationLimits.MaxDepthCeiling;

    /// <summary>The highest allowed <see cref="MaxSequenceSize"/>, the longest array of the runtime (<c>Array.MaxLength</c>, which the netstandard builds lack).</summary>
    internal const int MaxSequenceSizeCeiling = 2_147_483_591;

    private XPathLimits(
        int maxRecursionDepth,
        int maxSequenceSize,
        long maxRangeSize,
        long maxStringLength,
        int maxIntegerBits,
        int maxDecimalScale
    )
    {
        MaxIntegerBits = maxIntegerBits;
        MaxDecimalScale = maxDecimalScale;
        MaxRecursionDepth = maxRecursionDepth;
        MaxSequenceSize = maxSequenceSize;
        MaxRangeSize = maxRangeSize;
        MaxStringLength = maxStringLength;
    }

    /// <summary>Gets the defaults: the flow of <see cref="ValidationLimits.Default"/>.</summary>
    public static XPathLimits Default { get; } = From(ValidationLimits.Default);

    /// <summary>Gets the deepest nesting of the executable tree an evaluation recurses through.</summary>
    public int MaxRecursionDepth { get; }

    /// <summary>Gets the most items one sequence holds.</summary>
    public int MaxSequenceSize { get; }

    /// <summary>Gets the most items one range holds; a range whose start is above its end is empty and never breaches.</summary>
    public long MaxRangeSize { get; }

    /// <summary>Gets the most characters one string holds.</summary>
    public long MaxStringLength { get; }

    /// <summary>Gets the most bits the magnitude of one integer result holds.</summary>
    public int MaxIntegerBits { get; }

    /// <summary>Gets the highest absolute scale of a decimal operand or product.</summary>
    public int MaxDecimalScale { get; }

    /// <summary>Takes the budgets from the core validation limits, unchanged.</summary>
    /// <param name="limits">The core limits.</param>
    /// <returns>The limits: the depth is <see cref="ValidationLimits.MaxDepth"/>, the growth budgets are <see cref="ValidationLimits.MaxEntityExpansion"/>; the sequence size is clamped to <see cref="MaxSequenceSizeCeiling"/>, the integer width and the decimal scale to <see cref="int.MaxValue"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <see langword="null"/>.</exception>
    public static XPathLimits From(ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        var expansion = limits.MaxEntityExpansion;
        var numeric = (int)Math.Min(expansion, int.MaxValue);
        return new(
            limits.MaxDepth,
            (int)Math.Min(expansion, MaxSequenceSizeCeiling),
            expansion,
            expansion,
            numeric,
            numeric
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxRecursionDepth"/>.</summary>
    /// <param name="value">The new value, from one to <see cref="MaxRecursionDepthCeiling"/>.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one or above the ceiling.</exception>
    public XPathLimits WithMaxRecursionDepth(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxRecursionDepthCeiling);

        return new(value, MaxSequenceSize, MaxRangeSize, MaxStringLength, MaxIntegerBits, MaxDecimalScale);
    }

    /// <summary>Returns a copy with another <see cref="MaxSequenceSize"/>.</summary>
    /// <param name="value">The new value, from one to <see cref="MaxSequenceSizeCeiling"/>.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one or above the ceiling.</exception>
    public XPathLimits WithMaxSequenceSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxSequenceSizeCeiling);

        return new(MaxRecursionDepth, value, MaxRangeSize, MaxStringLength, MaxIntegerBits, MaxDecimalScale);
    }

    /// <summary>Returns a copy with another <see cref="MaxRangeSize"/>.</summary>
    /// <param name="value">The new value, at least one.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one.</exception>
    public XPathLimits WithMaxRangeSize(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1L);

        return new(MaxRecursionDepth, MaxSequenceSize, value, MaxStringLength, MaxIntegerBits, MaxDecimalScale);
    }

    /// <summary>Returns a copy with another <see cref="MaxStringLength"/>.</summary>
    /// <param name="value">The new value, at least one.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one.</exception>
    public XPathLimits WithMaxStringLength(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1L);

        return new(MaxRecursionDepth, MaxSequenceSize, MaxRangeSize, value, MaxIntegerBits, MaxDecimalScale);
    }

    /// <summary>Returns a copy with another <see cref="MaxIntegerBits"/>.</summary>
    /// <param name="value">The new value, at least 64: an integer up to 64 bits is inline and never charged.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below 64.</exception>
    public XPathLimits WithMaxIntegerBits(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 64);

        return new(MaxRecursionDepth, MaxSequenceSize, MaxRangeSize, MaxStringLength, value, MaxDecimalScale);
    }

    /// <summary>Returns a copy with another <see cref="MaxDecimalScale"/>.</summary>
    /// <param name="value">The new value, at least one.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one.</exception>
    public XPathLimits WithMaxDecimalScale(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        return new(MaxRecursionDepth, MaxSequenceSize, MaxRangeSize, MaxStringLength, MaxIntegerBits, value);
    }

    /// <summary>Checks a recursion depth; one comparison, no allocation.</summary>
    /// <param name="depth">The depth reached, the root being one.</param>
    /// <param name="location">The location of the expression that reaches it.</param>
    /// <exception cref="XPathException"><paramref name="depth"/> is above <see cref="MaxRecursionDepth"/> (<see cref="XPathLimitErrors.RecursionDepthExceeded"/>).</exception>
    public void ThrowIfTooDeep(int depth, in SourceSpan location)
    {
        if (depth > MaxRecursionDepth)
        {
            throw XPathLimitErrors.RecursionDepth(MaxRecursionDepth, location);
        }
    }

    /// <summary>
    /// Checks the size of a range before anything is built: <c>first to last</c> has <c>last - first + 1</c> items, computed without
    /// overflow, and none when <paramref name="first"/> is above <paramref name="last"/>. One comparison, no allocation.
    /// </summary>
    /// <param name="first">The first integer of the range.</param>
    /// <param name="last">The last integer of the range; an operand beyond 64 bits is clamped to the 64-bit range by the caller.</param>
    /// <param name="location">The location of the range expression.</param>
    /// <exception cref="XPathException">The range has more items than <see cref="MaxRangeSize"/> (<see cref="XPathLimitErrors.RangeSizeExceeded"/>).</exception>
    public void ThrowIfRangeTooLarge(long first, long last, in SourceSpan location)
    {
        // last - first is below 2^64, so it fits an unsigned subtraction; the size exceeds the limit exactly when that difference reaches it.
        if (first <= last && unchecked((ulong)last - (ulong)first) >= (ulong)MaxRangeSize)
        {
            throw XPathLimitErrors.RangeSize(MaxRangeSize, location);
        }
    }

    /// <summary>Checks the length of a string before it is built; one comparison, no allocation.</summary>
    /// <param name="length">The length the string will have.</param>
    /// <param name="location">The location of the expression that builds it.</param>
    /// <exception cref="XPathException"><paramref name="length"/> is above <see cref="MaxStringLength"/> (<see cref="XPathLimitErrors.StringLengthExceeded"/>).</exception>
    public void ThrowIfStringTooLong(long length, in SourceSpan location)
    {
        if (length > MaxStringLength)
        {
            throw XPathLimitErrors.StringLength(MaxStringLength, location);
        }
    }

    /// <summary>Checks the width of an integer result that is computed beyond 64 bits.</summary>
    /// <param name="value">The result.</param>
    /// <param name="location">The location of the expression.</param>
    /// <exception cref="XPathException">The magnitude of <paramref name="value"/> has more bits than <see cref="MaxIntegerBits"/> (<see cref="XPathLimitErrors.IntegerWidthExceeded"/>).</exception>
    public void ThrowIfIntegerTooWide(BigInteger value, in SourceSpan location)
    {
        if (BitLength(value) > MaxIntegerBits)
        {
            throw XPathLimitErrors.IntegerWidth(MaxIntegerBits, location);
        }
    }

    /// <summary>Checks the width of a product before it is computed: the product has the sum of the widths of the factors, or one bit less.</summary>
    /// <param name="left">The left factor.</param>
    /// <param name="right">The right factor.</param>
    /// <param name="location">The location of the expression.</param>
    /// <exception cref="XPathException">The product is certain to have more bits than <see cref="MaxIntegerBits"/> (<see cref="XPathLimitErrors.IntegerWidthExceeded"/>).</exception>
    public void ThrowIfProductTooWide(BigInteger left, BigInteger right, in SourceSpan location)
    {
        if (BitLength(left) + BitLength(right) - 1 > MaxIntegerBits)
        {
            throw XPathLimitErrors.IntegerWidth(MaxIntegerBits, location);
        }
    }

    /// <summary>Checks the scale of a decimal; two comparisons, no allocation.</summary>
    /// <param name="scale">The scale.</param>
    /// <param name="location">The location of the expression.</param>
    /// <exception cref="XPathException">The absolute <paramref name="scale"/> is above <see cref="MaxDecimalScale"/> (<see cref="XPathLimitErrors.DecimalScaleExceeded"/>).</exception>
    public void ThrowIfScaleTooLarge(int scale, in SourceSpan location)
    {
        if (scale > MaxDecimalScale || scale < -MaxDecimalScale)
        {
            throw XPathLimitErrors.DecimalScale(MaxDecimalScale, location);
        }
    }

    // The bits of the magnitude, so the sign does not move the boundary.
    private static long BitLength(BigInteger value)
    {
#if NET5_0_OR_GREATER
        return (value.Sign < 0 ? BigInteger.Negate(value) : value).GetBitLength();
#else
        var bytes = BigInteger.Abs(value).ToByteArray();
        var length = bytes.Length;
        while (length > 1 && bytes[length - 1] == 0)
        {
            length--;
        }

        var top = (int)bytes[length - 1];
        var bits = 0;
        while (top != 0)
        {
            bits++;
            top >>= 1;
        }

        return ((long)(length - 1) * 8) + bits;
#endif
    }
}
