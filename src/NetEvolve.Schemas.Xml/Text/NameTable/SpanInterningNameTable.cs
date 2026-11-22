namespace NetEvolve.Schemas.Xml.Text.NameTable;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// An <see cref="XmlNameTable"/> that interns strings keyed by character spans: a hit allocates nothing and equal names
/// share one string instance.
/// </summary>
/// <remarks>
/// Open addressing with linear probing over a power-of-two bucket array (load factor at most one half), growth with
/// rehash from the stored full hashes, and string equality on every hash match so distinct names with equal hashes stay
/// distinct. Symbol zero is the empty string. Thread safety: an unfrozen table is not thread-safe; a frozen table
/// allows concurrent reads without locks, and <see cref="TryIntern"/> on a frozen table never writes. The limit bounds
/// <see cref="Count"/> (the empty string included) per table.
/// </remarks>
public sealed class SpanInterningNameTable : XmlNameTable
{
    private const int MinBuckets = 8;
    private const int MaxBuckets = 1 << 30;

    private readonly int _maxDistinctNames;
    private readonly NameHash _hash;
    private string[] _strings;
    private int[] _hashes;
    private int[] _buckets; // symbol + 1; zero is an empty slot
    private int _count;
    private bool _frozen;

    /// <summary>Initializes a new instance of the <see cref="SpanInterningNameTable"/> class.</summary>
    /// <param name="maxDistinctNames">The maximum <see cref="Count"/>, the empty string included; at least one.</param>
    /// <param name="initialCapacity">The number of names the table holds before it first grows; at least one.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDistinctNames"/> or <paramref name="initialCapacity"/> is less than one.</exception>
    public SpanInterningNameTable(int maxDistinctNames, int initialCapacity = 64)
        : this(maxDistinctNames, initialCapacity, Fnv1a) { }

    internal SpanInterningNameTable(int maxDistinctNames, int initialCapacity, NameHash hash)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDistinctNames, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 1);
        ArgumentNullException.ThrowIfNull(hash);

        _maxDistinctNames = maxDistinctNames;
        _hash = hash;

        var capacity = Math.Min(initialCapacity, maxDistinctNames);
        var buckets = MinBuckets;
        while (buckets < MaxBuckets && buckets < capacity * 2L)
        {
            buckets <<= 1;
        }

        _buckets = new int[buckets];
        _strings = new string[Math.Min(capacity, buckets / 2)];
        _hashes = new int[_strings.Length];
        _strings[0] = string.Empty;
        _hashes[0] = _hash(ReadOnlySpan<char>.Empty);
        _count = 1;
        Insert(_hashes[0], 0);
    }

    /// <inheritdoc />
    public override int Count => _count;

    /// <inheritdoc />
    public override bool IsFrozen => _frozen;

    /// <inheritdoc />
    public override XmlNameTableStatus TryIntern(ReadOnlySpan<char> text, out XmlSymbol symbol)
    {
        var hash = _hash(text);
        if (Find(text, hash, out symbol))
        {
            return XmlNameTableStatus.Ok;
        }

        symbol = default;
        if (_frozen)
        {
            return XmlNameTableStatus.Frozen;
        }

        if (_count >= _maxDistinctNames)
        {
            return XmlNameTableStatus.LimitExceeded;
        }

        if (_count == _strings.Length)
        {
            var size = (int)Math.Min((long)_strings.Length * 2, _maxDistinctNames);
            Array.Resize(ref _strings, size);
            Array.Resize(ref _hashes, size);
        }

        if ((_count + 1L) * 2 > _buckets.Length && _buckets.Length < MaxBuckets)
        {
            Rehash(_buckets.Length * 2);
        }

        var index = _count++;
        _strings[index] = text.ToString();
        _hashes[index] = hash;
        Insert(hash, index);
        symbol = new XmlSymbol(index);
        return XmlNameTableStatus.Ok;
    }

    /// <inheritdoc />
    public override bool TryGet(ReadOnlySpan<char> text, out XmlSymbol symbol) => Find(text, _hash(text), out symbol);

    /// <inheritdoc />
    public override string GetString(XmlSymbol symbol)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(symbol.Value, _count - 1);
        return _strings[symbol.Value];
    }

    /// <inheritdoc />
    public override bool TryGetString(XmlSymbol symbol, out string? text)
    {
        if (symbol.Value < _count)
        {
            text = _strings[symbol.Value];
            return true;
        }

        text = null;
        return false;
    }

    /// <inheritdoc />
    public override void Freeze() => _frozen = true;

    private bool Find(ReadOnlySpan<char> text, int hash, out XmlSymbol symbol)
    {
        var buckets = _buckets;
        var mask = buckets.Length - 1;
        for (var slot = hash & mask; ; slot = (slot + 1) & mask)
        {
            var entry = buckets[slot];
            if (entry == 0)
            {
                symbol = default;
                return false;
            }

            var index = entry - 1;
            if (_hashes[index] == hash && text.SequenceEqual(_strings[index].AsSpan()))
            {
                symbol = new XmlSymbol(index);
                return true;
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

    private static int Fnv1a(ReadOnlySpan<char> text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return (int)(hash ^ (hash >> 15));
    }
}
