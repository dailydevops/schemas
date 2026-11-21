namespace NetEvolve.Schemas.Xsd.Compilation;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
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
    /// <param name="definitions">The model group definitions that were built.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    internal static void Run(
        BuildContext context,
        TypeDerivationCheck derivation,
        IReadOnlyList<ModelGroupDefinition> definitions,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(derivation);
        Argument.ThrowIfNull(definitions);
        Argument.ThrowIfNull(reporter);

        derivation.ReportAll(context.BuiltComplexTypes, reporter);
        CheckValueConstraints(context, version, reporter);
        CheckContents(context, version, reporter);
        CheckRestrictions(context, version, reporter);
        for (var i = 0; i < definitions.Count; i++)
        {
            AllGroupLimits10.CheckDefinition(definitions[i], version, reporter);
        }

        for (var i = 0; i < context.BuiltAttributeGroups.Count; i++)
        {
            IdAttributeUses.CheckGroup(context.BuiltAttributeGroups[i].Uses, version, reporter);
        }
    }

    private static void CheckValueConstraints(BuildContext context, XsdVersion version, XsdIssueReporter reporter)
    {
        for (var i = 0; i < context.FoundElements.Count; i++)
        {
            _ = ValueConstraintCheck.Check(context.FoundElements[i], context.Assessor, version, reporter);
        }

        for (var i = 0; i < context.BuiltAttributes.Count; i++)
        {
            _ = ValueConstraintCheck.Check(context.BuiltAttributes[i], context.Assessor, version, reporter);
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

    private static void CheckContents(BuildContext context, XsdVersion version, XsdIssueReporter reporter)
    {
        for (var i = 0; i < context.BuiltComplexTypes.Count; i++)
        {
            var type = context.BuiltComplexTypes[i];
            IdAttributeUses.CheckType(type.AttributeUses, version, reporter);
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

    // Particle Valid (Restriction) of XSD 1.0; XSD 1.1 checks the restriction by subsumption (XSD-78, #360).
    private static void CheckRestrictions(BuildContext context, XsdVersion version, XsdIssueReporter reporter)
    {
        if (version != XsdVersion.V10)
        {
            return;
        }

        var particles = new ParticleRestriction(new TypeDerivationOk(version));
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
