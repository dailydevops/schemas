namespace NetEvolve.Schemas.Xml.Dtd.Attributes;

using System;

/// <summary>The additional normalization of section 3.3.3 for an attribute whose declared type is not CDATA.</summary>
internal static class DtdAttributeNormalizer
{
    /// <summary>
    /// Discards the leading and trailing space characters (#x20) and replaces every other run of them by one space, in place.
    /// Only #x20 is touched: a tab or a line feed (which can reach the value only through a character reference) stays.
    /// </summary>
    /// <param name="value">The value after the CDATA normalization; the first characters of the span hold the result.</param>
    /// <returns>The length of the result, never more than the length of <paramref name="value"/>.</returns>
    internal static int Collapse(Span<char> value)
    {
        var length = 0;
        var pendingSpace = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == ' ')
            {
                // A space before the first character is discarded; one after the last character is never written.
                pendingSpace = length > 0;
                continue;
            }

            if (pendingSpace)
            {
                value[length++] = ' ';
                pendingSpace = false;
            }

            value[length++] = c;
        }

        return length;
    }
}
