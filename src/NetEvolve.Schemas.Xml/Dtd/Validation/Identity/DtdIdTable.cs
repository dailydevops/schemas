namespace NetEvolve.Schemas.Xml.Dtd.Validation.Identity;

using System;
using NetEvolve.Arguments;

/// <summary>
/// The set of the ID values of one document. A lookup takes a character span and allocates nothing; the names live in one
/// shared buffer (<see cref="DtdNameStore"/>), so adding a value costs a few integers and no string.
/// </summary>
/// <remarks>
/// Open addressing with linear probing over a power-of-two bucket array (load factor at most one half) and the stored full
/// hashes for the rehash; every hash match is confirmed by comparing the characters, so distinct names with equal hashes
/// stay distinct. <see cref="Clear"/> keeps the capacity for the next document. Not thread-safe.
/// </remarks>
internal sealed class DtdIdTable
{
    private const int MinBuckets = 64;
    private const int MaxBuckets = 1 << 30;

    private readonly DtdNameStore _names = new();
    private int[] _starts = new int[32];
    private int[] _lengths = new int[32];
    private int[] _hashes = new int[32];
    private int[] _buckets = new int[MinBuckets]; // entry + 1; zero is an empty slot
    private int _count;

    /// <summary>Gets the number of stored values.</summary>
    internal int Count => _count;

    /// <summary>Determines whether the table holds the value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the table holds the value.</returns>
    internal bool Contains(ReadOnlySpan<char> value) => Find(value, Hash(value)) >= 0;

    /// <summary>Adds a value unless the table holds it or already holds <paramref name="limit"/> values.</summary>
    /// <param name="value">The value.</param>
    /// <param name="limit">The most values the table may hold; at least one.</param>
    /// <returns>What happened.</returns>
    internal DtdIdAddResult TryAdd(ReadOnlySpan<char> value, int limit)
    {
        Argument.ThrowIfLessThan(limit, 1);

        var hash = Hash(value);
        if (Find(value, hash) >= 0)
        {
            return DtdIdAddResult.Duplicate;
        }

        if (_count >= limit)
        {
            return DtdIdAddResult.LimitExceeded;
        }

        if (_count == _starts.Length)
        {
            var size = (int)Math.Min(_starts.Length * 2L, int.MaxValue);
            Array.Resize(ref _starts, size);
            Array.Resize(ref _lengths, size);
            Array.Resize(ref _hashes, size);
        }

        if ((_count + 1L) * 2 > _buckets.Length && _buckets.Length < MaxBuckets)
        {
            Rehash(_buckets.Length * 2);
        }

        _starts[_count] = _names.Append(value);
        _lengths[_count] = value.Length;
        _hashes[_count] = hash;
        Insert(hash, _count);
        _count++;

        return DtdIdAddResult.Added;
    }

    /// <summary>Forgets every value and keeps the capacity.</summary>
    internal void Clear()
    {
        if (_count > 0)
        {
            Array.Clear(_buckets, 0, _buckets.Length);
        }

        _count = 0;
        _names.Clear();
    }

    private int Find(ReadOnlySpan<char> value, int hash)
    {
        var mask = _buckets.Length - 1;
        for (var slot = hash & mask; ; slot = (slot + 1) & mask)
        {
            var entry = _buckets[slot];
            if (entry == 0)
            {
                return -1;
            }

            var index = entry - 1;
            if (
                _hashes[index] == hash
                && _lengths[index] == value.Length
                && value.SequenceEqual(_names.Get(_starts[index], _lengths[index]))
            )
            {
                return index;
            }
        }
    }

    private void Insert(int hash, int index)
    {
        var mask = _buckets.Length - 1;
        var slot = hash & mask;
        while (_buckets[slot] != 0)
        {
            slot = (slot + 1) & mask;
        }

        _buckets[slot] = index + 1;
    }

    private void Rehash(int bucketCount)
    {
        _buckets = new int[bucketCount];
        for (var i = 0; i < _count; i++)
        {
            Insert(_hashes[i], i);
        }
    }

    // FNV-1a over the UTF-16 code units. The hash of SpanInterningNameTable is private to that type, which belongs to another
    // work package; a name table also interns a string per name, which this table avoids.
    private static int Hash(ReadOnlySpan<char> value)
    {
        var hash = 2166136261u;
        foreach (var c in value)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return (int)(hash ^ (hash >> 15));
    }
}
