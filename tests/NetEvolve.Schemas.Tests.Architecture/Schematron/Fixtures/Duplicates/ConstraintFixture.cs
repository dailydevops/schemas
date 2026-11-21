namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Duplicates;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Violating fixture: a second facet, found because it implements the facet seam.</summary>
internal abstract class ConstraintFixture : IFacet
{
    public abstract FacetKind Kind { get; }
}
