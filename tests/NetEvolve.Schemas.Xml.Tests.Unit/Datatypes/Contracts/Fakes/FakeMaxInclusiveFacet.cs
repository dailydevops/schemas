namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A value-space facet that accepts values up to an inclusive maximum.</summary>
internal sealed class FakeMaxInclusiveFacet(int maximum) : IValueFacet<int>
{
    public FacetKind Kind => FacetKind.MaxInclusive;

    public bool IsSatisfiedBy(in int value) => value <= maximum;
}
