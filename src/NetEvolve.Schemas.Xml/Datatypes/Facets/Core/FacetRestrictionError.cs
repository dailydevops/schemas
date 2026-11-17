namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>Why a derivation step is not a valid restriction of the facets of its base type.</summary>
/// <remarks>These are datatype-level reasons; mapping them to schema rule codes is the schema-level error reporting.</remarks>
public enum FacetRestrictionError
{
    /// <summary>The step is valid.</summary>
    None = 0,

    /// <summary>The facet is not applicable to the variety and primitive of the type.</summary>
    NotApplicable,

    /// <summary>The step changes a facet that the base type fixes.</summary>
    FixedFacetChanged,

    /// <summary>The <c>whiteSpace</c> of the step is weaker than that of the base type.</summary>
    WhiteSpaceLoosened,

    /// <summary>The step changes the <c>length</c> of the base type.</summary>
    LengthChanged,

    /// <summary>The <c>minLength</c> of the step is smaller than that of the base type.</summary>
    MinLengthLoosened,

    /// <summary>The <c>maxLength</c> of the step is greater than that of the base type.</summary>
    MaxLengthLoosened,

    /// <summary>The <c>length</c> conflicts with the effective <c>minLength</c> or <c>maxLength</c>.</summary>
    LengthConflict,

    /// <summary>The effective <c>minLength</c> is greater than the effective <c>maxLength</c>.</summary>
    MinLengthGreaterThanMaxLength,
}
