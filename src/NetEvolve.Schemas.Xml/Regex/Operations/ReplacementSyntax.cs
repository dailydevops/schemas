namespace NetEvolve.Schemas.Xml.Regex.Operations;

using System;
using NetEvolve.Schemas.Xml.Buffers;

// The replacement string of fn:replace: $N group references, \$ and \\ escapes.
internal static class ReplacementSyntax
{
    private const char Backslash = '\\';
    private const char Dollar = '$';

    // A $ must be followed by a digit and a backslash by a backslash or a $.
    internal static bool IsValid(ReadOnlySpan<char> replacement)
    {
        for (var i = 0; i < replacement.Length; i++)
        {
            if (replacement[i] == Backslash)
            {
                if (i + 1 >= replacement.Length || (replacement[i + 1] != Backslash && replacement[i + 1] != Dollar))
                {
                    return false;
                }

                i++;
            }
            else if (replacement[i] == Dollar && (i + 1 >= replacement.Length || !IsDigit(replacement[i + 1])))
            {
                return false;
            }
        }

        return true;
    }

    // Writes the replacement of one match; the string must be valid. The first digit after a $ is always part of the group
    // number, a further digit only while the number stays at most the number of groups of the pattern.
    internal static void Append(PooledBuffer<char> destination, ReadOnlySpan<char> replacement, in RegexMatch match)
    {
        ArgumentNullException.ThrowIfNull(destination);

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

            destination.Append(replacement.Slice(run, i - run));
            if (current == Backslash)
            {
                destination.Append(replacement.Slice(i + 1, 1));
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

                destination.Append(number <= match.GroupCount ? match.GetGroupText(number) : default);
            }

            run = i;
        }

        destination.Append(replacement.Slice(run));
    }

    private static bool IsDigit(char value) => (uint)(value - '0') <= 9;
}
