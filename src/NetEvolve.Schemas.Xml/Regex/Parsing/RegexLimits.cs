namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using NetEvolve.Arguments;

/// <summary>Immutable limits of the pattern parser; exceeding one is <c>RX0009</c>.</summary>
/// <remarks>Limitation: the defaults are those of the approved design; the program size limit belongs to the matcher.</remarks>
public sealed class RegexLimits
{
    private RegexLimits(int maxPatternLength, int maxNesting)
    {
        MaxPatternLength = maxPatternLength;
        MaxNesting = maxNesting;
    }

    /// <summary>Gets the default limits.</summary>
    public static RegexLimits Default { get; } = new(4096, 64);

    /// <summary>Gets the maximum pattern length in UTF-16 code units; the default is 4,096.</summary>
    public int MaxPatternLength { get; }

    /// <summary>Gets the maximum nesting of groups and character class subtractions; the default is 64.</summary>
    public int MaxNesting { get; }

    /// <summary>Returns a copy with another <see cref="MaxPatternLength"/>.</summary>
    /// <param name="value">The new value, one or greater.</param>
    /// <returns>The new limits.</returns>
    public RegexLimits WithMaxPatternLength(int value)
    {
        Argument.ThrowIfLessThan(value, 1);
        return new(value, MaxNesting);
    }

    /// <summary>Returns a copy with another <see cref="MaxNesting"/>.</summary>
    /// <param name="value">The new value, one or greater.</param>
    /// <returns>The new limits.</returns>
    public RegexLimits WithMaxNesting(int value)
    {
        Argument.ThrowIfLessThan(value, 1);
        return new(MaxPatternLength, value);
    }
}
