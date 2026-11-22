namespace NetEvolve.Schemas.Xml.Dtd.Validation.Identity;

using System;
using NetEvolve.Arguments;

/// <summary>
/// An append-only character buffer that holds many short names back to back, so storing a name costs no object of its own.
/// A name is addressed by the start and length that <see cref="Append"/> returned.
/// </summary>
/// <remarks>
/// <see cref="Clear"/> keeps the capacity, so a store that serves one document after another allocates only while it grows.
/// Not thread-safe.
/// </remarks>
internal sealed class DtdNameStore
{
    private char[] _chars = new char[256];
    private int _length;

    /// <summary>Appends the text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The start of the stored text.</returns>
    internal int Append(ReadOnlySpan<char> text)
    {
        var start = _length;
        var needed = start + text.Length;
        if (needed > _chars.Length)
        {
            Array.Resize(ref _chars, (int)Math.Min(Math.Max(needed, _chars.Length * 2L), int.MaxValue));
        }

        text.CopyTo(_chars.AsSpan(start));
        _length = needed;

        return start;
    }

    /// <summary>Gets stored text.</summary>
    /// <param name="start">The start that <see cref="Append"/> returned.</param>
    /// <param name="length">The length of the stored text.</param>
    /// <returns>The characters, valid until the next <see cref="Append"/> or <see cref="Clear"/>.</returns>
    internal ReadOnlySpan<char> Get(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);

        return _chars.AsSpan(start, length);
    }

    /// <summary>Forgets everything stored and keeps the capacity.</summary>
    internal void Clear() => _length = 0;
}
