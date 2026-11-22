namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>
/// One content model of a subsumption check: the particle compiled into a counting automaton, the particle and the term of every
/// position, and the counters that the automaton reads.
/// </summary>
/// <remarks>
/// Leaves are numbered in document order and a particle that cannot occur is left out, which is the numbering of the positions of
/// the automaton compiler. The element names of both sides of a check share one table of ids, so equal ids are equal names.
/// </remarks>
internal sealed class SubsumptionSide
{
    /// <summary>The ceiling of positions per content model: the ceiling of the automaton compiler.</summary>
    internal const int MaxPositions = CountingAutomatonCompiler.MaxPositions;

    /// <summary>The largest occurrence bound that is decided; larger bounds keep the sums of zone bounds from overflowing.</summary>
    internal const long MaxOccurrence = 1L << 60;

    private readonly Dictionary<XPathQName, int> _ids;
    private readonly List<XPathQName> _names;
    private readonly List<Particle> _particles = [];
    private readonly List<IXsdComponent> _terms = [];

    private SubsumptionSide(Particle root, Dictionary<XPathQName, int> ids, List<XPathQName> names)
    {
        Root = root;
        _ids = ids;
        _names = names;
    }

    /// <summary>Gets the content particle.</summary>
    internal Particle Root { get; }

    /// <summary>Gets how far the construction got.</summary>
    internal SideStatus Status { get; private set; }

    /// <summary>Gets the automaton, <see langword="null"/> unless <see cref="Status"/> is <see cref="SideStatus.Ready"/>.</summary>
    internal CountingAutomaton? Automaton { get; private set; }

    /// <summary>Gets the particle of each position.</summary>
    internal IReadOnlyList<Particle> Particles => _particles;

    /// <summary>Gets the term of each position: an element declaration or a wildcard.</summary>
    internal IReadOnlyList<IXsdComponent> Terms => _terms;

    /// <summary>Gets the counter variable of each counter slot of the automaton, 0 for a slot that is never read; variables count from 1.</summary>
    internal int[] VariableOfSlot { get; private set; } = [];

    /// <summary>Gets the largest value of each counter variable, variable 1 first.</summary>
    internal long[] Caps { get; private set; } = [];

    /// <summary>Builds the side of a content particle.</summary>
    /// <param name="root">The content particle.</param>
    /// <param name="ids">The shared table of element name ids.</param>
    /// <param name="names">The names in the order of their ids.</param>
    /// <returns>The side; its <see cref="Status"/> tells whether it can be compared.</returns>
    internal static SubsumptionSide Build(Particle root, Dictionary<XPathQName, int> ids, List<XPathQName> names)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(names);

        var side = new SubsumptionSide(root, ids, names);
        side.Construct();
        return side;
    }

    private void Construct()
    {
        var converted = Convert(Root) ?? AutomatonParticle.Sequence(1, 1);
        if (Status != SideStatus.Ready)
        {
            return;
        }

        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        Automaton = CountingAutomatonCompiler.Compile(converted, reporter, Root.Location);
        if (Automaton is null)
        {
            Status = SideStatus.TooLarge;
            return;
        }

        ReadCounters(Automaton);
    }

    // Terms that are not resolved and all groups are not compared; a group that is expanded too often is refused early,
    // because the same group definition can be used many times.
    private AutomatonParticle? Convert(Particle particle)
    {
        if (particle.MaxOccurs == 0 || particle.MinOccurs > particle.MaxOccurs || Status != SideStatus.Ready)
        {
            return null;
        }

        switch (particle.Term is ModelGroupDefinition definition ? definition.ModelGroup : particle.Term)
        {
            case ElementDeclaration element:
                return Leaf(particle, element)
                    ? AutomatonParticle.Element(particle.MinOccurs, particle.MaxOccurs, IdOf(element.Name))
                    : null;
            case IWildcard wildcard:
                return Leaf(particle, wildcard)
                    ? AutomatonParticle.Wildcard(particle.MinOccurs, particle.MaxOccurs, wildcard)
                    : null;
            case ModelGroup { Compositor: ModelGroupCompositor.All }:
                Status = SideStatus.NotComparable;
                return null;
            case ModelGroup group:
                var children = new List<AutomatonParticle>();
                for (var i = 0; i < group.Particles.Count; i++)
                {
                    if (Convert(group.Particles[i]) is { } child)
                    {
                        children.Add(child);
                    }
                }

                return group.Compositor == ModelGroupCompositor.Choice
                    ? AutomatonParticle.Choice(particle.MinOccurs, particle.MaxOccurs, children.ToArray())
                    : AutomatonParticle.Sequence(particle.MinOccurs, particle.MaxOccurs, children.ToArray());
            default:
                Status = SideStatus.NotComparable;
                return null;
        }
    }

    private bool Leaf(Particle particle, IXsdComponent term)
    {
        if (_terms.Count >= MaxPositions)
        {
            Status = SideStatus.TooLarge;
            return false;
        }

        _particles.Add(particle);
        _terms.Add(term);
        return true;
    }

    private int IdOf(XPathQName name)
    {
        if (!_ids.TryGetValue(name, out var id))
        {
            id = _names.Count;
            _ids.Add(name, id);
            _names.Add(name);
        }

        return id;
    }

    // The counters the guards read are the variables of the zone; their largest values come from the guards.
    private void ReadCounters(CountingAutomaton automaton)
    {
        var slots = automaton.CounterSlots;
        var caps = new long[slots];
        var read = new bool[slots];
        for (var t = 0; t < automaton.TransitionCount; t++)
        {
            var transition = automaton.GetTransition(t);
            ReadActions(automaton, transition.ActionStart, transition.ActionCount, caps, read);
        }

        for (var source = 0; source <= automaton.PositionCount; source++)
        {
            if (automaton.IsFinalSource(source, out var start, out var count))
            {
                ReadActions(automaton, start, count, caps, read);
            }
        }

        VariableOfSlot = new int[slots];
        var variableCaps = new List<long>();
        for (var slot = 0; slot < slots; slot++)
        {
            if (!read[slot])
            {
                continue;
            }

            if (caps[slot] >= MaxOccurrence)
            {
                Status = SideStatus.TooLarge;
                return;
            }

            variableCaps.Add(caps[slot]);
            VariableOfSlot[slot] = variableCaps.Count;
        }

        Caps = variableCaps.ToArray();
        Status = SideStatus.Ready;
    }

    private static void ReadActions(CountingAutomaton automaton, int start, int count, long[] caps, bool[] read)
    {
        for (var a = start; a < start + count; a++)
        {
            var action = automaton.GetAction(a);
            switch (action.Kind)
            {
                case AutomatonActionKind.Exit:
                    read[action.Slot] = true;
                    caps[action.Slot] = System.Math.Max(caps[action.Slot], action.Limit);
                    break;
                case AutomatonActionKind.Iterate:
                    read[action.Slot] = true;
                    caps[action.Slot] = System.Math.Max(caps[action.Slot], action.Cap);
                    break;
                default:
                    break;
            }
        }
    }
}
