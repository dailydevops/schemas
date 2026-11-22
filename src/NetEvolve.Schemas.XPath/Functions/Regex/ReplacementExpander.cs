namespace NetEvolve.Schemas.XPath.Functions.Regex;

using System;
using NetEvolve.Schemas.XPath.Functions.Strings;

/// <summary>
/// Writes the replacement string of <c>fn:replace</c> for one match (Functions and Operators 1.0, 7.6.3): <c>$N</c> is the text of group
/// N (<c>$0</c> the whole match, empty for a group that took no part or does not exist), <c>\$</c> a dollar sign, and <c>\\</c> a
/// backslash. The first digit after a dollar sign is always part of the group number, a further digit only while the number stays at
/// most the number of groups of the pattern.
/// </summary>
/// <remarks>The validity of the replacement string is checked before, once per call (<c>ReplacementSyntax</c> of the Xml package); the text is written to a <see cref="TextBuilder"/>, so the string length limit holds.</remarks>
internal static class ReplacementExpander
{
    private const char Backslash = '\\';
    private const char Dollar = '$';

    /// <summary>Appends the replacement for a match to a builder.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="replacement">The replacement string, which must be valid.</param>
    /// <param name="match">The matching segment.</param>
    /// <exception cref="NetEvolve.Schemas.XPath.Errors.XPathException">The text would be longer than the limit (XP0006).</exception>
    internal static void Append(ref TextBuilder builder, ReadOnlySpan<char> replacement, in MatchSegment match)
    {
        var run = 0;
        var i = 0;
        while (i < replacement.Length)
        {
            var current = replacement[i];
            if (current != Backslash && current != Dollar)
            {
                i++;
                continue;
            }

            builder.Append(replacement.Slice(run, i - run));
            if (current == Backslash)
            {
                builder.Append(replacement[i + 1]);
                i += 2;
            }
            else
            {
                var number = replacement[i + 1] - '0';
                i += 2;
                while (
                    i < replacement.Length
                    && IsDigit(replacement[i])
                    && (number * 10) + (replacement[i] - '0') <= match.GroupCount
                )
                {
                    number = (number * 10) + (replacement[i] - '0');
                    i++;
                }

                builder.Append(number <= match.GroupCount ? match.GetGroupText(number) : default);
            }

            run = i;
        }

        builder.Append(replacement.Slice(run));
    }

    private static bool IsDigit(char value) => (uint)(value - '0') <= 9;
}
