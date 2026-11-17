namespace NetEvolve.Schemas.Schematron.Svrl;

using System;
using System.Buffers;

/// <summary>A pooled buffer of length-prefixed text fields; one record per event of the active pattern.</summary>
/// <remarks>The text of an event is only valid during its callback, so the writer keeps a copy until the pattern ends.</remarks>
internal sealed class SvrlBuffer : IDisposable
{
    private char[] _data = ArrayPool<char>.Shared.Rent(256);
    private int _length;

    /// <summary>Gets the number of bytes the buffer holds, counted as UTF-16 code units.</summary>
    internal int ByteCount => _length * sizeof(char);

    internal ReadOnlySpan<char> Written => _data.AsSpan(0, _length);

    /// <summary>Appends one record marker.</summary>
    /// <param name="kind">The marker.</param>
    internal void PutKind(char kind)
    {
        Reserve(1);
        _data[_length++] = kind;
    }

    /// <summary>Appends a number below 2^32 as two code units.</summary>
    /// <param name="value">The number.</param>
    internal void PutNumber(int value)
    {
        Reserve(2);
        _data[_length++] = (char)(value >> 16);
        _data[_length++] = (char)(value & 0xFFFF);
    }

    /// <summary>Appends a text field.</summary>
    /// <param name="text">The text.</param>
    internal void PutText(ReadOnlySpan<char> text)
    {
        PutNumber(text.Length);
        Reserve(text.Length);
        text.CopyTo(_data.AsSpan(_length));
        _length += text.Length;
    }

    /// <summary>Appends a text field.</summary>
    /// <param name="text">The text.</param>
    internal void PutText(string text) => PutText(text.AsSpan());

    internal void Clear() => _length = 0;

    /// <inheritdoc />
    public void Dispose()
    {
        var data = _data;
        _data = [];
        _length = 0;
        if (data.Length > 0)
        {
            ArrayPool<char>.Shared.Return(data);
        }
    }

    private void Reserve(int count)
    {
        if (_data.Length - _length >= count)
        {
            return;
        }

        var grown = ArrayPool<char>.Shared.Rent(Math.Max(_data.Length * 2, _length + count));
        _data.AsSpan(0, _length).CopyTo(grown);
        ArrayPool<char>.Shared.Return(_data);
        _data = grown;
    }
}
