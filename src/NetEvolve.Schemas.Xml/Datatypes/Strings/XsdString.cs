namespace NetEvolve.Schemas.Xml.Datatypes.Strings;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>
/// The value of xs:string, xs:normalizedString, and xs:token: a zero-allocation view. <see cref="Range"/> selects a part of the
/// normalized view of the caller span, and <see cref="Mode"/> is the whitespace mode that produces that view.
/// </summary>
/// <remarks>Valid while the caller keeps the span it was parsed from. Text equality needs the spans, so it is a method; the operators compare range and mode only.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XsdString : IEquatable<XsdString>
{
    /// <summary>Initializes a new instance of the <see cref="XsdString"/> struct.</summary>
    /// <param name="range">The part of the normalized view.</param>
    /// <param name="mode">The whitespace mode that produces the normalized view.</param>
    public XsdString(TextRange range, WhitespaceMode mode)
    {
        Range = range;
        Mode = mode;
    }

    /// <summary>Gets the part of the normalized view that the value denotes.</summary>
    public TextRange Range { get; }

    /// <summary>Gets the whitespace mode that produces the normalized view.</summary>
    public WhitespaceMode Mode { get; }

    /// <summary>Determines whether both values carry the same range and mode; this does not compare text, see <see cref="Equals(ReadOnlySpan{char}, XsdString, ReadOnlySpan{char})"/>.</summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true"/> when range and mode are equal.</returns>
    public bool Equals(XsdString other) => Range == other.Range && Mode == other.Mode;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdString other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((Range.GetHashCode() * 31) + (int)Mode);

    /// <summary>Determines whether two values carry the same range and mode.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XsdString left, XsdString right) => left.Equals(right);

    /// <summary>Determines whether two values differ in range or mode.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XsdString left, XsdString right) => !left.Equals(right);

    /// <summary>Copies the normalized value into <paramref name="destination"/>.</summary>
    /// <param name="source">The span the value was parsed from.</param>
    /// <param name="destination">The target; at least as long as the value.</param>
    /// <returns>The number of characters written.</returns>
    public int CopyTo(ReadOnlySpan<char> source, Span<char> destination)
    {
        var normalized = WhitespaceNormalizer.Apply(source, Mode, out var rented);
        try
        {
            var view = Range.Slice(normalized);
            view.CopyTo(destination);
            return view.Length;
        }
        finally
        {
            rented?.Dispose();
        }
    }

    /// <summary>Determines whether two values are equal over their normalized views, by code unit.</summary>
    /// <param name="source">The span this value was parsed from.</param>
    /// <param name="other">The other value.</param>
    /// <param name="otherSource">The span the other value was parsed from.</param>
    /// <returns><see langword="true"/> when the normalized views are equal.</returns>
    public bool Equals(ReadOnlySpan<char> source, XsdString other, ReadOnlySpan<char> otherSource)
    {
        var first = WhitespaceNormalizer.Apply(source, Mode, out var firstBuffer);
        try
        {
            var second = WhitespaceNormalizer.Apply(otherSource, other.Mode, out var secondBuffer);
            try
            {
                return Range.Slice(first).SequenceEqual(other.Range.Slice(second));
            }
            finally
            {
                secondBuffer?.Dispose();
            }
        }
        finally
        {
            firstBuffer?.Dispose();
        }
    }
}
