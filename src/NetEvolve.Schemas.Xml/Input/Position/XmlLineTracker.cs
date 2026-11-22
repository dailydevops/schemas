namespace NetEvolve.Schemas.Xml.Input.Position;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Normalizes line ends (XML 1.0 Fifth Edition, section 2.11) and tracks the position of the decoded character stream.
/// </summary>
/// <remarks>
/// CRLF and a lone CR become one LF and advance the line by one; the column counts UTF-16 code units and a tab is one column.
/// A CR at the end of a chunk is emitted as LF at once and a LF that starts the next chunk is dropped, so no chunk
/// boundary changes the result. The offset counts the decoded input, so CRLF advances it by two.
/// Limitation: XML 1.1 line ends (NEL, U+2028) are not recognized.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal struct XmlLineTracker
{
    // Zero-based; Position reports one-based values.
    private int _line;
    private int _column;
    private long _offset;
    private bool _afterCr;

    /// <summary>Gets the position of the next input character.</summary>
    internal readonly XmlPosition Position => new(_line + 1, _column + 1, _offset);

    /// <summary>Copies <paramref name="input"/> to <paramref name="destination"/> with normalized line ends and advances the position.</summary>
    /// <param name="input">The next chunk of decoded characters.</param>
    /// <param name="destination">The target; its length must be at least that of <paramref name="input"/>.</param>
    /// <returns>The number of characters written.</returns>
    internal int Normalize(ReadOnlySpan<char> input, Span<char> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, input.Length);

        if (input.IsEmpty)
        {
            return 0;
        }

        var read = 0;
        var written = 0;

        if (_afterCr)
        {
            _afterCr = false;
            if (input[0] == '\n')
            {
                read = 1;
                _offset++;
            }
        }

        while (read < input.Length)
        {
            var rest = input.Slice(read);
            var index = rest.IndexOfAny('\r', '\n');
            var run = index < 0 ? rest.Length : index;

            rest.Slice(0, run).CopyTo(destination.Slice(written));
            written += run;
            read += run;
            _column += run;
            _offset += run;

            if (index < 0)
            {
                break;
            }

            destination[written++] = '\n';
            _line++;
            _column = 0;
            _offset++;

            if (input[read++] == '\r')
            {
                if (read == input.Length)
                {
                    _afterCr = true;
                }
                else if (input[read] == '\n')
                {
                    read++;
                    _offset++;
                }
            }
        }

        return written;
    }
}
