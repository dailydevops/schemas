namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Buffers;

/// <summary>The whitespace processing of the <c>whiteSpace</c> facet on spans. Normalized input is passed through without a copy.</summary>
internal static class WhitespaceNormalizer
{
    /// <summary>Normalizes <paramref name="source"/> into <paramref name="destination"/>, which must be at least as long as the source.</summary>
    /// <param name="source">The lexical form.</param>
    /// <param name="mode">The whitespace mode.</param>
    /// <param name="destination">The target; it may not overlap the source.</param>
    /// <returns>The number of characters written.</returns>
    internal static int Normalize(ReadOnlySpan<char> source, WhitespaceMode mode, Span<char> destination)
    {
        Argument.ThrowIfLessThan(destination.Length, source.Length);
        if (mode == WhitespaceMode.Preserve)
        {
            source.CopyTo(destination);
            return source.Length;
        }

        var length = 0;
        var pendingSpace = false;
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (!IsXmlSpace(c))
            {
                if (pendingSpace)
                {
                    destination[length++] = ' ';
                    pendingSpace = false;
                }

                destination[length++] = c;
            }
            else if (mode == WhitespaceMode.Replace)
            {
                destination[length++] = ' ';
            }
            else
            {
                pendingSpace = length > 0;
            }
        }

        return length;
    }

    /// <summary>Determines whether the mode would leave <paramref name="source"/> unchanged.</summary>
    /// <param name="source">The lexical form.</param>
    /// <param name="mode">The whitespace mode.</param>
    /// <returns><see langword="true"/> when no character changes.</returns>
    internal static bool IsNormalized(ReadOnlySpan<char> source, WhitespaceMode mode)
    {
        if (mode == WhitespaceMode.Preserve)
        {
            return true;
        }

        var previousSpace = true; // starts true so a leading space counts as a collapsible one
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (c is '\t' or '\n' or '\r')
            {
                return false;
            }

            if (mode == WhitespaceMode.Collapse && c == ' ' && previousSpace)
            {
                return false;
            }

            previousSpace = c == ' ';
        }

        return mode != WhitespaceMode.Collapse || source.IsEmpty || !previousSpace;
    }

    /// <summary>Applies the mode, renting a buffer only when the text changes.</summary>
    /// <param name="source">The lexical form.</param>
    /// <param name="mode">The whitespace mode.</param>
    /// <param name="rented">The buffer that backs the result, <see langword="null"/> when the source is returned; the caller disposes it.</param>
    /// <returns>The normalized text.</returns>
    internal static ReadOnlySpan<char> Apply(
        ReadOnlySpan<char> source,
        WhitespaceMode mode,
        out PooledBuffer<char>? rented
    )
    {
        if (IsNormalized(source, mode))
        {
            rented = null;
            return source;
        }

        rented = new PooledBuffer<char>(source.Length);
        rented.EnsureCapacity(source.Length);
        rented.Advance(Normalize(source, mode, rented.GetFreeSpan()));
        return rented.WrittenSpan;
    }

    /// <summary>Maps an offset in the normalized text back to the offset in the original text.</summary>
    /// <param name="source">The original lexical form.</param>
    /// <param name="mode">The whitespace mode.</param>
    /// <param name="normalizedOffset">The offset in the normalized text.</param>
    /// <returns>The offset in <paramref name="source"/>; its length when the offset is past the end.</returns>
    internal static int ToSourceOffset(ReadOnlySpan<char> source, WhitespaceMode mode, int normalizedOffset)
    {
        if (mode != WhitespaceMode.Collapse)
        {
            return normalizedOffset;
        }

        var kept = 0;
        var pendingAt = -1;
        for (var i = 0; i < source.Length; i++)
        {
            if (IsXmlSpace(source[i]))
            {
                if (kept > 0 && pendingAt < 0)
                {
                    pendingAt = i;
                }

                continue;
            }

            if (pendingAt >= 0)
            {
                if (kept == normalizedOffset)
                {
                    return pendingAt;
                }

                kept++;
                pendingAt = -1;
            }

            if (kept == normalizedOffset)
            {
                return i;
            }

            kept++;
        }

        return source.Length;
    }

    internal static bool IsXmlSpace(char c) => c is ' ' or '\t' or '\n' or '\r';
}
