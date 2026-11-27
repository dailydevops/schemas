namespace NetEvolve.Schemas.Xsd.Compilation;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Versioning.Rules10;

/// <summary>
/// The constraint checks of the pipeline that need resolved components (Structures 5.1): the derivation of every complex type,
/// the value constraints, the limits of the <c>all</c> group, the consistency of the element declarations of a content model,
/// the attributes of type <c>ID</c>, and the restriction of a content particle in XSD 1.0. The simple type facets are checked while
/// the types are built, because the facets of a type depend on the facets of its base.
/// </summary>
/// <remarks>
/// A type that extends a base type holds the content of the base in its own content model, so a violation inside inherited content
/// is reported for the base type and again for every type that extends it, each time at the location of the offending particle.
/// </remarks>
internal static class CompileChecks
{
    /// <summary>Runs the checks.</summary>
    /// <param name="context">The build context that holds what was built.</param>
    /// <param name="derivation">The derivation check.</param>
    /// <param name="particles">The particle restriction step of XSD 1.0, <see langword="null"/> when the pipeline does not compose it.</param>
    /// <param name="definitions">The model group definitions that were built.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The substitution groups of the global element declarations.</returns>
    internal static SubstitutionGroups Run(
        BuildContext context,
        TypeDerivationCheck derivation,
        ParticleRestriction? particles,
        IReadOnlyList<ModelGroupDefinition> definitions,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(reporter);

        derivation.ReportAll(context.BuiltComplexTypes, reporter);
        CheckValueConstraints(context, version, reporter);
        var groups = CheckSubstitutionGroups(context, derivation, reporter);
        particles?.Substitutions = groups;
        CheckContents(context, version, reporter);
        CheckRestrictions(context, particles, reporter);
        for (var i = 0; i < definitions.Count; i++)
        {
            AllGroupLimits10.CheckDefinition(definitions[i], version, reporter);
        }

        for (var i = 0; i < context.BuiltAttributeGroups.Count; i++)
        {
            IdAttributeUses.CheckGroup(context.BuiltAttributeGroups[i].Uses, version, reporter);
        }

        return groups;
    }

    private static void CheckValueConstraints(BuildContext context, XsdVersion version, XsdIssueReporter reporter)
    {
        // Datatypes 1.0, 3.2.19: an enumeration value of a type derived from NOTATION names a notation declaration.
        for (var i = 0; i < context.NotationValues.Count; i++)
        {
            var (name, location) = context.NotationValues[i];
            if (!context.Registry.TryGet(XsdSymbolSpace.Notations, name, out _))
            {
                reporter.Report(XsdRuleCodes.SrcResolve, location, "No notation named '" + name + "' is declared.");
            }
        }

        for (var i = 0; i < context.FoundElements.Count; i++)
        {
            var element = context.FoundElements[i];
            CheckNotationType(context, element.TypeDefinition, element.Location, version, reporter);
            _ = ValueConstraintCheck.Check(element, context.Assessor, version, reporter);
        }

        for (var i = 0; i < context.BuiltAttributes.Count; i++)
        {
            var attribute = context.BuiltAttributes[i];
            CheckNotationType(context, attribute.TypeDefinition, attribute.Location, version, reporter);
            _ = ValueConstraintCheck.Check(attribute, context.Assessor, version, reporter);
        }

        // The use of a local declaration states the constraint of the declaration, which was checked with it.
        for (var i = 0; i < context.BuiltUses.Count; i++)
        {
            if (context.BuiltUses[i].Declaration.IsGlobal)
            {
                _ = ValueConstraintCheck.Check(context.BuiltUses[i], context.Assessor, version, reporter);
            }
        }
    }

    // Datatypes 1.0, 3.2.19: a type derived from NOTATION that a declaration uses carries an enumeration; a restriction step in between needs none.
    // The built-in NOTATION itself (no base type) is not checked.
    private static void CheckNotationType(
        BuildContext context,
        IXsdComponent? type,
        DocumentLocation location,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        if (
            type is SimpleTypeDefinition { BaseType: not null }
            && !context.HasEnumeration(type)
            && FacetBase.Resolve(type, version) is { Primitive: PrimitiveKind.Notation }
        )
        {
            reporter.Report(
                XsdRuleCodes.EnumerationRequiredNotation,
                location,
                "A type derived from NOTATION must specify an enumeration."
            );
        }
    }

    // Substitution Group OK (Transitive) of the global declarations that name a head; a head that is not resolved was reported by the resolver.
    private static SubstitutionGroups CheckSubstitutionGroups(
        BuildContext context,
        TypeDerivationCheck derivation,
        XsdIssueReporter reporter
    )
    {
        var global = new List<ISubstitutionElement>();
        for (var i = 0; i < context.FoundElements.Count; i++)
        {
            var element = context.FoundElements[i];
            if (element.IsGlobal && (element.TypeDefinition is not null || element.InheritsType))
            {
                global.Add(element);
            }
        }

        return SubstitutionGroups.Build(global, derivation, reporter);
    }

    private static void CheckContents(BuildContext context, XsdVersion version, XsdIssueReporter reporter)
    {
        for (var i = 0; i < context.BuiltComplexTypes.Count; i++)
        {
            var type = context.BuiltComplexTypes[i];
            IdAttributeUses.CheckType(type.AttributeUses, version, reporter);
            if (type.NeverOccurringContent is { } never)
            {
                AllGroupLimits10.CheckContent(never, version, reporter);
            }

            if (type.ContentParticle is not { } content)
            {
                continue;
            }

            AllGroupLimits10.CheckContent(content, version, reporter);
            var group = content.Term switch
            {
                ModelGroup own => own,
                ModelGroupDefinition definition => definition.ModelGroup,
                _ => null,
            };
            if (group is not null)
            {
                ElementDeclarationsConsistent.Check(group, reporter);
            }
        }
    }

    // Particle Valid (Restriction) of XSD 1.0 is a step that the pipeline composes for 1.0 only; XSD 1.1 checks the restriction by
    // subsumption (XSD-78, #360). A pipeline without the step checks nothing here (design #146 DP-15).
    private static void CheckRestrictions(
        BuildContext context,
        ParticleRestriction? particles,
        XsdIssueReporter reporter
    )
    {
        if (particles is null)
        {
            return;
        }

        for (var i = 0; i < context.BuiltComplexTypes.Count; i++)
        {
            var type = context.BuiltComplexTypes[i];
            if (
                type.DerivationMethod == XsdDerivationMethod.Restriction
                && type.BaseType is ComplexTypeDefinition parent
                && !ReferenceEquals(parent, ComplexTypeDefinition.AnyType)
                && type.ContentParticle is { } restriction
                && parent.ContentParticle is { } original
            )
            {
                particles.Report(restriction, original, reporter);
            }
        }
    }
}
