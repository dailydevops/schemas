namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>
/// The <c>enumeration</c> facet of a list type (Datatypes 1.0, section 4.3.5): a list value satisfies it when its item sequence equals
/// one of the listed item sequences, item by item in the value space of the item type.
/// </summary>
/// <typeparam name="TValue">The value type of the items.</typeparam>
/// <remarks>Limitation: every item of the lexical form is parsed again for each listed sequence it is compared with.</remarks>
internal sealed class ListEnumerationFacet<TValue> : ILexicalFacet
{
    private readonly IDatatypeDescriptor<TValue> _type;
    private readonly TValue[][] _sequences;

    /// <summary>Initializes a new instance of the <see cref="ListEnumerationFacet{TValue}"/> class.</summary>
    /// <param name="type">The item type whose value space defines equality.</param>
    /// <param name="sequences">The listed item sequences.</param>
    internal ListEnumerationFacet(IDatatypeDescriptor<TValue> type, IReadOnlyList<TValue[]> sequences)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(sequences);
        _type = type;
        _sequences = [.. sequences];
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.Enumeration;

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical)
    {
        for (var i = 0; i < _sequences.Length; i++)
        {
            if (Matches(lexical, _sequences[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool Matches(ReadOnlySpan<char> lexical, TValue[] sequence)
    {
        var cursor = new XsdListCursor(new TextRange(0, lexical.Length));
        var index = 0;
        while (cursor.TryNext(lexical, out var range))
        {
            if (index == sequence.Length)
            {
                return false;
            }

            var parsed = _type.Parse(range.Slice(lexical));
            if (!parsed.Success || !Equal(parsed.Value, sequence[index]))
            {
                return false;
            }

            index++;
        }

        return index == sequence.Length;
    }

    private bool Equal(in TValue a, in TValue b)
    {
        var order = _type.Compare(in a, in b);
        return order == XsdOrder.Equal || (order == XsdOrder.Incomparable && _type.IsIdentical(in a, in b));
    }
}
