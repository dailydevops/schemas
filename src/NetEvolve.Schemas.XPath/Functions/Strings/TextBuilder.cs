namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Builds the text of a result without intermediate strings: the caller's scratch buffer (usually on the stack) first, a pooled array
/// when the text outgrows it, and the one result string at the end. The length is checked against
/// <see cref="XPathLimits.MaxStringLength"/> before any growth, so a result that the limit forbids is never built.
/// </summary>
/// <remarks>Dispose it, so that a pooled array goes back; the caller's scratch buffer is never owned.</remarks>
internal ref struct TextBuilder
{
    private static readonly SourceSpan NoLocation = new(-1, 0);

    private readonly XPathLimits _limits;
    private Span<char> _buffer;
    private char[]? _rented;

    /// <summary>Initializes a new instance of the <see cref="TextBuilder"/> struct.</summary>
    /// <param name="initial">The scratch buffer to start with.</param>
    /// <param name="limits">The evaluation limits.</param>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <see langword="null"/>.</exception>
    public TextBuilder(Span<char> initial, XPathLimits limits)
    {
        Argument.ThrowIfNull(limits);

        _limits = limits;
        _buffer = initial;
        _rented = null;
        Length = 0;
    }

    /// <summary>Gets the number of code units written.</summary>
    public int Length { get; private set; }

    /// <summary>Appends one code unit.</summary>
    /// <param name="value">The code unit.</param>
    /// <exception cref="XPathException">The text would be longer than the limit (XP0006).</exception>
    public void Append(char value)
    {
        Reserve(1);
        _buffer[Length++] = value;
    }

    /// <summary>Appends code units.</summary>
    /// <param name="text">The code units.</param>
    /// <exception cref="XPathException">The text would be longer than the limit (XP0006).</exception>
    public void Append(ReadOnlySpan<char> text)
    {
        Reserve(text.Length);
        text.CopyTo(_buffer.Slice(Length));
        Length += text.Length;
    }

    /// <summary>Appends a code point as one code unit, or as a surrogate pair above U+FFFF.</summary>
    /// <param name="codePoint">The code point; the value of a surrogate code unit is appended as that unit.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="codePoint"/> is negative or above U+10FFFF.</exception>
    /// <exception cref="XPathException">The text would be longer than the limit (XP0006).</exception>
    public void AppendCodePoint(int codePoint)
    {
        Argument.ThrowIfLessThan(codePoint, 0);
        Argument.ThrowIfGreaterThan(codePoint, 0x10FFFF);

        if (codePoint < 0x10000)
        {
            Append((char)codePoint);
            return;
        }

        Reserve(2);
        var offset = codePoint - 0x10000;
        _buffer[Length++] = (char)(0xD800 + (offset >> 10));
        _buffer[Length++] = (char)(0xDC00 + (offset & 0x3FF));
    }

    /// <summary>Creates the string of the text written so far.</summary>
    /// <returns>The string.</returns>
    public override readonly string ToString() => _buffer.Slice(0, Length).ToString();

    /// <summary>Returns the pooled array, if any.</summary>
    public void Dispose()
    {
        var rented = _rented;
        _rented = null;
        _buffer = default;
        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private void Reserve(int count)
    {
        var needed = (long)Length + count;
        _limits.ThrowIfStringTooLong(needed, NoLocation);
        if (needed > _buffer.Length)
        {
            Grow(needed);
        }
    }

    private void Grow(long needed)
    {
        Argument.ThrowIfGreaterThan(needed, int.MaxValue);

        var next = ArrayPool<char>.Shared.Rent((int)Math.Max(needed, (long)_buffer.Length * 2));
        _buffer.Slice(0, Length).CopyTo(next);
        var previous = _rented;
        _rented = next;
        _buffer = next;
        if (previous is not null)
        {
            ArrayPool<char>.Shared.Return(previous);
        }
    }
}
