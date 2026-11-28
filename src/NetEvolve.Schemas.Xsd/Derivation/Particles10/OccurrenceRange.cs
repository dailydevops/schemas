namespace NetEvolve.Schemas.Xsd.Derivation.Particles10;

using System;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>The arithmetic on occurrence ranges that Particle Valid (Restriction) needs (Structures 1.0, 3.8.6 and 3.9.6).</summary>
/// <remarks>
/// Limitations: <see cref="Particle"/> reads an occurrence above <see cref="long.MaxValue"/> as unbounded (design #146 DP-7), so
/// such values are compared clamped and not exactly; sums and products saturate at <see cref="Particle.Unbounded"/> instead of
/// overflowing, so a total that exceeds the clamp is unbounded.
/// </remarks>
internal static class OccurrenceRange
{
    /// <summary>Multiplies two occurrence values; zero stays zero, and a product that reaches the clamp is unbounded.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The product, <see cref="Particle.Unbounded"/> when it does not fit.</returns>
    internal static long Multiply(long left, long right)
    {
        if (left == 0 || right == 0)
        {
            return 0;
        }

        return left == Particle.Unbounded || right == Particle.Unbounded || left > Particle.Unbounded / right
            ? Particle.Unbounded
            : left * right;
    }

    /// <summary>Adds two occurrence values; a sum that reaches the clamp is unbounded.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The sum, <see cref="Particle.Unbounded"/> when it does not fit.</returns>
    internal static long Add(long left, long right) =>
        left == Particle.Unbounded || right == Particle.Unbounded || left > Particle.Unbounded - right
            ? Particle.Unbounded
            : left + right;

    /// <summary>
    /// Computes the effective total range of a particle (Effective Total Range (all and sequence) and (choice), 3.8.6): how often
    /// elements and wildcards can occur below it in total, its own occurrence included.
    /// </summary>
    /// <param name="particle">The particle; a particle that is not a group counts its own occurrence.</param>
    /// <param name="min">The minimum.</param>
    /// <param name="max">The maximum, <see cref="Particle.Unbounded"/> for unbounded.</param>
    /// <remarks>The group structure must not be circular, <see cref="ParticleStructure.IsCircular"/> tells.</remarks>
    internal static void EffectiveTotalRange(Particle particle, out long min, out long max)
    {
        ArgumentNullException.ThrowIfNull(particle);

        if (ParticleStructure.GroupOf(particle) is not { } group)
        {
            min = particle.MinOccurs;
            max = particle.MaxOccurs;
            return;
        }

        var isChoice = group.Compositor == ModelGroupCompositor.Choice;
        long innerMin = 0;
        long innerMax = 0;
        var first = true;
        for (var i = 0; i < group.Particles.Count; i++)
        {
            // A member that never occurs corresponds to no particle (3.9.2).
            if (group.Particles[i].MaxOccurs == 0)
            {
                continue;
            }

            EffectiveTotalRange(group.Particles[i], out var memberMin, out var memberMax);
            if (isChoice)
            {
                innerMin = first ? memberMin : Math.Min(innerMin, memberMin);
                first = false;
                innerMax = Math.Max(innerMax, memberMax);
            }
            else
            {
                innerMin = Add(innerMin, memberMin);
                innerMax = Add(innerMax, memberMax);
            }
        }

        min = Multiply(particle.MinOccurs, innerMin);
        max = Multiply(particle.MaxOccurs, innerMax);
    }
}
