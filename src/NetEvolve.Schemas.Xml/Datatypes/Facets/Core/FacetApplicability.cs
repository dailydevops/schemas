namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The applicability table: which constraining facet a restriction may specify on which variety and primitive (XSD 1.1 Part 2, section 4.1.5).</summary>
/// <remarks>
/// Limitation: the table follows the specification, so the length facets are applicable to the binary types; their evaluation
/// is a later package. <c>assertions</c> and <c>explicitTimezone</c> are applicable in 1.1 only.
/// </remarks>
public static class FacetApplicability
{
    /// <summary>Determines whether a facet may be specified on a type.</summary>
    /// <param name="facet">The facet.</param>
    /// <param name="variety">The variety of the type.</param>
    /// <param name="primitive">The primitive ancestor of an atomic type; <see cref="PrimitiveKind.None"/> for lists and unions.</param>
    /// <param name="version">The XSD version.</param>
    /// <returns><see langword="true"/> when the facet is applicable.</returns>
    public static bool IsApplicable(
        FacetKind facet,
        DatatypeVariety variety,
        PrimitiveKind primitive,
        XsdVersion version
    )
    {
        if (facet == FacetKind.None)
        {
            return false;
        }

        if (facet == FacetKind.Assertions)
        {
            return version == XsdVersion.V11;
        }

        if (facet is FacetKind.Pattern or FacetKind.Enumeration)
        {
            return true;
        }

        return variety switch
        {
            DatatypeVariety.Union => false,
            DatatypeVariety.List => facet
                is FacetKind.Length
                    or FacetKind.MinLength
                    or FacetKind.MaxLength
                    or FacetKind.WhiteSpace,
            _ => facet switch
            {
                FacetKind.WhiteSpace => true,
                FacetKind.Length or FacetKind.MinLength or FacetKind.MaxLength => HasLength(primitive),
                FacetKind.TotalDigits or FacetKind.FractionDigits => primitive == PrimitiveKind.DecimalType,
                FacetKind.ExplicitTimezone => version == XsdVersion.V11 && HasTimezone(primitive),
                _ => IsBounded(primitive),
            },
        };
    }

    private static bool HasLength(PrimitiveKind primitive) =>
        primitive
            is PrimitiveKind.StringType
                or PrimitiveKind.HexBinary
                or PrimitiveKind.Base64Binary
                or PrimitiveKind.AnyUri
                or PrimitiveKind.QName
                or PrimitiveKind.Notation;

    private static bool HasTimezone(PrimitiveKind primitive) =>
        primitive
            is PrimitiveKind.DateTime
                or PrimitiveKind.Time
                or PrimitiveKind.Date
                or PrimitiveKind.GYearMonth
                or PrimitiveKind.GYear
                or PrimitiveKind.GMonthDay
                or PrimitiveKind.GDay
                or PrimitiveKind.GMonth;

    // The bounds (min and max, inclusive and exclusive) need an ordered value space.
    private static bool IsBounded(PrimitiveKind primitive) =>
        primitive
            is PrimitiveKind.DecimalType
                or PrimitiveKind.FloatType
                or PrimitiveKind.DoubleType
                or PrimitiveKind.Duration
        || HasTimezone(primitive);
}
