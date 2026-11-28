namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The <c>enumeration</c> facet for string types, whose value space is the whitespace-normalized text: an ordinal comparison.</summary>
public sealed class LexicalEnumerationFacet : ILexicalFacet
{
    // Beyond this many values a hashed lookup beats the linear scan.
    private const int TableThreshold = 8;

    private readonly string[] _values;
    private readonly SpanInterningNameTable? _table;
    private readonly bool _containsEmpty;

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

        if (_values.Length <= TableThreshold)
        {
            return;
        }

        // Symbol zero of the table is the empty string, so the empty value is tracked separately.
        _table = new SpanInterningNameTable(_values.Length + 1, _values.Length + 1);
        foreach (var value in _values)
        {
            if (value.Length == 0)
            {
                _containsEmpty = true;
            }
            else
            {
                _ = _table.TryIntern(value.AsSpan(), out _);
            }
        }
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.Enumeration;

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical)
    {
        if (_table is not null)
        {
            return lexical.IsEmpty ? _containsEmpty : _table.TryGet(lexical, out _);
        }

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
