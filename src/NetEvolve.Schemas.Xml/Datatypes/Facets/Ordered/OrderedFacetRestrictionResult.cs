namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The outcome of checking a derivation step of the ordered and numeric facets: valid, or the reason and the facet it concerns.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct OrderedFacetRestrictionResult
{
    /// <summary>Initializes a new instance of the <see cref="OrderedFacetRestrictionResult"/> struct.</summary>
    /// <param name="error">The reason, <see cref="OrderedFacetRestrictionError.None"/> when valid.</param>
    /// <param name="facet">The facet the reason concerns, <see cref="FacetKind.None"/> when valid.</param>
    public OrderedFacetRestrictionResult(OrderedFacetRestrictionError error, FacetKind facet)
    {
        Error = error;
        Facet = facet;
    }

    /// <summary>Gets the reason, <see cref="OrderedFacetRestrictionError.None"/> when valid.</summary>
    public OrderedFacetRestrictionError Error { get; }

    /// <summary>Gets the facet the reason concerns, <see cref="FacetKind.None"/> when valid.</summary>
    public FacetKind Facet { get; }

    /// <summary>Gets a value indicating whether the step is a valid restriction.</summary>
    public bool IsValid => Error == OrderedFacetRestrictionError.None;
}
