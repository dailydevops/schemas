namespace NetEvolve.Schemas.Xsd.Derivation.Particles10;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>
/// The structure of a particle as Particle Valid (Restriction) reads it (Structures 1.0, 3.9.6): the model group behind a term, and
/// the removal of pointless occurrences of <c>sequence</c>, <c>choice</c>, and <c>all</c> (clause 2.2).
/// </summary>
/// <remarks>
/// <para>
/// A pointless group is ignored: an empty <c>sequence</c> or <c>all</c> or an empty <c>choice</c> that may be skipped vanishes; a
/// <c>sequence</c> or <c>choice</c> that occurs once and has one member is replaced by the member; a <c>sequence</c> that occurs
/// once and sits in a <c>sequence</c>, or a <c>choice</c> that occurs once and sits in a <c>choice</c>, is replaced by its members.
/// The specification ignores an <c>all</c> with one member whatever its occurrence; this implementation replaces it by the member
/// with the product of both occurrence ranges (an <c>all</c> occurs at most once), which keeps its language, so the
/// occurrence of the <c>all</c> is not lost. A violation is located at the particle that took the place of an ignored group.
/// </para>
/// <para>
/// Limitation: a group that contains itself through group definitions is not walked (<see cref="IsCircular"/>); the resolver reports
/// it as <c>mg-props-correct.2</c>. The depth of the recursion over nested groups is bounded by the depth of the schema documents
/// and not by a ceiling of its own.
/// </para>
/// </remarks>
internal static class ParticleStructure
{
    /// <summary>Gets the model group of a term: the group itself, or the group of a model group definition.</summary>
    /// <param name="particle">The particle.</param>
    /// <returns>The group, <see langword="null"/> when the term is an element declaration, a wildcard, or not resolved.</returns>
    internal static ModelGroup? GroupOf(Particle particle)
    {
        ArgumentNullException.ThrowIfNull(particle);

        return particle.Term switch
        {
            ModelGroup group => group,
            ModelGroupDefinition definition => definition.ModelGroup,
            _ => null,
        };
    }

    /// <summary>Determines whether a particle contains a model group that contains itself through group definitions.</summary>
    /// <param name="particle">The particle.</param>
    /// <returns><see langword="true"/> when a group can be reached from itself.</returns>
    internal static bool IsCircular(Particle particle)
    {
        ArgumentNullException.ThrowIfNull(particle);

        return Reaches(particle, []);
    }

    /// <summary>Removes the pointless group at the top of a particle.</summary>
    /// <param name="particle">The particle.</param>
    /// <returns>The particle that stands for it, <see langword="null"/> when it is ignored as an empty group.</returns>
    internal static Particle? Simplify(Particle particle)
    {
        ArgumentNullException.ThrowIfNull(particle);

        var current = particle;
        while (GroupOf(current) is { } group)
        {
            var members = group.Particles;
            if (members.Count == 0)
            {
                // An empty choice that must occur matches nothing and is not pointless.
                return group.Compositor == ModelGroupCompositor.Choice && current.MinOccurs > 0 ? current : null;
            }

            if (members.Count > 1)
            {
                return current;
            }

            var only = members[0];
            if (current.MinOccurs == 1 && current.MaxOccurs == 1)
            {
                current = only;
            }
            else if (group.Compositor == ModelGroupCompositor.All && only.Term is { } term)
            {
                current = Particle.Create(
                    only.Location,
                    OccurrenceRange.Multiply(current.MinOccurs, only.MinOccurs),
                    OccurrenceRange.Multiply(current.MaxOccurs, only.MaxOccurs),
                    term
                );
            }
            else
            {
                return current;
            }
        }

        return current;
    }

    /// <summary>Lists the members of a group after the pointless ones are removed and nested groups of the same kind are spliced in.</summary>
    /// <param name="group">The group.</param>
    /// <returns>The members in order.</returns>
    internal static List<Particle> Members(ModelGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var members = new List<Particle>(group.Particles.Count);
        Append(members, group, group.Compositor);
        return members;
    }

    private static void Append(List<Particle> members, ModelGroup group, ModelGroupCompositor compositor)
    {
        for (var i = 0; i < group.Particles.Count; i++)
        {
            var member = Simplify(group.Particles[i]);
            if (member is null)
            {
                continue;
            }

            if (
                compositor != ModelGroupCompositor.All
                && member.MinOccurs == 1
                && member.MaxOccurs == 1
                && GroupOf(member) is { } nested
                && nested.Compositor == compositor
            )
            {
                Append(members, nested, compositor);
            }
            else
            {
                members.Add(member);
            }
        }
    }

    // A group is on the path while its members are walked (false) and done afterwards (true); meeting one on the path is a cycle.
    private static bool Reaches(Particle particle, Dictionary<ModelGroup, bool> states)
    {
        if (GroupOf(particle) is not { } group)
        {
            return false;
        }

        if (states.TryGetValue(group, out var done))
        {
            return !done;
        }

        states[group] = false;
        for (var i = 0; i < group.Particles.Count; i++)
        {
            if (Reaches(group.Particles[i], states))
            {
                return true;
            }
        }

        states[group] = true;
        return false;
    }
}
