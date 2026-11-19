namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// The bounds facets <c>minInclusive</c>, <c>minExclusive</c>, <c>maxInclusive</c>, and <c>maxExclusive</c> (XSD 1.1 Part 2,
/// sections 4.3.7 to 4.3.10): a value satisfies the facet when it is ordered against the bound as the facet requires.
/// </summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
/// <remarks>
/// <para>
/// The order is <see cref="IDatatypeDescriptor{TValue}.Compare"/> of the type, so decimal, integer, float, double, date and time,
/// Gregorian, and duration values use the order of their value space. A value that is not ordered against the bound
/// (<see cref="XsdOrder.Incomparable"/>), such as a dateTime without timezone inside the 14 hour window of the bound, a duration
/// that the four reference dateTimes do not order, or a NaN, fails every bounds facet.
/// </para>
/// <para>
/// Limitation: NaN as the bound follows the order of the type, where NaN equals itself (#172), so an inclusive NaN bound accepts NaN
/// and an exclusive one does not; a finite value never satisfies a NaN bound. Parse the bound with the descriptor and version that
/// the facet restricts: values of different kinds are incomparable.
/// </para>
/// </remarks>
public sealed class BoundFacet<TValue> : IValueFacet<TValue>
{
    private readonly IDatatypeDescriptor<TValue> _type;
    private readonly TValue _bound;

    /// <summary>Initializes a new instance of the <see cref="BoundFacet{TValue}"/> class.</summary>
    /// <param name="kind">The facet: <see cref="FacetKind.MinInclusive"/>, <see cref="FacetKind.MinExclusive"/>, <see cref="FacetKind.MaxInclusive"/>, or <see cref="FacetKind.MaxExclusive"/>.</param>
    /// <param name="type">The type whose value space defines the order.</param>
    /// <param name="bound">The bound, a value of <paramref name="type"/>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="kind"/> is not one of the four bounds facets.</exception>
    public BoundFacet(FacetKind kind, IDatatypeDescriptor<TValue> type, TValue bound)
    {
        Argument.ThrowIfNull(type);
        Argument.ThrowIfLessThan((int)kind, (int)FacetKind.MaxInclusive);
        Argument.ThrowIfGreaterThan((int)kind, (int)FacetKind.MinInclusive);
        Kind = kind;
        _type = type;
        _bound = bound;
    }

    /// <inheritdoc />
    public FacetKind Kind { get; }

    /// <summary>Gets the bound.</summary>
    public TValue Bound => _bound;

    /// <inheritdoc />
    public bool IsSatisfiedBy(in TValue value)
    {
        var order = _type.Compare(in value, in _bound);
        return Kind switch
        {
            FacetKind.MinInclusive => order is XsdOrder.Greater or XsdOrder.Equal,
            FacetKind.MinExclusive => order == XsdOrder.Greater,
            FacetKind.MaxInclusive => order is XsdOrder.Less or XsdOrder.Equal,
            _ => order == XsdOrder.Less,
        };
    }
}
