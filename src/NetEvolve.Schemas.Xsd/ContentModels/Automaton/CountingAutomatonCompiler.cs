namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Compiles an <see cref="AutomatonParticle"/> tree into a <see cref="CountingAutomaton"/> (design #146 DP-7): leaf
/// particles become positions, the Glushkov first, last, and follow relations become transitions, and a particle whose
/// range is not <c>{1,1}</c>, <c>{0,1}</c>, <c>{0,unbounded}</c>, or <c>{1,unbounded}</c> gets one counter slot.
/// </summary>
/// <remarks>
/// <para>
/// Limitations: two internal, non-configurable ceilings apply per model, 65,536 positions and 1,048,576 transitions;
/// the follow relation is stored as explicit transitions, so it is quadratic in positions up to that ceiling (a
/// structural follow storage is a later optimization that keeps this design). The transitions of one source are
/// scanned linearly when matching.
/// </para>
/// <para>
/// A particle with <c>maxOccurs</c> of zero is removed. A choice without alternatives matches nothing unless
/// <c>minOccurs</c> is zero.
/// </para>
/// </remarks>
internal static class CountingAutomatonCompiler
{
    /// <summary>The ceiling of positions per content model, the figure of design #122.</summary>
    internal const int MaxPositions = 65_536;

    /// <summary>The ceiling of transitions per content model.</summary>
    internal const int MaxTransitions = 1 << 20;

    /// <summary>Compiles a particle tree.</summary>
    /// <param name="root">The root particle.</param>
    /// <param name="reporter">The reporter for the ceilings.</param>
    /// <param name="location">The location reported with a ceiling issue.</param>
    /// <returns>The automaton, or <see langword="null"/> when a ceiling is exceeded (and reported).</returns>
    internal static CountingAutomaton? Compile(
        AutomatonParticle root,
        XsdIssueReporter reporter,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(reporter);

        var builder = new Builder();
        return builder.Build(root, reporter, location);
    }

    private sealed class Node
    {
        public Node(AutomatonParticle particle, Node? parent)
        {
            Particle = particle;
            Parent = parent;
            Counted =
                particle.MaxOccurs >= 2
                && !(particle.MinOccurs <= 1 && particle.MaxOccurs == AutomatonParticle.Unbounded);
        }

        public AutomatonParticle Particle { get; }

        public Node? Parent { get; }

        public bool Counted { get; }

        public List<Node> Children { get; } = [];

        public int Position { get; set; } = -1;

        public int Slot { get; set; } = -1;

        public bool BodyNullable { get; set; }

        public bool Nullable { get; set; }

        public List<int> First { get; } = [];

        public List<int> Last { get; } = [];
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Edge
    {
        public Edge(int source, int target, int actionStart, int actionCount)
        {
            Source = source;
            Target = target;
            ActionStart = actionStart;
            ActionCount = actionCount;
        }

        public int Source { get; }

        public int Target { get; }

        public int ActionStart { get; }

        public int ActionCount { get; }
    }

    private sealed class Builder
    {
        private readonly List<Node> _positions = [];
        private readonly List<Edge> _edges = [];
        private readonly List<AutomatonAction> _actions = [];
        private readonly List<long> _slotMinimums = [];
        private int _slots;
        private bool _tooManyPositions;
        private bool _tooManyTransitions;

        public CountingAutomaton? Build(AutomatonParticle root, XsdIssueReporter reporter, DocumentLocation location)
        {
            // A removed root (maxOccurs zero) is the empty sequence.
            var rootNode = Create(root.MaxOccurs == 0 ? AutomatonParticle.Sequence(1, 1) : root, null);
            if (!_tooManyPositions)
            {
                Compute(rootNode);
            }

            if (_tooManyPositions)
            {
                reporter.Report(
                    XsdRuleCodes.EngineRules[0],
                    location,
                    "The content model has more than " + MaxPositions + " positions."
                );
                return null;
            }

            if (_tooManyTransitions)
            {
                reporter.Report(
                    XsdRuleCodes.EngineRules[1],
                    location,
                    "The content model has more than " + MaxTransitions + " transitions."
                );
                return null;
            }

            return Finish(rootNode);
        }

        private Node Create(AutomatonParticle particle, Node? parent)
        {
            var node = new Node(particle, parent);
            if (particle.Kind is AutomatonParticleKind.Element or AutomatonParticleKind.Wildcard)
            {
                node.Position = _positions.Count;
                _positions.Add(node);
                if (_positions.Count > MaxPositions)
                {
                    _tooManyPositions = true;
                }

                return node;
            }

            foreach (var child in particle.Children)
            {
                if (child.MaxOccurs == 0)
                {
                    continue;
                }

                node.Children.Add(Create(child, node));
                if (_tooManyPositions)
                {
                    break;
                }
            }

            return node;
        }

        private void Compute(Node node)
        {
            var particle = node.Particle;
            switch (particle.Kind)
            {
                case AutomatonParticleKind.Element:
                case AutomatonParticleKind.Wildcard:
                    node.First.Add(node.Position);
                    node.Last.Add(node.Position);
                    node.BodyNullable = false;
                    break;
                case AutomatonParticleKind.Sequence:
                    ComputeSequence(node);
                    break;
                default:
                    ComputeChoice(node);
                    break;
            }

            if (_tooManyTransitions)
            {
                return;
            }

            node.Nullable = node.BodyNullable || particle.MinOccurs == 0;

            if (particle.MaxOccurs >= 2)
            {
                foreach (var p in node.Last)
                {
                    foreach (var q in node.First)
                    {
                        if (!Emit(_positions[p], _positions[q], node, true))
                        {
                            return;
                        }
                    }
                }
            }
        }

        private void ComputeSequence(Node node)
        {
            var children = node.Children;
            var allNullable = true;
            foreach (var child in children)
            {
                Compute(child);
                if (_tooManyTransitions)
                {
                    return;
                }

                allNullable &= child.Nullable;
            }

            node.BodyNullable = allNullable;

            foreach (var child in children)
            {
                node.First.AddRange(child.First);
                if (!child.Nullable)
                {
                    break;
                }
            }

            for (var i = children.Count - 1; i >= 0; i--)
            {
                node.Last.AddRange(children[i].Last);
                if (!children[i].Nullable)
                {
                    break;
                }
            }

            for (var i = 0; i < children.Count; i++)
            {
                for (var j = i + 1; j < children.Count; j++)
                {
                    foreach (var p in children[i].Last)
                    {
                        foreach (var q in children[j].First)
                        {
                            if (!Emit(_positions[p], _positions[q], node, false))
                            {
                                return;
                            }
                        }
                    }

                    if (!children[j].Nullable)
                    {
                        break;
                    }
                }
            }
        }

        private void ComputeChoice(Node node)
        {
            var anyNullable = false;
            foreach (var child in node.Children)
            {
                Compute(child);
                if (_tooManyTransitions)
                {
                    return;
                }

                anyNullable |= child.Nullable;
                node.First.AddRange(child.First);
                node.Last.AddRange(child.Last);
            }

            node.BodyNullable = anyNullable;
        }

        private bool Emit(Node source, Node target, Node top, bool iterate)
        {
            if (_edges.Count >= MaxTransitions)
            {
                _tooManyTransitions = true;
                return false;
            }

            var start = _actions.Count;
            AddExits(source, top);
            if (iterate)
            {
                var max = top.Particle.MaxOccurs;
                if (top.Counted)
                {
                    // An unbounded counter saturates at its minimum, so its configurations stay finite.
                    _actions.Add(
                        new AutomatonAction(
                            AutomatonActionKind.Iterate,
                            SlotOf(top),
                            max,
                            max == AutomatonParticle.Unbounded ? top.Particle.MinOccurs : max
                        )
                    );
                }
            }

            AddEnters(target, top);
            _edges.Add(new Edge(source.Position + 1, target.Position, start, _actions.Count - start));
            return true;
        }

        private void AddExits(Node from, Node? top)
        {
            for (var x = from; x != top && x is not null; x = x.Parent)
            {
                // A counter is at least one once entered, and a nullable body lets empty iterations pad the minimum.
                if (x.Counted && !x.BodyNullable && x.Particle.MinOccurs >= 2)
                {
                    _actions.Add(new AutomatonAction(AutomatonActionKind.Exit, SlotOf(x), x.Particle.MinOccurs, 0));
                }
            }
        }

        private void AddEnters(Node from, Node? top)
        {
            for (var x = from; x != top && x is not null; x = x.Parent)
            {
                if (x.Counted)
                {
                    _actions.Add(new AutomatonAction(AutomatonActionKind.Enter, SlotOf(x), 1, 0));
                }
            }
        }

        private int SlotOf(Node node)
        {
            if (node.Slot < 0)
            {
                node.Slot = _slots++;
                _slotMinimums.Add(node.Particle.MinOccurs);
            }

            return node.Slot;
        }

        /// <summary>Counting sort of the edges by source: the per-source start offsets and the grouped transitions.</summary>
        private int[] GroupBySource(int positionCount, out AutomatonTransition[] transitions)
        {
            var sourceStart = new int[positionCount + 2];
            foreach (var edge in _edges)
            {
                sourceStart[edge.Source + 1]++;
            }

            for (var i = 0; i <= positionCount; i++)
            {
                sourceStart[i + 1] += sourceStart[i];
            }

            var next = new int[positionCount + 1];
            Array.Copy(sourceStart, next, positionCount + 1);
            transitions = new AutomatonTransition[_edges.Count];
            foreach (var edge in _edges)
            {
                transitions[next[edge.Source]++] = new AutomatonTransition(
                    edge.Target,
                    edge.ActionStart,
                    edge.ActionCount
                );
            }

            return sourceStart;
        }

        private CountingAutomaton? Finish(Node root)
        {
            var positionCount = _positions.Count;

            foreach (var q in root.First)
            {
                var start = _actions.Count;
                AddEnters(_positions[q], null);
                _edges.Add(new Edge(0, q, start, _actions.Count - start));
            }

            var finalStart = new int[positionCount + 1];
            var finalCount = new int[positionCount + 1];
            for (var i = 0; i < finalStart.Length; i++)
            {
                finalStart[i] = -1;
            }

            if (root.Nullable)
            {
                finalStart[0] = 0;
            }

            foreach (var p in root.Last)
            {
                var start = _actions.Count;
                AddExits(_positions[p], null);
                finalStart[p + 1] = start;
                finalCount[p + 1] = _actions.Count - start;
            }

            var sourceStart = GroupBySource(positionCount, out var transitions);

            var names = new int[positionCount][];
            var wildcards = new IWildcard?[positionCount];
            for (var i = 0; i < positionCount; i++)
            {
                var particle = _positions[i].Particle;
                names[i] = particle.NameIds ?? [];
                wildcards[i] = particle.WildcardComponent;
            }

            return new CountingAutomaton(
                names,
                wildcards,
                sourceStart,
                transitions,
                _actions.ToArray(),
                finalStart,
                finalCount,
                _slots,
                _slotMinimums.ToArray()
            );
        }
    }
}
