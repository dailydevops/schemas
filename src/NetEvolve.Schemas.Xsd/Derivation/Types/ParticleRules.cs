namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>Particle Valid (Extension) and Particle Emptiable (Structures 3.9.6) for the derivation checks.</summary>
/// <remarks>
/// Limitations: particles are identical when their occurrence ranges are equal and their terms are the same component, or
/// model groups of the same compositor whose particles are identical in order; an element declaration or a wildcard that was
/// declared a second time with equal properties is not recognized as the same. An empty <c>choice</c> that must occur is
/// not emptiable (nothing can match it).
/// </remarks>
internal static class ParticleRules
{
    /// <summary>Determines whether a particle is a valid extension of another (<c>cos-particle-extend</c>).</summary>
    /// <param name="extension">The extension.</param>
    /// <param name="baseParticle">The base.</param>
    /// <returns><see langword="true"/> when they are the same particle, or the extension is a sequence that occurs once and starts with the base, or both are <c>all</c> groups and the extension holds the members of the base first (XSD 1.1).</returns>
    internal static bool IsValidExtension(Particle extension, Particle baseParticle)
    {
        ArgumentNullException.ThrowIfNull(extension);
        ArgumentNullException.ThrowIfNull(baseParticle);

        return ReferenceEquals(extension, baseParticle)
            || (
                extension.MinOccurs == 1
                && extension.MaxOccurs == 1
                && extension.Term
                    is ModelGroup { Compositor: ModelGroupCompositor.Sequence, Particles.Count: > 0 } group
                && IsIdentical(group.Particles[0], baseParticle)
            )
            || IsMergedAllGroup(extension, baseParticle);
    }

    // Structures 1.1, 3.4.2 clause 4.2.3.2: two all groups merge into one all group that starts with the particles of the base.
    private static bool IsMergedAllGroup(Particle extension, Particle baseParticle)
    {
        if (
            extension.MaxOccurs != 1
            || extension.Term is not ModelGroup { Compositor: ModelGroupCompositor.All } merged
            || (baseParticle.Term is ModelGroupDefinition definition ? definition.ModelGroup : baseParticle.Term)
                is not ModelGroup { Compositor: ModelGroupCompositor.All } own
            || merged.Particles.Count < own.Particles.Count
        )
        {
            return false;
        }

        for (var i = 0; i < own.Particles.Count; i++)
        {
            if (!IsIdentical(merged.Particles[i], own.Particles[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a particle can match nothing (Particle Emptiable, <c>cos-group-emptiable</c>).</summary>
    /// <param name="particle">The particle.</param>
    /// <returns><see langword="true"/> when its minimum total range is zero.</returns>
    internal static bool IsEmptiable(Particle particle)
    {
        ArgumentNullException.ThrowIfNull(particle);

        return IsEmptiable(particle, []);
    }

    private static bool IsIdentical(Particle left, Particle right) =>
        ReferenceEquals(left, right)
        || (
            left.MinOccurs == right.MinOccurs
            && left.MaxOccurs == right.MaxOccurs
            && IsTermIdentical(left.Term, right.Term)
        );

    private static bool IsTermIdentical(IXsdComponent? left, IXsdComponent? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (
            left is not ModelGroup leftGroup
            || right is not ModelGroup rightGroup
            || leftGroup.Compositor != rightGroup.Compositor
            || leftGroup.Particles.Count != rightGroup.Particles.Count
        )
        {
            return false;
        }

        for (var i = 0; i < leftGroup.Particles.Count; i++)
        {
            if (!IsIdentical(leftGroup.Particles[i], rightGroup.Particles[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsEmptiable(Particle particle, HashSet<ModelGroupDefinition> path)
    {
        if (particle.MinOccurs == 0)
        {
            return true;
        }

        switch (particle.Term)
        {
            case ModelGroup group:
                return IsEmptiable(group, path);
            case ModelGroupDefinition definition when path.Add(definition):
                var emptiable = IsEmptiable(definition.ModelGroup, path);
                _ = path.Remove(definition);
                return emptiable;
            default:
                // An element, a wildcard, an unresolved term, or a group definition that contains itself.
                return false;
        }
    }

    private static bool IsEmptiable(ModelGroup group, HashSet<ModelGroupDefinition> path)
    {
        var isChoice = group.Compositor == ModelGroupCompositor.Choice;
        for (var i = 0; i < group.Particles.Count; i++)
        {
            var emptiable = IsEmptiable(group.Particles[i], path);

            // A choice needs one emptiable alternative; a sequence or an all group needs every particle to be emptiable.
            if (emptiable == isChoice)
            {
                return isChoice;
            }
        }

        return !isChoice;
    }
}
