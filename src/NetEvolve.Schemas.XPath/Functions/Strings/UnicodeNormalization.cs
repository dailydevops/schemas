namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// The normalization forms NFC, NFD, NFKC, and NFKD of <c>fn:normalize-unicode</c> (Unicode Standard Annex 15), applied by embedded
/// tables (<see cref="NormalizationTables"/>) of the one pinned Unicode version, so every target framework gives the same result and the
/// platform's normalization, which differs between operating systems and runtimes, is never consulted.
/// </summary>
/// <remarks>
/// Limitations: the fully-normalized form is not supported (DEF-75, #87). A surrogate that is not part of a valid pair is a starter that
/// takes part in no decomposition and no composition. The work area is a pooled array of the code points of the string, so the
/// work is bounded by the length of the string and by the largest decomposition (18 code points).
/// </remarks>
internal static class UnicodeNormalization
{
    // The conjoining jamo and the precomposed syllables of the Hangul script, which the algorithm of Unicode 3.12 handles without tables.
    private const int SyllableBase = 0xAC00;
    private const int LeadBase = 0x1100;
    private const int VowelBase = 0x1161;
    private const int TrailBase = 0x11A7;
    private const int LeadCount = 19;
    private const int VowelCount = 21;
    private const int TrailCount = 28;
    private const int VowelTrailCount = VowelCount * TrailCount;
    private const int SyllableCount = LeadCount * VowelTrailCount;

    // A combining run longer than this is sorted by key; a shorter one by insertion, which needs no work area.
    private const int InsertionSortRun = 32;
    private const int InitialBuffer = 256;
    private const int LengthMask = 31;
    private const int LengthBits = 5;

    /// <summary>Reads the name of a normalization form: trimmed of white space and case-insensitive for the ASCII letters.</summary>
    /// <param name="name">The name of the form.</param>
    /// <param name="form">Receives the form.</param>
    /// <returns><see langword="true"/> for <c>NFC</c>, <c>NFD</c>, <c>NFKC</c>, and <c>NFKD</c>; <see langword="false"/> for every other name, the empty one included.</returns>
    public static bool TryParseForm(ReadOnlySpan<char> name, out UnicodeNormalizationForm form)
    {
        var start = 0;
        var end = name.Length;
        while (start < end && WhitespaceNormalizer.IsXmlSpace(name[start]))
        {
            start++;
        }

        while (end > start && WhitespaceNormalizer.IsXmlSpace(name[end - 1]))
        {
            end--;
        }

        var trimmed = name.Slice(start, end - start);
        if (Matches(trimmed, "NFC"))
        {
            form = UnicodeNormalizationForm.Nfc;
        }
        else if (Matches(trimmed, "NFD"))
        {
            form = UnicodeNormalizationForm.Nfd;
        }
        else if (Matches(trimmed, "NFKC"))
        {
            form = UnicodeNormalizationForm.Nfkc;
        }
        else if (Matches(trimmed, "NFKD"))
        {
            form = UnicodeNormalizationForm.Nfkd;
        }
        else
        {
            form = default;
            return false;
        }

        return true;
    }

    /// <summary>Gets the canonical combining class of a code point.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The class, from 0 (a starter) to 254.</returns>
    public static int CombiningClass(int codePoint)
    {
        var first = NormalizationTables.CombiningFirst;
        var index = Array.BinarySearch(first, codePoint);
        if (index >= 0)
        {
            return NormalizationTables.CombiningValue[index];
        }

        index = ~index - 1;
        return index >= 0 && codePoint <= NormalizationTables.CombiningLast[index]
            ? NormalizationTables.CombiningValue[index]
            : 0;
    }

    /// <summary>Normalizes a string.</summary>
    /// <param name="text">The string.</param>
    /// <param name="form">The normalization form.</param>
    /// <param name="limits">The evaluation limits; a decomposition may be longer than the string.</param>
    /// <returns>The normalized string; <paramref name="text"/> itself when it is already normalized.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="limits"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="form"/> is not a member of <see cref="UnicodeNormalizationForm"/>.</exception>
    /// <exception cref="XPathException">The normalized string is longer than the limit (XP0006).</exception>
    public static string Normalize(string text, UnicodeNormalizationForm form, XPathLimits limits)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)form, (int)UnicodeNormalizationForm.Nfc);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)form, (int)UnicodeNormalizationForm.Nfkd);

        // ASCII is stable under all four forms.
        if (IsAscii(text))
        {
            return text;
        }

        var compatibility = form is UnicodeNormalizationForm.Nfkc or UnicodeNormalizationForm.Nfkd;
        var pool = ArrayPool<int>.Shared;
        var buffer = pool.Rent(text.Length + InitialBuffer);
        try
        {
            var count = 0;
            var span = text.AsSpan();
            var index = 0;
            while (index < span.Length)
            {
                count = Decompose(CodePoints.Next(span, ref index), compatibility, ref buffer, count, pool);
            }

            Reorder(buffer, count);
            if (form is UnicodeNormalizationForm.Nfc or UnicodeNormalizationForm.Nfkc)
            {
                count = Compose(buffer, count);
            }

            Span<char> initial = stackalloc char[InitialBuffer];
            var builder = new TextBuilder(initial, limits);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    builder.AppendCodePoint(buffer[i]);
                }

                var result = builder.ToString();
                return string.Equals(result, text, StringComparison.Ordinal) ? text : result;
            }
            finally
            {
                builder.Dispose();
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    private static bool Matches(ReadOnlySpan<char> name, string upperCase)
    {
        if (name.Length != upperCase.Length)
        {
            return false;
        }

        for (var i = 0; i < name.Length; i++)
        {
            // Only the ASCII letters fold, so that a Kelvin sign or a dotless i is not taken for a letter of the name.
            var unit = name[i] is >= 'a' and <= 'z' ? (char)(name[i] - 32) : name[i];
            if (unit != upperCase[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAscii(string text)
    {
        foreach (var unit in text)
        {
            if (unit >= 0x80)
            {
                return false;
            }
        }

        return true;
    }

    // Appends the full decomposition of a code point to the work area and returns the new count.
    private static int Decompose(int codePoint, bool compatibility, ref int[] buffer, int count, ArrayPool<int> pool)
    {
        // The code points below U+00A0 have no decomposition, canonical or compatibility.
        if (codePoint < 0xA0)
        {
            return Add(codePoint, ref buffer, count, pool);
        }

        var syllable = codePoint - SyllableBase;
        if ((uint)syllable < SyllableCount)
        {
            count = Add(LeadBase + (syllable / VowelTrailCount), ref buffer, count, pool);
            count = Add(VowelBase + (syllable % VowelTrailCount / TrailCount), ref buffer, count, pool);
            var trail = TrailBase + (syllable % TrailCount);
            return trail == TrailBase ? count : Add(trail, ref buffer, count, pool);
        }

        // A compatibility mapping, when asked for, wins over the canonical one; the tables hold full (recursive) decompositions.
        var mapping = default(ReadOnlySpan<int>);
        var found =
            compatibility
            && TryGet(
                NormalizationTables.CompatibilityKeys,
                NormalizationTables.CompatibilityRefs,
                NormalizationTables.CompatibilityData,
                codePoint,
                out mapping
            );
        if (!found)
        {
            found = TryGet(
                NormalizationTables.CanonicalKeys,
                NormalizationTables.CanonicalRefs,
                NormalizationTables.CanonicalData,
                codePoint,
                out mapping
            );
        }

        if (!found)
        {
            return Add(codePoint, ref buffer, count, pool);
        }

        foreach (var part in mapping)
        {
            count = Add(part, ref buffer, count, pool);
        }

        return count;
    }

    private static int Add(int codePoint, ref int[] buffer, int count, ArrayPool<int> pool)
    {
        if (count == buffer.Length)
        {
            var bigger = pool.Rent(buffer.Length * 2);
            Array.Copy(buffer, bigger, count);
            pool.Return(buffer);
            buffer = bigger;
        }

        buffer[count] = codePoint;
        return count + 1;
    }

    private static bool TryGet(int[] keys, int[] references, int[] data, int codePoint, out ReadOnlySpan<int> mapping)
    {
        var index = Array.BinarySearch(keys, codePoint);
        if (index < 0)
        {
            mapping = default;
            return false;
        }

        var reference = references[index];
        mapping = new ReadOnlySpan<int>(data, reference >> LengthBits, reference & LengthMask);
        return true;
    }

    // The canonical ordering algorithm: every run of code points with a non-zero combining class is stably sorted by class.
    private static void Reorder(int[] buffer, int count)
    {
        var index = 0;
        while (index < count)
        {
            if (CombiningClass(buffer[index]) == 0)
            {
                index++;
                continue;
            }

            var start = index;
            while (index < count && CombiningClass(buffer[index]) != 0)
            {
                index++;
            }

            if (index - start > 1)
            {
                SortRun(buffer, start, index);
            }
        }
    }

    private static void SortRun(int[] buffer, int start, int end)
    {
        if (end - start <= InsertionSortRun)
        {
            for (var i = start + 1; i < end; i++)
            {
                var codePoint = buffer[i];
                var combining = CombiningClass(codePoint);
                var j = i - 1;
                while (j >= start && CombiningClass(buffer[j]) > combining)
                {
                    buffer[j + 1] = buffer[j];
                    j--;
                }

                buffer[j + 1] = codePoint;
            }

            return;
        }

        // A long run: sort the keys (class, position), which keeps equal classes in their order, then write the code points back.
        var length = end - start;
        var keys = ArrayPool<long>.Shared.Rent(length);
        var copy = ArrayPool<int>.Shared.Rent(length);
        try
        {
            for (var i = 0; i < length; i++)
            {
                copy[i] = buffer[start + i];
                keys[i] = ((long)CombiningClass(copy[i]) << 32) | (uint)i;
            }

            Array.Sort(keys, 0, length);
            for (var i = 0; i < length; i++)
            {
                buffer[start + i] = copy[(int)(keys[i] & uint.MaxValue)];
            }
        }
        finally
        {
            ArrayPool<long>.Shared.Return(keys);
            ArrayPool<int>.Shared.Return(copy);
        }
    }

    // The canonical composition algorithm: a code point is combined with the last starter when no code point of the same or a higher
    // class (or a starter) stands between them and the pair has a primary composite.
    private static int Compose(int[] buffer, int count)
    {
        var write = 0;
        var starter = -1;
        var lastClass = 0;
        for (var read = 0; read < count; read++)
        {
            var codePoint = buffer[read];
            var combining = CombiningClass(codePoint);
            if (
                starter >= 0
                && (lastClass < combining || lastClass == 0)
                && TryCompose(buffer[starter], codePoint, out var composite)
            )
            {
                buffer[starter] = composite;
                continue;
            }

            if (combining == 0)
            {
                starter = write;
            }

            lastClass = combining;
            buffer[write++] = codePoint;
        }

        return write;
    }

    private static bool TryCompose(int first, int second, out int composite)
    {
        var lead = first - LeadBase;
        var vowel = second - VowelBase;
        if ((uint)lead < LeadCount && (uint)vowel < VowelCount)
        {
            composite = SyllableBase + (((lead * VowelCount) + vowel) * TrailCount);
            return true;
        }

        var syllable = first - SyllableBase;
        var trail = second - TrailBase;
        if ((uint)syllable < SyllableCount && syllable % TrailCount == 0 && (uint)(trail - 1) < TrailCount - 1)
        {
            composite = first + trail;
            return true;
        }

        var index = Array.BinarySearch(NormalizationTables.CompositionKeys, ((long)first << 32) | (uint)second);
        if (index >= 0)
        {
            composite = NormalizationTables.CompositionValues[index];
            return true;
        }

        composite = 0;
        return false;
    }
}
