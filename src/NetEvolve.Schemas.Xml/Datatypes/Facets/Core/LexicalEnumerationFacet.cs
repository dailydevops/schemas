namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The <c>enumeration</c> facet for string types, whose value space is the whitespace-normalized text: an ordinal comparison.</summary>
public sealed class LexicalEnumerationFacet : ILexicalFacet
{
    private readonly string[] _values;

    /// <summary>Initializes a new instance of the <see cref="LexicalEnumerationFacet"/> class.</summary>
    /// <param name="values">The listed values.</param>
    public LexicalEnumerationFacet(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = new string[values.Count];
        for (var i = 0; i < _values.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(values[i]);
            _values[i] = values[i];
        }
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.Enumeration;

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical)
    {
        for (var i = 0; i < _values.Length; i++)
        {
            if (lexical.SequenceEqual(_values[i].AsSpan()))
            {
                return true;
            }
        }

        return false;
    }
}
