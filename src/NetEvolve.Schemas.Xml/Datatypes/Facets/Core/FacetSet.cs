namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The facets that one derivation step specifies, with the fixed flags. <see langword="default"/> is a step that specifies nothing.</summary>
/// <remarks>Limitation: only length, minLength, maxLength, and whiteSpace are modeled; <c>enumeration</c> is a list of typed values and is carried by <see cref="EnumerationFacet{TValue}"/>.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct FacetSet
{
    /// <summary>Initializes a new instance of the <see cref="FacetSet"/> struct.</summary>
    /// <param name="length">The <c>length</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="minLength">The <c>minLength</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="maxLength">The <c>maxLength</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="whiteSpace">The <c>whiteSpace</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="fixedFacets">The facets that further restrictions may not change.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">A length is negative.</exception>
    public FacetSet(
        int? length = null,
        int? minLength = null,
        int? maxLength = null,
        WhitespaceMode? whiteSpace = null,
        FacetKindSet fixedFacets = default
    )
    {
        if (length.HasValue)
        {
            Argument.ThrowIfLessThan(length.Value, 0);
        }

        if (minLength.HasValue)
        {
            Argument.ThrowIfLessThan(minLength.Value, 0);
        }

        if (maxLength.HasValue)
        {
            Argument.ThrowIfLessThan(maxLength.Value, 0);
        }

        Length = length;
        MinLength = minLength;
        MaxLength = maxLength;
        WhiteSpace = whiteSpace;
        FixedFacets = fixedFacets;
    }

    /// <summary>Gets the <c>length</c> facet.</summary>
    public int? Length { get; }

    /// <summary>Gets the <c>minLength</c> facet.</summary>
    public int? MinLength { get; }

    /// <summary>Gets the <c>maxLength</c> facet.</summary>
    public int? MaxLength { get; }

    /// <summary>Gets the <c>whiteSpace</c> facet.</summary>
    public WhitespaceMode? WhiteSpace { get; }

    /// <summary>Gets the facets that further restrictions may not change.</summary>
    public FacetKindSet FixedFacets { get; }

    /// <summary>Gets the facets that the step specifies.</summary>
    public FacetKindSet Specified
    {
        get
        {
            var set = default(FacetKindSet);
            if (Length.HasValue)
            {
                set = set.With(FacetKind.Length);
            }

            if (MinLength.HasValue)
            {
                set = set.With(FacetKind.MinLength);
            }

            if (MaxLength.HasValue)
            {
                set = set.With(FacetKind.MaxLength);
            }

            return WhiteSpace.HasValue ? set.With(FacetKind.WhiteSpace) : set;
        }
    }
}
