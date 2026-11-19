namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>The outcome of checking a restriction step: whether it is valid, and the facets that restrictions of the new type may not change.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct FacetCheckResult
{
    internal FacetCheckResult(bool isValid, FacetKindSet fixedFacets)
    {
        IsValid = isValid;
        Fixed = fixedFacets;
    }

    /// <summary>Gets a value indicating whether the step is a valid restriction and no issue was reported.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the fixed facets of the new type: those of the base type, those the type system fixes, and those the step fixes.</summary>
    public FacetKindSet Fixed { get; }
}
