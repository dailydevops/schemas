namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The constraining facets (XSD 1.1 Part 2, section 4.3), in no particular order, plus <see cref="None"/>.</summary>
public enum FacetKind
{
    /// <summary>No facet; the failure did not come from a facet.</summary>
    None = 0,

    /// <summary>The <c>length</c> facet.</summary>
    Length,

    /// <summary>The <c>minLength</c> facet.</summary>
    MinLength,

    /// <summary>The <c>maxLength</c> facet.</summary>
    MaxLength,

    /// <summary>The <c>pattern</c> facet.</summary>
    Pattern,

    /// <summary>The <c>enumeration</c> facet.</summary>
    Enumeration,

    /// <summary>The <c>whiteSpace</c> facet.</summary>
    WhiteSpace,

    /// <summary>The <c>maxInclusive</c> facet.</summary>
    MaxInclusive,

    /// <summary>The <c>maxExclusive</c> facet.</summary>
    MaxExclusive,

    /// <summary>The <c>minExclusive</c> facet.</summary>
    MinExclusive,

    /// <summary>The <c>minInclusive</c> facet.</summary>
    MinInclusive,

    /// <summary>The <c>totalDigits</c> facet.</summary>
    TotalDigits,

    /// <summary>The <c>fractionDigits</c> facet.</summary>
    FractionDigits,

    /// <summary>The <c>assertions</c> facet, XSD 1.1 only.</summary>
    Assertions,

    /// <summary>The <c>explicitTimezone</c> facet, XSD 1.1 only.</summary>
    ExplicitTimezone,
}
