namespace NetEvolve.Schemas.Schematron.Evaluation.Dispatch;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The rule index of the active patterns (SCH-72), derived from the compiled rule contexts: per node kind the patterns that have a
/// context without a name test (the fallback list), and per node kind and expanded name the patterns that have a context for that
/// name, merged with the fallback list. A context lists the node kinds it can match and the expanded names of its last step
/// (<see cref="CompiledPattern.CandidateNames"/>). Immutable after construction and shared by every run of the same patterns.
/// </summary>
internal sealed class DispatchTable
{
    /// <summary>The number of node kind positions: document, element, attribute, text, comment, processing instruction.</summary>
    internal const int KindCount = 6;

    private readonly bool[]?[] _any = new bool[]?[KindCount];
    private readonly Dictionary<XPathQName, bool[]>?[] _named = new Dictionary<XPathQName, bool[]>?[KindCount];

    /// <summary>Initializes a new instance of the <see cref="DispatchTable"/> class.</summary>
    /// <param name="sets">The active patterns in document order.</param>
    internal DispatchTable(IReadOnlyList<CompiledRuleSet> sets)
    {
        ArgumentNullException.ThrowIfNull(sets);

        SetCount = sets.Count;
        for (var set = 0; set < sets.Count; set++)
        {
            var rules = sets[set].Rules;
            for (var r = 0; r < rules.Length; r++)
            {
                Add(rules[r].Context, set);
            }
        }

        // A node with a name that a context lists is a candidate of the contexts without a name test as well.
        for (var position = 0; position < KindCount; position++)
        {
            if (_any[position] is not { } fallback || _named[position] is not { } table)
            {
                continue;
            }

            foreach (var flags in table.Values)
            {
                for (var i = 0; i < flags.Length; i++)
                {
                    flags[i] |= fallback[i];
                }
            }
        }
    }

    /// <summary>Gets the number of patterns.</summary>
    internal int SetCount { get; }

    /// <summary>Determines whether a node kind has contexts with a name test, so that the name of a node matters.</summary>
    /// <param name="position">The node kind position.</param>
    /// <returns><see langword="true"/> when at least one context of this kind lists a name.</returns>
    internal bool HasNames(int position) => _named[position] is not null;

    /// <summary>Gets the patterns that can match a node.</summary>
    /// <param name="position">The node kind position.</param>
    /// <param name="name">The expanded name of the node; ignored when <see cref="HasNames"/> is <see langword="false"/>.</param>
    /// <returns>One flag per pattern, or <see langword="null"/> when no pattern can match such a node.</returns>
    internal bool[]? Candidates(int position, XPathQName name) =>
        _named[position] is { } table && table.TryGetValue(name, out var flags) ? flags : _any[position];

    /// <summary>Gets the position of a node kind in the tables.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The position, or -1 for a kind that is never visited.</returns>
    internal static int PositionOf(QueryNodeKinds kind) =>
        kind switch
        {
            QueryNodeKinds.Document => 0,
            QueryNodeKinds.Element => 1,
            QueryNodeKinds.Attribute => 2,
            QueryNodeKinds.Text => 3,
            QueryNodeKinds.Comment => 4,
            QueryNodeKinds.ProcessingInstruction => 5,
            _ => -1,
        };

    private static QueryNodeKinds KindAt(int position) =>
        position switch
        {
            0 => QueryNodeKinds.Document,
            1 => QueryNodeKinds.Element,
            2 => QueryNodeKinds.Attribute,
            3 => QueryNodeKinds.Text,
            4 => QueryNodeKinds.Comment,
            _ => QueryNodeKinds.ProcessingInstruction,
        };

    private void Add(CompiledPattern context, int set)
    {
        for (var position = 0; position < KindCount; position++)
        {
            if ((context.NodeKinds & KindAt(position)) == QueryNodeKinds.None)
            {
                continue;
            }

            if (context.CandidateNames.Length == 0)
            {
                (_any[position] ??= new bool[SetCount])[set] = true;
                continue;
            }

            var table = _named[position] ??= [];
            foreach (var name in context.CandidateNames)
            {
                if (!table.TryGetValue(name, out var flags))
                {
                    flags = new bool[SetCount];
                    table.Add(name, flags);
                }

                flags[set] = true;
            }
        }
    }
}
