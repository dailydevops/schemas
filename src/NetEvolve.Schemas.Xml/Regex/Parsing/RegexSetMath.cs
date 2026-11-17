namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System;
using System.Collections.Generic;

/// <summary>Interval arithmetic on sets of code points held as sorted, non-adjacent pairs (first, last) in an <see cref="int"/> array.</summary>
internal static class RegexSetMath
{
    internal const int MaxCodePoint = 0x10FFFF;

    /// <summary>Collects ranges in any order and normalizes them to a sorted, merged array.</summary>
    internal sealed class Builder
    {
        private long[] _items = new long[8];
        private int _count;

        internal bool IsEmpty => _count == 0;

        internal void Add(int first, int last)
        {
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, _count * 2);
            }

            _items[_count++] = ((long)first << 32) | (uint)last;
        }

        internal void AddAll(int[] ranges)
        {
            for (var i = 0; i < ranges.Length; i += 2)
            {
                Add(ranges[i], ranges[i + 1]);
            }
        }

        internal int[] Build()
        {
            Array.Sort(_items, 0, _count);
            var result = new List<int>(_count * 2);
            for (var i = 0; i < _count; i++)
            {
                var first = (int)(_items[i] >> 32);
                var last = (int)(_items[i] & 0xFFFFFFFF);
                var end = result.Count - 1;
                if (end >= 0 && first <= result[end] + 1)
                {
                    if (last > result[end])
                    {
                        result[end] = last;
                    }
                }
                else
                {
                    result.Add(first);
                    result.Add(last);
                }
            }

            return result.ToArray();
        }
    }

    /// <summary>Returns the complement over U+0000 to U+10FFFF.</summary>
    internal static int[] Negate(int[] ranges)
    {
        var result = new List<int>(ranges.Length + 2);
        var next = 0;
        for (var i = 0; i < ranges.Length; i += 2)
        {
            if (ranges[i] > next)
            {
                result.Add(next);
                result.Add(ranges[i] - 1);
            }

            next = ranges[i + 1] + 1;
        }

        if (next <= MaxCodePoint)
        {
            result.Add(next);
            result.Add(MaxCodePoint);
        }

        return result.ToArray();
    }

    /// <summary>Returns the ranges of <paramref name="minuend"/> that are not in <paramref name="subtrahend"/>.</summary>
    internal static int[] Subtract(int[] minuend, int[] subtrahend)
    {
        var result = new List<int>(minuend.Length);
        var j = 0;
        for (var i = 0; i < minuend.Length; i += 2)
        {
            var first = minuend[i];
            var last = minuend[i + 1];
            while (j < subtrahend.Length && subtrahend[j + 1] < first)
            {
                j += 2;
            }

            var k = j;
            while (first <= last && k < subtrahend.Length && subtrahend[k] <= last)
            {
                if (subtrahend[k] > first)
                {
                    result.Add(first);
                    result.Add(subtrahend[k] - 1);
                }

                first = subtrahend[k + 1] + 1;
                k += 2;
            }

            if (first <= last)
            {
                result.Add(first);
                result.Add(last);
            }
        }

        return result.ToArray();
    }
}
