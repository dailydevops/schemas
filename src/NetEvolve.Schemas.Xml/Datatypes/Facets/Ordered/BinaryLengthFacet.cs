namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// The <c>length</c>, <c>minLength</c>, and <c>maxLength</c> facets on <c>hexBinary</c> and <c>base64Binary</c> (XSD 1.1 Part 2,
/// sections 4.3.1 to 4.3.3): the length is the number of octets of the value, not the number of characters of the lexical form.
/// </summary>
/// <remarks>
/// The octets are counted by the lexical scan of the type on the whitespace-normalized form, so nothing is decoded or allocated.
/// The framework rules for restricting the length facets (minLength against maxLength, fixed flags) are those of
/// <c>FacetRestriction</c> in the core facets, which already lists the binary types as applicable.
/// </remarks>
public sealed class BinaryLengthFacet : ILexicalFacet
{
    private BinaryLengthFacet(FacetKind kind, int value, BinaryKind encoding)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)encoding, (int)BinaryKind.HexBinary);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)encoding, (int)BinaryKind.Base64Binary);
        Kind = kind;
        Value = value;
        Encoding = encoding;
    }

    /// <inheritdoc />
    public FacetKind Kind { get; }

    /// <summary>Gets the facet value, in octets.</summary>
    public int Value { get; }

    /// <summary>Gets the lexical space whose octets are counted.</summary>
    public BinaryKind Encoding { get; }

    /// <summary>Creates a <c>length</c> facet.</summary>
    /// <param name="value">The exact length in octets.</param>
    /// <param name="encoding">The lexical space of the restricted type.</param>
    /// <returns>The facet.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative or <paramref name="encoding"/> is not defined.</exception>
    public static BinaryLengthFacet Length(int value, BinaryKind encoding) => new(FacetKind.Length, value, encoding);

    /// <summary>Creates a <c>minLength</c> facet.</summary>
    /// <param name="value">The minimum length in octets.</param>
    /// <param name="encoding">The lexical space of the restricted type.</param>
    /// <returns>The facet.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative or <paramref name="encoding"/> is not defined.</exception>
    public static BinaryLengthFacet MinLength(int value, BinaryKind encoding) =>
        new(FacetKind.MinLength, value, encoding);

    /// <summary>Creates a <c>maxLength</c> facet.</summary>
    /// <param name="value">The maximum length in octets.</param>
    /// <param name="encoding">The lexical space of the restricted type.</param>
    /// <returns>The facet.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative or <paramref name="encoding"/> is not defined.</exception>
    public static BinaryLengthFacet MaxLength(int value, BinaryKind encoding) =>
        new(FacetKind.MaxLength, value, encoding);

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical)
    {
        // The descriptor checked the lexical form before the facets, so the scan succeeds; an invalid form fails the facet.
        if (BinaryLexical.Scan(lexical, Encoding, out var octets, out _) != DatatypeError.None)
        {
            return false;
        }

        return Kind switch
        {
            FacetKind.Length => octets == Value,
            FacetKind.MinLength => octets >= Value,
            _ => octets <= Value,
        };
    }
}
