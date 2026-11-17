namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A facet that is only identified by its kind.</summary>
internal sealed class FakeFacet : IFacet
{
    public FakeFacet(FacetKind kind) => Kind = kind;

    public FacetKind Kind { get; }
}
