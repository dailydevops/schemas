namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

/// <summary>
/// Maps the datatype-level reasons of the Xml package to the schema rules of the catalog (design #146 DP-9: a foreign result is mapped,
/// never exposed).
/// </summary>
/// <remarks>
/// A change of a facet that the base type fixes is reported under the valid-restriction rule of that facet, because each of these rules
/// states the fixed clause itself (Datatypes sections 4.3.1.4 to 4.3.12.4). A reason that no row covers falls back to
/// <c>st-restrict-facets</c>.
/// </remarks>
internal static class FacetRuleCodes
{
    /// <summary>Gets the rule for a violation of the length or whiteSpace facets.</summary>
    /// <param name="result">The result of the core facet check.</param>
    /// <returns>The code of the violated rule.</returns>
    internal static DiagnosticCode Of(FacetRestrictionResult result) =>
        result.Error switch
        {
            FacetRestrictionError.NotApplicable => XsdRuleCodes.CosApplicableFacets,
            FacetRestrictionError.FixedFacetChanged => ValidRestriction(result.Facet),
            FacetRestrictionError.WhiteSpaceLoosened => XsdRuleCodes.WhiteSpaceValidRestriction,
            FacetRestrictionError.LengthChanged => XsdRuleCodes.LengthValidRestriction,
            FacetRestrictionError.MinLengthLoosened => XsdRuleCodes.MinLengthValidRestriction,
            FacetRestrictionError.MaxLengthLoosened => XsdRuleCodes.MaxLengthValidRestriction,
            FacetRestrictionError.LengthConflict => XsdRuleCodes.LengthMinLengthMaxLength,
            FacetRestrictionError.MinLengthGreaterThanMaxLength => XsdRuleCodes.MinLengthLessThanEqualToMaxLength,
            _ => XsdRuleCodes.StRestrictFacets,
        };

    /// <summary>Gets the rule for a violation of the bounds or digits facets.</summary>
    /// <param name="result">The result of the ordered facet check.</param>
    /// <param name="upper">The upper bound of the step that the lower bound is compared with, <see cref="FacetKind.None"/> when there is none.</param>
    /// <returns>The code of the violated rule.</returns>
    internal static DiagnosticCode Of(OrderedFacetRestrictionResult result, FacetKind upper) =>
        result.Error switch
        {
            OrderedFacetRestrictionError.NotApplicable => XsdRuleCodes.CosApplicableFacets,
            OrderedFacetRestrictionError.FixedFacetChanged or OrderedFacetRestrictionError.BoundOutsideBase =>
                ValidRestriction(result.Facet),
            OrderedFacetRestrictionError.InclusiveAndExclusive => result.Facet == FacetKind.MaxExclusive
                ? XsdRuleCodes.MaxInclusiveMaxExclusive
                : XsdRuleCodes.MinInclusiveMinExclusive,
            OrderedFacetRestrictionError.LowerBoundAboveUpperBound => LowerAboveUpper(result.Facet, upper),
            OrderedFacetRestrictionError.TotalDigitsLoosened => XsdRuleCodes.TotalDigitsValidRestriction,
            OrderedFacetRestrictionError.FractionDigitsLoosened => XsdRuleCodes.FractionDigitsValidRestriction,
            OrderedFacetRestrictionError.FractionDigitsGreaterThanTotalDigits => XsdRuleCodes.FractionDigitsTotalDigits,
            OrderedFacetRestrictionError.ExplicitTimezoneChanged => XsdRuleCodes.TimezoneValidRestriction,
            _ => XsdRuleCodes.StRestrictFacets,
        };

    /// <summary>Gets the valid-restriction rule of a facet.</summary>
    /// <param name="facet">The facet.</param>
    /// <returns>The code of the rule, for example <c>maxLength-valid-restriction</c> for <see cref="FacetKind.MaxLength"/>.</returns>
    internal static DiagnosticCode ValidRestriction(FacetKind facet) =>
        facet switch
        {
            FacetKind.Length => XsdRuleCodes.LengthValidRestriction,
            FacetKind.MinLength => XsdRuleCodes.MinLengthValidRestriction,
            FacetKind.MaxLength => XsdRuleCodes.MaxLengthValidRestriction,
            FacetKind.Enumeration => XsdRuleCodes.EnumerationValidRestriction,
            FacetKind.WhiteSpace => XsdRuleCodes.WhiteSpaceValidRestriction,
            FacetKind.MaxInclusive => XsdRuleCodes.MaxInclusiveValidRestriction,
            FacetKind.MaxExclusive => XsdRuleCodes.MaxExclusiveValidRestriction,
            FacetKind.MinExclusive => XsdRuleCodes.MinExclusiveValidRestriction,
            FacetKind.MinInclusive => XsdRuleCodes.MinInclusiveValidRestriction,
            FacetKind.TotalDigits => XsdRuleCodes.TotalDigitsValidRestriction,
            FacetKind.FractionDigits => XsdRuleCodes.FractionDigitsValidRestriction,
            FacetKind.ExplicitTimezone => XsdRuleCodes.TimezoneValidRestriction,
            _ => XsdRuleCodes.StRestrictFacets,
        };

    private static DiagnosticCode LowerAboveUpper(FacetKind lower, FacetKind upper) =>
        (lower, upper) switch
        {
            (FacetKind.MinInclusive, FacetKind.MaxInclusive) => XsdRuleCodes.MinInclusiveLessThanEqualToMaxInclusive,
            (FacetKind.MinExclusive, FacetKind.MaxExclusive) => XsdRuleCodes.MinExclusiveLessThanEqualToMaxExclusive,
            (FacetKind.MinExclusive, FacetKind.MaxInclusive) => XsdRuleCodes.MinExclusiveLessThanMaxInclusive,
            (FacetKind.MinInclusive, FacetKind.MaxExclusive) => XsdRuleCodes.MinInclusiveLessThanMaxExclusive,
            _ => XsdRuleCodes.StRestrictFacets,
        };
}
