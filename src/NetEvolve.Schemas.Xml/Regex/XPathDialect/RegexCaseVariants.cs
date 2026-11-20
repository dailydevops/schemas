namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.Unicode;

/// <summary>
/// The case variants of a code point for the flag <c>i</c>: every code point whose simple case folding equals the folding
/// of the code point. The classes are built once from the embedded folding table, so every framework gets the same ones.
/// </summary>
/// <remarks>Limitation: only the simple foldings of the pinned Unicode version are used, not the full multi-character foldings.</remarks>
internal static class RegexCaseVariants
{
    /// <summary>The highest code point scanned for a folding; a test proves that no code point above it folds.</summary>
    internal const int ScanLimit = 0x2FFFF;

    private static readonly Classes Data = Classes.Build();

    /// <summary>Gets the case variants of a code point, itself included.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <param name="variants">The variants in ascending order; empty when the code point has no variant.</param>
    /// <returns><see langword="true"/> when the code point has at least one other variant.</returns>
    internal static bool TryGetVariants(int codePoint, out ReadOnlySpan<int> variants)
    {
        var slot = Data.Find(codePoint);
        if (slot < 0)
        {
            variants = default;
            return false;
        }

        variants = Data.Class(slot);
        return true;
    }

    /// <summary>Adds the case variants of every member to a set.</summary>
    /// <param name="ranges">The set as sorted range pairs.</param>
    /// <returns>The set with all variants of its members, as sorted and merged range pairs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ranges"/> is <see langword="null"/>.</exception>
    internal static int[] Close(int[] ranges)
    {
        Argument.ThrowIfNull(ranges);

        var set = new RegexCharacterSet(ranges);
        var builder = new RegexSetMath.Builder();
        builder.AddAll(ranges);
        for (var slot = 0; slot < Data.ClassCount; slot++)
        {
            var members = Data.Class(slot);
            if (!AnyIn(set, members))
            {
                continue;
            }

            for (var i = 0; i < members.Length; i++)
            {
                builder.Add(members[i], members[i]);
            }
        }

        return builder.Build();
    }

    private static bool AnyIn(RegexCharacterSet set, ReadOnlySpan<int> members)
    {
        for (var i = 0; i < members.Length; i++)
        {
            if (set.Contains(members[i]))
            {
                return true;
            }
        }

        return false;
    }

    // The classes as one array of members, grouped by class and ascending within it, and an index from code point to class.
    private sealed class Classes
    {
        private readonly int[] _members;
        private readonly int[] _classStarts;
        private readonly int[] _memberCodePoints;
        private readonly int[] _memberClasses;

        private Classes(int[] members, int[] classStarts, int[] memberCodePoints, int[] memberClasses)
        {
            _members = members;
            _classStarts = classStarts;
            _memberCodePoints = memberCodePoints;
            _memberClasses = memberClasses;
        }

        internal int ClassCount => _classStarts.Length - 1;

        internal static Classes Build()
        {
            // One entry (folded, code point) per code point that folds, plus (folded, folded) for the target itself.
            var entries = new List<long>();
            for (var cp = 0; cp <= ScanLimit; cp++)
            {
                var folded = UnicodeTables.SimpleCaseFold(cp);
                if (folded != cp)
                {
                    entries.Add(((long)folded << 32) | (uint)cp);
                    entries.Add(((long)folded << 32) | (uint)folded);
                }
            }

            entries.Sort();
            var members = new List<int>(entries.Count);
            var starts = new List<int>();
            var lookup = new List<long>(entries.Count);
            var previous = -1L;
            var previousFolded = -1;
            foreach (var entry in entries)
            {
                if (entry == previous)
                {
                    continue;
                }

                previous = entry;
                var folded = (int)(entry >> 32);
                if (folded != previousFolded)
                {
                    starts.Add(members.Count);
                    previousFolded = folded;
                }

                var codePoint = (int)(entry & 0xFFFFFFFF);
                members.Add(codePoint);
                lookup.Add(((long)codePoint << 32) | (uint)(starts.Count - 1));
            }

            starts.Add(members.Count);
            lookup.Sort();
            var memberCodePoints = new int[lookup.Count];
            var memberClasses = new int[lookup.Count];
            for (var i = 0; i < lookup.Count; i++)
            {
                memberCodePoints[i] = (int)(lookup[i] >> 32);
                memberClasses[i] = (int)(lookup[i] & 0xFFFFFFFF);
            }

            return new Classes(members.ToArray(), starts.ToArray(), memberCodePoints, memberClasses);
        }

        internal int Find(int codePoint)
        {
            var index = Array.BinarySearch(_memberCodePoints, codePoint);
            return index < 0 ? -1 : _memberClasses[index];
        }

        internal ReadOnlySpan<int> Class(int slot) =>
            new(_members, _classStarts[slot], _classStarts[slot + 1] - _classStarts[slot]);
    }
}
