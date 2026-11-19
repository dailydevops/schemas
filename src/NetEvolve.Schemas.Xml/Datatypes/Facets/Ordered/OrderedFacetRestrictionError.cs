namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

/// <summary>Why a derivation step is not a valid restriction of the ordered and numeric facets of its base type.</summary>
/// <remarks>These are datatype-level reasons; mapping them to schema rule codes is the schema-level error reporting.</remarks>
public enum OrderedFacetRestrictionError
{
    /// <summary>The step is valid.</summary>
    None = 0,

    /// <summary>The facet is not applicable to the primitive type or to the XSD version, for example <c>explicitTimezone</c> in 1.0 mode.</summary>
    NotApplicable,

    /// <summary>The step changes a facet that the base type fixes.</summary>
    FixedFacetChanged,

    /// <summary>The step specifies both the inclusive and the exclusive bound of one side.</summary>
    InclusiveAndExclusive,

    /// <summary>A bound of the step lies outside the value space of the bounds of the base type, or is not ordered against them.</summary>
    BoundOutsideBase,

    /// <summary>The lower bound of the step is not below the upper bound, so the value space is empty.</summary>
    LowerBoundAboveUpperBound,

    /// <summary>The <c>totalDigits</c> of the step is greater than that of the base type.</summary>
    TotalDigitsLoosened,

    /// <summary>The <c>fractionDigits</c> of the step is greater than that of the base type.</summary>
    FractionDigitsLoosened,

    /// <summary>The effective <c>fractionDigits</c> is greater than the effective <c>totalDigits</c>.</summary>
    FractionDigitsGreaterThanTotalDigits,

    /// <summary>The base type requires or prohibits a timezone and the step specifies another value.</summary>
    ExplicitTimezoneChanged,
}
