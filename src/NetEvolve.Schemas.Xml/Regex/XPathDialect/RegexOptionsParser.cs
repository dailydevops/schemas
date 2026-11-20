namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>Parses the <c>$flags</c> string of the XPath regular expression functions.</summary>
public static class RegexOptionsParser
{
    /// <summary>Parses flag letters.</summary>
    /// <param name="letters">The letters in any order, repeated or not.</param>
    /// <param name="flags">The options, <see cref="RegexOptions.None"/> on an error.</param>
    /// <param name="position">The index of the first invalid letter, or -1 on success.</param>
    /// <returns><see cref="RegexError.None"/> or <see cref="RegexError.InvalidFlag"/>.</returns>
    public static RegexError TryParse(ReadOnlySpan<char> letters, out RegexOptions flags, out int position)
    {
        _ = letters;
        flags = RegexOptions.None;
        position = 0;
        return RegexError.InvalidFlag;
    }
}
