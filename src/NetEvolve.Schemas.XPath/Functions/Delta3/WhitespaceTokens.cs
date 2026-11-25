namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System;

/// <summary>
/// Reads the whitespace-separated tokens of a string: the separators are the characters of <c>fn:normalize-space</c> (the space,
/// the tab, the line feed, and the carriage return), so the tokens are those of <c>fn:tokenize(fn:normalize-space($input), ' ')</c>
/// without the empty one. Allocates nothing.
/// </summary>
internal static class WhitespaceTokens
{
    /// <summary>Finds the next token.</summary>
    /// <param name="text">The string.</param>
    /// <param name="position">Where to look from; set to the end of the token.</param>
    /// <param name="start">The start of the token when the result is <see langword="true"/>.</param>
    /// <param name="length">The length of the token when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when there is a token.</returns>
    internal static bool Next(ReadOnlySpan<char> text, ref int position, out int start, out int length)
    {
        var index = position;
        while (index < text.Length && IsSeparator(text[index]))
        {
            index++;
        }

        start = index;
        while (index < text.Length && !IsSeparator(text[index]))
        {
            index++;
        }

        length = index - start;
        position = index;
        return length > 0;
    }

    private static bool IsSeparator(char value) => value is ' ' or '\t' or '\n' or '\r';
}
