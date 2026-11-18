namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class ValidationRulesTests
{
    [Test]
    [Arguments(FacetKind.Length, "cvc-length-valid")]
    [Arguments(FacetKind.MinLength, "cvc-minLength-valid")]
    [Arguments(FacetKind.MaxLength, "cvc-maxLength-valid")]
    [Arguments(FacetKind.Pattern, "cvc-pattern-valid")]
    [Arguments(FacetKind.Enumeration, "cvc-enumeration-valid")]
    [Arguments(FacetKind.MaxInclusive, "cvc-maxInclusive-valid")]
    [Arguments(FacetKind.MaxExclusive, "cvc-maxExclusive-valid")]
    [Arguments(FacetKind.MinExclusive, "cvc-minExclusive-valid")]
    [Arguments(FacetKind.MinInclusive, "cvc-minInclusive-valid")]
    [Arguments(FacetKind.TotalDigits, "cvc-totalDigits-valid")]
    [Arguments(FacetKind.FractionDigits, "cvc-fractionDigits-valid")]
    [Arguments(FacetKind.Assertions, "cvc-assertions-valid")]
    [Arguments(FacetKind.ExplicitTimezone, "cvc-explicitTimezone-valid")]
    [Arguments(FacetKind.WhiteSpace, "cvc-datatype-valid")]
    [Arguments(FacetKind.None, "cvc-datatype-valid")]
    public async Task ForFacet_MapsTheFailedFacetToItsRule(FacetKind facet, string ruleId)
    {
        var rule = ValidationRules.ForFacet(facet);

        await Assert.That(rule.Code).IsEqualTo(ValidationHarness.Code(ruleId));
    }
}
