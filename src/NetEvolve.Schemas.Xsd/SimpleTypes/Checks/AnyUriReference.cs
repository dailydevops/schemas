namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System;

/// <summary>
/// The XSD 1.0 lexical space of <c>anyURI</c> (Datatypes 1.0, section 3.2.17): an RFC 2396 URI reference, where the escaping of XLink
/// makes every non-ASCII character and every white space character acceptable.
/// </summary>
/// <remarks>
/// The authority, path, and query are checked by character class only; the scheme and the required scheme-specific part are checked
/// structurally, so a value that starts or ends with a colon is rejected. XSD 1.1 accepts every string and does not call this check.
/// </remarks>
internal static class AnyUriReference
{
    private const string AsciiPunctuation = "-_.!~*'();/?:@&=+$,[]";

    /// <summary>Gets a value indicating whether the value is a URI reference in the XSD 1.0 sense.</summary>
    /// <param name="value">The lexical form.</param>
    /// <returns><see langword="true"/> when the value is in the lexical space.</returns>
    internal static bool IsValid(ReadOnlySpan<char> value)
    {
        value = value.Trim(" \t\r\n".AsSpan());
        var delimiter = -1;
        var fragments = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '%')
            {
                if (i + 2 >= value.Length || !IsHex(value[i + 1]) || !IsHex(value[i + 2]))
                {
                    return false;
                }

                i += 2;
            }
            else if (c == '#')
            {
                fragments++;
                delimiter = delimiter < 0 ? i : delimiter;
            }
            else if (c < 0x80 && !IsAsciiAllowed(c))
            {
                return false;
            }
            else if (delimiter < 0 && c is ':' or '/' or '?')
            {
                delimiter = i;
            }
        }

        if (fragments > 1)
        {
            return false;
        }

        // A colon before the first slash, question mark, or number sign ends a scheme, which then needs a scheme-specific part.
        if (delimiter < 0 || value[delimiter] != ':')
        {
            return true;
        }

        var end = value.IndexOf('#');
        return IsScheme(value.Slice(0, delimiter)) && delimiter + 1 < (end < 0 ? value.Length : end);
    }

    private static bool IsScheme(ReadOnlySpan<char> scheme)
    {
        if (scheme.IsEmpty || !IsAsciiLetter(scheme[0]))
        {
            return false;
        }

        foreach (var c in scheme)
        {
            if (!IsAsciiLetter(c) && !(c is >= '0' and <= '9') && c is not ('+' or '-' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiAllowed(char c) =>
        IsAsciiLetter(c)
        || c is >= '0' and <= '9'
        || AsciiPunctuation.Contains(c, StringComparison.Ordinal)
        || c is ' ' or '\t' or '\r' or '\n';

    private static bool IsAsciiLetter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    private static bool IsHex(char c) => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
}
