namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;
using System.Threading;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.Unicode;

/// <summary>
/// Case-insensitive matching by case classes: two code points are equivalent when their simple case foldings are equal, as
/// the embedded tables define it. The parser expands a literal or a character class to every equivalent code point, so the
/// matcher needs no folding for them.
/// </summary>
internal static class RegexCaseFolding
{
    private const int Shift = 21;
    private const int Mask = (1 << Shift) - 1;

    // Every code point whose folding differs from itself, as (folded << 21 | code point), sorted.
    private static long[]? _pairs;

    /// <summary>Gets the code points equivalent to <paramref name="codePoint"/>, itself included.</summary>
    /// <returns><see langword="false"/> when it has no equivalent but itself.</returns>
    internal static bool TryGetClass(int codePoint, out int[] ranges)
    {
        var pairs = Pairs();
        var folded = UnicodeTables.SimpleCaseFold(codePoint);
        var start = LowerBound(pairs, (long)folded << Shift);
        var builder = new RegexSetMath.Builder();
        var count = 0;
        for (var i = start; i < pairs.Length && (int)(pairs[i] >> Shift) == folded; i++)
        {
            var member = (int)(pairs[i] & Mask);
            builder.Add(member, member);
            count++;
        }

        ranges = [];
        if (count == 0)
        {
            return false;
        }

        builder.Add(folded, folded);
        ranges = builder.Build();
        return true;
    }

    /// <summary>Adds to a set every code point equivalent to one of its members.</summary>
    internal static int[] Close(int[] ranges)
    {
        var pairs = Pairs();
        var set = new RegexCharacterSet(ranges);
        var builder = new RegexSetMath.Builder();
        builder.AddAll(ranges);
        for (var i = 0; i < pairs.Length; )
        {
            var folded = (int)(pairs[i] >> Shift);
            var end = i;
            var hit = set.Contains(folded);
            while (end < pairs.Length && (int)(pairs[end] >> Shift) == folded)
            {
                hit |= set.Contains((int)(pairs[end] & Mask));
                end++;
            }

            if (hit)
            {
                builder.Add(folded, folded);
                for (var k = i; k < end; k++)
                {
                    builder.Add((int)(pairs[k] & Mask), (int)(pairs[k] & Mask));
                }
            }

            i = end;
        }

        return builder.Build();
    }

    private static long[] Pairs()
    {
        var cached = Volatile.Read(ref _pairs);
        if (cached is not null)
        {
            return cached;
        }

        var found = new long[2048];
        var count = 0;
        for (var cp = 0; cp <= RegexSetMath.MaxCodePoint; cp++)
        {
            var folded = UnicodeTables.SimpleCaseFold(cp);
            if (folded == cp)
            {
                continue;
            }

            if (count == found.Length)
            {
                Array.Resize(ref found, count * 2);
            }

            found[count++] = ((long)folded << Shift) | (uint)cp;
        }

        var result = new long[count];
        Array.Copy(found, result, count);
        Array.Sort(result);
        return Interlocked.CompareExchange(ref _pairs, result, null) ?? result;
    }

    private static int LowerBound(long[] pairs, long key)
    {
        var low = 0;
        var high = pairs.Length;
        while (low < high)
        {
            var mid = (int)((uint)(low + high) >> 1);
            if (pairs[mid] < key)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }
}
