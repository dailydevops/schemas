namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Checks and applies a derivation step over the facet sets of the base type (XSD 1.1 Part 2, section 4.3 and the facet-specific constraints).</summary>
/// <remarks>Limitation: only length, minLength, maxLength, and whiteSpace are checked; the facets of later packages plug in here.</remarks>
public static class FacetRestriction
{
    /// <summary>Determines whether a step is a valid restriction of its base type.</summary>
    /// <param name="baseSet">The effective facets of the base type.</param>
    /// <param name="derived">The facets the step specifies.</param>
    /// <param name="variety">The variety of the derived type.</param>
    /// <param name="primitive">The primitive ancestor, <see cref="PrimitiveKind.None"/> for lists and unions.</param>
    /// <param name="version">The XSD version.</param>
    /// <returns>The first violation in the order applicability, fixed, loosened, then consistency; valid otherwise.</returns>
    public static FacetRestrictionResult Validate(
        FacetSet baseSet,
        FacetSet derived,
        DatatypeVariety variety,
        PrimitiveKind primitive,
        XsdVersion version
    )
    {
        var specified = derived.Specified;
        foreach (var kind in Checked)
        {
            if (specified.Contains(kind) && !FacetApplicability.IsApplicable(kind, variety, primitive, version))
            {
                return new(FacetRestrictionError.NotApplicable, kind);
            }
        }

        var fixedChange = FixedChange(baseSet, derived);
        if (fixedChange != FacetKind.None)
        {
            return new(FacetRestrictionError.FixedFacetChanged, fixedChange);
        }

        // The whiteSpace modes are ordered by strength: preserve, replace, collapse. A null mode never compares.
        if (derived.WhiteSpace < baseSet.WhiteSpace)
        {
            return new(FacetRestrictionError.WhiteSpaceLoosened, FacetKind.WhiteSpace);
        }

        if (Differs(baseSet.Length, derived.Length))
        {
            return new(FacetRestrictionError.LengthChanged, FacetKind.Length);
        }

        if (derived.MinLength < baseSet.MinLength)
        {
            return new(FacetRestrictionError.MinLengthLoosened, FacetKind.MinLength);
        }

        if (derived.MaxLength > baseSet.MaxLength)
        {
            return new(FacetRestrictionError.MaxLengthLoosened, FacetKind.MaxLength);
        }

        return CheckConsistency(Apply(baseSet, derived));
    }

    /// <summary>Computes the effective facets of the derived type: the step overlays the base, and the fixed flags are united.</summary>
    /// <param name="baseSet">The effective facets of the base type.</param>
    /// <param name="derived">The facets the step specifies.</param>
    /// <returns>The effective facets.</returns>
    public static FacetSet Apply(FacetSet baseSet, FacetSet derived) =>
        new(
            derived.Length ?? baseSet.Length,
            derived.MinLength ?? baseSet.MinLength,
            derived.MaxLength ?? baseSet.MaxLength,
            derived.WhiteSpace ?? baseSet.WhiteSpace,
            baseSet.FixedFacets.Union(derived.FixedFacets)
        );

    private static FacetRestrictionResult CheckConsistency(FacetSet effective)
    {
        if (effective.Length < effective.MinLength)
        {
            return new(FacetRestrictionError.LengthConflict, FacetKind.MinLength);
        }

        if (effective.Length > effective.MaxLength)
        {
            return new(FacetRestrictionError.LengthConflict, FacetKind.MaxLength);
        }

        return effective.MinLength > effective.MaxLength
            ? new(FacetRestrictionError.MinLengthGreaterThanMaxLength, FacetKind.MinLength)
            : default;
    }

    private static FacetKind FixedChange(FacetSet baseSet, FacetSet derived)
    {
        if (Changed(baseSet, FacetKind.Length, baseSet.Length, derived.Length))
        {
            return FacetKind.Length;
        }

        if (Changed(baseSet, FacetKind.MinLength, baseSet.MinLength, derived.MinLength))
        {
            return FacetKind.MinLength;
        }

        if (Changed(baseSet, FacetKind.MaxLength, baseSet.MaxLength, derived.MaxLength))
        {
            return FacetKind.MaxLength;
        }

        return
            baseSet.FixedFacets.Contains(FacetKind.WhiteSpace)
            && baseSet.WhiteSpace.HasValue
            && derived.WhiteSpace.HasValue
            && baseSet.WhiteSpace != derived.WhiteSpace
            ? FacetKind.WhiteSpace
            : FacetKind.None;
    }

    private static readonly FacetKind[] Checked =
    [
        FacetKind.Length,
        FacetKind.MinLength,
        FacetKind.MaxLength,
        FacetKind.WhiteSpace,
    ];

    private static bool Differs(int? baseValue, int? derivedValue) =>
        baseValue.HasValue && derivedValue.HasValue && baseValue != derivedValue;

    private static bool Changed(FacetSet baseSet, FacetKind kind, int? baseValue, int? derivedValue) =>
        baseSet.FixedFacets.Contains(kind) && Differs(baseValue, derivedValue);
}
