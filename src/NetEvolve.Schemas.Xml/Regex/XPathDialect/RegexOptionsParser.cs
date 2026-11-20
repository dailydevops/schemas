namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>Parses the <c>$flags</c> string of the XPath regular expression functions into <see cref="RegexOptions"/>.</summary>
/// <remarks>Limitation: only the letters <c>s</c>, <c>m</c>, <c>i</c>, and <c>x</c> are flags; the letter <c>q</c> of the later Functions and Operators editions is deferred by the approved design (AP-13 A) and is an invalid flag.</remarks>
public static class RegexOptionsParser
{
    /// <summary>Parses flag letters.</summary>
    /// <param name="letters">The letters in any order, each of them any number of times.</param>
    /// <param name="options">The options, <see cref="RegexOptions.None"/> on an error.</param>
    /// <param name="position">The zero-based index of the first invalid letter, or -1 on success.</param>
    /// <returns><see cref="RegexError.None"/>, or <see cref="RegexError.InvalidFlag"/> when a letter is not a flag.</returns>
    public static RegexError TryParse(ReadOnlySpan<char> letters, out RegexOptions options, out int position)
    {
        var result = RegexOptions.None;
        for (var i = 0; i < letters.Length; i++)
        {
            var flag = letters[i] switch
            {
                's' => RegexOptions.DotAll,
                'm' => RegexOptions.MultiLine,
                'i' => RegexOptions.IgnoreCase,
                'x' => RegexOptions.FreeSpacing,
                _ => RegexOptions.None,
            };
            if (flag == RegexOptions.None)
            {
                options = RegexOptions.None;
                position = i;
                return RegexError.InvalidFlag;
            }

            result |= flag;
        }

        options = result;
        position = -1;
        return RegexError.None;
    }
}
