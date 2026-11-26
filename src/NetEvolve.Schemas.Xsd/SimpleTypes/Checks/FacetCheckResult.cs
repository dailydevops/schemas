namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;

/// <summary>The outcome of checking a restriction step: whether it is valid, and the facets that restrictions of the new type may not change.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct FacetCheckResult
{
    internal FacetCheckResult(bool isValid, FacetKindSet fixedFacets, PatternFacet? compiledPattern = null)
    {
        IsValid = isValid;
        Fixed = fixedFacets;
        CompiledPattern = compiledPattern;
    }

    /// <summary>Gets a value indicating whether the step is a valid restriction and no issue was reported.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the fixed facets of the new type: those of the base type, those the type system fixes, and those the step fixes.</summary>
    public FacetKindSet Fixed { get; }

    /// <summary>Gets the facet that the check compiled from the only pattern of the step, <see langword="null"/> when the step states no pattern, several, or one that did not compile.</summary>
    public PatternFacet? CompiledPattern { get; }
}
