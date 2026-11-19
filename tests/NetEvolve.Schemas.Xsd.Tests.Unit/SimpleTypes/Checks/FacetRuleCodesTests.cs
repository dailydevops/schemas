namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

/// <summary>Pins the mapping from the datatype-level reasons of the Xml package to the rules of the catalog, one row per reason.</summary>
public class FacetRuleCodesTests
{
    [Test]
    [Arguments(FacetRestrictionError.NotApplicable, FacetKind.Length, "cos-applicable-facets")]
    [Arguments(FacetRestrictionError.NotApplicable, FacetKind.WhiteSpace, "cos-applicable-facets")]
    [Arguments(FacetRestrictionError.FixedFacetChanged, FacetKind.Length, "length-valid-restriction")]
    [Arguments(FacetRestrictionError.FixedFacetChanged, FacetKind.MinLength, "minLength-valid-restriction")]
    [Arguments(FacetRestrictionError.FixedFacetChanged, FacetKind.MaxLength, "maxLength-valid-restriction")]
    [Arguments(FacetRestrictionError.FixedFacetChanged, FacetKind.WhiteSpace, "whiteSpace-valid-restriction")]
    [Arguments(FacetRestrictionError.WhiteSpaceLoosened, FacetKind.WhiteSpace, "whiteSpace-valid-restriction")]
    [Arguments(FacetRestrictionError.LengthChanged, FacetKind.Length, "length-valid-restriction")]
    [Arguments(FacetRestrictionError.MinLengthLoosened, FacetKind.MinLength, "minLength-valid-restriction")]
    [Arguments(FacetRestrictionError.MaxLengthLoosened, FacetKind.MaxLength, "maxLength-valid-restriction")]
    [Arguments(FacetRestrictionError.LengthConflict, FacetKind.MinLength, "length-minLength-maxLength")]
    [Arguments(FacetRestrictionError.LengthConflict, FacetKind.MaxLength, "length-minLength-maxLength")]
    [Arguments(
        FacetRestrictionError.MinLengthGreaterThanMaxLength,
        FacetKind.MinLength,
        "minLength-less-than-equal-to-maxLength"
    )]
    public async Task CoreReason_MapsToItsRule(FacetRestrictionError error, FacetKind facet, string rule)
    {
        var code = FacetRuleCodes.Of(new FacetRestrictionResult(error, facet));

        await Assert.That(code).IsEqualTo(FacetFixture.CodeOf(rule));
    }

    [Test]
    [Arguments(OrderedFacetRestrictionError.NotApplicable, FacetKind.TotalDigits, "cos-applicable-facets")]
    [Arguments(
        OrderedFacetRestrictionError.FixedFacetChanged,
        FacetKind.MinInclusive,
        "minInclusive-valid-restriction"
    )]
    [Arguments(
        OrderedFacetRestrictionError.FixedFacetChanged,
        FacetKind.MinExclusive,
        "minExclusive-valid-restriction"
    )]
    [Arguments(
        OrderedFacetRestrictionError.FixedFacetChanged,
        FacetKind.MaxInclusive,
        "maxInclusive-valid-restriction"
    )]
    [Arguments(
        OrderedFacetRestrictionError.FixedFacetChanged,
        FacetKind.MaxExclusive,
        "maxExclusive-valid-restriction"
    )]
    [Arguments(OrderedFacetRestrictionError.FixedFacetChanged, FacetKind.TotalDigits, "totalDigits-valid-restriction")]
    [Arguments(
        OrderedFacetRestrictionError.FixedFacetChanged,
        FacetKind.FractionDigits,
        "fractionDigits-valid-restriction"
    )]
    [Arguments(
        OrderedFacetRestrictionError.FixedFacetChanged,
        FacetKind.ExplicitTimezone,
        "timezone-valid-restriction"
    )]
    [Arguments(OrderedFacetRestrictionError.InclusiveAndExclusive, FacetKind.MaxExclusive, "maxInclusive-maxExclusive")]
    [Arguments(OrderedFacetRestrictionError.InclusiveAndExclusive, FacetKind.MinExclusive, "minInclusive-minExclusive")]
    [Arguments(OrderedFacetRestrictionError.BoundOutsideBase, FacetKind.MinInclusive, "minInclusive-valid-restriction")]
    [Arguments(OrderedFacetRestrictionError.BoundOutsideBase, FacetKind.MinExclusive, "minExclusive-valid-restriction")]
    [Arguments(OrderedFacetRestrictionError.BoundOutsideBase, FacetKind.MaxInclusive, "maxInclusive-valid-restriction")]
    [Arguments(OrderedFacetRestrictionError.BoundOutsideBase, FacetKind.MaxExclusive, "maxExclusive-valid-restriction")]
    [Arguments(
        OrderedFacetRestrictionError.TotalDigitsLoosened,
        FacetKind.TotalDigits,
        "totalDigits-valid-restriction"
    )]
    [Arguments(
        OrderedFacetRestrictionError.FractionDigitsLoosened,
        FacetKind.FractionDigits,
        "fractionDigits-valid-restriction"
    )]
    [Arguments(
        OrderedFacetRestrictionError.FractionDigitsGreaterThanTotalDigits,
        FacetKind.FractionDigits,
        "fractionDigits-totalDigits"
    )]
    [Arguments(
        OrderedFacetRestrictionError.ExplicitTimezoneChanged,
        FacetKind.ExplicitTimezone,
        "timezone-valid-restriction"
    )]
    public async Task OrderedReason_MapsToItsRule(OrderedFacetRestrictionError error, FacetKind facet, string rule)
    {
        var code = FacetRuleCodes.Of(new OrderedFacetRestrictionResult(error, facet), FacetKind.None);

        await Assert.That(code).IsEqualTo(FacetFixture.CodeOf(rule));
    }

    [Test]
    [Arguments(FacetKind.MinInclusive, FacetKind.MaxInclusive, "minInclusive-less-than-equal-to-maxInclusive")]
    [Arguments(FacetKind.MinExclusive, FacetKind.MaxExclusive, "minExclusive-less-than-equal-to-maxExclusive")]
    [Arguments(FacetKind.MinExclusive, FacetKind.MaxInclusive, "minExclusive-less-than-maxInclusive")]
    [Arguments(FacetKind.MinInclusive, FacetKind.MaxExclusive, "minInclusive-less-than-maxExclusive")]
    public async Task LowerBoundAboveUpperBound_MapsByThePairOfBounds(FacetKind lower, FacetKind upper, string rule)
    {
        var result = new OrderedFacetRestrictionResult(OrderedFacetRestrictionError.LowerBoundAboveUpperBound, lower);

        await Assert.That(FacetRuleCodes.Of(result, upper)).IsEqualTo(FacetFixture.CodeOf(rule));
    }

    [Test]
    [Arguments(FacetKind.Length, "length-valid-restriction")]
    [Arguments(FacetKind.MinLength, "minLength-valid-restriction")]
    [Arguments(FacetKind.MaxLength, "maxLength-valid-restriction")]
    [Arguments(FacetKind.Enumeration, "enumeration-valid-restriction")]
    [Arguments(FacetKind.WhiteSpace, "whiteSpace-valid-restriction")]
    [Arguments(FacetKind.MaxInclusive, "maxInclusive-valid-restriction")]
    [Arguments(FacetKind.MaxExclusive, "maxExclusive-valid-restriction")]
    [Arguments(FacetKind.MinExclusive, "minExclusive-valid-restriction")]
    [Arguments(FacetKind.MinInclusive, "minInclusive-valid-restriction")]
    [Arguments(FacetKind.TotalDigits, "totalDigits-valid-restriction")]
    [Arguments(FacetKind.FractionDigits, "fractionDigits-valid-restriction")]
    [Arguments(FacetKind.ExplicitTimezone, "timezone-valid-restriction")]
    [Arguments(FacetKind.Pattern, "st-restrict-facets")]
    [Arguments(FacetKind.None, "st-restrict-facets")]
    public async Task ValidRestriction_NamesTheRuleOfTheFacet(FacetKind facet, string rule) =>
        await Assert.That(FacetRuleCodes.ValidRestriction(facet)).IsEqualTo(FacetFixture.CodeOf(rule));
}
