namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton.AutomatonTestKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism.DeterminismKit;

/// <summary>
/// Compares the region analysis with the definition on the unfolded model: every occurrence is copied, and two follow
/// positions with the same name that are copies of two different particles are ambiguous (design #146 DP-8).
/// </summary>
public class DeterminismDifferentialTests
{
    private const int NameCount = 3;

    [Test]
    public async Task RandomModels_AgreeWithTheUnfoldedDefinition()
    {
        var random = new Lcg(278);
        var mismatches = new List<string>();
        var ambiguous = 0;
        for (var model = 0; model < 400; model++)
        {
            var particle = Generate(random, 3);
            var expected = Reference(particle);
            var actual = Actual(particle);
            ambiguous += expected.Count > 0 ? 1 : 0;
            if (!expected.SetEquals(actual))
            {
                mismatches.Add(
                    $"model {model} (seed 278): expected [{string.Join(";", expected)}], actual [{string.Join(";", actual)}]"
                );
            }
        }

        await Assert.That(mismatches).IsEmpty();

        // The generator must produce both verdicts in numbers, or the comparison proves nothing.
        await Assert.That(ambiguous).IsGreaterThan(40);
        await Assert.That(ambiguous).IsLessThan(360);
    }

    private static AutomatonParticle Generate(Lcg random, int depth)
    {
        var (min, max) = Range(random);
        if (depth == 0 || random.Next(3) == 0)
        {
            return El(random.Next(NameCount) + 1, min, max);
        }

        var count = random.Next(1, 4);
        var children = new AutomatonParticle[count];
        for (var i = 0; i < count; i++)
        {
            children[i] = Generate(random, depth - 1);
        }

        return random.Next(2) == 0 ? Seq(min, max, children) : Cho(min, max, children);
    }

    private static (long Min, long Max) Range(Lcg random) =>
        random.Next(12) switch
        {
            0 => (0, 1),
            1 => (0, Unbounded),
            2 => (1, Unbounded),
            3 => (2, 3),
            4 => (0, 2),
            5 => (2, Unbounded),
            6 => (2, 2),
            7 => (1, 3),
            8 => (3, 5),
            9 => (0, 5),
            10 => (5, 5),
            _ => (1, 1),
        };

    /// <summary>The ambiguous pairs reported by the check as positions, smaller first.</summary>
    private static HashSet<string> Actual(AutomatonParticle particle)
    {
        var outcome = Check(particle);
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var issue in outcome.Issues)
        {
            // The message names the second particle last, as source(line,column).
            var message = issue.Message;
            var start = message.LastIndexOf(Source + "(", StringComparison.Ordinal) + Source.Length + 1;
            var line = message.Substring(start, message.IndexOf(',', start) - start);
            pairs.Add(issue.Location.Line.ToString(CultureInfo.InvariantCulture) + "," + line);
        }

        return pairs;
    }

    /// <summary>The pairs of original particles, as one-based positions, whose copies compete in the unfolded model.</summary>
    private static HashSet<string> Reference(AutomatonParticle root)
    {
        var originals = new Dictionary<AutomatonParticle, int>();
        Number(root, originals);

        var copies = new List<(int Original, int Name)>();
        var tree = Unfold(root, originals, copies);
        var follow = new HashSet<int>[copies.Count];
        for (var i = 0; i < follow.Length; i++)
        {
            follow[i] = [];
        }

        Analyze(tree, follow);

        var pairs = new HashSet<string>(StringComparer.Ordinal);
        Compete(tree.First, copies, pairs);
        for (var i = 0; i < follow.Length; i++)
        {
            Compete(follow[i], copies, pairs);
        }

        return pairs;
    }

    private static void Number(AutomatonParticle particle, Dictionary<AutomatonParticle, int> originals)
    {
        if (particle.MaxOccurs == 0)
        {
            return;
        }

        if (particle.Kind == AutomatonParticleKind.Element)
        {
            originals[particle] = originals.Count + 1;
            return;
        }

        foreach (var child in particle.Children)
        {
            Number(child, originals);
        }
    }

    private static void Compete(HashSet<int> state, List<(int Original, int Name)> copies, HashSet<string> pairs)
    {
        foreach (var left in state)
        {
            foreach (var right in state)
            {
                var (leftOriginal, leftName) = copies[left];
                var (rightOriginal, rightName) = copies[right];
                if (leftOriginal < rightOriginal && leftName == rightName)
                {
                    pairs.Add(leftOriginal + "," + rightOriginal);
                }
            }
        }
    }

    private static Node Unfold(
        AutomatonParticle particle,
        Dictionary<AutomatonParticle, int> originals,
        List<(int Original, int Name)> copies
    )
    {
        if (particle.MaxOccurs == 0)
        {
            return Node.Empty();
        }

        var parts = new List<Node>();
        for (long i = 0; i < particle.MinOccurs; i++)
        {
            parts.Add(Body(particle, originals, copies));
        }

        if (particle.MaxOccurs == AutomatonParticle.Unbounded)
        {
            parts.Add(Node.Star(Body(particle, originals, copies)));
        }
        else if (particle.MaxOccurs > particle.MinOccurs)
        {
            // x{0,k} is (x (x (...)?)?)?, so no iteration competes with a later copy of itself.
            var optional = Node.Empty();
            for (var i = particle.MinOccurs; i < particle.MaxOccurs; i++)
            {
                optional = Node.Choice(Node.Sequence(Body(particle, originals, copies), optional), Node.Empty());
            }

            parts.Add(optional);
        }

        return Node.Sequence([.. parts]);
    }

    private static Node Body(
        AutomatonParticle particle,
        Dictionary<AutomatonParticle, int> originals,
        List<(int Original, int Name)> copies
    )
    {
        if (particle.Kind == AutomatonParticleKind.Element)
        {
            copies.Add((originals[particle], particle.NameIds![0]));
            return Node.Leaf(copies.Count - 1);
        }

        var children = new Node[particle.Children.Length];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = Unfold(particle.Children[i], originals, copies);
        }

        // A choice without alternatives does not occur in generated models.
        return particle.Kind == AutomatonParticleKind.Sequence ? Node.Sequence(children) : Node.Choice(children);
    }

    /// <summary>Computes nullable, first, and last of every node and the follow sets of the positions.</summary>
    private static void Analyze(Node node, HashSet<int>[] follow)
    {
        foreach (var child in node.Children)
        {
            Analyze(child, follow);
        }

        switch (node.Kind)
        {
            case NodeKind.Leaf:
                node.First.Add(node.Position);
                node.Last.Add(node.Position);
                break;
            case NodeKind.Sequence:
                AnalyzeSequence(node, follow);
                break;
            case NodeKind.Choice:
                foreach (var child in node.Children)
                {
                    node.Nullable |= child.Nullable;
                    node.First.UnionWith(child.First);
                    node.Last.UnionWith(child.Last);
                }

                break;
            default:
                node.Nullable = true;
                node.First.UnionWith(node.Children[0].First);
                node.Last.UnionWith(node.Children[0].Last);
                foreach (var last in node.Last)
                {
                    follow[last].UnionWith(node.First);
                }

                break;
        }
    }

    private static void AnalyzeSequence(Node node, HashSet<int>[] follow)
    {
        var children = node.Children;
        node.Nullable = true;
        foreach (var child in children)
        {
            node.Nullable &= child.Nullable;
        }

        for (var i = 0; i < children.Count; i++)
        {
            node.First.UnionWith(children[i].First);
            if (!children[i].Nullable)
            {
                break;
            }
        }

        for (var i = children.Count - 1; i >= 0; i--)
        {
            node.Last.UnionWith(children[i].Last);
            if (!children[i].Nullable)
            {
                break;
            }
        }

        for (var i = 0; i < children.Count; i++)
        {
            for (var j = i + 1; j < children.Count; j++)
            {
                foreach (var last in children[i].Last)
                {
                    follow[last].UnionWith(children[j].First);
                }

                if (!children[j].Nullable)
                {
                    break;
                }
            }
        }
    }

    private enum NodeKind
    {
        Leaf,
        Sequence,
        Choice,
        Star,
    }

    private sealed class Node
    {
        private Node(NodeKind kind, int position, List<Node> children)
        {
            Kind = kind;
            Position = position;
            Children = children;
        }

        public NodeKind Kind { get; }

        public int Position { get; }

        public List<Node> Children { get; }

        public bool Nullable { get; set; }

        public HashSet<int> First { get; } = [];

        public HashSet<int> Last { get; } = [];

        public static Node Leaf(int position) => new(NodeKind.Leaf, position, []);

        public static Node Empty() => new(NodeKind.Sequence, -1, []);

        public static Node Sequence(params Node[] children) => new(NodeKind.Sequence, -1, [.. children]);

        public static Node Choice(params Node[] children) => new(NodeKind.Choice, -1, [.. children]);

        public static Node Star(Node child) => new(NodeKind.Star, -1, [child]);
    }
}
