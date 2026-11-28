namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

using System;
using System.Runtime.InteropServices;

/// <summary>An offset and a length into the caller's span; the view that string-like values carry because a span cannot be a generic argument.</summary>
/// <remarks>The range is valid while the caller keeps the span it was produced from.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct TextRange : IEquatable<TextRange>
{
    /// <summary>Initializes a new instance of the <see cref="TextRange"/> struct.</summary>
    /// <param name="offset">The zero-based offset.</param>
    /// <param name="length">The length.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is negative.</exception>
    public TextRange(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
        Offset = offset;
        Length = length;
    }

    /// <summary>Gets the zero-based offset.</summary>
    public int Offset { get; }

    /// <summary>Gets the length.</summary>
    public int Length { get; }

    /// <summary>Gets the part of the caller's span that the range denotes.</summary>
    /// <param name="source">The span the range was produced from.</param>
    /// <returns>The slice.</returns>
    public ReadOnlySpan<char> Slice(ReadOnlySpan<char> source) => source.Slice(Offset, Length);

    /// <inheritdoc />
    public bool Equals(TextRange other) => Offset == other.Offset && Length == other.Length;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TextRange other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((Offset * 31) + Length);

    /// <summary>Determines whether two ranges are equal.</summary>
    /// <param name="left">The first range.</param>
    /// <param name="right">The second range.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(TextRange left, TextRange right) => left.Equals(right);

    /// <summary>Determines whether two ranges differ.</summary>
    /// <param name="left">The first range.</param>
    /// <param name="right">The second range.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(TextRange left, TextRange right) => !left.Equals(right);
}
