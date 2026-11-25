namespace NetEvolve.Schemas.Xsd.SimpleTypes.V11;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// Checks the <c>explicitTimezone</c> facet of a restriction step (Datatypes 1.1, section 4.3.14): the facet applies to the date and time
/// types in 1.1 only, it is stated once, its value is one of <see langword="required"/>, <c>prohibited</c>, and <c>optional</c>, and it is a valid
/// restriction of the facet of the base type (<c>timezone-valid-restriction</c>).
/// </summary>
/// <remarks>
/// <para>
/// The check reads the datatype of the Xml package and re-implements none of it: the applicability table, the restriction rule, and the
/// evaluation of the facet are those of the Xml package; this class maps their results to the rules of the catalog. The step is given as
/// the lexical facet declarations of <see cref="SimpleTypeFacetCheck"/>, and the check returns the same result type, so the caller threads
/// the fixed flags in the same way.
/// </para>
/// <para>
/// <see cref="SimpleTypeFacetCheck"/> ignores <c>explicitTimezone</c> (XSD-54), so the caller runs both checks on a step and merges the
/// <see cref="FacetCheckResult.Fixed"/> sets. Limitations: <c>xs:dateTimeStamp</c> requires the timezone in its lexical mapping and its
/// datatype carries no facet, so a base derived from it is read as <see langword="required"/>; the facets of a base that is not built in
/// are read from its facet list; nothing calls the check until the
/// compile pipeline is wired (XSD-75, XSD-78).
/// </para>
/// </remarks>
internal static class ExplicitTimezoneCheck
{
    /// <summary>Checks the <c>explicitTimezone</c> declarations of a restriction step and reports every violation as a located schema error.</summary>
    /// <param name="baseType">The resolved base type: a built-in type or a simple type definition.</param>
    /// <param name="baseFixed">The facets that the base type fixes, as an earlier call returned them.</param>
    /// <param name="step">The facets of the restriction in document order; facets other than <c>explicitTimezone</c> are ignored.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="facet">The facet of the first valid declaration, to add to the facets of the new type; <see langword="null"/> when the step has none.</param>
    /// <returns>The outcome; <see cref="FacetCheckResult.Fixed"/> adds <c>explicitTimezone</c> when a valid declaration is fixed.</returns>
    internal static FacetCheckResult Check(
        IXsdComponent baseType,
        FacetKindSet baseFixed,
        IReadOnlyList<FacetDeclaration> step,
        XsdVersion version,
        XsdIssueReporter reporter,
        out ExplicitTimezoneFacet? facet
    )
    {
        facet = null;
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(reporter);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        var facts = FacetBase.Resolve(baseType, version);
        if (facts is null)
        {
            return new FacetCheckResult(true, baseFixed);
        }

        var isValid = true;
        var seen = false;
        var fixedFacets = baseFixed;
        for (var i = 0; i < step.Count; i++)
        {
            var declaration = step[i];
            if (declaration.Kind != FacetKind.ExplicitTimezone)
            {
                continue;
            }

            var repeated = seen;
            seen = true;
            if (Accept(baseType, facts, baseFixed, declaration, repeated, version, reporter) is not { } mode)
            {
                isValid = false;
                continue;
            }

            facet = new ExplicitTimezoneFacet(mode);
            if (declaration.IsFixed)
            {
                fixedFacets = fixedFacets.With(FacetKind.ExplicitTimezone);
            }
        }

        return new FacetCheckResult(isValid, fixedFacets);
    }

    // The value of a declaration that passes every rule, null after the first violation was reported.
    private static ExplicitTimezoneMode? Accept(
        IXsdComponent baseType,
        FacetBase facts,
        FacetKindSet baseFixed,
        FacetDeclaration declaration,
        bool repeated,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        // Datatypes 4.1.5 (cos-applicable-facets): all date and time types share the value type of the dateTime datatype.
        if (
            facts.Descriptor is not IDatatypeDescriptor<XsdDateTime> type
            || !FacetApplicability.IsApplicable(declaration.Kind, facts.Variety, facts.Primitive, version)
        )
        {
            reporter.Report(
                XsdRuleCodes.CosApplicableFacets,
                declaration.Location,
                "The facet 'explicitTimezone' is not applicable to the base type."
            );
            return null;
        }

        // Datatypes 4.1.3 (src-single-facet-value).
        if (repeated)
        {
            reporter.Report(
                XsdRuleCodes.SrcSingleFacetValue,
                declaration.Location,
                "The facet 'explicitTimezone' is stated more than once."
            );
            return null;
        }

        if (!TryParse(declaration.Value, out var mode))
        {
            reporter.Report(
                XsdRuleCodes.StRestrictFacets,
                declaration.Location,
                "The value of the facet 'explicitTimezone' is not valid for the facet."
            );
            return null;
        }

        var result = OrderedFacetRestriction.Validate(
            type,
            facts.Primitive,
            new OrderedFacetSet<XsdDateTime>(explicitTimezone: ModeOf(baseType, type), fixedFacets: baseFixed),
            new OrderedFacetSet<XsdDateTime>(explicitTimezone: mode)
        );
        if (result.IsValid)
        {
            return mode;
        }

        var code = FacetRuleCodes.Of(result, FacetKind.None);
        reporter.Report(
            code,
            declaration.Location,
            "The facet 'explicitTimezone' is not a valid restriction of the base type: "
                + XsdRuleLookup.Find(code).Title
                + "."
        );
        return null;
    }

    // Datatypes 4.3.14.1: the three values of the facet.
    private static bool TryParse(string lexical, out ExplicitTimezoneMode mode)
    {
        switch (lexical)
        {
            case "required":
                mode = ExplicitTimezoneMode.Required;
                return true;
            case "prohibited":
                mode = ExplicitTimezoneMode.Prohibited;
                return true;
            case "optional":
                mode = ExplicitTimezoneMode.Optional;
                return true;
            default:
                mode = ExplicitTimezoneMode.Optional;
                return false;
        }
    }

    // The effective value of the base type: dateTimeStamp requires the timezone through its lexical mapping, every other type has the
    // last facet of its facet list, or none.
    private static ExplicitTimezoneMode? ModeOf(IXsdComponent baseType, IDatatypeDescriptor builtIn)
    {
        for (var type = builtIn; type is not null; type = type.BaseType)
        {
            if (string.Equals(type.Name, "dateTimeStamp", StringComparison.Ordinal))
            {
                return ExplicitTimezoneMode.Required;
            }
        }

        var facets = baseType is SimpleTypeDefinition definition ? definition.Facets : builtIn.Facets;
        ExplicitTimezoneMode? mode = null;
        for (var i = 0; i < facets.Count; i++)
        {
            if (facets[i] is ExplicitTimezoneFacet timezone)
            {
                mode = timezone.Mode;
            }
        }

        return mode;
    }
}
