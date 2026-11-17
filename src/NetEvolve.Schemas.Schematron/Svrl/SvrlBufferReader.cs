namespace NetEvolve.Schemas.Schematron.Svrl;

using System;

/// <summary>Reads the records of a <see cref="SvrlBuffer"/> in the order they were written.</summary>
internal ref struct SvrlBufferReader
{
    private readonly ReadOnlySpan<char> _data;
    private int _position;

    internal SvrlBufferReader(ReadOnlySpan<char> data)
    {
        _data = data;
        _position = 0;
    }

    internal readonly bool AtEnd => _position >= _data.Length;

    internal char ReadKind() => _data[_position++];

    internal int ReadNumber()
    {
        var value = (_data[_position] << 16) | _data[_position + 1];
        _position += 2;
        return value;
    }

    internal ReadOnlySpan<char> ReadText()
    {
        var length = ReadNumber();
        var text = _data.Slice(_position, length);
        _position += length;
        return text;
    }
}
