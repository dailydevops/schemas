namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>The key-sequences of one constraint in one scope, found by their hash in the value space.</summary>
internal sealed class KeyTable
{
    private readonly Dictionary<int, List<KeySequence>> _buckets = [];

    /// <summary>Gets the number of key-sequences.</summary>
    internal int Count { get; private set; }

    /// <summary>Determines whether a key-sequence with pairwise equal values is in the table.</summary>
    /// <param name="sequence">The key-sequence.</param>
    /// <param name="space">The value space that compares the values.</param>
    /// <returns><see langword="true"/> when the table holds an equal key-sequence.</returns>
    internal bool Contains(KeySequence sequence, ValueSpace space)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        ArgumentNullException.ThrowIfNull(space);

        if (!_buckets.TryGetValue(sequence.Hash, out var bucket))
        {
            return false;
        }

        for (var i = 0; i < bucket.Count; i++)
        {
            if (AreEqual(bucket[i], sequence, space))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Adds a key-sequence.</summary>
    /// <param name="sequence">The key-sequence.</param>
    internal void Add(KeySequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence);

        if (!_buckets.TryGetValue(sequence.Hash, out var bucket))
        {
            bucket = [];
            _buckets.Add(sequence.Hash, bucket);
        }

        bucket.Add(sequence);
        Count++;
    }

    private static bool AreEqual(KeySequence first, KeySequence second, ValueSpace space)
    {
        if (first.Lexicals.Length != second.Lexicals.Length)
        {
            return false;
        }

        for (var i = 0; i < first.Lexicals.Length; i++)
        {
            if (!space.AreEqual(first.Types[i], first.Lexicals[i], second.Types[i], second.Lexicals[i]))
            {
                return false;
            }
        }

        return true;
    }
}
