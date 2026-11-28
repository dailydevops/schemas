namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>
/// The <c>length</c>, <c>minLength</c>, and <c>maxLength</c> facets (XSD 1.1 Part 2, sections 4.3.1 to 4.3.3). It checks the
/// whitespace-normalized lexical form and counts code points for a string type or items for a list type.
/// </summary>
/// <remarks>Limitation: the binary types, whose length counts octets, are a later package. A lone surrogate counts as one character.</remarks>
public sealed class LengthFacet : ILexicalFacet
{
    private LengthFacet(FacetKind kind, int value, LengthUnit unit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
        Kind = kind;
        Value = value;
        Unit = unit;
    }

    /// <inheritdoc />
    public FacetKind Kind { get; }

    /// <summary>Gets the facet value.</summary>
    public int Value { get; }

    /// <summary>Gets the unit that is counted.</summary>
    public LengthUnit Unit { get; }

    /// <summary>Creates a <c>length</c> facet.</summary>
    /// <param name="value">The exact length.</param>
    /// <param name="unit">The unit that is counted.</param>
    /// <returns>The facet.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public static LengthFacet Length(int value, LengthUnit unit) => new(FacetKind.Length, value, unit);

    /// <summary>Creates a <c>minLength</c> facet.</summary>
    /// <param name="value">The minimum length.</param>
    /// <param name="unit">The unit that is counted.</param>
    /// <returns>The facet.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public static LengthFacet MinLength(int value, LengthUnit unit) => new(FacetKind.MinLength, value, unit);

    /// <summary>Creates a <c>maxLength</c> facet.</summary>
    /// <param name="value">The maximum length.</param>
    /// <param name="unit">The unit that is counted.</param>
    /// <returns>The facet.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public static LengthFacet MaxLength(int value, LengthUnit unit) => new(FacetKind.MaxLength, value, unit);

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical)
    {
        var length = Unit == LengthUnit.Items ? CountItems(lexical) : CountCodePoints(lexical);
        return Kind switch
        {
            FacetKind.Length => length == Value,
            FacetKind.MinLength => length >= Value,
            _ => length <= Value,
        };
    }

    private static int CountCodePoints(ReadOnlySpan<char> text)
    {
        var count = text.Length;
        for (var i = 1; i < text.Length; i++)
        {
            if (char.IsLowSurrogate(text[i]) && char.IsHighSurrogate(text[i - 1]))
            {
                count--;
                i++; // The low surrogate is consumed; the next high surrogate starts a new pair.
            }
        }

        return count;
    }

    private static int CountItems(ReadOnlySpan<char> text)
    {
        var cursor = new XsdListCursor(new TextRange(0, text.Length));
        var count = 0;
        while (cursor.TryNext(text, out _))
        {
            count++;
        }

        return count;
    }
}
