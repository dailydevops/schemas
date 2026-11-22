namespace NetEvolve.Schemas.XPath.Limits;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The budgets that stop an evaluation before it exhausts the stack or memory: the recursion depth, the size of a sequence, the size of
/// a range (<c>a to b</c>), and the length of a string. Expressions can come from untrusted schemas, so every path that grows without
/// bound is charged against one of them, and a breach ends in a coded diagnostic (<see cref="XPathLimitErrors"/>) with the limit name,
/// the configured value, and the expression location. Immutable and thread-safe.
/// </summary>
/// <remarks>
/// <para>
/// The defaults flow from the core <see cref="ValidationLimits"/> (<see cref="From"/>): the recursion depth is
/// <see cref="ValidationLimits.MaxDepth"/>, because both are nesting depths; the three growth budgets (sequence size, range size, string
/// length) are <see cref="ValidationLimits.MaxEntityExpansion"/>, because each bounds the output of a construct that produces more than it
/// reads. A caller overrides each budget with its <c>With</c> method. Every budget is finite and at least one; there is no unlimited value.
/// </para>
/// <para>
/// Limitation: the limits are implementation limits allowed by XPath 2.0 (2.3.1) and Functions and Operators (FOAR0002); a valid
/// expression that exceeds a budget fails with a library error code. A budget is per value (one sequence, one range, one string), not a
/// total over an evaluation; a work budget with cancellation is #641. A range is charged by its size even when its items are
/// produced lazily. Numeric growth (an integer beyond 64 bits, a decimal scale) is not bounded here (#639).
/// </para>
/// </remarks>
internal sealed class XPathLimits
{
    /// <summary>The highest allowed <see cref="MaxRecursionDepth"/>, the ceiling of <see cref="ValidationLimits.MaxDepth"/>.</summary>
    internal const int MaxRecursionDepthCeiling = ValidationLimits.MaxDepthCeiling;

    /// <summary>The highest allowed <see cref="MaxSequenceSize"/>, the longest array of the runtime (<c>Array.MaxLength</c>, which the netstandard builds lack).</summary>
    internal const int MaxSequenceSizeCeiling = 2_147_483_591;

    private XPathLimits(int maxRecursionDepth, int maxSequenceSize, long maxRangeSize, long maxStringLength)
    {
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

    /// <summary>Takes the budgets from the core validation limits, unchanged.</summary>
    /// <param name="limits">The core limits.</param>
    /// <returns>The limits: the depth is <see cref="ValidationLimits.MaxDepth"/>, the growth budgets are <see cref="ValidationLimits.MaxEntityExpansion"/>; only the sequence size is clamped, to <see cref="MaxSequenceSizeCeiling"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <see langword="null"/>.</exception>
    public static XPathLimits From(ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        var expansion = limits.MaxEntityExpansion;
        return new(limits.MaxDepth, (int)Math.Min(expansion, MaxSequenceSizeCeiling), expansion, expansion);
    }

    /// <summary>Returns a copy with another <see cref="MaxRecursionDepth"/>.</summary>
    /// <param name="value">The new value, from one to <see cref="MaxRecursionDepthCeiling"/>.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one or above the ceiling.</exception>
    public XPathLimits WithMaxRecursionDepth(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxRecursionDepthCeiling);

        return new(value, MaxSequenceSize, MaxRangeSize, MaxStringLength);
    }

    /// <summary>Returns a copy with another <see cref="MaxSequenceSize"/>.</summary>
    /// <param name="value">The new value, from one to <see cref="MaxSequenceSizeCeiling"/>.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one or above the ceiling.</exception>
    public XPathLimits WithMaxSequenceSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxSequenceSizeCeiling);

        return new(MaxRecursionDepth, value, MaxRangeSize, MaxStringLength);
    }

    /// <summary>Returns a copy with another <see cref="MaxRangeSize"/>.</summary>
    /// <param name="value">The new value, at least one.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one.</exception>
    public XPathLimits WithMaxRangeSize(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1L);

        return new(MaxRecursionDepth, MaxSequenceSize, value, MaxStringLength);
    }

    /// <summary>Returns a copy with another <see cref="MaxStringLength"/>.</summary>
    /// <param name="value">The new value, at least one.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is below one.</exception>
    public XPathLimits WithMaxStringLength(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1L);

        return new(MaxRecursionDepth, MaxSequenceSize, MaxRangeSize, value);
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
}
