namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

/// <summary>
/// The Unicode codepoint collation (<c>http://www.w3.org/2005/xpath-functions/collation/codepoint</c>): strings are ordered by their
/// Unicode codepoints, which is not the order of the UTF-16 code units that <see cref="string.CompareOrdinal(string, string)"/> gives
/// when a supplementary character meets a character from U+E000 to U+FFFF.
/// </summary>
/// <remarks>
/// Limitation: the codepoint collation is the only collation (#123 AP19, DEF-43). An unpaired surrogate counts as the codepoint of
/// its own code unit.
/// </remarks>
internal static class CodepointComparison
{
    /// <summary>Compares two strings by codepoint.</summary>
    /// <param name="first">The first string.</param>
    /// <param name="second">The second string.</param>
    /// <returns>Less than zero when the first string precedes the second, zero when they are equal, greater than zero otherwise.</returns>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static int Compare(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var length = first.Length < second.Length ? first.Length : second.Length;
        for (var i = 0; i < length; i++)
        {
            var a = first[i];
            var b = second[i];
            if (a != b)
            {
                return Fold(a) - Fold(b);
            }
        }

        return first.Length - second.Length;
    }

    // The code units D800 to DFFF are the surrogates of the supplementary codepoints, which follow U+E000 to U+FFFF: moving the
    // surrogates above that range and the range down makes the code unit order the codepoint order.
    private static int Fold(char unit) =>
        unit < 0xD800 ? unit
        : unit >= 0xE000 ? unit - 0x800
        : unit + 0x2000;
}
