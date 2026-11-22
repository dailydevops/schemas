namespace NetEvolve.Schemas.Xml.Datatypes.Binary;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The value of <c>hexBinary</c> and <c>base64Binary</c> (XSD 1.1 Part 2, sections 3.3.15 and 3.3.16): a finite sequence of octets.</summary>
/// <remarks>Limitation: both types share this value type, so a hexBinary and a base64Binary value with the same octets are equal here; the length facets are left to DT-55.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XsdBinary : IEquatable<XsdBinary>
{
    private readonly byte[]? _octets;

    internal XsdBinary(byte[] octets) => _octets = octets;

    /// <summary>Gets the length in octets, not in characters of the lexical form.</summary>
    public int Length => _octets?.Length ?? 0;

    /// <summary>Parses a binary value without whitespace handling; only the single spaces that base64Binary allows are accepted.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="kind">The lexical space to accept.</param>
    /// <returns>The value, or the error with its offset.</returns>
    public static ParseResult<XsdBinary> Parse(ReadOnlySpan<char> lexical, BinaryKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, (int)BinaryKind.HexBinary);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)BinaryKind.Base64Binary);
        var error = BinaryLexical.Scan(lexical, kind, out var octets, out var offset);
        return error == DatatypeError.None
            ? new ParseResult<XsdBinary>(Map(lexical, kind, octets))
            : new ParseResult<XsdBinary>(error, offset);
    }

    /// <summary>Maps a form that passed the lexical scan.</summary>
    internal static XsdBinary Map(ReadOnlySpan<char> lexical, BinaryKind kind, int octets) =>
        new(BinaryLexical.Decode(lexical, kind, octets));

    /// <summary>Gets the octets without a copy.</summary>
    /// <returns>The octets.</returns>
    public ReadOnlySpan<byte> AsSpan() => _octets;

    /// <summary>Copies the octets.</summary>
    /// <returns>A new array.</returns>
    public byte[] ToArray() => _octets is null ? [] : (byte[])_octets.Clone();

    /// <summary>Gets the canonical lexical form: upper-case digits for hexBinary, no spaces for base64Binary.</summary>
    /// <param name="kind">The lexical space.</param>
    /// <returns>The canonical form.</returns>
    public string ToString(BinaryKind kind) =>
        kind == BinaryKind.HexBinary ? BinaryLexical.ToHex(AsSpan()) : Convert.ToBase64String(_octets ?? []);

    /// <summary>Gets the canonical <c>hexBinary</c> form.</summary>
    /// <returns>The canonical form.</returns>
    public override string ToString() => ToString(BinaryKind.HexBinary);

    /// <inheritdoc />
    public bool Equals(XsdBinary other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdBinary other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var octet in AsSpan())
        {
            hash = unchecked((hash * 31) + octet);
        }

        return hash;
    }

    /// <summary>Determines whether two values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XsdBinary left, XsdBinary right) => left.Equals(right);

    /// <summary>Determines whether two values differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when not equal.</returns>
    public static bool operator !=(XsdBinary left, XsdBinary right) => !left.Equals(right);
}
