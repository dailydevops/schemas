namespace NetEvolve.Schemas.Xsd.Validation.Core;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;

/// <summary>The rules the streaming validator reports, looked up once in the catalog.</summary>
internal static class ValidationRules
{
    internal static readonly XsdRule Elt = XsdRuleLookup.Find(XsdRuleCodes.CvcElt);
    internal static readonly XsdRule Type = XsdRuleLookup.Find(XsdRuleCodes.CvcType);
    internal static readonly XsdRule AssessElt = XsdRuleLookup.Find(XsdRuleCodes.CvcAssessElt);
    internal static readonly XsdRule ComplexType = XsdRuleLookup.Find(XsdRuleCodes.CvcComplexType);
    internal static readonly XsdRule Au = XsdRuleLookup.Find(XsdRuleCodes.CvcAu);
    internal static readonly XsdRule Wildcard = XsdRuleLookup.Find(XsdRuleCodes.CvcWildcard);
    internal static readonly XsdRule WildcardNamespace = XsdRuleLookup.Find(XsdRuleCodes.CvcWildcardNamespace);

    /// <summary>Gets the rule of a failed facet; a lexical or list failure, and a facet without a rule, is Datatype Valid.</summary>
    /// <param name="facet">The failed facet.</param>
    /// <returns>The rule.</returns>
    internal static XsdRule ForFacet(FacetKind facet) =>
        facet switch
        {
            FacetKind.Length => Facet.Length,
            FacetKind.MinLength => Facet.MinLength,
            FacetKind.MaxLength => Facet.MaxLength,
            FacetKind.Pattern => Facet.Pattern,
            FacetKind.Enumeration => Facet.Enumeration,
            FacetKind.MaxInclusive => Facet.MaxInclusive,
            FacetKind.MaxExclusive => Facet.MaxExclusive,
            FacetKind.MinExclusive => Facet.MinExclusive,
            FacetKind.MinInclusive => Facet.MinInclusive,
            FacetKind.TotalDigits => Facet.TotalDigits,
            FacetKind.FractionDigits => Facet.FractionDigits,
            FacetKind.Assertions => Facet.Assertions,
            FacetKind.ExplicitTimezone => Facet.ExplicitTimezone,
            _ => Facet.Datatype,
        };

    private static class Facet
    {
        internal static readonly XsdRule Datatype = XsdRuleLookup.Find(XsdRuleCodes.CvcDatatypeValid);
        internal static readonly XsdRule Length = XsdRuleLookup.Find(XsdRuleCodes.CvcLengthValid);
        internal static readonly XsdRule MinLength = XsdRuleLookup.Find(XsdRuleCodes.CvcMinLengthValid);
        internal static readonly XsdRule MaxLength = XsdRuleLookup.Find(XsdRuleCodes.CvcMaxLengthValid);
        internal static readonly XsdRule Pattern = XsdRuleLookup.Find(XsdRuleCodes.CvcPatternValid);
        internal static readonly XsdRule Enumeration = XsdRuleLookup.Find(XsdRuleCodes.CvcEnumerationValid);
        internal static readonly XsdRule MaxInclusive = XsdRuleLookup.Find(XsdRuleCodes.CvcMaxInclusiveValid);
        internal static readonly XsdRule MaxExclusive = XsdRuleLookup.Find(XsdRuleCodes.CvcMaxExclusiveValid);
        internal static readonly XsdRule MinExclusive = XsdRuleLookup.Find(XsdRuleCodes.CvcMinExclusiveValid);
        internal static readonly XsdRule MinInclusive = XsdRuleLookup.Find(XsdRuleCodes.CvcMinInclusiveValid);
        internal static readonly XsdRule TotalDigits = XsdRuleLookup.Find(XsdRuleCodes.CvcTotalDigitsValid);
        internal static readonly XsdRule FractionDigits = XsdRuleLookup.Find(XsdRuleCodes.CvcFractionDigitsValid);
        internal static readonly XsdRule Assertions = XsdRuleLookup.Find(XsdRuleCodes.CvcAssertionsValid);
        internal static readonly XsdRule ExplicitTimezone = XsdRuleLookup.Find(XsdRuleCodes.CvcExplicitTimezoneValid);
    }
}
