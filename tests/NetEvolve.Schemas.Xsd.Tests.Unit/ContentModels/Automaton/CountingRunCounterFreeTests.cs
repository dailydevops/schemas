namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static AutomatonTestKit;

/// <summary>
/// Runs the counter-free step path and the general path over the same random models and inputs and asserts the same
/// outcome after every step.
/// </summary>
public class CountingRunCounterFreeTests
{
    private const int NameCount = 5;
    private const int Inputs = 400;
    private const int MaxLength = 8;

    private static readonly XPathQName Wild = new("urn:w", "x");

    [Test]
    public async Task RandomCounterFreeModels_FastPathAgreesWithTheGeneralPath()
    {
        var random = new Lcg(1264);
        var mismatches = new List<string>();
        var wildcard = new FakeWildcard([], XsdProcessContents.Lax, "urn:w");
        for (var model = 0; model < 150; model++)
        {
            var particle = Generate(random, 3, wildcard);
            var automaton = Compile(particle);
            await Assert.That(automaton.CounterSlots).IsEqualTo(0);

            for (var n = 0; n < Inputs; n++)
            {
                var fast = new CountingRun(automaton);
                var general = new CountingRun(automaton) { ForceGeneralPath = true };
                var length = random.Next(MaxLength + 1);
                for (var step = 0; step < length; step++)
                {
                    var (id, name) = Token(random);
                    var a = fast.TryStep(id, name, out var fastMatch);
                    var b = general.TryStep(id, name, out var generalMatch);
                    if (
                        a != b
                        || (
                            a
                            && (
                                fastMatch.ParticleIndex != generalMatch.ParticleIndex
                                || fastMatch.IsWildcard != generalMatch.IsWildcard
                            )
                        )
                        || fast.CanEnd() != general.CanEnd()
                        || Expected(fast) != Expected(general)
                    )
                    {
                        mismatches.Add($"model {model}, input {n}, step {step}");
                        break;
                    }
                }
            }
        }

        await Assert.That(mismatches).IsEmpty();
    }

    private static string Expected(CountingRun run)
    {
        Span<int> ids = stackalloc int[16];
        var count = run.CollectExpected(ids, out var wildcard);
        return $"{string.Join(",", ids[..count].ToArray())}|{wildcard}";
    }

    private static (int Id, XPathQName Name) Token(Lcg random) =>
        random.Next(8) switch
        {
            5 => (-1, Wild),
            6 => (random.Next(1, NameCount + 1), Wild),
            7 => (-1, default),
            var i => (i + 1, default),
        };

    private static AutomatonParticle Generate(Lcg random, int depth, IWildcard wildcard)
    {
        var (min, max) = random.Next(4) switch
        {
            0 => (0L, 1L),
            1 => (0L, Unbounded),
            2 => (1L, Unbounded),
            _ => (1L, 1L),
        };
        var kind = depth == 0 ? random.Next(2) : random.Next(4);
        if (kind == 0)
        {
            return AutomatonParticle.Element(min, max, random.Next(1, NameCount + 1));
        }

        if (kind == 1)
        {
            return random.Next(3) == 0
                ? AutomatonParticle.Wildcard(min, max, wildcard)
                : AutomatonParticle.Element(min, max, random.Next(1, 3), random.Next(3, NameCount + 1));
        }

        var children = new AutomatonParticle[random.Next(1, 5)];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = Generate(random, depth - 1, wildcard);
        }

        return kind == 2
            ? AutomatonParticle.Sequence(min, max, children)
            : AutomatonParticle.Choice(min, max, children);
    }
}
