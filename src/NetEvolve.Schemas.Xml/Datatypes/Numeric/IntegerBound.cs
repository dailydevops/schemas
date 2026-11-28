namespace NetEvolve.Schemas.Xml.Datatypes.Numeric;

using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>An inclusive bound of the value space of an integer type: the <c>minInclusive</c> or <c>maxInclusive</c> facet.</summary>
internal sealed class IntegerBound : IFoldableBound<XsdInteger>
{
    private readonly BigInteger _bound;

    internal IntegerBound(FacetKind kind, BigInteger bound)
    {
        Kind = kind;
        _bound = bound;
    }

    public FacetKind Kind { get; }

    public bool IsSatisfiedBy(in XsdInteger value) =>
        Kind == FacetKind.MinInclusive ? value.Value >= _bound : value.Value <= _bound;

    public bool IsImpliedBy(IValueFacet<XsdInteger> other) =>
        other is IntegerBound bound
        && bound.Kind == Kind
        && (Kind == FacetKind.MinInclusive ? bound._bound >= _bound : bound._bound <= _bound);
}
