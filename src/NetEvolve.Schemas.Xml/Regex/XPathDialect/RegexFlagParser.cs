namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>Reads the flags string of the XPath regex functions.</summary>
public static class RegexFlagParser
{
    /// <summary>Parses a flags string; a letter may repeat, any letter other than <c>s m i x q</c> is an error.</summary>
    /// <param name="text">The flags string.</param>
    /// <param name="flags">The flags read so far; <see cref="RegexFlagSet.None"/> on an error.</param>
    /// <param name="position">The index of the invalid letter, or -1 on success.</param>
    /// <returns><see cref="RegexError.None"/> or <see cref="RegexError.InvalidFlag"/>.</returns>
    public static RegexError TryParse(ReadOnlySpan<char> text, out RegexFlagSet flags, out int position)
    {
        flags = RegexFlagSet.None;
        position = -1;
        var result = RegexFlagSet.None;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case 's':
                    result |= RegexFlagSet.DotAll;
                    break;
                case 'm':
                    result |= RegexFlagSet.MultiLine;
                    break;
                case 'i':
                    result |= RegexFlagSet.IgnoreCase;
                    break;
                case 'x':
                    result |= RegexFlagSet.Extended;
                    break;
                case 'q':
                    result |= RegexFlagSet.Literal;
                    break;
                default:
                    position = i;
                    return RegexError.InvalidFlag;
            }
        }

        flags = result;
        return RegexError.None;
    }
}
