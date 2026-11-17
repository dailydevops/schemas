namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>An immutable set of <see cref="FacetKind"/> values, used for the fixed flags of a derivation step. <see langword="default"/> is the empty set.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct FacetKindSet
{
    private readonly uint _bits;

    private FacetKindSet(uint bits) => _bits = bits;

    /// <summary>Determines whether the set contains a facet.</summary>
    /// <param name="kind">The facet.</param>
    /// <returns><see langword="true"/> when contained.</returns>
    public bool Contains(FacetKind kind) => (_bits & (1u << (int)kind)) != 0;

    /// <summary>Creates a set that additionally contains a facet.</summary>
    /// <param name="kind">The facet.</param>
    /// <returns>The new set.</returns>
    public FacetKindSet With(FacetKind kind) => new(_bits | (1u << (int)kind));

    /// <summary>Creates the union of this set and another.</summary>
    /// <param name="other">The other set.</param>
    /// <returns>The union.</returns>
    public FacetKindSet Union(FacetKindSet other) => new(_bits | other._bits);
}
