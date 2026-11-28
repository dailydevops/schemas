namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>
/// The <c>enumeration</c> facet of a list of string-like items, whose value space is the text: the item sequence of a list value must
/// equal one of the listed item sequences, compared item by item with an ordinal comparison.
/// </summary>
public sealed class ListLexicalEnumerationFacet : ILexicalFacet
{
    private readonly string[] _values;

    /// <summary>Initializes a new instance of the <see cref="ListLexicalEnumerationFacet"/> class.</summary>
    /// <param name="values">The listed values, each a whitespace-separated item sequence.</param>
    public ListLexicalEnumerationFacet(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = [.. values];
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.Enumeration;

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical)
    {
        for (var i = 0; i < _values.Length; i++)
        {
            if (Matches(lexical, _values[i].AsSpan()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(ReadOnlySpan<char> lexical, ReadOnlySpan<char> listed)
    {
        var left = new XsdListCursor(new TextRange(0, lexical.Length));
        var right = new XsdListCursor(new TextRange(0, listed.Length));
        while (true)
        {
            var hasLeft = left.TryNext(lexical, out var a);
            var hasRight = right.TryNext(listed, out var b);
            if (hasLeft != hasRight)
            {
                return false;
            }

            if (!hasLeft)
            {
                return true;
            }

            if (!a.Slice(lexical).SequenceEqual(b.Slice(listed)))
            {
                return false;
            }
        }
    }
}
