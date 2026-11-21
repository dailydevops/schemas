namespace NetEvolve.Schemas.Xsd.Versioning.Rules10;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>
/// All Group Limited (Structures 1.0, section 3.8.6, <c>cos-all-limited</c>): in strict 1.0 mode an <c>all</c> group is the
/// whole content of a complex type or the model group of a model group definition, it occurs once, and its members are element
/// declarations that occur at most once. Everything else is a located schema error.
/// </summary>
/// <remarks>
/// <para>
/// The check reports a nested <c>all</c> group (inline, or through a group reference) at its particle, a member whose
/// <c>maxOccurs</c> is above 1 at the member, and a wildcard or a model group as member at the member. A group reference is
/// judged by the compositor of its target only; the members of a referenced definition are checked by
/// <see cref="CheckDefinition"/>, so no violation is reported twice. A particle whose term is unresolved is skipped, as the
/// resolver has reported it.
/// </para>
/// <para>
/// Limitations: this check is a no-op in 1.1 mode, where the relaxed rule is XSD-19 (#275); nothing calls the check until the
/// compile pipeline is wired (XSD-75, XSD-78).
/// </para>
/// </remarks>
internal static class AllGroupLimits10
{
    /// <summary>Checks the content particle of a complex type.</summary>
    /// <param name="content">The particle of an element-only or mixed content type.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void CheckContent(Particle content, XsdVersion version, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(content);
        Argument.ThrowIfNull(reporter);
        GuardVersion(version);

        if (version == XsdVersion.V10)
        {
            VisitParticle(content, true, reporter);
        }
    }

    /// <summary>Checks the model group of a model group definition.</summary>
    /// <param name="definition">The definition, after its references are resolved.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void CheckDefinition(ModelGroupDefinition definition, XsdVersion version, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(definition);
        Argument.ThrowIfNull(reporter);
        GuardVersion(version);

        if (version == XsdVersion.V10)
        {
            VisitGroup(definition.ModelGroup, reporter);
        }
    }

    private static void GuardVersion(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
    }

    // A particle that may hold an all group at its top: the content of a complex type, or any particle below a compositor.
    private static void VisitParticle(Particle particle, bool isContent, XsdIssueReporter reporter)
    {
        switch (particle.Term)
        {
            case ModelGroup group:
                if (group.Compositor == ModelGroupCompositor.All)
                {
                    CheckPlacement(particle, isContent, reporter);
                    CheckMembers(group, reporter);
                }
                else
                {
                    VisitMembers(group, reporter);
                }

                break;
            case ModelGroupDefinition { ModelGroup.Compositor: ModelGroupCompositor.All }:
                CheckPlacement(particle, isContent, reporter);
                break;
        }
    }

    private static void VisitGroup(ModelGroup group, XsdIssueReporter reporter)
    {
        if (group.Compositor == ModelGroupCompositor.All)
        {
            CheckMembers(group, reporter);
        }
        else
        {
            VisitMembers(group, reporter);
        }
    }

    private static void VisitMembers(ModelGroup group, XsdIssueReporter reporter)
    {
        for (var i = 0; i < group.Particles.Count; i++)
        {
            VisitParticle(group.Particles[i], false, reporter);
        }
    }

    private static void CheckPlacement(Particle particle, bool isContent, XsdIssueReporter reporter)
    {
        if (!isContent)
        {
            Report(
                reporter,
                particle,
                "An all group must be the whole content of a complex type or of a group definition."
            );
        }
        else if (particle.MaxOccurs > 1)
        {
            Report(reporter, particle, "An all group as the content of a complex type must not occur more than once.");
        }
    }

    private static void CheckMembers(ModelGroup group, XsdIssueReporter reporter)
    {
        for (var i = 0; i < group.Particles.Count; i++)
        {
            var member = group.Particles[i];
            if (member.MaxOccurs > 1)
            {
                Report(reporter, member, "A member of an all group must not occur more than once in strict 1.0 mode.");
            }

            switch (member.Term)
            {
                case IWildcard:
                    Report(reporter, member, "An all group must not contain a wildcard in strict 1.0 mode.");
                    break;
                case ModelGroup or ModelGroupDefinition:
                    Report(reporter, member, "An all group must not contain a model group in strict 1.0 mode.");
                    break;
            }
        }
    }

    private static void Report(XsdIssueReporter reporter, Particle particle, string message) =>
        reporter.Report(XsdRuleCodes.CosAllLimited, particle.Location, message);
}
