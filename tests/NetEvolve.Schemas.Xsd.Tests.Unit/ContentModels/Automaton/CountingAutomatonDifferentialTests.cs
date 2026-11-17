namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using static AutomatonTestKit;

/// <summary>Compares the automaton with an unfolding reference matcher on random particles with distinct leaf names.</summary>
public class CountingAutomatonDifferentialTests
{
    private const int NameCount = 4;
    private const int MaxLength = 5;

    [Test]
    public async Task RandomModels_AgreeWithTheUnfoldedReference()
    {
        var random = new Lcg(236);
        var mismatches = new List<string>();
        for (var model = 0; model < 80; model++)
        {
            var next = 1;
            var particle = Generate(random, 3, ref next);
            var automaton = Compile(particle);
            foreach (var input in Inputs())
            {
                var expected = Reference(particle, input, 0).Contains(input.Length);
                if (Accepts(automaton, input) != expected)
                {
                    mismatches.Add($"model {model} (seed 236), input [{string.Join(",", input)}]");
                }
            }
        }

        await Assert.That(mismatches).IsEmpty();
    }

    private static AutomatonParticle Generate(Lcg random, int depth, ref int nextName)
    {
        var (min, max) = Range(random);
        if (depth == 0 || nextName > NameCount || random.Next(3) == 0)
        {
            return nextName > NameCount ? Seq(min, max) : El(nextName++, min, max);
        }

        var count = random.Next(1, 4);
        var children = new AutomatonParticle[count];
        for (var i = 0; i < count; i++)
        {
            children[i] = Generate(random, depth - 1, ref nextName);
        }

        return random.Next(2) == 0 ? Seq(min, max, children) : Cho(min, max, children);
    }

    private static (long Min, long Max) Range(Lcg random) =>
        random.Next(8) switch
        {
            0 => (0, 1),
            1 => (0, Unbounded),
            2 => (1, Unbounded),
            3 => (2, 3),
            4 => (0, 2),
            5 => (2, Unbounded),
            6 => (2, 2),
            _ => (1, 1),
        };

    private static IEnumerable<int[]> Inputs()
    {
        for (var length = 0; length <= MaxLength; length++)
        {
            var total = (int)Math.Pow(NameCount, length);
            for (var code = 0; code < total; code++)
            {
                var input = new int[length];
                var rest = code;
                for (var i = 0; i < length; i++)
                {
                    input[i] = (rest % NameCount) + 1;
                    rest /= NameCount;
                }

                yield return input;
            }
        }
    }

    /// <summary>The end offsets of every way the particle, with its occurrences, matches the input from an offset.</summary>
    private static HashSet<int> Reference(AutomatonParticle particle, int[] input, int start)
    {
        var result = new HashSet<int>();
        var current = new HashSet<int> { start };
        if (particle.MinOccurs == 0)
        {
            result.Add(start);
        }

        var limit = Math.Min(particle.MaxOccurs, particle.MinOccurs + input.Length + 1);
        for (long k = 1; k <= limit && current.Count > 0; k++)
        {
            var after = new HashSet<int>();
            foreach (var position in current)
            {
                after.UnionWith(Body(particle, input, position));
            }

            current = after;
            if (k >= particle.MinOccurs)
            {
                result.UnionWith(current);
            }
        }

        return result;
    }

    private static HashSet<int> Body(AutomatonParticle particle, int[] input, int start)
    {
        switch (particle.Kind)
        {
            case AutomatonParticleKind.Element:
                return start < input.Length && input[start] == particle.NameIds![0] ? [start + 1] : [];
            case AutomatonParticleKind.Sequence:
                var positions = new HashSet<int> { start };
                foreach (var child in particle.Children)
                {
                    var after = new HashSet<int>();
                    foreach (var position in positions)
                    {
                        after.UnionWith(Reference(child, input, position));
                    }

                    positions = after;
                }

                return positions;
            default:
                var union = new HashSet<int>();
                foreach (var child in particle.Children)
                {
                    union.UnionWith(Reference(child, input, start));
                }

                return union;
        }
    }
}
