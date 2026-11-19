namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>Compiles hand-built particles and runs the determinism check over them.</summary>
internal static class DeterminismKit
{
    public const string Source = "file:///a.xsd";
    public const string Ns = "urn:test";
    public const string Target = "urn:t";

    /// <summary>The expanded name of a schema name id.</summary>
    public static XPathQName NameOf(int id) => new(Ns, "e" + id);

    /// <summary>Creates a wildcard particle with a one-based line; the namespace attribute value is parsed as in a schema document.</summary>
    public static AutomatonParticle Wild(
        int line,
        string namespaceValue,
        long min = 1,
        long max = 1,
        string processContents = "lax"
    )
    {
        if (!Wildcard.TryCreate(namespaceValue, processContents, Target, Locate(line), out var wildcard))
        {
            throw new InvalidOperationException("Not a wildcard: " + namespaceValue);
        }

        return AutomatonParticle.Wildcard(min, max, wildcard!);
    }

    /// <summary>The location of a one-based line, the form of the located terms.</summary>
    public static DocumentLocation Locate(int line) => new(Source, line, 1);

    /// <summary>The terms of the positions in document order: a located element per element particle, the wildcard itself otherwise.</summary>
    public static List<IXsdComponent> Terms(AutomatonParticle root)
    {
        var terms = new List<IXsdComponent>();
        Collect(root, terms);
        return terms;
    }

    /// <summary>Compiles the particle and checks it; element particles are located on the line of their position, starting at one.</summary>
    public static Outcome Check(AutomatonParticle root, Func<int, XPathQName>? nameOf = null)
    {
        var model = AutomatonTestKit.Compile(root);
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var deterministic = DeterminismCheck.Check(
            model,
            Terms(root),
            nameOf ?? NameOf,
            new XsdIssueReporter(collector),
            Locate(99)
        );
        return new Outcome(deterministic, collector.ToResult().Issues);
    }

    private static void Collect(AutomatonParticle particle, List<IXsdComponent> terms)
    {
        // The compiler removes a particle that cannot occur, so it has no position.
        if (particle.MaxOccurs == 0)
        {
            return;
        }

        switch (particle.Kind)
        {
            case AutomatonParticleKind.Element:
                terms.Add(
                    new FakeComponent(XsdComponentKind.ElementDeclaration, "e" + particle.NameIds![0], terms.Count + 1)
                );
                break;
            case AutomatonParticleKind.Wildcard:
                terms.Add(particle.WildcardComponent!);
                break;
            default:
                foreach (var child in particle.Children)
                {
                    Collect(child, terms);
                }

                break;
        }
    }

    /// <summary>The verdict and the issues of one check.</summary>
    internal sealed class Outcome(bool deterministic, IReadOnlyList<ValidationIssue> issues)
    {
        public bool Deterministic { get; } = deterministic;

        public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
    }
}
