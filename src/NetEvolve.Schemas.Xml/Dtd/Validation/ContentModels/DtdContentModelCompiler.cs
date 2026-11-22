namespace NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// Compiles the particle tree of an element content model into a <see cref="DtdContentModel"/> (Glushkov construction).
/// The walk uses an explicit stack, so the depth of a hand-built tree cannot overflow the call stack.
/// </summary>
/// <remarks>
/// <para>
/// Two bounds keep the compilation from blowing up: the number of positions is limited by the caller
/// (<c>MaxContentModelStates</c>), and the work, that is the sets built and the follow transitions added, is limited by
/// <see cref="MaxWork"/>. The follow relation of a model such as <c>(a?,b?,c?,...)</c> is quadratic in the number of
/// positions, so the second bound is what stops a model that is small in positions but large as an automaton. Both
/// failures end with the same limit code of the caller (design #122, section 9).
/// </para>
/// <para>Not thread-safe: an instance keeps scratch memory between compilations.</para>
/// </remarks>
internal sealed class DtdContentModelCompiler
{
    /// <summary>The bound on the sets built and the follow transitions added for one model: 4,194,304.</summary>
    internal const int MaxWork = 1 << 22;

    private static readonly int[] StartOnly = [DtdContentModel.StartPosition];

    private readonly XmlNameTable _names;
    private readonly int _maxPositions;

    private Frame[] _frames = new Frame[16];
    private Result[] _results = new Result[16];
    private int[] _positionNames = new int[16];
    private int[] _from = new int[16];
    private int[] _to = new int[16];
    private byte[] _positionSeen = [];
    private byte[] _nameSeen = [];
    private int _frameCount;
    private int _resultCount;
    private int _positions;
    private int _pairs;
    private long _work;

    /// <summary>Initializes a new instance of the <see cref="DtdContentModelCompiler"/> class.</summary>
    /// <param name="names">The table that gives every element name its symbol; it must not be frozen.</param>
    /// <param name="maxPositions">The most positions one model may have; at least one.</param>
    internal DtdContentModelCompiler(XmlNameTable names, int maxPositions)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPositions, 1);

        _names = names;
        _maxPositions = maxPositions;
    }

    /// <summary>Interns an element name in the table of the compiler.</summary>
    /// <param name="name">The name.</param>
    /// <param name="symbol">The symbol of the name.</param>
    /// <returns><see langword="false"/> when the table cannot take the name.</returns>
    internal bool TryIntern(string name, out int symbol)
    {
        ArgumentNullException.ThrowIfNull(name);

        var status = _names.TryIntern(name.AsSpan(), out var interned);
        symbol = interned.Value;

        return status == XmlNameTableStatus.Ok;
    }

    /// <summary>Gets the name that belongs to a symbol of the table of the compiler.</summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The name.</returns>
    internal string NameOf(int symbol) => _names.GetString(new XmlSymbol(symbol));

    /// <summary>Compiles a content model.</summary>
    /// <param name="root">The particle tree; <see langword="null"/> is a model that matches only empty content.</param>
    /// <param name="model">The compiled model when the result is <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the model exceeds the positions or the work bound.</returns>
    internal bool TryCompile(DtdContentParticle? root, out DtdContentModel? model)
    {
        model = null;
        _frameCount = 0;
        _resultCount = 0;
        _positions = 0;
        _pairs = 0;
        _work = 0;
        _positionNames[0] = -1;

        if (root is null)
        {
            PushResult(true, [], []);
        }
        else if (!Walk(root))
        {
            return false;
        }

        var top = _results[0];
        if (!AddPairs(StartOnly, top.First))
        {
            return false;
        }

        model = Build(top);

        return true;
    }

    private bool Walk(DtdContentParticle root)
    {
        PushFrame(root);
        while (_frameCount > 0)
        {
            var index = _frameCount - 1;
            var node = _frames[index].Node;
            if (node.Kind == DtdParticleKind.Name)
            {
                if (!Leaf(node))
                {
                    return false;
                }

                _frameCount--;
                continue;
            }

            var next = _frames[index].Next;
            if (next < node.Items.Count)
            {
                _frames[index].Next = next + 1;
                PushFrame(node.Items[next]);
                continue;
            }

            if (!Combine(node))
            {
                return false;
            }

            _frameCount--;
        }

        return true;
    }

    private bool Leaf(DtdContentParticle node)
    {
        if (_positions >= _maxPositions || !TryIntern(node.Name ?? string.Empty, out var symbol))
        {
            return false;
        }

        _positions++;
        if (_positions == _positionNames.Length)
        {
            Array.Resize(ref _positionNames, _positionNames.Length * 2);
        }

        _positionNames[_positions] = symbol;
        int[] single = [_positions];
        PushResult(false, single, single);

        return Occurrence(node.Occurrence);
    }

    private bool Combine(DtdContentParticle node)
    {
        var count = node.Items.Count;
        var start = _resultCount - count;
        var sequence = node.Kind == DtdParticleKind.Sequence;
        var nullable = sequence;
        int[]? first;
        int[]? last;

        if (sequence)
        {
            for (var i = 0; i < count - 1; i++)
            {
                for (var j = i + 1; j < count; j++)
                {
                    if (!AddPairs(_results[start + i].Last, _results[start + j].First))
                    {
                        return false;
                    }

                    if (!_results[start + j].Nullable)
                    {
                        break;
                    }
                }
            }

            for (var i = 0; i < count; i++)
            {
                nullable &= _results[start + i].Nullable;
            }

            first = Union(start, count, fromStart: true, nullableOnly: true);
            last = Union(start, count, fromStart: false, nullableOnly: true);
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                nullable |= _results[start + i].Nullable;
            }

            first = Union(start, count, fromStart: true, nullableOnly: false);
            last = Union(start, count, fromStart: false, nullableOnly: false);
        }

        if (first is null || last is null)
        {
            return false;
        }

        _resultCount = start;
        PushResult(nullable, first, last);

        return Occurrence(node.Occurrence);
    }

    // The union of the first (or last) sets of the children. For a sequence it runs from the first child (or the last)
    // up to and including the first child that is not nullable; for a choice it covers every child. A single contributing
    // set is shared, not copied, so nested groups of one member cost nothing.
    private int[]? Union(int start, int count, bool fromStart, bool nullableOnly)
    {
        var total = 0;
        var parts = 0;
        for (var n = 0; n < count; n++)
        {
            var i = fromStart ? n : count - 1 - n;
            var result = _results[start + i];
            total += (fromStart ? result.First : result.Last).Length;
            parts++;
            if (nullableOnly && !result.Nullable)
            {
                break;
            }
        }

        if (parts == 1)
        {
            var only = _results[start + (fromStart ? 0 : count - 1)];

            return fromStart ? only.First : only.Last;
        }

        if (!Charge(total))
        {
            return null;
        }

        var union = new int[total];
        var at = 0;
        for (var n = 0; n < parts; n++)
        {
            var i = fromStart ? n : count - 1 - n;
            var set = fromStart ? _results[start + i].First : _results[start + i].Last;
            set.CopyTo(union, at);
            at += set.Length;
        }

        return union;
    }

    // The occurrence indicator of the node whose result is on top: it makes the node nullable and, for a repeat, lets
    // the last positions of the node be followed by its first positions.
    private bool Occurrence(DtdOccurrence occurrence)
    {
        ref var result = ref _results[_resultCount - 1];
        if ((occurrence is DtdOccurrence.ZeroOrMore or DtdOccurrence.OneOrMore) && !AddPairs(result.Last, result.First))
        {
            return false;
        }

        if (occurrence is DtdOccurrence.Optional or DtdOccurrence.ZeroOrMore)
        {
            result.Nullable = true;
        }

        return true;
    }

    private bool AddPairs(int[] from, int[] to)
    {
        var count = (long)from.Length * to.Length;
        if (count == 0)
        {
            return true;
        }

        if (!Charge(count))
        {
            return false;
        }

        var needed = _pairs + (int)count;
        if (needed > _from.Length)
        {
            var size = Math.Max(needed, (int)Math.Min((long)_from.Length * 2, MaxWork));
            Array.Resize(ref _from, size);
            Array.Resize(ref _to, size);
        }

        foreach (var f in from)
        {
            foreach (var t in to)
            {
                _from[_pairs] = f;
                _to[_pairs] = t;
                _pairs++;
            }
        }

        return true;
    }

    private bool Charge(long amount)
    {
        _work += amount;

        return _work <= MaxWork;
    }

    private void PushFrame(DtdContentParticle node)
    {
        if (_frameCount == _frames.Length)
        {
            Array.Resize(ref _frames, _frames.Length * 2);
        }

        _frames[_frameCount++] = new Frame(node);
    }

    private void PushResult(bool nullable, int[] first, int[] last)
    {
        if (_resultCount == _results.Length)
        {
            Array.Resize(ref _results, _results.Length * 2);
        }

        _results[_resultCount++] = new Result(nullable, first, last);
    }

    // Orders the transitions by source position, drops duplicates, and finds a name that two follow positions share.
    private DtdContentModel Build(Result top)
    {
        var size = _positions + 1;
        var offsets = SortBySource(size, out var sorted);
        var finalOffsets = Compact(sorted, offsets, size, out var write, out var ambiguous);

        var names = new int[size];
        Array.Copy(_positionNames, names, size);
        var isLast = new bool[size];
        isLast[DtdContentModel.StartPosition] = top.Nullable;
        foreach (var position in top.Last)
        {
            isLast[position] = true;
        }

        Array.Resize(ref sorted, write);

        return new DtdContentModel(names, finalOffsets, sorted, isLast, ambiguous);
    }

    // Counting sort of the transitions by source: the target of every transition in source order, and where each source starts.
    private int[] SortBySource(int size, out int[] sorted)
    {
        var offsets = new int[size + 1];
        for (var i = 0; i < _pairs; i++)
        {
            offsets[_from[i] + 1]++;
        }

        for (var p = 0; p < size; p++)
        {
            offsets[p + 1] += offsets[p];
        }

        var cursor = new int[size];
        Array.Copy(offsets, cursor, size);
        sorted = new int[_pairs];
        for (var i = 0; i < _pairs; i++)
        {
            sorted[cursor[_from[i]]++] = _to[i];
        }

        return offsets;
    }

    // Removes the repeated targets of each source in place (the write index never passes the read index) and records the
    // first name that two different targets of one source share: the model is then not deterministic (Appendix E).
    private int[] Compact(int[] sorted, int[] offsets, int size, out int write, out int ambiguous)
    {
        if (_positionSeen.Length < size)
        {
            _positionSeen = new byte[size];
        }

        if (_nameSeen.Length < _names.Count)
        {
            _nameSeen = new byte[_names.Count];
        }

        ambiguous = -1;
        write = 0;
        var finalOffsets = new int[size + 1];
        for (var p = 0; p < size; p++)
        {
            var begin = write;
            for (var i = offsets[p]; i < offsets[p + 1]; i++)
            {
                var target = sorted[i];
                if (_positionSeen[target] != 0)
                {
                    continue;
                }

                _positionSeen[target] = 1;
                sorted[write++] = target;
                var name = _positionNames[target];
                if (_nameSeen[name] != 0 && ambiguous < 0)
                {
                    ambiguous = name;
                }

                _nameSeen[name] = 1;
            }

            for (var i = begin; i < write; i++)
            {
                _positionSeen[sorted[i]] = 0;
                _nameSeen[_positionNames[sorted[i]]] = 0;
            }

            finalOffsets[p + 1] = write;
        }

        return finalOffsets;
    }

    private struct Result
    {
        internal Result(bool nullable, int[] first, int[] last)
        {
            Nullable = nullable;
            First = first;
            Last = last;
        }

        internal bool Nullable;

        internal int[] First;

        internal int[] Last;
    }

    private struct Frame
    {
        internal Frame(DtdContentParticle node)
        {
            Node = node;
            Next = 0;
        }

        internal DtdContentParticle Node;

        internal int Next;
    }
}
