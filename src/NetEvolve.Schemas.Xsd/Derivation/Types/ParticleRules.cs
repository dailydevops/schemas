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
    /// <returns><see langword="true"/> when they are the same particle, or the extension is a sequence that occurs once and starts with the base.</returns>
    internal static bool IsValidExtension(Particle extension, Particle baseParticle)
    {
        Argument.ThrowIfNull(extension);
        Argument.ThrowIfNull(baseParticle);

        return ReferenceEquals(extension, baseParticle)
            || (
                extension.MinOccurs == 1
                && extension.MaxOccurs == 1
                && extension.Term
                    is ModelGroup { Compositor: ModelGroupCompositor.Sequence, Particles.Count: > 0 } group
                && IsIdentical(group.Particles[0], baseParticle)
            );
    }

    /// <summary>Determines whether a particle can match nothing (Particle Emptiable, <c>cos-group-emptiable</c>).</summary>
    /// <param name="particle">The particle.</param>
    /// <returns><see langword="true"/> when its minimum total range is zero.</returns>
    internal static bool IsEmptiable(Particle particle)
    {
        Argument.ThrowIfNull(particle);

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
