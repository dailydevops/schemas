namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Enumerates the whitespace-separated items of a list lexical form as ranges, without allocating.</summary>
[StructLayout(LayoutKind.Auto)]
public ref struct XsdListCursor
{
    private readonly int _end;
    private int _position;

    /// <summary>Initializes a cursor over a range of the caller's span.</summary>
    /// <param name="range">The range of the whole list lexical form.</param>
    public XsdListCursor(TextRange range)
    {
        _position = range.Offset;
        _end = range.Offset + range.Length;
    }

    /// <summary>Advances to the next item.</summary>
    /// <param name="source">The span the range was produced from.</param>
    /// <param name="item">The range of the item, relative to <paramref name="source"/>.</param>
    /// <returns><see langword="false"/> when no item is left.</returns>
    public bool TryNext(ReadOnlySpan<char> source, out TextRange item)
    {
        while (_position < _end && WhitespaceNormalizer.IsXmlSpace(source[_position]))
        {
            _position++;
        }

        var start = _position;
        while (_position < _end && !WhitespaceNormalizer.IsXmlSpace(source[_position]))
        {
            _position++;
        }

        item = new TextRange(start, _position - start);
        return _position > start;
    }
}
