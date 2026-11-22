namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The <c>enumeration</c> facet (XSD 1.1 Part 2, section 4.3.5): a value satisfies it when it equals one of the listed values in the value space.</summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
/// <remarks>
/// Limitation: equality is <see cref="XsdOrder.Equal"/> from <see cref="IDatatypeDescriptor{TValue}.Compare"/>; when the type is
/// unordered, or the values are incomparable, identity from <see cref="IDatatypeDescriptor{TValue}.IsIdentical"/> decides. Span views such as
/// <c>XsdString</c> have no text, so string types use <see cref="LexicalEnumerationFacet"/>.
/// </remarks>
public sealed class EnumerationFacet<TValue> : IValueFacet<TValue>
{
    private readonly IDatatypeDescriptor<TValue> _type;
    private readonly TValue[] _values;

    /// <summary>Initializes a new instance of the <see cref="EnumerationFacet{TValue}"/> class.</summary>
    /// <param name="type">The type whose value space defines equality.</param>
    /// <param name="values">The listed values.</param>
    public EnumerationFacet(IDatatypeDescriptor<TValue> type, IReadOnlyList<TValue> values)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(values);
        _type = type;
        _values = new TValue[values.Count];
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i] = values[i];
        }
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.Enumeration;

    /// <inheritdoc />
    public bool IsSatisfiedBy(in TValue value)
    {
        for (var i = 0; i < _values.Length; i++)
        {
            var order = _type.Compare(in value, in _values[i]);
            if (
                order == XsdOrder.Equal
                || (order == XsdOrder.Incomparable && _type.IsIdentical(in value, in _values[i]))
            )
            {
                return true;
            }
        }

        return false;
    }
}
